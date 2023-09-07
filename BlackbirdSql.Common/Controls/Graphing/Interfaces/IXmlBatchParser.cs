// sqlmgmt, Version=16.200.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91
// Microsoft.SqlServer.Management.SqlMgmt.ShowPlan.IXmlBatchParser


using BlackbirdSql.Common.Controls.Graphing.Gram;

namespace BlackbirdSql.Common.Controls.Graphing.Interfaces;

public interface IXmlBatchParser
{
	string GetSingleStatementXml(object dataSource, int statementIndex);

	StmtBlockType GetSingleStatementObject(object dataSource, int statementIndex);
}
