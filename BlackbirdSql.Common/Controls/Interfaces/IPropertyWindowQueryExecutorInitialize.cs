﻿#region Assembly Microsoft.VisualStudio.Data.Tools.SqlEditor, Version=17.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// location unknown
// Decompiled with ICSharpCode.Decompiler 7.1.0.6543
#endregion

using BlackbirdSql.Common.Model.QueryExecution;


namespace BlackbirdSql.Common.Controls.Interfaces;

public interface IPropertyWindowQueryExecutorInitialize
{
	bool IsInitialized();

	void Initialize(QueryExecutor queryExecutor);
}
