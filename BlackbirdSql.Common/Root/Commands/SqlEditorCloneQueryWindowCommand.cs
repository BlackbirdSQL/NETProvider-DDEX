﻿#region Assembly Microsoft.VisualStudio.Data.Tools.SqlEditor, Version=17.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// location unknown
// Decompiled with ICSharpCode.Decompiler 7.1.0.6543
#endregion

using System;
using System.Data;
using BlackbirdSql.Core;
using BlackbirdSql.Common.Model;
using BlackbirdSql.Common.Model.QueryExecution;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Data;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Utilities;
using BlackbirdSql.Common.Interfaces;

namespace BlackbirdSql.Common.Commands;


public class SqlEditorCloneQueryWindowCommand : AbstractSqlEditorCommand
{
	public SqlEditorCloneQueryWindowCommand()
	{
	}

	public SqlEditorCloneQueryWindowCommand(ISqlEditorWindowPane editorWindow)
		: base(editorWindow)
	{
	}

	protected override int HandleQueryStatus(ref OLECMD prgCmd, IntPtr pCmdText)
	{
		prgCmd.cmdf = (uint)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED);

		return VSConstants.S_OK;
	}

	protected override int HandleExec(uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
	{
		using (DpiAwareness.EnterDpiScope(DpiAwarenessContext.SystemAware))
		{
			Cmd.OpenNewMiscellaneousSqlFile(new ServiceProvider(Controller.Instance.DdexPackage.OleServiceProvider));
			AuxiliaryDocData auxiliaryDocDataForEditor = GetAuxiliaryDocDataForEditor();
			ISqlEditorWindowPane lastFocusedSqlEditor = ((IBEditorPackage)Controller.Instance.DdexPackage).LastFocusedSqlEditor;
			if (lastFocusedSqlEditor != null)
			{
				AuxiliaryDocData auxDocData = ((IBEditorPackage)Controller.Instance.DdexPackage).GetAuxiliaryDocData(lastFocusedSqlEditor.DocData);
				if (auxDocData != null)
				{
					QueryExecutor queryExecutor = auxiliaryDocDataForEditor.QueryExecutor;
					ConnectionStrategy connectionStrategy = auxDocData.QueryExecutor.ConnectionStrategy;
					connectionStrategy.SetConnectionInfo(queryExecutor.ConnectionStrategy.UiConnectionInfo);
					IDbConnection connection = connectionStrategy.Connection;
					if (queryExecutor.IsConnected && connection.State != ConnectionState.Open)
					{
						connection.Open();
					}

					auxDocData.IsQueryWindow = auxiliaryDocDataForEditor.IsQueryWindow;
				}
			}
		}

		return VSConstants.S_OK;
	}
}
