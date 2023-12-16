﻿// Microsoft.VisualStudio.Data.Tools.SqlEditor, Version=17.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// Microsoft.VisualStudio.Data.Tools.SqlEditor.VSIntegration.SqlEditorToolbarCommandHandler<T>

using System;
using BlackbirdSql.Common.Controls;
using BlackbirdSql.Common.Ctl;
using BlackbirdSql.Common.Ctl.Commands;
using BlackbirdSql.Common.Ctl.Interfaces;

using Microsoft.VisualStudio.OLE.Interop;

// namespace Microsoft.VisualStudio.Data.Tools.SqlEditor.VSIntegration
namespace BlackbirdSql.EditorExtension.Ctl
{
	public sealed class SqlEditorToolbarCommandHandler<T>(Guid clsidCmdSet, uint cmdId)
		: IBTabbedEditorToolbarCommandHandler where T : AbstractSqlEditorCommand, new()
	{
		private readonly GuidId _GuidId = new GuidId(clsidCmdSet, cmdId);

		public GuidId GuidId => _GuidId;

		public int HandleQueryStatus(AbstractTabbedEditorPane editorPane, ref OLECMD prgCmd, IntPtr pCmdText)
		{
			if (editorPane is SqlEditorTabbedEditorPane sqlEditorTabbedEditorPane)
			{
				return new T
				{
					EditorWindow = sqlEditorTabbedEditorPane
				}.QueryStatus(ref prgCmd, pCmdText);
			}

			return (int)Constants.MSOCMDERR_E_NOTSUPPORTED;
		}

		public int HandleExec(AbstractTabbedEditorPane editorPane, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
		{
			if (editorPane is SqlEditorTabbedEditorPane sqlEditorTabbedEditorPane)
			{
				return new T
				{
					EditorWindow = sqlEditorTabbedEditorPane
				}.Exec(nCmdexecopt, pvaIn, pvaOut);
			}

			return (int)Constants.MSOCMDERR_E_NOTSUPPORTED;
		}
	}
}
