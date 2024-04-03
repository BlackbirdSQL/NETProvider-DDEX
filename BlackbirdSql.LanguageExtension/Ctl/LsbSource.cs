// Microsoft.VisualStudio.Data.Tools.SqlLanguageServices, Version=17.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// Microsoft.VisualStudio.Data.Tools.SqlLanguageServices.Source
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Babel;
using BlackbirdSql.Common.Ctl.Interfaces;
using BlackbirdSql.Common.Model;
using BlackbirdSql.Common.Model.Events;
using BlackbirdSql.Core;
using BlackbirdSql.Core.Ctl.Diagnostics;
using BlackbirdSql.Core.Model;
using BlackbirdSql.LanguageExtension.Ctl.Config;
using BlackbirdSql.LanguageExtension.Model;
using BlackbirdSql.LanguageExtension.Model.Interfaces;
using BlackbirdSql.LanguageExtension.Services;
using Microsoft.SqlServer.Management.SqlParser.Binder;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;

using Cmd = BlackbirdSql.Common.Cmd;
using VS = BlackbirdSql.Common.VS;



namespace BlackbirdSql.LanguageExtension.Ctl;


public sealed class LsbSource : Microsoft.VisualStudio.Package.Source, IVsUserDataEvents
{

	public LsbSource(AbstractLanguageService service, IVsTextLines textLines, Colorizer colorizer)
		: base(service, textLines, colorizer)
	{
		// Tracer.Trace(typeof(LsbSource), ".ctor.");

		_SqlLanguageService = service;
		_IntelliSenseEnabled = service.Prefs.EnableIntellisense;
		AuxilliaryDocData auxDocData = GetAuxilliaryDocData();

		if (auxDocData != null)
		{
			if (!auxDocData.IntellisenseEnabled.HasValue)
				auxDocData.IntellisenseEnabled = _IntelliSenseEnabled;
			else
				_IntelliSenseEnabled = auxDocData.IntellisenseEnabled.Value;

			auxDocData.QryMgr.ScriptExecutionCompletedEvent += HandleScriptExecutionCompleted;
			auxDocData.QryMgr.BatchExecutionCompletedEvent += HandleBatchExecutionCompleted;
		}

		_IsServerSupported = false;
		_ParseManager = new LsbParseManager(this);
		_PrvChangeCount = -1;
		_IsReallyDirty = false;
		OutliningEnabled = _SqlLanguageService.Preferences.AutoOutlining;

		Reset();

	}


	public const int C_MinServerVersionSupported = 9;

	private readonly object _LockObject = new object();

	private bool _IsSqlCmdModeEnabled;

	private bool _IntelliSenseEnabled;

	private int _PrvChangeCount;

	private bool _IsReallyDirty;

	private bool _HasPendingRegions;

	private bool _IsServerSupported;

	private readonly LsbParseManager _ParseManager;

	private readonly AbstractLanguageService _SqlLanguageService;

	public ITextUndoTransaction CurrentCommitUndoTransaction { get; set; }

	public bool IsSqlCmdModeEnabled => _IsSqlCmdModeEnabled;

	private LsbMetadataProviderProvider MetadataProviderProviderInstance { get; set; } = null;

	public bool IsServerSupported => _IsServerSupported;

	public bool IsDisconnectedMode
	{
		get
		{
			bool flag = false;
			AuxilliaryDocData auxDocData = GetAuxilliaryDocData();
			if (auxDocData != null)
				flag = auxDocData.QryMgr.IsConnected;

			return !flag;
		}
	}



	/// <summary>
	/// The current DatsetKey
	/// </summary>
	public string DatabaseName
	{
		get
		{
			string name = null;
			IBMetadataProviderProvider metadataProviderProvider = GetMetadataProviderProvider();

			if (metadataProviderProvider != null && metadataProviderProvider is LsbMetadataProviderProvider)
			{
				AuxilliaryDocData auxDocData = GetAuxilliaryDocData();
				if (auxDocData != null)
				{
					IDbConnection connection = auxDocData.QryMgr.ConnectionStrategy.Connection;

					// TODO: Use datasetKey
					if (connection != null)
						name = connection.Database;
				}
			}

			if (name == null)
			{
				if (metadataProviderProvider != null)
				{
					// TODO: Use datasetKey
					name = metadataProviderProvider.DatabaseName;
				}
				else // if (!IsDisconnectedMode)
				{
					name = GetDatabaseNameFromConnectionInfo(ConnectionInfo);
				}
			}
			return name;
		}
	}

	public ParseResult ParseResult => _ParseManager.ParseResult;

	public bool IntelliSenseEnabled => _IntelliSenseEnabled;

	public IEnumerable<Region> HiddenRegions => _ParseManager.HiddenRegions;

	public IEnumerable<Error> Errors => _ParseManager.Errors;

	public bool IsReallyDirty => _IsReallyDirty;

	public bool HasPendingRegions
	{
		get
		{
			return _HasPendingRegions;
		}
		set
		{
			_HasPendingRegions = value;
		}
	}

	public ConnectionPropertyAgent ConnectionInfo
	{
		get
		{
			ConnectionPropertyAgent result = null;

			AuxilliaryDocData auxDocData = GetAuxilliaryDocData();

			if (auxDocData != null)
				result = auxDocData.QryMgr.ConnectionStrategy.ConnectionInfo;

			return result;
		}
	}



	public IBMetadataProviderProvider GetMetadataProviderProvider()
	{
		// TBC: MetaDataProvider not yet implemented

		if (Cmd.ToBeCompleted)
			return null;

		IBMetadataProviderProvider metadataProviderProvider = null;

		AuxilliaryDocData auxDocData = ((IBEditorPackage)LanguageExtensionPackage.Instance).GetAuxilliaryDocData(GetTextLines());

		if (auxDocData != null && auxDocData.IntellisenseEnabled.AsBool())
		{
			metadataProviderProvider = (IBMetadataProviderProvider)auxDocData.Strategy.MetadataProviderProvider;
			metadataProviderProvider ??= MetadataProviderProviderInstance;

			if (metadataProviderProvider == null && auxDocData.QryMgr.IsConnected)
			{
				ConnectionPropertyAgent connectionInfo = auxDocData.QryMgr.ConnectionStrategy.ConnectionInfo;
				if (connectionInfo != null && !string.IsNullOrEmpty(GetDatabaseNameFromConnectionInfo(connectionInfo)))
				{
					MetadataProviderProviderInstance = LsbMetadataProviderProvider.Cache.Instance.Acquire(auxDocData.QryMgr);
					metadataProviderProvider = MetadataProviderProviderInstance;
				}
			}
		}

		return metadataProviderProvider;
	}

	public string GetDatabaseNameFromConnectionInfo(ConnectionPropertyAgent uici)
	{
		string result = null;

		if (uici != null)
			result = uici.DatasetKey;

		return result;
	}

	private void HandleBatchExecutionCompleted(object sender, QESQLBatchExecutedEventArgs args)
	{
		AuxilliaryDocData auxDocData = GetAuxilliaryDocData();

		if (GetMetadataProviderProvider() is LsbMetadataProviderProvider smoMetadataProviderProvider
			&& auxDocData != null && LsbMetadataProviderProvider.CheckForDatabaseChangesAfterQueryExecution)
		{
			smoMetadataProviderProvider.AsyncAddDatabaseToDriftDetectionSet(auxDocData.QryMgr.ConnectionStrategy.Connection.Database);
		}
	}

	private void HandleScriptExecutionCompleted(object sender, ScriptExecutionCompletedEventArgs args)
	{
		if (GetMetadataProviderProvider() is LsbMetadataProviderProvider smoMetadataProviderProvider
			&& LsbMetadataProviderProvider.CheckForDatabaseChangesAfterQueryExecution)
		{
			smoMetadataProviderProvider.AsyncCheckForDatabaseChanges();
		}
	}

	private AuxilliaryDocData GetAuxilliaryDocData()
	{
		IVsTextLines docData = GetTextLines();
		return ((IBEditorPackage)LanguageExtensionPackage.Instance).GetAuxilliaryDocData(docData);
	}

	public bool ExecuteParseRequest(string text)
	{
		_IsReallyDirty = _PrvChangeCount != base.ChangeCount;
		_PrvChangeCount = base.ChangeCount;
		IBinder binder = null;

		AuxilliaryDocData auxDocData = null;
		IBMetadataProviderProvider metadataProviderProvider = GetMetadataProviderProvider();

		if (metadataProviderProvider != null)
		{
			try
			{
				ManualResetEvent buildEvent = metadataProviderProvider.BuildEvent;
				// TraceUtils.Trace(GetType(), "ExecuteParseRequest()", "waiting for metadata provider...(ThreadName = " + Thread.CurrentThread.Name + ")");

				if (buildEvent.WaitOne(2000))
				{
					binder = metadataProviderProvider.Binder;
					// TraceUtils.Trace(GetType(), "ExecuteParseRequest()", "...done waiting for metadata provider(ThreadName = " + Thread.CurrentThread.Name + ")");
				}
				else
				{
					// TraceUtils.Trace(GetType(), "ExecuteParseRequest()", "...timed out waiting for metadata provider(ThreadName = " + Thread.CurrentThread.Name + ")");
				}
			}
			catch (Exception ex)
			{
				Diag.Dug(ex);
				// TraceUtils.LogExCatch(GetType(), e, "ExecuteParseRequest():  Hit Exception While Extracting MetadataProvider from Database Project");
			}
		}
		else
		{
			_ = IsDisconnectedMode;
			auxDocData = GetAuxilliaryDocData();
		}

		ParseOptions parseOptions;

		if (auxDocData != null)
		{
			if (metadataProviderProvider != null)
			{
				parseOptions = metadataProviderProvider.CreateParseOptions();
				parseOptions.BatchSeparator = auxDocData.QryMgr.LiveSettings.EditorContextBatchSeparator;
			}
			else
			{
				parseOptions = new ParseOptions(auxDocData.QryMgr.LiveSettings.EditorContextBatchSeparator);
			}
		}
		else
		{
			parseOptions = new ParseOptions("GO");
		}
		return _ParseManager.ExecuteParseRequest(text, parseOptions, binder, DatabaseName);
	}



	public void Reset()
	{
		_HasPendingRegions = true;
		_ParseManager.Reset();
		ReleaseSmoMetadataProviderProvider();
	}

	public override CommentInfo GetCommentFormat()
	{
		return LsbConfiguration.MyCommentInfo;
	}

	public override TextSpan CommentLines(TextSpan span, string lineComment)
	{
		if (span.iStartLine < span.iEndLine && span.iEndIndex == 0)
		{
			span.iEndLine--;
		}
		int num = span.iEndLine - span.iStartLine + 1;
		bool[] array = new bool[num];
		int num2 = -1;
		for (int i = 0; i < num; i++)
		{
			int line = span.iStartLine + i;
			array[i] = false;
			int num3 = ScanToNonWhitespaceChar(line);
			if (num3 != GetLineLength(line))
			{
				array[i] = true;
				if (num2 == -1 || num3 < num2)
				{
					num2 = num3;
				}
			}
		}
		for (int i = 0; i < num; i++)
		{
			if (array[i])
			{
				int num4 = span.iStartLine + i;
				SetText(num4, num2, num4, num2, lineComment);
			}
		}
		return span;
	}

	public override TextSpan UncommentLines(TextSpan span, string lineComment)
	{
		int length = lineComment.Length;
		for (int i = span.iStartLine; i <= span.iEndLine; i++)
		{
			if (span.iStartLine == span.iEndLine || i != span.iEndLine || span.iEndIndex > 0)
			{
				string line = GetLine(i);
				int num = line.IndexOf(lineComment, StringComparison.Ordinal);
				int num2 = 0;
				if (num >= 0 && num == ScanToNonWhitespaceChar(i))
				{
					SetText(i, num, i, num + length, string.Empty);
					num2 = length;
				}
				if (i == span.iEndLine)
				{
					span.iEndIndex = line.Length - num2;
				}
			}
		}
		span.iStartIndex = 0;
		return span;
	}

	public override CompletionSet CreateCompletionSet()
	{
		return new LsbCompletionSet(LanguageService.GetImageList(), this);
	}

	public override void OnCommand(IVsTextView textView, VSConstants.VSStd2KCmdID command, char ch)
	{
		base.OnCommand(textView, command, ch);
		if (textView != null && base.LanguageService != null && base.LanguageService.Preferences.EnableCodeSense)
		{
			LsbCompletionSet sqlCompletionSet = (LsbCompletionSet)CompletionSet;

			if (command == VSConstants.VSStd2KCmdID.TYPECHAR && sqlCompletionSet != null
				&& sqlCompletionSet.IsDisplayed && IsExplicitFilteringRequired(sqlCompletionSet.GetTextTypedSoFar()))
			{
				sqlCompletionSet.ExplicitFilterDeclarationList();
			}
			if ((command == VSConstants.VSStd2KCmdID.BACKSPACE || command == VSConstants.VSStd2KCmdID.DELETE) && sqlCompletionSet != null && sqlCompletionSet.IsDisplayed)
			{
				sqlCompletionSet.ResetTextMarker();
				sqlCompletionSet.ExplicitFilterDeclarationList();
			}
			if ((command == VSConstants.VSStd2KCmdID.UP || command == VSConstants.VSStd2KCmdID.DOWN) && base.CompletionSet.IsDisplayed && sqlCompletionSet.InPreviewMode && sqlCompletionSet.InPreviewModeOutline)
			{
				sqlCompletionSet.InPreviewModeOutline = false;
				sqlCompletionSet.UpdateCompletionStatus(forceSelect: true);
			}
		}
	}

	public static bool IsExplicitFilteringRequired(string textTypedSofar)
	{
		if (string.IsNullOrEmpty(textTypedSofar) || textTypedSofar[0] == Cmd.OpenSquareBracket || textTypedSofar[0] == Cmd.DoubleQuote || char.IsLetter(textTypedSofar[0]))
		{
			return false;
		}
		return true;
	}

	void IVsUserDataEvents.OnUserDataChange(ref Guid riidKey, object vtNewValue)
	{
		if (riidKey.Equals(VS.CLSID_PropOleSql))
		{
			_IsSqlCmdModeEnabled = (bool)vtNewValue;
			(GetColorizer().Scanner as LsbLineScanner).IsSqlCmdModeEnabled = _IsSqlCmdModeEnabled;
			IsDirty = true;
		}
		else if (riidKey.Equals(VS.CLSID_PropBatchSeparator))
		{
			string batchSeparator = vtNewValue as string;
			(GetColorizer().Scanner as LsbLineScanner).BatchSeparator = batchSeparator;
			IsDirty = true;
		}
		else if (riidKey.Equals(VS.CLSID_PropSqlVersion))
		{
			int num = 0;
			string text = vtNewValue as string;
			if (!string.IsNullOrEmpty(text))
			{
				string[] array = text.Split('.');
				int result = 0;
				if (array.Length != 0 && array[0] != null)
				{
					int.TryParse(array[0], out result);
				}
				num = result;
			}
			if (num != 0)
			{
				Reset();
				if (num >= C_MinServerVersionSupported)
				{
					_IsServerSupported = true;
				}
				else
				{
					_IsServerSupported = false;
				}
			}
			else
			{
				_IsServerSupported = false;
				Reset();
			}
			IsDirty = true;
		}
		else if (riidKey.Equals(VS.CLSID_PropIntelliSenseEnabled))
		{
			_IntelliSenseEnabled = (bool)vtNewValue;
			IsDirty = true;
		}
		else if (riidKey.Equals(VS.CLSID_PropDatabaseChanged))
		{
			IsDirty = true;
			Reset();
		}
	}

	public override DocumentTask CreateErrorTaskItem(TextSpan span, MARKERTYPE markerType, string filename)
	{
		AuxilliaryDocData auxDocData = ((IBEditorPackage)LanguageExtensionPackage.Instance).GetAuxilliaryDocData(GetTextLines());
		if (auxDocData != null)
		{
			IBSqlEditorErrorTaskFactory errorTaskFactory = auxDocData.Strategy.GetErrorTaskFactory();

			if (errorTaskFactory != null)
				return errorTaskFactory.CreateErrorTaskItem(span, markerType, filename, GetTextLines());
		}

		return base.CreateErrorTaskItem(span, markerType, filename);
	}

	public override DocumentTask CreateErrorTaskItem(TextSpan span, string filename, string message, TaskPriority priority, TaskCategory category, MARKERTYPE markerType, TaskErrorCategory errorCategory)
	{
		AuxilliaryDocData auxDocData = ((IBEditorPackage)LanguageExtensionPackage.Instance).GetAuxilliaryDocData(GetTextLines());
		if (auxDocData != null)
		{
			IBSqlEditorErrorTaskFactory errorTaskFactory = auxDocData.Strategy.GetErrorTaskFactory();

			if (errorTaskFactory != null)
			{
				DocumentTask documentTask = errorTaskFactory.CreateErrorTaskItem(span, markerType, filename, GetTextLines());
				if (documentTask != null)
				{
					documentTask.Priority = priority;
					documentTask.Category = category;
					documentTask.ErrorCategory = errorCategory;
					documentTask.Text = message;
					documentTask.IsTextEditable = false;
					documentTask.IsCheckedEditable = false;
				}
				return documentTask;
			}
		}
		return base.CreateErrorTaskItem(span, filename, message, priority, category, markerType, errorCategory);
	}

	public override void Dispose()
	{
		lock (_LockObject)
		{
			base.Dispose();
			ReleaseSmoMetadataProviderProvider();
			AuxilliaryDocData auxDocData = GetAuxilliaryDocData();
			if (auxDocData != null)
			{
				auxDocData.QryMgr.BatchExecutionCompletedEvent -= HandleBatchExecutionCompleted;
				auxDocData.QryMgr.ScriptExecutionCompletedEvent -= HandleScriptExecutionCompleted;
			}
		}
	}

	public bool TryEnterDisposeLock()
	{
		return Monitor.TryEnter(_LockObject);
	}

	public void ExitDisposeLock()
	{
		Monitor.Exit(_LockObject);
	}

	private void ReleaseSmoMetadataProviderProvider()
	{
		if (MetadataProviderProviderInstance != null)
		{
			LsbMetadataProviderProvider.Cache.Instance.Release(MetadataProviderProviderInstance);
			MetadataProviderProviderInstance = null;
		}
	}
}
