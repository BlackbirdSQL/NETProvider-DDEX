﻿
using BlackbirdSql.Shared.Properties;
using BlackbirdSql.Sys.Ctl.ComponentModel;



namespace BlackbirdSql.Shared.Ctl.ComponentModel;


public sealed class GlobalizedCategoryAttribute : AbstractGlobalizedCategoryAttribute
{
	public override System.Resources.ResourceManager ResMgr => AttributeResources.ResourceManager;


	public GlobalizedCategoryAttribute(string resourceName) : base(resourceName)
	{
	}
}
