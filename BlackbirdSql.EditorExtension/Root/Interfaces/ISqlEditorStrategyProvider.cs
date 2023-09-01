﻿#region Assembly Microsoft.VisualStudio.Data.Tools.SqlEditor, Version=17.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\Extensions\Microsoft\SQLEditor\Microsoft.VisualStudio.Data.Tools.SqlEditor.dll
// Decompiled with ICSharpCode.Decompiler 7.1.0.6543
#endregion

using System;
using BlackbirdSql.Common.Model;
using BlackbirdSql.Common.Model.Interfaces;

// using Microsoft.VisualStudio.Data.Tools.SqlEditor.DataModel;




// namespace Microsoft.VisualStudio.Data.Tools.SqlEditor.Interfaces
namespace BlackbirdSql.EditorExtension.Interfaces
{
	public interface ISqlEditorStrategyProvider : IDisposable
	{
		ISqlEditorStrategy CreateEditorStrategy(string documentMoniker, AuxiliaryDocData auxiliaryDocData);
	}
}