// Warning: Some assembly references could not be resolved automatically. This might lead to incorrect decompilation of some parts,
// for ex. property getter/setter access. To get optimal decompilation results, please manually add the missing references to the list of loaded assemblies.
// sqlmgmt, Version=16.200.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91
// Microsoft.SqlServer.Management.SqlMgmt.ShowPlan.StmtCondTypeCondition
using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;
using Microsoft.SqlServer.Management.SqlMgmt.ShowPlan;

[Serializable]
[GeneratedCode("xsd", "4.8.3928.0")]
[DebuggerStepThrough]
[DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://schemas.microsoft.com/sqlserver/2004/07/showplan")]
public class StmtCondTypeCondition
{
	private QueryPlanType queryPlanField;

	private FunctionType[] uDFField;

	public QueryPlanType QueryPlan
	{
		get
		{
			return queryPlanField;
		}
		set
		{
			queryPlanField = value;
		}
	}

	[XmlElement("UDF")]
	public FunctionType[] UDF
	{
		get
		{
			return uDFField;
		}
		set
		{
			uDFField = value;
		}
	}
}
