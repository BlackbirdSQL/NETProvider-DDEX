﻿#region Assembly Microsoft.VisualStudio.Data.Tools.SqlEditor, Version=17.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// location unknown
// Decompiled with ICSharpCode.Decompiler 7.1.0.6543
#endregion

using System;
using BlackbirdSql.Common.Controls.Interfaces;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;




namespace BlackbirdSql.Common.Ctl.Commands;


public class SqlEditorToggleIntellisenseCommand : AbstractSqlEditorCommand
{
	public SqlEditorToggleIntellisenseCommand()
	{
	}

	public SqlEditorToggleIntellisenseCommand(IBSqlEditorWindowPane editorWindow)
		: base(editorWindow)
	{
	}

	protected override int HandleQueryStatus(ref OLECMD prgCmd, IntPtr pCmdText)
	{
		if (IsDwEditorConnection)
		{
			if (AuxDocData != null)
				StoredAuxDocData.IntellisenseEnabled = false;

			return (int)Constants.MSOCMDERR_E_NOTSUPPORTED;
		}

		prgCmd.cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED;

		if (AuxDocData == null)
			return VSConstants.S_OK;

		if (!StoredIsExecuting)
			prgCmd.cmdf |= (uint)OLECMDF.OLECMDF_ENABLED;


		if (StoredAuxDocData.IntellisenseEnabled.AsBool())
			prgCmd.cmdf |= (uint)OLECMDF.OLECMDF_LATCHED;

		return VSConstants.S_OK;
	}

	protected override int HandleExec(uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
	{
		if (AuxDocData != null)
			StoredAuxDocData.IntellisenseEnabled = !StoredAuxDocData.IntellisenseEnabled.AsBool();

		return VSConstants.S_OK;
	}
}
