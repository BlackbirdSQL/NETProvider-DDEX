﻿using System.Configuration;
using System.Reflection;
using BlackbirdSql.Common;



namespace System.Data.Common.BlackbirdSql;

/// <summary>
/// Static class for adding a DslClient as a DotNet system DBProviderFactory on the fly
/// </summary>
public static class DbProviderFactoriesEx
{

	// Something wrong here. Working intermittently. Scopes are changing during build so we're going to do this two ways
	//
	// TBC: Working now so one of these methods can probably be dropped


	/// <summary>
	/// Adds the Firebird client BlackbirdSql.Data.DslClient as a DotNet DBProviderFactory
	/// using the ConfigurationManager
	/// This doesn't seem to be taking
	/// </summary>
	public static bool ConfigurationManagerRegisterFactory(Type factoryClass, string factoryName, string factoryDescription)
	{
		Diag.Dug();

		/*
		 * ConfigurationManager.GetSection()
		 * ---------------------------------
		 * 		 
		 * Returns a System.Data.DataTable containing System.Data.DataRow objects that contain
		 * the following data of DbProviderFactories. 
		 * Ordinal	Column name				Description
		 * -------	-----------				-----------
		 * 0		Name					Human-readable name for the data provider.
		 * 1		Description				Human-readable description of the data provider.
		 * 2		InvariantName			Name that can be used programmatically to refer to the data provider.
		 *									eg. FirebirdSql.Data.FirebirdClient
		 * 3		AssemblyQualifiedName	Fully qualified name of the factory class, which contains enough
		 *									information to instantiate the object.
		 *									eg. FirebirdSql.Data.FirebirdClient.FirebirdClientFactory,
		 *										FirebirdSql.Data.FirebirdClient, Version=9.1.0.0, Culture=neutral,
		 *										PublicKeyToken=3750abcc3150b00c
		*/

		string invariantName = factoryClass.Assembly.GetName().Name;

		if (ConfigurationManager.GetSection("system.data") is not DataSet dataSet)
		{
			Diag.Dug("No \"system.data\" section found in configuration manager!");
			throw new Exception("No \"system.data\" section found in configuration manager!");
		}

		int num = dataSet.Tables.IndexOf("DbProviderFactories");
		DataRow row;

		DataTable table;
		if (num == -1)
		{
			Diag.Dug(String.Format("Registration of \"{0}\" section", "DbProviderFactories"));
			table = dataSet.Tables.Add("DbProviderFactories");
		}
		else
		{
			Diag.Dug(String.Format("\"{0}\" section aready exists", "DbProviderFactories"));
			table = dataSet.Tables[num];
			if ((row = table.Rows.Find(invariantName)) != null)
			{
				Diag.Dug(false,
					String.Format("'DbProviderFactories' section (Columns:{0}) aready contains [{1}:{2}:{3}:{4}] as [{5}:{6}:{7}:{8}]",
					table.Columns.Count, invariantName, factoryName, factoryDescription, factoryClass.AssemblyQualifiedName,
					row[2].ToString(), row[0].ToString(), row[1].ToString(), row[3].ToString()));

				table.Dispose();

				return false;
			}


		}

		Diag.Dug(false,
			String.Format("Registering BlackbirdSql in DbProviderFactories section (Columns:{0}) [{1}:{2}:{3}:{4}]",
			table.Columns.Count, invariantName, factoryName, factoryDescription, factoryClass.AssemblyQualifiedName));

		table.Rows.Add(factoryName, factoryDescription, invariantName, factoryClass.AssemblyQualifiedName);

		table.AcceptChanges();
		table.Dispose();

		return true;
	}



	/// <summary>
	/// Adds the Firebird client BlackbirdSql.Data.DslClient as a DotNet DBProviderFactory
	/// using DbProviderFactories directly
	/// </summary>

	public static bool DbProviderFactoriesRegisterFactory(Type factoryClass, string factoryName, string factoryDescription)
	{
		/*
		 * Spreading this code out for brevity
		*/

		/*
		 * 
		 * DbProviderFactories.GetFactoryClasses()
		 * ---------------------------------------
		 * 		 
		 * Returns a System.Data.DataTable containing System.Data.DataRow objects that contain
		 * the following data of DbProviderFactories. 
		 * Ordinal	Column name				Description
		 * -------	-----------				-----------
		 * 0		Name					Human-readable name for the data provider.
		 * 1		Description				Human-readable description of the data provider.
		 * 2		InvariantName			Name that can be used programmatically to refer to the data provider.
		 *									eg. FirebirdSql.Data.FirebirdClient
		 * 3		AssemblyQualifiedName	Fully qualified name of the factory class, which contains enough
		 *									information to instantiate the object.
		 *									eg. FirebirdSql.Data.FirebirdClient.FirebirdClientFactory,
		 *										FirebirdSql.Data.FirebirdClient, Version=9.1.0.0, Culture=neutral,
		 *										PublicKeyToken=3750abcc3150b00c
		*/
		DataTable table = DbProviderFactories.GetFactoryClasses();
		DataRow row;

		string invariantName = factoryClass.Assembly.GetName().Name;

		if ((row = table.Rows.Find(invariantName)) != null)
		{
			Diag.Dug(false,
				String.Format("'DbProviderFactories' section (Columns:{0}) aready contains [{1}:{2}:{3}:{4}] as [{5}:{6}:{7}:{8}]",
				table.Columns.Count, invariantName, factoryName, factoryDescription, factoryClass.AssemblyQualifiedName,
				row[2].ToString(), row[0].ToString(), row[1].ToString(), row[3].ToString()));

			table.Dispose();

			return false;
		}

		Diag.Dug(false,
			String.Format("Registering BlackbirdSql in DbProviderFactories section (Columns:{0}) [{1}:{2}:{3}:{4}]",
			table.Columns.Count, invariantName, factoryName, factoryDescription, factoryClass.AssemblyQualifiedName));

		table.Rows.Add(factoryName, factoryDescription, invariantName, factoryClass.AssemblyQualifiedName);

		Type dbProviderFactories = typeof(DbProviderFactories);
		FieldInfo fieldInfo = dbProviderFactories.GetField("_providerTable", BindingFlags.Static | BindingFlags.NonPublic);

		fieldInfo.SetValue(null, table);


		// EntityFramework
		// DbConfiguration

		return true;
	}

}
