﻿//
// $License = https://github.com/BlackbirdSQL/NETProvider-DDEX/blob/master/Docs/license.txt
// $Authors = GA Christos (greg@blackbirdsql.org)
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Microsoft.VisualStudio.Data.Framework;
using Microsoft.VisualStudio.Data.Framework.AdoDotNet;
using Microsoft.VisualStudio.Data.Services;
using Microsoft.VisualStudio.Data.Services.SupportEntities;

using BlackbirdSql.Common;
using BlackbirdSql.Common.Extensions.Commands;
using BlackbirdSql.VisualStudio.Ddex.Schema;
using FirebirdSql.Data.FirebirdClient;
using System.Linq.Expressions;

namespace BlackbirdSql.VisualStudio.Ddex;


// =========================================================================================================
//											TObjectSelector Class
//
/// <summary>
/// Implementation of <see cref="IVsDataObjectSelector"/> enumerator interface
/// </summary>
// =========================================================================================================
class TObjectSelector : AdoDotNetObjectSelector
{

	// ---------------------------------------------------------------------------------
	#region Variables - TObjectSelector
	// ---------------------------------------------------------------------------------


	// Unused - option to distinctly enumerate
	/*
	readonly Dictionary<string, Func<FbConnection, object[], DataTable>> _objectSelectors
		= new()
		{
			{ "Tables",  SelectTables },
			{ "TableColumns", SelectTableColumns },
			{ "TableTriggers", SelectTableTriggers },
			{ "Views", SelectViews },
			{ "ViewColumns", SelectViewColumns },
			{ "ViewTriggers", SelectViewTriggers }
		};
	*/


	#endregion Variables





	// =========================================================================================================
	#region Constructors / Destructors - TObjectSelector
	// =========================================================================================================


	public TObjectSelector()
	{
		// Diag.Trace();
	}


	public TObjectSelector(IVsDataConnection connection) : base(connection)
	{
		// Diag.Trace();
	}


	#endregion Constructors / Destructors





	// =========================================================================================================
	#region Method Implementations - TObjectSelector
	// =========================================================================================================


	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Data object enumeration
	/// </summary>
	/// <remarks>
	/// Also intercepts enumerations from the SE for <see cref="AbstractCommandProvider"/> and
	/// sets <see cref="DataToolsCommands.CommandObjectType"/> to the correct node system object type
	/// </remarks>
	// ---------------------------------------------------------------------------------
	protected override IVsDataReader SelectObjects(string typeName, object[] restrictions,
		string[] properties, object[] parameters)
	{
		// Diag.Trace();

		if (typeName == null)
		{
			ArgumentNullException ex = new("typeName");
			Diag.Dug(ex);
			throw ex;
		}

		if (parameters == null || parameters.Length < 1 || parameters.Length > 2 || parameters[0] is not string)
		{
			ArgumentNullException ex = new("Parameters are invalid");
			Diag.Dug(ex);
			throw ex;
		}

		if (base.Site == null)
		{
			InvalidOperationException ex = new();
			Diag.Dug(ex);
			throw ex;
		}


		object lockedProviderObject = base.Site.GetLockedProviderObject();


		if (lockedProviderObject == null)
		{
			NotImplementedException ex = new();
			Diag.Dug(ex);
			throw ex;
		}


		try
		{
			if (lockedProviderObject is not DbConnection dbConnection)
			{
				NotImplementedException ex = new();
				Diag.Dug(ex);
				throw ex;
			}


			try
			{
				if (DataToolsCommands.CommandObjectType != DataToolsCommands.DataObjectType.None
					&& typeName == "Table" && parameters != null && parameters.Length > 0 && (string)parameters[0] == "Tables"
					&& (restrictions == null || restrictions.Length < 3 || (restrictions.Length > 2 && restrictions[2] == null)))
				{
					if (restrictions == null || restrictions.Length < 4)
					{
						object[] objs = new object[4];

						for (int i = 0; restrictions != null && i < restrictions.Length; i++)
							objs[i] = restrictions[i];

						restrictions = objs;
					}
					restrictions[3] = DataToolsCommands.CommandObjectType == DataToolsCommands.DataObjectType.User ? "TABLE" : "SYSTEM TABLE";
				}
			}
			catch (Exception ex)
			{
				Diag.Dug(ex);
				throw ex;
			}

			DataToolsCommands.CommandObjectType = DataToolsCommands.DataObjectType.None;

			/*
			string str = "Type: " + typeName + " Parameters: ";

			if (parameters != null)
			{
				foreach (object o in parameters)
				{
					if (o == null)
						str += "null,";
					else
						str += o.ToString() + ",";
				}

			}
			str += " Restrictions: ";
			*/

			string[] array = null;

			try
			{
				if (restrictions != null)
				{
					array = new string[restrictions.Length];
					for (int i = 0; i < array.Length; i++)
					{
						// str += (restrictions[i] != null) ? restrictions[i].ToString() : "null,";
						array[i] = restrictions[i]?.ToString();
					}
				}
			}
			catch (Exception ex)
			{
				Diag.Dug(ex);
				throw ex;
			}


			base.Site.EnsureConnected();


			DataTable schema;

			try
			{
				schema = DslSchemaFactory.GetSchema((FbConnection)dbConnection, parameters[0].ToString(), array);
			}
			catch (Exception ex)
			{
				Diag.Dug(ex);
				throw;
			}

			try
			{
				if (parameters.Length == 2 && parameters[1] is DictionaryEntry entry)
				{
					if (entry.Value is object[] array2)
					{
						IDictionary<string, object> mappings = GetMappings(array2);
						ApplyMappings(schema, mappings);
					}
				}
			}
			catch (Exception ex)
			{
				Diag.Dug(ex);
				throw;
			}

			try
			{
				return new AdoDotNetTableReader(schema);
			}
			catch (Exception ex)
			{
				Diag.Dug(ex);
				throw;
			}

		}
		finally
		{
			base.Site.UnlockProviderObject();
		}

	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Override included for TABLE_TYPE hack
	/// </summary>
	/// <param name="typeName"></param>
	/// <param name="parameters"></param>
	/// <returns>The list of supported reestrictions</returns>
	// ---------------------------------------------------------------------------------
	protected override IList<string> GetSupportedRestrictions(string typeName, object[] parameters)
	{
		// Diag.Trace();

		IList<string> list;

		try
		{
			list = base.GetSupportedRestrictions(typeName, parameters);
		}
		catch (Exception ex)
		{
			Diag.Dug(ex);
			throw;
		}

		/*
		string str = "Type: " + typeName + " Parameters: ";
		if (parameters != null)
		{
			foreach (object o in parameters)
			{
				if (o == null)
					str += "null,";
				else
					str += o.ToString() + ",";
			}

		}
		str += " Restrictions: ";
		if (list != null)
		{
			for (int i = 0; i < list.Count; i++)
				str += list[i] + ",";
		}

		// Diag.Trace(str);
		*/

		// Table type hack
		if (typeName == "Table" || typeName == "SystemTable")
		{
			IList<string> array = new string[list.Count + 1];

			for (int i = 0; i < list.Count; i++)
				array[i] = list[i];

			array[list.Count] = "TABLE_TYPE";
			list = array;
		}
		return list;
	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Override for debugging
	/// </summary>
	/// <param name="typeName"></param>
	/// <param name="parameters"></param>
	/// <returns></returns>
	// ---------------------------------------------------------------------------------
	protected override IList<string> GetRequiredRestrictions(string typeName, object[] parameters)
	{

		// Diag.Trace();
		IList<string> list;

		try
		{
			list = base.GetRequiredRestrictions(typeName, parameters);
		}
		catch (Exception ex)
		{
			Diag.Dug(ex);
			throw;
		}

		/*
		string str = "Type: " + typeName + " Parameters: ";
		if (parameters != null)
		{
			foreach (object o in parameters)
			{
				if (o == null)
					str += "null,";
				else
					str += o.ToString() + ",";
			}

		}
		str += " Restrictions: ";
		if (list != null)
		{
			for (int i = 0; i < list.Count; i++)
				str += list[i] + ",";
		}

		// Diag.Trace(str);
		*/

		return list;
	}



	// Everything below from this point is not used - option to enumerate each without using the underlying framework

	/*
	protected override IVsDataReader SelectObjectsEx(
	string typeName,
	object[] restrictions,
	string[] properties,
	object[] parameters)
	{

		IVsDataReader dataReader;

		var connection = (FbConnection)Site.GetLockedProviderObject();
		try
		{
			Site.EnsureConnected();

			var collectionName = (string)parameters[0];
			if (!_objectSelectors.TryGetValue(collectionName, out var selectObjects))
			{
				Diag.Dug(true, string.Format(Resources.UnknownCollection, collectionName));
				throw new ArgumentException(string.Format(Resources.UnknownCollection, collectionName));
			}

			var dataTable = selectObjects(
				connection,
				restrictions ?? Array.Empty<object>());

			if (parameters.Length == 2)
			{
				ApplyMappings(dataTable, GetMappings((object[])((DictionaryEntry)parameters[1]).Value));
			}

			dataReader = new AdoDotNetTableReader(dataTable);
		}
		finally
		{
			Site.UnlockProviderObject();
		}

		return dataReader;
	}

	static DataTable SelectTables(FbConnection connection, object[] restrictions)
	{

		var command = connection.CreateCommand();
		command.CommandText =
		@"
            SELECT name, sql
            FROM system_tables
            WHERE type = 'table'
                AND ($name IS NULL OR name = $name)
        ";
		command.Parameters.AddWithValue("$name", (restrictions.Length <= 0 ? null : restrictions[0]) ?? DBNull.Value);

		var dataTable = new DataTable();
		using (var reader = command.ExecuteReader())
		{
			dataTable.Load(reader);
		}

		return dataTable;
	}

	static DataTable SelectTableColumns(FbConnection connection, object[] restrictions)
	{

		var command = connection.CreateCommand();
		command.CommandText =
		@"
            SELECT t.name AS ""table"", cid, c.name, c.type, ""notnull"", dflt_value, pk, hidden
            FROM system_columns AS t
            JOIN pragma_table_xinfo(t.name) AS c
            WHERE t.type = 'table'
                AND ($table IS NULL OR t.name = $table)
                AND ($name IS NULL OR c.name = $name)
        ";
		command.Parameters.AddWithValue("$table", (restrictions.Length <= 0 ? null : restrictions[0]) ?? DBNull.Value);
		command.Parameters.AddWithValue("$name", (restrictions.Length <= 1 ? null : restrictions[1]) ?? DBNull.Value);

		var dataTable = new DataTable
		{
			Columns =
			{
				{ "table" },
				{ "cid", typeof(long) },
				{ "name" },
				{ "type" },
				{ "notnull", typeof(long) },
				{ "dflt_value" },
				{ "pk", typeof(long) },
				{ "hidden", typeof(long) }
			}
		};
		using (var reader = command.ExecuteReader())
		{
			while (reader.Read())
			{
				var values = new object[8];
				reader.GetValues(values);

				// NB: Can't use Load because dflt_value is nullable without a declared type
				dataTable.Rows.Add(values);
			}
		}

		// TODO: Get collSeq and autoinc via table_column_metadata
		return dataTable;
	}

	static DataTable SelectTableTriggers(FbConnection connection, object[] restrictions)
	{
		var command = connection.CreateCommand();
		command.CommandText =
		@"
            SELECT t.name AS ""table"", r.name, r.sql
            FROM system_triggers AS t
            JOIN system_triggers AS r ON r.tbl_name = t.name
            WHERE t.type = 'table'
                AND r.type == 'trigger'
                AND ($table IS NULL OR t.name = $table)
                AND ($name IS NULL OR r.name = $name)
        ";
		command.Parameters.AddWithValue("$table", (restrictions.Length <= 0 ? null : restrictions[0]) ?? DBNull.Value);
		command.Parameters.AddWithValue("$name", (restrictions.Length <= 1 ? null : restrictions[1]) ?? DBNull.Value);

		var dataTable = new DataTable();
		using (var reader = command.ExecuteReader())
		{
			dataTable.Load(reader);
		}

		return dataTable;
	}

	static DataTable SelectViews(FbConnection connection, object[] restrictions)
	{
		var command = connection.CreateCommand();
		command.CommandText =
		@"
            SELECT name, sql
            FROM system_views
            WHERE type = 'view'
                AND ($name IS NULL OR name = $name)
        ";
		command.Parameters.AddWithValue("$name", (restrictions.Length <= 0 ? null : restrictions[0]) ?? DBNull.Value);

		var dataTable = new DataTable();
		using (var reader = command.ExecuteReader())
		{
			dataTable.Load(reader);
		}

		return dataTable;
	}

	static DataTable SelectViewColumns(FbConnection connection, object[] restrictions)
	{
		var command = connection.CreateCommand();
		command.CommandText =
		@"
            SELECT v.name AS ""view"", cid, c.name, c.type
            FROM system_viewcolumns AS v
            JOIN pragma_table_xinfo(v.name) AS c
            WHERE v.type = 'view'
                AND ($view IS NULL OR v.name = $view)
                AND ($name IS NULL OR c.name = $name)
        ";
		command.Parameters.AddWithValue("$view", (restrictions.Length <= 0 ? null : restrictions[0]) ?? DBNull.Value);
		command.Parameters.AddWithValue("$name", (restrictions.Length <= 1 ? null : restrictions[1]) ?? DBNull.Value);

		var dataTable = new DataTable();
		using (var reader = command.ExecuteReader())
		{
			dataTable.Load(reader);
		}

		return dataTable;
	}

	static DataTable SelectViewTriggers(FbConnection connection, object[] restrictions)
	{
		var command = connection.CreateCommand();
		command.CommandText =
		@"
            SELECT v.name AS ""view"", r.name, r.sql
            FROM system_viewtriggers AS v
            JOIN system_viewtriggers AS r ON r.tbl_name = v.name
            WHERE v.type = 'view'
                AND r.type == 'trigger'
                AND ($view IS NULL OR v.name = $view)
                AND ($name IS NULL OR r.name = $name)
        ";
		command.Parameters.AddWithValue("$view", (restrictions.Length <= 0 ? null : restrictions[0]) ?? DBNull.Value);
		command.Parameters.AddWithValue("$name", (restrictions.Length <= 1 ? null : restrictions[1]) ?? DBNull.Value);

		var dataTable = new DataTable();
		using (var reader = command.ExecuteReader())
		{
			dataTable.Load(reader);
		}

		return dataTable;
	}
	*/


	#endregion Method Implementations

}
