﻿#region Assembly Microsoft.VisualStudio.Data.Tools.SqlEditor, Version=17.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// location unknown
// Decompiled with ICSharpCode.Decompiler 7.1.0.6543
#endregion


using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

using BlackbirdSql.Core;
using BlackbirdSql.Core.Diagnostics;
using BlackbirdSql.Core.Model;
using BlackbirdSql.Common.Config;
using BlackbirdSql.Common.Model.Interfaces;
using BlackbirdSql.Common.Model.QueryExecution;
using BlackbirdSql.Common.Properties;

using FirebirdSql.Data.FirebirdClient;

using Microsoft.VisualStudio.Shell;
using BlackbirdSql.Core.Extensions;
using Microsoft.VisualStudio.LanguageServer.Client;
using BlackbirdSql.Common.Config.Interfaces;

namespace BlackbirdSql.Common.Model;


public abstract class ConnectionStrategy : IDisposable
{
	protected DbConnectionStringBuilder _Csb = null;

	public delegate void ConnectionChangedEvent(object sender, ConnectionChangedEventArgs args);

	public delegate void DatabaseChangedEvent(object sender, EventArgs args);

	public class ConnectionChangedEventArgs : EventArgs
	{
		public IDbConnection PreviousConnection { get; private set; }

		public ConnectionChangedEventArgs(IDbConnection previousConnection)
		{
			PreviousConnection = previousConnection;
		}
	}

	private static readonly Color DefaultColor = SystemColors.Control;

	private IDbConnection _Connection;

	protected UIConnectionInfo _UiConnectionInfo;

	protected readonly object _InstanceLock = new object();


	public virtual UIConnectionInfo UiConnectionInfo
	{
		get
		{
			lock (_InstanceLock)
			{
				return _UiConnectionInfo;
			}
		}
		private set
		{
			lock (_InstanceLock)
			{
				_UiConnectionInfo = value;
			}
		}
	}

	public IDbConnection Connection
	{
		get
		{
			lock (_InstanceLock)
			{
				return _Connection;
			}
		}
	}

	public virtual string DisplayServerName
	{
		get
		{
			UIConnectionInfo connectionInfo = UiConnectionInfo;
			if (connectionInfo != null && !string.IsNullOrEmpty(connectionInfo.DataSource))
			{
				return connectionInfo.ServerNameNoDot;
			}

			return string.Empty;
		}
	}

	public virtual string DisplayDatabaseName
	{
		get
		{
			lock (_InstanceLock)
			{
				if (Connection != null && !string.IsNullOrEmpty(Connection.Database))
				{
					return Path.GetFileNameWithoutExtension(Connection.Database);
				}

				if (UiConnectionInfo != null)
				{
					return Path.GetFileNameWithoutExtension(UiConnectionInfo.Database);
				}

				return string.Empty;
			}
		}
	}

	public virtual string DisplayUserName
	{
		get
		{
			string text = string.Empty;
			UIConnectionInfo connectionInfo = UiConnectionInfo;
			if (connectionInfo != null)
			{
				text = connectionInfo.UserID;
			}

			return text;
		}
	}


	public virtual Color StatusBarColor
	{
		get
		{
			UIConnectionInfo connectionInfo = UiConnectionInfo;
			Color result = UserSettings.Instance.Current.StatusBar.StatusBarColor;
			if (connectionInfo != null && UseCustomColor(connectionInfo))
			{
				result = GetCustomColor(connectionInfo);
			}

			return result;
		}
	}

	public virtual bool IsExecutionPlanAndQueryStatsSupported => true;

	public event ConnectionChangedEvent ConnectionChanged;

	protected event ConnectionChangedEvent ConnectionChangedPriority;

	public event DatabaseChangedEvent DatabaseChanged;

	public ConnectionStrategy()
	{
	}

	protected void SetDbConnection(IDbConnection value)
	{
		lock (_InstanceLock)
		{
			IDbConnection connection = _Connection;
			if (connection == value)
			{
				return;
			}

			if (_Connection != null)
			{
				if (_Connection.State != 0)
				{
					_Connection.Close();
				}

				_Connection.Dispose();
			}

			_Connection = value;
			OnConnectionChangedPriority(connection);
			OnConnectionChanged(connection);
		}
	}

	public void SetConnectionInfo(UIConnectionInfo uici)
	{
		IDbConnection connection = CreateDbConnectionFromConnectionInfo(uici, tryOpenConnection: false);
		SetConnectionInfo(uici, connection);
	}

	public void SetConnectionInfo(UIConnectionInfo uici, IDbConnection connection)
	{
		if (uici != null && connection == null)
		{
			ArgumentNullException ex = new("connection");
			Diag.Dug(ex);
			throw ex;
		}

		lock (_InstanceLock)
		{
			UiConnectionInfo = uici;
			SetDbConnection(connection);
		}
	}


	protected virtual void AcquireConnectionInfo(bool tryOpenConnection, out UIConnectionInfo uici, out IDbConnection connection)
	{
		uici = new UIConnectionInfo();
		connection = CreateDbConnectionFromConnectionInfo(uici, tryOpenConnection);
	}

	protected virtual void ChangeConnectionInfo(bool tryOpenConnection, out UIConnectionInfo uici, out IDbConnection connection)
	{
		AcquireConnectionInfo(tryOpenConnection, out uici, out connection);
	}

	protected abstract IDbConnection CreateDbConnectionFromConnectionInfo(UIConnectionInfo uici, bool tryOpenConnection);

	public virtual void ApplyConnectionOptions(IDbConnection connection, IQueryExecutionSettings s)
	{
		return;

		/* Probably not needed

		lock (_InstanceLock)
		{
			FbConnection conn = (FbConnection)connection;

			Tracer.Trace(typeof(SqlConnectionStrategy), "ApplyConnectionOptions()", "starting");
			StringBuilder stringBuilder = new StringBuilder(512);
			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0} {1}", s.SetRowCountString, s.SetTextSizeString);
			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, " {0} {1} {2} {3} {4} {5} {6}", s.SetNoCountString, s.SetConcatenationNullString, s.SetArithAbortString, s.SetLockTimeoutString, s.SetQueryGovernorCostString, s.SetDeadlockPriorityLowString, s.SetTransactionIsolationLevelString);
			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, " {0} {1} {2} {3} {4} {5} {6}", s.SetAnsiNullsString, s.SetAnsiNullDefaultString, s.SetAnsiPaddingString, s.SetAnsiWarningsString, s.SetCursorCloseOnCommitString, s.SetImplicitTransactionString, s.SetQuotedIdentifierString);
			string text = stringBuilder.ToString();
			Tracer.Trace(typeof(SqlConnectionStrategy), "ApplyConnectionOptions()", ": Executing script: \"{0}\"", text);
			IDbCommand dbCommand = null;
			try
			{
				dbCommand = conn.CreateCommand();
				dbCommand.CommandText = text;
				dbCommand.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				Diag.Dug(ex);
				Tracer.LogExCatch(typeof(SqlConnectionStrategy), ex);
				StringBuilder stringBuilder2 = new StringBuilder(100);
				stringBuilder2.AppendFormat(SharedResx.UnableToApplyConnectionSettings, ex.Message);
				Cmd.ShowMessageBoxEx(string.Empty, stringBuilder2.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
			finally
			{
				if (dbCommand != null)
				{
					dbCommand.Dispose();
					dbCommand = null;
				}
			}
		}
		*/
	}

	public virtual int GetExecutionTimeout()
	{
		int result = 0;

		if (UiConnectionInfo != null)
			return UiConnectionInfo.CommandTimeout;

		return result;
	}

	public abstract bool IsTransactionOpen();

	public abstract void CommitOpenTransactions();

	public IDbConnection EnsureConnection(bool tryOpenConnection)
	{
		lock (_InstanceLock)
		{
			if (Connection == null || tryOpenConnection && Connection.State != ConnectionState.Open)
			{
				Tracer.Trace(GetType(), Tracer.Level.Verbose, "EnsureConnection", "Connection is null or not open");
				AcquireConnectionInfo(tryOpenConnection, out var uici, out var connection);
				if (uici != null && connection == null)
				{
					connection = CreateDbConnectionFromConnectionInfo(uici, tryOpenConnection: false);
				}

				UiConnectionInfo = uici;
				SetDbConnection(connection);
			}

			return Connection;
		}
	}

	public IDbConnection ChangeConnection(bool tryOpenConnection)
	{
		lock (_InstanceLock)
		{
			if (tryOpenConnection)
			{
				ChangeConnectionInfo(tryOpenConnection, out var uici, out var connection);
				if (uici != null)
				{
					connection ??= CreateDbConnectionFromConnectionInfo(uici, tryOpenConnection: false);

					UiConnectionInfo = uici;
					SetDbConnection(connection);
				}
			}

			return Connection;
		}
	}

	public virtual void ResetConnection()
	{
		lock (_InstanceLock)
		{
			UiConnectionInfo = null;
			SetDbConnection(null);
		}
	}

	private void OnConnectionChanged(IDbConnection previousConnection)
	{
		lock (_InstanceLock)
		{
			ConnectionChangedEventArgs args = new ConnectionChangedEventArgs(previousConnection);
			ConnectionChanged?.Invoke(this, args);
		}
	}

	private void OnConnectionChangedPriority(IDbConnection previousConnection)
	{
		lock (_InstanceLock)
		{
			ConnectionChangedEventArgs args = new ConnectionChangedEventArgs(previousConnection);
			ConnectionChangedPriority?.Invoke(this, args);
		}
	}

	public abstract List<string> GetAvailableDatabases();

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		lock (_InstanceLock)
		{
			if (disposing && Connection != null)
			{
				Connection.Close();
				Connection.Dispose();
				SetDbConnection(null);
			}
		}
	}

	public virtual void SetDatasetKeyOnConnection(string selectedDatasetKey, DbConnectionStringBuilder csb)
	{
		try
		{
			lock (_InstanceLock)
			{
				_Csb = csb;


				if (Connection != null /* && Connection.State == ConnectionState.Open */)
				{
					_Csb ??= XmlParser.GetCsbFromDatabases(selectedDatasetKey);
					if (_Csb != null)
					{
						bool isOpen = Connection.State == ConnectionState.Open;
						if (isOpen)
							Connection.Close();

						Connection.ConnectionString = _Csb.ConnectionString;
						// UiConnectionInfo.ConnectionStringBuilder = _Csb;
						UiConnectionInfo.Parse(_Csb);
						DatabaseChanged?.Invoke(this, new EventArgs());
						if (isOpen)
							Connection.Open();
					}
				}
			}
		}
		catch (FbException e)
		{
			Tracer.LogExCatch(typeof(ConnectionStrategy), e);
			Cmd.ShowMessageBoxEx(null, string.Format(CultureInfo.CurrentCulture, SharedResx.ErrDatabaseNotAccessible, selectedDatasetKey), null, MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	public virtual object GetPropertiesWindowDisplayObject()
	{
		return null;
	}

	public abstract void ResetAndEnableConnectionStatistics();

	public virtual string GetEditorCaption(bool ignoreSettings)
	{
		StringBuilder stringBuilder = new StringBuilder(string.Empty, 80);
		if (UserSettings.Instance.Current.StatusBar.TabTextIncludeServerName || ignoreSettings)
		{
			stringBuilder.Append(DisplayServerName);
		}

		if (UserSettings.Instance.Current.StatusBar.TabTextIncludeDatabaseName || ignoreSettings)
		{
			if (stringBuilder.Length != 0)
			{
				stringBuilder.Append(".");
			}

			stringBuilder.Append(DisplayDatabaseName);
		}

		if ((UserSettings.Instance.Current.StatusBar.TabTextIncludeLoginName || ignoreSettings) && !string.IsNullOrEmpty(DisplayUserName))
		{
			if (stringBuilder.Length != 0)
			{
				if (ignoreSettings)
				{
					stringBuilder.Append(Environment.NewLine);
				}
				else
				{
					stringBuilder.Append(" ");
				}
			}

			stringBuilder.AppendFormat("({0})", DisplayUserName);
		}

		return stringBuilder.ToString();
	}

	private static bool UseCustomColor(UIConnectionInfo uici)
	{
		return false;
		/*
		bool result = false;
		if (uici != null)
		{
			object obj = uici.AdvancedOptions["USE_CUSTOM_CONNECTION_COLOR"];
			if (obj != null)
			{
				if (obj is string text && bool.TryParse(text, out var result2))
				{
					result = result2;
				}
			}
		}

		return result;
		*/
	}

	private static Color GetCustomColor(UIConnectionInfo uici)
	{
		Color result = DefaultColor;
		/*
		if (uici != null)
		{
			object obj = uici.AdvancedOptions["CUSTOM_CONNECTION_COLOR"];
			if (obj != null)
			{
				if (obj is string text)
				{
					if (int.TryParse(text, out int result2))
					{
						result = Color.FromArgb(result2);
					}
				}
			}
		}
		*/
		return result;
	}

	protected void CreateAndOpenConnectionWithCommonMessageLoop(UIConnectionInfo uici, string connectingInfoMessage, string errorPrescription, out IDbConnection connection)
	{
		connection = null;
		IDbConnection c = null;
		Exception exception = null;
		ManualResetEvent resetEvent = new ManualResetEvent(initialState: false);
		Action action = delegate
		{
			try
			{
				c = CreateDbConnectionFromConnectionInfo(uici, tryOpenConnection: false);
				c.Open();
			}
			catch (Exception ex)
			{
				exception = ex;
			}
			finally
			{
				resetEvent.Set();
			}
		};
		CommonMessagePump obj = new CommonMessagePump
		{
			AllowCancel = true,
			EnableRealProgress = false,
			Timeout = TimeSpan.MaxValue,
			WaitTitle = SharedResx.CommonMessageLoopConnecting
		};
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(string.Format(CultureInfo.CurrentCulture, SharedResx.CommonMessageLoopAttemptingToConnect, uici.DataSource));
		if (connectingInfoMessage != null)
		{
			stringBuilder.Append(Environment.NewLine + Environment.NewLine);
			stringBuilder.Append(connectingInfoMessage);
		}

		obj.WaitText = stringBuilder.ToString();
		action.BeginInvoke(null, null);
		if (obj.ModalWaitForHandles(resetEvent) != CommonMessagePumpExitCode.HandleSignaled)
		{
			((Action)delegate
			{
				int connectionTimeout = 15; // Ns2.SqlServerConnectionService.GetConnectionTimeout(uici);
				if (resetEvent.WaitOne(2 * connectionTimeout) && c != null)
				{
					c.Close();
					c.Dispose();
				}
			}).BeginInvoke(null, null);
		}
		else
		{
			connection = c;
		}

		if (exception == null)
		{
			return;
		}

		Tracer.LogExCatch(typeof(ConnectionStrategy), exception);
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
		if (!Cmd.IsInAutomationFunction())
		{
			string value = string.Format(CultureInfo.CurrentCulture, SharedResx.CommonMessageLoopFailedToOpenConnection, uici.DataSource);
			string value2 = string.Format(CultureInfo.CurrentCulture, SharedResx.CommonMessageLoopErrorMessage, exception.Message);
			StringBuilder stringBuilder2 = new StringBuilder();
			stringBuilder2.Append(value);
			stringBuilder2.Append(Environment.NewLine + Environment.NewLine);
			if (!string.IsNullOrEmpty(errorPrescription))
			{
				stringBuilder2.Append(errorPrescription);
				stringBuilder2.Append(Environment.NewLine + Environment.NewLine);
			}

			stringBuilder2.Append(value2);
			Cmd.ShowMessageBoxEx(string.Empty, stringBuilder2.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
	}

	public virtual string GetCustomQuerySuccessMessage()
	{
		return null;
	}

	public abstract Version GetServerVersion();

	public abstract string GetProductLevel();

	public abstract IBatchExecutionHandler CreateBatchExecutionHandler();



	public static void PopulateConnectionStringBuilder(DbConnectionStringBuilder scsb, UIConnectionInfo connectionInfo)
	{
		if (connectionInfo.Database != null)
		{
			connectionInfo.PopulateConnectionStringBuilder(scsb, false);
			// scsb.TrustServerCertificate = SqlServerConnectionService.GetTrustServerCertificate(connectionInfo);
			// scsb.Encrypt = SqlServerConnectionService.GetEncryptConnection(connectionInfo);


			// bool flag = false;

			/*
			if (SqlAuthenticationMethodUtils.IsAuthenticationSupported())
			{
				if (connectionInfo.AuthenticationType == 2)
				{
					SqlAuthenticationMethodUtils.SetAuthentication(scsb, SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryPassword.ToString());
				}
				else if (connectionInfo.AuthenticationType == 3)
				{
					 SqlAuthenticationMethodUtils.SetAuthentication(scsb, SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryIntegrated.ToString());
					flag = true;
				}
				else if (connectionInfo.AuthenticationType == 5)
				{
					SqlAuthenticationMethodUtils.SetAuthentication(scsb, SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryInteractive.ToString());
				}
			}
			*/

			/*
			if (!flag)
			{
				if (SqlServerConnectionService.IsWindowsAuthentication(connectionInfo))
				{
					scsb.IntegratedSecurity = true;
				}
				else
				{
					Scsb.UserID = connectionInfo.UserName;
					if (connectionInfo.AuthenticationType != 5)
					{
						scsb.Password = connectionInfo.Password;
					}

					scsb.PersistSecurityInfo = connectionInfo.PersistPassword;
				}
			}
			*/

		}

		((FbConnectionStringBuilder)scsb).Pooling = false;
	}


}
