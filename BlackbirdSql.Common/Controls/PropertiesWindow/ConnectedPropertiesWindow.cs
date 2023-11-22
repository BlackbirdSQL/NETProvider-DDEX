﻿// Microsoft.VisualStudio.Data.Tools.SqlEditor, Version=17.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// Microsoft.VisualStudio.Data.Tools.SqlEditor.UI.PropertyWindow.SqlConnectionPropertiesDisplay

using System;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Reflection;
using BlackbirdSql.Common.Controls.Interfaces;
using BlackbirdSql.Common.Ctl.ComponentModel;
using BlackbirdSql.Common.Model;
using BlackbirdSql.Common.Model.QueryExecution;
using BlackbirdSql.Common.Properties;
using BlackbirdSql.Core.Ctl.Diagnostics;
using BlackbirdSql.Core.Ctl.Extensions;
using BlackbirdSql.Core.Model;
using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Data.Services;


namespace BlackbirdSql.Common.Controls.PropertiesWindow
{
	public class ConnectedPropertiesWindow : AbstractPropertiesWindow, IBPropertyWindowQueryManagerInitialize
	{
		private bool _isInitialized;

		[Browsable(false)]
		private SqlConnectionStrategy Strategy { get; set; }

		[Browsable(false)]
		private QueryManager QryMgr { get; set; }

		[Browsable(false)]
		private IDbConnection Connection => Strategy.Connection;

		[GlobalizedCategory("PropertyWindowConnectionDetails")]
		[GlobalizedDescription("PropertyWindowNameDescription")]
		[GlobalizedDisplayName("PropertyWindowNameDisplayName")]
		public string Name
		{
			get
			{
				if (Connection != null)
				{
					CsbAgent csa = new(Connection);
					csa.RegisterDataset();
					return csa.DatasetKey;
				}

				return string.Empty;
			}
		}

		[GlobalizedCategory("PropertyWindowConnectionDetails")]
		[GlobalizedDescription("PropertyWindowServerNameDescription")]
		[GlobalizedDisplayName("PropertyWindowServerNameDisplayName")]
		public string DataSource
		{
			get
			{
				if (Connection != null)
				{
					return ((FbConnection)Connection).DataSource;
				}

				return string.Empty;
			}
		}

		[GlobalizedCategory("PropertyWindowConnectionDetails")]
		[GlobalizedDescription("PropertyWindowServerVersionDescription")]
		[GlobalizedDisplayName("PropertyWindowServerVersionDisplayName")]
		public string ServerVersion
		{
			get
			{
				Version version = null;
				if (Connection != null && Connection.State == ConnectionState.Open)
				{
					version = FbServerProperties.ParseServerVersion(((FbConnection)Connection).ServerVersion);
					// string text = ReliableConnectionHelper.ReadServerVersion(Connection);
					// version = ((text != null) ? new Version(text) : null);
				}

				if (version == null)
					return string.Empty;

				return version.ToString();
			}
		}

		[GlobalizedCategory("PropertyWindowConnectionDetails")]
		[GlobalizedDescription("PropertyWindowClientVersionDescription")]
		[GlobalizedDisplayName("PropertyWindowClientVersionDisplayName")]
		public string ClientVersion
		{
			get
			{
				return typeof(FbConnection).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
			}
		}

		[GlobalizedCategory("PropertyWindowConnectionDetails")]
		[GlobalizedDescription("PropertyWindowDatabaseDescription")]
		[GlobalizedDisplayName("PropertyWindowDatabaseDisplayName")]
		public string Database
		{
			get
			{
				if (Connection != null)
					return ((FbConnection)Connection).Database;

				return string.Empty;
			}
		}

		[GlobalizedCategory("PropertyWindowConnectionDetails")]
		[GlobalizedDescription("PropertyWindowPortDescription")]
		[GlobalizedDisplayName("PropertyWindowPortDisplayName")]
		public int Port
		{
			get
			{
				if (Connection != null)
				{
					CsbAgent csa = new(Connection.ConnectionString);
					return csa.Port;
				}

				return 0;
			}
		}


		[GlobalizedCategory("PropertyWindowQueryExecutionDetailsCategory")]
		[GlobalizedDescription("PropertyWindowStartTimeDescription")]
		[GlobalizedDisplayName("PropertyWindowStartTimeDisplayName")]
		public string StartTime
		{
			get
			{
				if (QryMgr == null || !QryMgr.QueryExecutionStartTime.HasValue)
				{
					return string.Empty;
				}

				return FormatTime(QryMgr.QueryExecutionStartTime.Value);
			}
		}

		[GlobalizedCategory("PropertyWindowQueryExecutionDetailsCategory")]
		[GlobalizedDescription("PropertyWindowFinishTimeDescription")]
		[GlobalizedDisplayName("PropertyWindowFinishTimeDisplayName")]
		public string FinishTime
		{
			get
			{
				if (QryMgr == null || !QryMgr.QueryExecutionEndTime.HasValue)
				{
					return string.Empty;
				}

				return FormatTime(QryMgr.QueryExecutionEndTime.Value);
			}
		}

		[GlobalizedCategory("PropertyWindowQueryExecutionDetailsCategory")]
		[GlobalizedDescription("PropertyWindowElapsedTimeDescription")]
		[GlobalizedDisplayName("PropertyWindowElapsedTimeDisplayName")]
		public string ElapsedTime
		{
			get
			{
				if (QryMgr == null || !QryMgr.QueryExecutionEndTime.HasValue || !QryMgr.QueryExecutionStartTime.HasValue)
				{
					return string.Empty;
				}

				return (QryMgr.QueryExecutionEndTime.Value - QryMgr.QueryExecutionStartTime.Value).FmtSqlStats();
			}
		}

		[GlobalizedCategory("PropertyWindowQueryExecutionDetailsCategory")]
		[GlobalizedDescription("PropertyWindowTotalRowsReturnedDescription")]
		[GlobalizedDisplayName("PropertyWindowTotalRowsReturnedDisplayName")]
		public string TotalRowsReturned
		{
			get
			{
				if (QryMgr != null)
				{
					return QryMgr.RowsAffected.ToString(CultureInfo.CurrentCulture);
				}

				return string.Empty;
			}
		}

		[GlobalizedCategory("PropertyWindowConnectionDetails")]
		[GlobalizedDescription("PropertyWindowStateDescription")]
		[GlobalizedDisplayName("PropertyWindowStateDisplayName")]
		public string State
		{
			get
			{
				string result = string.Empty;
				if (Connection != null)
				{
					result = GetStateString(Connection.State);
				}

				return result;
			}
		}

		[GlobalizedCategory("PropertyWindowConnectionDetails")]
		[GlobalizedDescription("PropertyWindowLoginNameDescription")]
		[GlobalizedDisplayName("PropertyWindowLoginNameDisplayName")]
		public string LoginName => GetLoginName();

		[GlobalizedCategory("PropertyWindowConnectionDetails")]
		[GlobalizedDescription("PropertyWindowSessionIDDescription")]
		[GlobalizedDisplayName("PropertyWindowSessionIDDisplayName")]
		public string SessionTracingID => Strategy.TracingID;

		[GlobalizedCategory("PropertyWindowConnection")]
		[GlobalizedDescription("PropertyWindowConnInfoDescription")]
		[GlobalizedDisplayName("PropertyWindowConnInfoDisplayName")]
		public string ConnectionInfo
		{
			get
			{
				string empty = string.Empty;
				if (Connection != null)
				{
					return string.Format(CultureInfo.CurrentCulture, ControlsResources.PropertyWindowConnectionInfo, Name, GetLoginName());
				}

				return empty;
			}
		}

		// private static string DomainUserName => string.Format(CultureInfo.InvariantCulture, "{0}\\{1}", Environment.UserDomainName, Environment.UserName);

		public ConnectedPropertiesWindow(SqlConnectionStrategy connectionStrategy)
		{
			Strategy = connectionStrategy;
		}

		public bool IsInitialized()
		{
			return _isInitialized;
		}

		public void Initialize(QueryManager qryMgr)
		{
			QryMgr = qryMgr;
			_isInitialized = true;
		}
		public override string GetClassName()
		{
			return AttributeResources.PropertyWindowCurrentConnectionParameters;
		}


		private string GetLoginName()
		{
            Core.Model.UIConnectionInfo connectionInfo = QryMgr.ConnectionStrategy.UiConnectionInfo;
			string result = string.Empty;
			if (connectionInfo != null)
			{
				result = connectionInfo.UserID;
			}

			return result;
		}

		private string FormatTime(DateTime time)
		{
			if (!(time != DateTime.MinValue))
			{
				return string.Empty;
			}

			return time.ToString(CultureInfo.CurrentCulture);
		}


		private string GetStateString(ConnectionState state)
		{
			return state switch
			{
				ConnectionState.Broken => ControlsResources.ConnectionStateBroken,
				ConnectionState.Closed => ControlsResources.ConnectionStateClosed,
				ConnectionState.Connecting => ControlsResources.ConnectionStateConnecting,
				ConnectionState.Executing => ControlsResources.ConnectionStateExecuting,
				ConnectionState.Fetching => ControlsResources.ConnectionStateFetching,
				_ => ControlsResources.ConnectionStateOpen,
			};
		}
	}
}