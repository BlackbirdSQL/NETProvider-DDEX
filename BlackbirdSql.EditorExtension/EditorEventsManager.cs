﻿// $License = https://github.com/BlackbirdSQL/NETProvider-DDEX/blob/master/Docs/license.txt
// $Authors = GA Christos (greg@blackbirdsql.org)

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BlackbirdSql.Common.Controls;
using BlackbirdSql.Common.Ctl;
using BlackbirdSql.Common.Ctl.Interfaces;
using BlackbirdSql.Common.Model;
using BlackbirdSql.Core;
using BlackbirdSql.Core.Ctl;
using BlackbirdSql.Core.Ctl.Diagnostics;
using BlackbirdSql.Core.Ctl.Interfaces;
using BlackbirdSql.Core.Model.Enums;
using BlackbirdSql.EditorExtension.Ctl.Events;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Data.Services.SupportEntities;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

using Cmd = BlackbirdSql.Common.Cmd;
using IOleUndoManager = Microsoft.VisualStudio.OLE.Interop.IOleUndoManager;
using Native = BlackbirdSql.Core.Native;



namespace BlackbirdSql.EditorExtension;


// =========================================================================================================
//										EditorEventsManager Class
//
/// <summary>
/// Manages Solution, RDT and Selection events for the editor extension.
/// </summary>
// =========================================================================================================
public sealed class EditorEventsManager : AbstractEventsManager
{

	// ---------------------------------------------------------------------------------
	#region Constructors / Destructors - EditorEventsManager
	// ---------------------------------------------------------------------------------


	/// <summary>
	/// .ctor
	/// </summary>
	private EditorEventsManager(IBPackageController controller) : base(controller)
	{
	}


	/// <summary>
	/// Access to the static at the instance local level. This allows the base class to access and update
	/// the localized static instance.
	/// </summary>
	protected override IBEventsManager InternalInstance
	{
		get { return _Instance; }
		set { _Instance = value; }
	}


	/// <summary>
	/// Gets the instance of the Events Manager for this assembly.
	/// We do not auto-create to avoid instantiation confusion.
	/// Use CreateInstance() to instantiate.
	/// </summary>
	public static IBEventsManager Instance => _Instance ??
		throw Diag.ExceptionInstance(typeof(EditorEventsManager));


	/// <summary>
	/// Creates the singleton instance of the Events Manager for this assembly.
	/// Instantiation must always occur here and not by the Instance accessor to avoid
	/// confusion.
	/// </summary>
	public static EditorEventsManager CreateInstance(IBPackageController controller) =>
		new EditorEventsManager(controller);



	public override void Dispose()
	{
		Controller.OnAfterAttributeChangeEvent -= OnAfterAttributeChange;
		Controller.OnAfterAttributeChangeExEvent -= OnAfterAttributeChangeEx;
		Controller.OnAfterDocumentWindowHideEvent -= OnAfterDocumentWindowHide;
		Controller.OnAfterSaveEvent -= OnAfterSave;
		Controller.OnBeforeDocumentWindowShowEvent -= OnBeforeDocumentWindowShow;
		Controller.OnBeforeLastDocumentUnlockEvent -= OnBeforeLastDocumentUnlock;
		Controller.OnBeforeSaveEvent -= OnBeforeSave;
		Controller.OnBeforeSaveAsyncEvent -= OnBeforeSaveAsync;
		Controller.OnCmdUIContextChangedEvent -= OnCmdUIContextChanged;
		Controller.OnElementValueChangedEvent -= OnElementValueChanged;
		Controller.OnBeforeCloseProjectEvent -= OnBeforeCloseProject;
		Controller.OnQueryCloseProjectEvent -= OnQueryCloseProject;
		Controller.OnSelectionChangedEvent -= OnSelectionChanged;
		Controller.OnNewQueryRequestedEvent -= OnNewQueryRequested;
	}


	#endregion Constructors / Destructors




	// =========================================================================================================
	#region Constants and Fields - EditorEventsManager
	// =========================================================================================================


	public const int C_EID_UndoManager = 0;
	public const int C_EID_WindowFrame = 1;
	public const int C_EID_DocumentFrame = 2;
	public const int C_EID_StartupProject = 3;
	public const int C_EID_PropertyBrowserSID = 4;
	public const int C_EID_UserContext = 5;
	public const int C_EID_ResultList = 6;
	public const int C_EID_LastWindowFrame = 7;


	private static IBEventsManager _Instance;

	private uint _PublishingPreviewCommitOffCookie;
	// private uint _PreviewCommitOffCookie;
	private uint _NotBuildingCookie;
	private uint _SolutionOpeningCookie;

	private uint _SolutionOrProjectUpgradingCookie;

	private uint _ServerExplorerCookie;


	#endregion Constants and Fields





	// =========================================================================================================
	#region Property Accessors - EditorEventsManager
	// =========================================================================================================


	EditorExtensionPackage EditorPackage => (EditorExtensionPackage)DdexPackage;




	public bool IsServerExplorerActive
	{
		get { return GetUiContextValue(_ServerExplorerCookie); }

		set { SetUiContextValue(_ServerExplorerCookie, value); }
	}


	public bool IsVisualStudioBusy
	{
		get
		{
			if (GetUiContextValue(_NotBuildingCookie) && !GetUiContextValue(_SolutionOpeningCookie)
				&& !GetUiContextValue(_SolutionOrProjectUpgradingCookie) && !GetUiContextValue(_PublishingPreviewCommitOffCookie))
			{
				return false;
			}

			return true;
		}
	}


	public bool IsPublishing
	{
		get { return GetUiContextValue(_PublishingPreviewCommitOffCookie); }

		set { SetUiContextValue(_PublishingPreviewCommitOffCookie, value); }
	}


	public bool IsPreviewCommitOff
	{
		get { return IsPublishing; }

		set { IsPublishing = value; }
	}


	public object CurrentDocument
	{
		get
		{
			object pvar = null;

			if (CurrentDocumentFrame != null)
			{
				Diag.ThrowIfNotOnUIThread();

				Exf(CurrentDocumentFrame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out pvar));
			}

			return pvar;
		}
	}


	public object CurrentDocumentView
	{
		get
		{
			object pvar = null;

			if (CurrentDocumentFrame != null)
			{
				Diag.ThrowIfNotOnUIThread();

				Exf(CurrentDocumentFrame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out pvar));
			}

			return pvar;
		}
	}


	public object CurrentWindow
	{
		get
		{
			object pvar = null;

			if (CurrentWindowFrame != null)
			{
				Diag.ThrowIfNotOnUIThread();

				Exf(CurrentWindowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out pvar));
			}

			return pvar;
		}
	}


	public ISelectionContainer CurrentSelectionContainer { get; private set; }
	public IOleUndoManager CurrentUndoManager { get; private set; }
	public IVsWindowFrame CurrentDocumentFrame { get; private set; }
	public IVsWindowFrame CurrentWindowFrame { get; private set; }

	public event EventHandler<MonitorSelectionEventArgs> MonitorWindowChangedEvent;
	public event EventHandler<MonitorSelectionEventArgs> MonitorDocumentChangedEvent;
	public event EventHandler<MonitorSelectionEventArgs> MonitorDocumentWindowChangedEvent;
	public event EventHandler<MonitorSelectionEventArgs> MonitorUndoManagerChangedEvent;
	public event EventHandler<MonitorSelectionEventArgs> MonitorSelectionChangedEvent;

	#endregion Property Accessors




	// =========================================================================================================
	#region Methods - EditorEventsManager
	// =========================================================================================================


	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Cleans up any SE sql editor documents that may have been left dangling.
	/// </summary>
	// ---------------------------------------------------------------------------------
	private int CleanupTemporarySqlItems(IVsUIHierarchy projectHierarchy)
	{

		var itemid = VSConstants.VSITEMID_ROOT;
		object objProj = null;

		// Get the hierarchy root node.
		try
		{
			projectHierarchy.GetProperty(itemid, (int)__VSHPROPID.VSHPROPID_ExtObject, out objProj);
		}
		catch (Exception ex)
		{
			Diag.ThrowException(ex);
		}

		if (objProj == null || objProj is not Project project)
		{
			project = null;
			Diag.ThrowException(new ApplicationException("Hierarchy is not a project"));
		}


		if (project.ProjectItems == null || project.ProjectItems.Count == 0)
			return VSConstants.S_OK;




		foreach (RunningDocumentInfo docInfo in RdtManager.Enumerator)
		{
			if (!(docInfo.Hierarchy == projectHierarchy) ||
				string.IsNullOrWhiteSpace(docInfo.Moniker) || docInfo.DocData == null)
			{
				continue;
			}
			AuxiliaryDocData docData = EditorPackage.GetAuxiliaryDocData(docInfo.DocData);

			if (docData == null)
				continue;

			try
			{
				projectHierarchy.GetProperty(docInfo.ItemId, (int)__VSHPROPID.VSHPROPID_ExtObject, out objProj);
			}
			catch (Exception ex)
			{
				Diag.Dug(ex);
				throw ex;
			}

			if (objProj == null || objProj is not ProjectItem projectItem)
			{
				ArgumentException ex = new($"Could not get project item in hierarchy {projectHierarchy}, itemId: {docInfo.ItemId}.");
				Diag.Dug(ex);
				throw ex;
			}

			// Tracer.Trace(GetType(), "CleanupTemporarySqlItems()", "Found project item: {0}.", projectItem.Name);

			try
			{
				RemoveTemporarySqlItem(projectItem);
			}
			catch { }


		}


		return VSConstants.S_OK;
	}


	private bool GetUiContextValue(uint cookie)
	{
		Diag.ThrowIfNotOnUIThread();

		SelectionMonitor.IsCmdUIContextActive(cookie, out int pfActive);

		if (pfActive != 1)
			return false;

		return true;
	}


	public static void RegisterForDirtyChangeNotification(uint docCookie)
	{
		if (!RdtManager.TryGetDocDataFromCookie(docCookie, out object docData))
			return;

		IComponentModel componentModel = Core.Controller.GetService<SComponentModel, IComponentModel>();
		if (componentModel == null)
			return;

		IVsEditorAdaptersFactoryService service = componentModel.GetService<IVsEditorAdaptersFactoryService>();
		if (service == null)
			return;

		ITextBuffer documentBuffer = service.GetDocumentBuffer((IVsTextBuffer)docData);
		ITextDocumentFactoryService service2 = componentModel.GetService<ITextDocumentFactoryService>();
		if (service2 != null)
		{
			service2.TryGetTextDocument(documentBuffer, out ITextDocument textDocument);
			textDocument.DirtyStateChanged += OnTextDocumentDirtyStateChanged;
		}
	}


	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Removes a temporary editor item from the Misc project.
	/// </summary>
	// ---------------------------------------------------------------------------------
	private bool RemoveTemporarySqlItem(ProjectItem projectItem)
	{
		// Tracer.Trace(GetType(), "RemoveTemporarySqlItem()");

		if (projectItem.FileCount == 0 || UnsafeCmd.Kind(projectItem.Kind) != "MiscItem")
			return false;

		// FileNames is 1 based indexing - How/Why??? - A VB team did this!
		string filepath = projectItem.FileNames[1];

		if (!filepath.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase))
			return false;

		try
		{
			projectItem.Delete();
			return true;
		}
#if DEBUG
		catch (Exception ex)
		{
			Diag.Dug(ex);
		}
#else
		catch
		{
		}
#endif

		try
		{
			File.Delete(filepath);
			return true;
		}
#if DEBUG
		catch (Exception ex)
		{
			Diag.Dug(ex);
		}
#else
		catch
		{
		}
#endif


		return false;
	}



	private void SetUiContextValue(uint cookie, bool value)
	{
		Diag.ThrowIfNotOnUIThread();

		SelectionMonitor.SetCmdUIContext(cookie, value ? 1 : 0);
	}

	public bool IsCommandContextActive(Guid commandContext)
	{
		int pfActive = 0;
		if (SelectionMonitor != null)
		{
			Diag.ThrowIfNotOnUIThread();

			Exf(SelectionMonitor.GetCmdUIContextCookie(ref commandContext, out var pdwCmdUICookie));
			Exf(SelectionMonitor.IsCmdUIContextActive(pdwCmdUICookie, out pfActive));
		}

		return pfActive == 1;
	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Hooks onto the controller's RDT and Selection events.
	/// </summary>
	// ---------------------------------------------------------------------------------
	public override void Initialize()
	{
		Controller.OnAfterAttributeChangeEvent += OnAfterAttributeChange;
		Controller.OnAfterAttributeChangeExEvent += OnAfterAttributeChangeEx;
		Controller.OnAfterDocumentWindowHideEvent += OnAfterDocumentWindowHide;
		Controller.OnAfterSaveEvent += OnAfterSave;
		Controller.OnAfterSaveAsyncEvent += OnAfterSaveAsync;
		Controller.OnBeforeDocumentWindowShowEvent += OnBeforeDocumentWindowShow;
		Controller.OnBeforeLastDocumentUnlockEvent += OnBeforeLastDocumentUnlock;
		Controller.OnBeforeSaveEvent += OnBeforeSave;
		Controller.OnBeforeSaveAsyncEvent += OnBeforeSaveAsync;
		Controller.OnCmdUIContextChangedEvent += OnCmdUIContextChanged;
		Controller.OnElementValueChangedEvent += OnElementValueChanged;
		Controller.OnBeforeCloseProjectEvent += OnBeforeCloseProject;
		Controller.OnQueryCloseProjectEvent += OnQueryCloseProject;
		Controller.OnSelectionChangedEvent += OnSelectionChanged;
		Controller.OnNewQueryRequestedEvent += OnNewQueryRequested;

		if (!ThreadHelper.CheckAccess())
		{
			// Fire and wait.

			bool result = ThreadHelper.JoinableTaskFactory.Run(async delegate
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				InitializeUnsafe();
				return true;
			});

			return;
		}

		InitializeUnsafe();

	}


	private void InitializeUnsafe()
	{
		Diag.ThrowIfNotOnUIThread();

		Controller.EnsureMonitorSelection();

		Exf(SelectionMonitor.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_DocumentFrame, out var pvarValue));
		CurrentDocumentFrame = pvarValue as IVsWindowFrame;

		Exf(SelectionMonitor.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_WindowFrame, out pvarValue));
		CurrentWindowFrame = pvarValue as IVsWindowFrame;

		Exf(SelectionMonitor.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_UndoManager, out pvarValue));
		CurrentUndoManager = pvarValue as IOleUndoManager;

		Guid rguidCmdUI = Core.VS.UICONTEXT_PublishingPreviewCommitOff;
		Exf(SelectionMonitor.GetCmdUIContextCookie(ref rguidCmdUI, out _PublishingPreviewCommitOffCookie));

		// rguidCmdUI = new(ServiceData.PreviewCommitOffGuid);
		// Exf(MonitorSelection.GetCmdUIContextCookie(ref rguidCmdUI, out _PreviewCommitOffCookie));


		rguidCmdUI = VSConstants.StandardToolWindows.ServerExplorer;
		Exf(SelectionMonitor.GetCmdUIContextCookie(ref rguidCmdUI, out _ServerExplorerCookie));

		rguidCmdUI = VSConstants.UICONTEXT.NotBuildingAndNotDebugging_guid;
		Exf(SelectionMonitor.GetCmdUIContextCookie(ref rguidCmdUI, out _NotBuildingCookie));

		rguidCmdUI = VSConstants.UICONTEXT.SolutionOpening_guid;
		Exf(SelectionMonitor.GetCmdUIContextCookie(ref rguidCmdUI, out _SolutionOpeningCookie));

		rguidCmdUI = VSConstants.UICONTEXT.SolutionOrProjectUpgrading_guid;
		Exf(SelectionMonitor.GetCmdUIContextCookie(ref rguidCmdUI, out _SolutionOrProjectUpgradingCookie));

	}



	public bool HasAnyAuxiliaryDocData()
	{
		lock (EditorPackage.LockLocal)
			return EditorPackage.AuxiliaryDocDataTable.Count > 0;
	}



	private async Task<bool> ResetDocumentStatusAsync(AuxiliaryDocData auxDocData, bool resetIntellisense)
	{
		try
		{
			// Tracer.Trace(GetType(), "ResetDocumentStatusAsync()", "ENTER!!!");

			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

			// Hack to correct intellisense target. 
			if (resetIntellisense)
				auxDocData.IntellisenseEnabled = true;

			// Hack to kickstart dirty state title update.
			uint saveOpts = (uint)__VSRDTSAVEOPTIONS.RDTSAVEOPT_ForceSave;
			RdtManager.SaveDocuments(saveOpts, null, uint.MaxValue, auxDocData.DocCookie);

			// RegisterForDirtyChangeNotification(auxDocData.DocCookie);

		}
		catch (Exception ex)
		{
			Diag.Dug(ex);
		}
		finally
		{
			Controller.EnableRdtEvents();

			// Tracer.Trace(GetType(), "ResetDocumentStatusAsync()", "FINALLY: Intellisense and RdtEvents enabled.");
		}

		return true;

	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Cleans up any SE sql editor documents that may have been left dangling.
	/// </summary>
	// ---------------------------------------------------------------------------------
	private bool QueryAbortClose(IVsHierarchy hierarchy)
	{

		foreach (RunningDocumentInfo item in RdtManager.Enumerator)
		{
			if (item.Hierarchy == hierarchy && !string.IsNullOrWhiteSpace(item.Moniker)
				&& item.DocData != null)
			{
				AuxiliaryDocData docData = EditorPackage.GetAuxiliaryDocData(item.DocData);

				if (docData != null && Cmd.ShouldStopCloseDialog(docData, GetType()))
				{
					return true;
				}
			}
		}


		return false;
	}


	#endregion Methods





	// =========================================================================================================
	#region IVs Events Implementation and Event handling - EditorEventsManager
	// =========================================================================================================


	public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
	{
		return VSConstants.S_OK;
	}



	public int OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld,
		uint itemidOld, string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew,
		string pszMkDocumentNew)
	{


		// The following code corrects the Intellisense target and kickstarts dirty state title update after a save (moniker rename).
		// If we don't kickstart, dirty state title updates take +- 120 seconds to resume. This is a strange anomaly.

		// If it's not a name change, exit.
		if ((grfAttribs & (uint)__VSRDTATTRIB.RDTA_MkDocument) == 0)
			return VSConstants.S_OK;


		RunningDocumentInfo docInfo = RdtManager.GetDocumentInfo(docCookie);
		docInfo.Sync();

		object docData = docInfo.DocData;

		AuxiliaryDocData auxDocData = EditorPackage.GetAuxiliaryDocData(docData);

		// If no auxdocdata, it's not ours, exit.
		if (auxDocData == null)
			return VSConstants.S_OK;

		// Set the auxdocdata cookie if it was never set. 
		if (auxDocData.DocCookie != docCookie)
			auxDocData.DocCookie = docCookie;

		// Break the link between auxdocdata and the explorer moniker, if it exists, because this is now a disk file.
		if (auxDocData.ExplorerMoniker != null)
		{
			DesignerExplorerServices.MonikerCsaTable.Remove(auxDocData.ExplorerMoniker);
			auxDocData.ExplorerMoniker = null;
		}



		// If no intellisense, no need to fix targeting.
		bool resetIntellisense = auxDocData.IntellisenseEnabled.HasValue && auxDocData.IntellisenseEnabled.Value;

		// Tracer.Trace(GetType(), "OnAfterAttributeChangeEx()", "AuxiliaryDocData exists. \nOld mk: {0}\nNew mk: {1}",
		//	pszMkDocumentOld, pszMkDocumentNew);

		// Disable rdt events to avoid any recursion.
		Controller.DisableRdtEvents();

		if (resetIntellisense)
			auxDocData.IntellisenseEnabled = false;

		// Fire and forget.
		Task<bool> payload() =>
			ResetDocumentStatusAsync(auxDocData, resetIntellisense);

		_ = Task.Factory.StartNew(payload, default, TaskCreationOptions.PreferFairness, TaskScheduler.Default);

		// Tracer.Trace(GetType(), "OnAfterAttributeChangeEx()", "DONE!!! Intellisense and RdtEvents disabled");

		return VSConstants.S_OK;
	}


	public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
	{
		// Tracer.Trace(GetType(), "OnAfterDocumentWindowHide()");

		RunningDocumentInfo docInfo;

		lock (RdtManager.LockGlobal)
			docInfo = RdtManager.GetDocumentInfo(docCookie);

		if (docInfo.IsDocumentInitialized
			&& Native.Succeeded(pFrame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out var pvar)))
		{
			if (pvar is TabbedEditorWindowPane sqlEditorTabbedEditorPane
				&& sqlEditorTabbedEditorPane == EditorPackage.LastFocusedSqlEditor)
			{
				EditorPackage.LastFocusedSqlEditor = null;
			}
		}

		return VSConstants.S_OK;

	}



	public int OnAfterSave(uint docCookie)
	{
		// Tracer.Trace(GetType(), "OnAfterSave()", "DocCookie: {0}.", docCookie);

		return VSConstants.S_OK;

	}



	public IVsTask OnAfterSaveAsync(uint cookie, uint flags)
	{
		// Tracer.Trace(GetType(), "OnAfterSaveAsync()", "DocCookie: {0}.", cookie);

		RunningDocumentInfo documentInfo = RdtManager.GetDocumentInfo(cookie);

		if (!documentInfo.IsDocumentInitialized || documentInfo.DocData == null)
			return null;

		documentInfo.Sync();

		AuxiliaryDocData auxDocData = EditorPackage.GetAuxiliaryDocData(documentInfo.DocData);

		if (auxDocData == null)
			return null;

		if (auxDocData.DocCookie != cookie)
			auxDocData.DocCookie = cookie;

		if (!auxDocData.IsVirtualWindow)
			return null;

		auxDocData.IsVirtualWindow = false;


		return null;
	}



	public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
	{
		Diag.ThrowIfNotOnUIThread();

		RunningDocumentInfo documentInfo = RdtManager.GetDocumentInfo(docCookie);

		if (!documentInfo.IsDocumentInitialized)
			return VSConstants.S_OK;


		if (!Native.Succeeded(pFrame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out var pvar)))
			return VSConstants.S_OK;


		if (pvar is TabbedEditorWindowPane sqlEditorTabbedEditorPane)
			EditorPackage.LastFocusedSqlEditor = sqlEditorTabbedEditorPane;

		return VSConstants.S_OK;
	}



	public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
	{
		if (dwReadLocksRemaining == 0 && dwEditLocksRemaining == 0)
		{
			RunningDocumentInfo documentInfo = RdtManager.GetDocumentInfo(docCookie);

			if (documentInfo.IsDocumentInitialized && EditorPackage.ContainsEditorStatus(documentInfo.DocData))
			{
				EditorPackage.RemoveEditorStatus(documentInfo.DocData);
			}
		}
		else if (dwEditLocksRemaining == 1 && RdtManager.ShouldKeepDocDataAliveOnClose(docCookie))
		{
			RunningDocumentInfo documentInfo2 = RdtManager.GetDocumentInfo(docCookie);

			if (documentInfo2.IsDocumentInitialized && EditorPackage.ContainsEditorStatus(documentInfo2.DocData))
			{
				AuxiliaryDocData auxDocData = EditorPackage.GetAuxiliaryDocData(documentInfo2.DocData);

				if (auxDocData != null)
					auxDocData.IntellisenseEnabled = null;
			}
		}

		return VSConstants.S_OK;
	}


	public int OnBeforeSave(uint docCookie)
	{
		// Tracer.Trace(GetType(), "OnBeforeSave()", "DocCookie: {0}, moniker: {1}.", docCookie, mkDocument);

		return VSConstants.S_OK;
	}


	IVsTask OnBeforeSaveAsync(uint cookie, uint flags, IVsTask saveTask)
	{
		return null;
	}



	public int OnCmdUIContextChanged(uint cookie, int fActive)
	{
		return VSConstants.S_OK;
	}



	public int OnElementValueChanged(uint elementid, object oldValue, object newValue)
	{
		switch ((VSConstants.VSSELELEMID)elementid)
		{
			case VSConstants.VSSELELEMID.SEID_WindowFrame:
				CurrentWindowFrame = newValue as IVsWindowFrame;
				MonitorWindowChangedEvent?.Invoke(this, new MonitorSelectionEventArgs(oldValue, newValue));

				break;
			case VSConstants.VSSELELEMID.SEID_DocumentFrame:
				CurrentDocumentFrame = newValue as IVsWindowFrame;
				MonitorDocumentWindowChangedEvent?.Invoke(this, new MonitorSelectionEventArgs(oldValue, newValue));

				if (MonitorDocumentChangedEvent != null)
				{
					object pvar = null;
					if (oldValue is IVsWindowFrame vsWindowFrame)
					{
						Exf(vsWindowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out pvar));
					}

					object pvar2 = null;
					if (CurrentDocumentFrame != null)
					{
						Exf(CurrentDocumentFrame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out pvar2));
					}

					if (pvar != pvar2)
					{
						MonitorDocumentChangedEvent(this, new MonitorSelectionEventArgs(oldValue, newValue));
					}
				}

				break;
			case VSConstants.VSSELELEMID.SEID_UndoManager:
				CurrentUndoManager = newValue as IOleUndoManager;
				MonitorUndoManagerChangedEvent?.Invoke(this, new MonitorSelectionEventArgs(oldValue, newValue));

				break;
		}

		return VSConstants.S_OK;
	}



	public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
	{
		// This event is fired after the project's items are no longer available. ?????????

		return VSConstants.S_OK;

	}



	public int OnQueryCloseProject(IVsHierarchy hierarchy, int removing, ref int cancel)
	{
		// Tracer.Trace(GetType(), "OnQueryCloseProject()");

		if (!UnsafeCmd.IsVirtualProjectKind(hierarchy))
			return VSConstants.S_OK;

		if (!HasAnyAuxiliaryDocData())
			return VSConstants.S_OK;

		if (QueryAbortClose(hierarchy))
			cancel = 1;
		else if (UnsafeCmd.IsMiscFilesProject(hierarchy))
			CleanupTemporarySqlItems(hierarchy as IVsUIHierarchy);


		return VSConstants.S_OK;

	}




	public int OnSelectionChanged(IVsHierarchy pHierOld, uint itemidOld, IVsMultiItemSelect pMisOld, ISelectionContainer pScOld,
		IVsHierarchy pHierNew, uint itemidNew, IVsMultiItemSelect pMisNew, ISelectionContainer pScNew)
	{
		CurrentSelectionContainer = pScNew;
		MonitorSelectionChangedEvent?.Invoke(this, new MonitorSelectionEventArgs(pScOld, pScNew));

		return VSConstants.S_OK;
	}

	/// <summary>
	/// This roadblocks because key services in DataTools.Interop are protected.
	/// </summary>
	public int OnNewQueryRequested(IVsDataViewHierarchy site, EnNodeSystemType nodeSystemType)
	{
		// This roadbloacks atm because of protection modfiers. TBC
		// new QueryDesignerDocument(site).Show(nodeSystemType);
		// host.QueryDesignerProviderTelemetry(qualityMetricProvider);
		return VSConstants.S_OK;
	}

	#endregion IVs Events Implementation and Event handling





	// =========================================================================================================
	#region Event handling - EditorEventsManager
	// =========================================================================================================


	private static void OnTextDocumentDirtyStateChanged(object sender, EventArgs e)
	{
		try
		{
			ITextDocument textDocument = (ITextDocument)sender;
			uint docCookie = RdtManager.GetRdtCookie(textDocument.FilePath);

			// Tracer.Trace(typeof(EditorEventsManager), "OnTextDocumentDirtyStateChanged()", "DocCookie: {0}, Filepath: {1}.", docCookie, textDocument.FilePath);

			// RdtManager.UpdateDirtyState(docCookie);
		}
		catch (Exception ex)
		{
			Diag.Dug(ex);
		}
	}



	#endregion Event handling

}