﻿// $License = https://github.com/BlackbirdSQL/NETProvider-DDEX/blob/master/Docs/license.txt
// $Authors = GA Christos (greg@blackbirdsql.org)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using BlackbirdSql.Core;
using BlackbirdSql.Core.Model;
using BlackbirdSql.Sys;
using BlackbirdSql.VisualStudio.Ddex.Properties;
using Microsoft.VisualStudio.Data.Framework.AdoDotNet;
using Microsoft.VisualStudio.Data.Services.SupportEntities;



namespace BlackbirdSql.VisualStudio.Ddex.Ctl;


// =========================================================================================================
//										TObjectSelectorRoot Class
//
/// <summary>
/// Implementation of <see cref="IVsDataObjectSelector"/> enumerator interface for the root node
/// </summary>
// =========================================================================================================
public class TObjectSelectorRoot : AdoDotNetRootObjectSelector
{

	// ---------------------------------------------------------------------------------
	#region Fields - TObjectSelectorRoot
	// ---------------------------------------------------------------------------------

	private Csb _Csa = null;



	#endregion Fields





	// =========================================================================================================
	#region Constructors / Destructors - TObjectSelectorRoot
	// =========================================================================================================


	/*
	public TObjectSelectorRoot() : base()
	{
		// Tracer.Trace(GetType(), "TObjectSelectorRoot.TObjectSelectorRoot()");
	}
	*/

	#endregion Constructors / Destructors





	// =================================================================================
	#region Property Accessors - TObjectSelectorRoot
	// =================================================================================


	#endregion Property Accessors





	// =========================================================================================================
	#region Implementations - TObjectSelectorRoot
	// =========================================================================================================



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Enumerates the root node object.
	/// </summary>
	/// <param name="typeName"></param>
	/// <param name="restrictions"></param>
	/// <param name="properties"></param>
	/// <param name="parameters"></param>
	/// <returns>A data reader of the root object</returns>
	// ---------------------------------------------------------------------------------
	protected override IVsDataReader SelectObjects(string typeName, object[] restrictions, string[] properties, object[] parameters)
	{
		// Tracer.Trace(GetType(), "SelectObjects()", "typeName: {0}.", typeName);


		try
		{
			if (typeName == null)
				throw new ArgumentNullException("typeName");

			if (parameters == null || parameters.Length != 1 || parameters[0] is not string)
				throw new ArgumentNullException(Resources.ExceptionInvalidParameters);

			if (Site == null)
				throw new InvalidOperationException(Resources.ExceptionSiteIsNull);
		}
		catch (Exception ex)
		{
			Diag.Dug(ex);
			throw;
		}

		// Tracer.Trace(GetType(), "SelectObjects()", "TYPE IVsDataConnection: {0}.", Site.GetType().FullName);

		object lockedProviderObject = null;
		IVsDataReader reader = null;

		try
		{
			if (RctManager.ShutdownState)
				return null;

			Site.EnsureConnected();

			lockedProviderObject = Site.GetLockedProviderObject();
			if (lockedProviderObject == null)
				throw new NotImplementedException("Site.GetLockedProviderObject()");

			DbConnection connection = DbNative.CastToAssemblyConnection(lockedProviderObject);


			// VS glitch. Null if ado has picked up a project data model firebird assembly.
			if (connection == null)
			{
				connection = (DbConnection)DbNative.CreateDbConnection(Site.DecryptedConnectionString());
				connection.Open();
			}

			// Tracer.Trace(GetType(), "SelectObjects()", "Site type: {0}", Site.GetType().FullName);

			if (_Csa == null || _Csa.Invalidated((IDbConnection)lockedProviderObject))
			{
				_Csa = RctManager.EnsureVolatileInstance((IDbConnection)lockedProviderObject,
					RctManager.GetConnectionSource());
			}

			DataTable schema = CreateSchema(connection, typeName, parameters);


			reader = new AdoDotNetTableReader(schema);
		}
		catch (DbException exf)
		{
			Tracer.Warning(GetType(), "SelectObjects", "{0} error: {1}.", DbNative.DbEngineName, exf.Message);

			LinkageParser parser = LinkageParser.GetInstance((IDbConnection)lockedProviderObject);
			if (parser != null)
				AbstractLinkageParser.DisposeInstance((IDbConnection)lockedProviderObject, parser.Loaded);

			lockedProviderObject = null;
			Site.UnlockProviderObject();
			Site.Close();

			reader = new AdoDotNetTableReader(new DataTable());
		}
		catch (Exception ex)
		{
			lockedProviderObject = null;
			Diag.Dug(ex);
			throw ex;
		}
		finally
		{
			// Only force create the parser 2nd time in.
			if (lockedProviderObject != null)
			{
				// Tracer.Trace(GetType(), "SelectObjects()", "Finally.");

				if (!RctManager.IsEdmConnectionSource)
					LinkageParser.AsyncEnsureLoading((IDbConnection)lockedProviderObject);

				Site.UnlockProviderObject();
			}
		}

		return reader;
	}


	#endregion Implementations





	// =========================================================================================================
	#region Methods - TObjectSelectorRoot
	// =========================================================================================================


	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Reads in the data source information schema and adds connection property
	/// descriptor columns to it as well as additional root node properties defined in
	/// <see cref="Common.Schema.CoreProperties.SourceInformationTypes"/>.
	/// </summary>
	/// <param name="connection"></param>
	/// <param name="parameters"></param>
	/// <returns>Thr root node ready DataSourceInformation schema.</returns>
	// ---------------------------------------------------------------------------------
	private DataTable CreateSchema(DbConnection connection, string typeName, object[] parameters)
	{
		// Tracer.Trace(GetType(), "CreateSchema()", "typename: {0}", typeName);

		DataTable schema = new DataTable();

		if (string.IsNullOrWhiteSpace(typeName))
			typeName = "Root";

		Describer[] describers = typeName == "Database"
			? [.. Csb.Describers.DescriberKeys]
			: [Csb.Describers[SysConstants.C_KeyExDatasetKey], Csb.Describers[SysConstants.C_KeyExConnectionKey]];


		foreach (Describer describer in describers)
			schema.Columns.Add(describer.Name, describer.DataType);

		schema.AcceptChanges();

		schema.BeginLoadData();

		DataRow row = schema.NewRow();

		foreach (DataColumn column in schema.Columns)
			row[column.ColumnName] = RetrieveValue(connection, column.ColumnName);


		schema.Rows.Add(row);

		schema.EndLoadData();
		schema.AcceptChanges();

		/*
		string str = $"Data row for {typeName}: ";

		foreach (DataColumn col in schema.Columns)
		{
			str += col.ColumnName + ":" + (schema.Rows[0][col.Ordinal] == null ? "null" : (schema.Rows[0][col.Ordinal] == DBNull.Value ? "DBNull" : schema.Rows[0][col.Ordinal].ToString())) + ", ";
		}
		*/
		// Tracer.Trace(GetType(), "CreateSchema()", "{0}", str);

		if (parameters != null && parameters.Length == 1 && parameters[0] is DictionaryEntry entry)
		{
			if (entry.Value is object[] array)
			{
				IDictionary<string, object> mappings = GetMappings(array);
				ApplyMappings(schema, mappings);
			}
		}

		// Tracer.Trace(GetType(), "CreateSchema()", "Schema type '{0}' loaded with {1} rows.", typeName, schema.Rows.Count);


		return schema;
	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Retrieves a value for a specified node column.
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	// ---------------------------------------------------------------------------------
	private object RetrieveValue(DbConnection connection, string name, bool retrying = false)
	{
		object retval;
		string strval;
		object errval = DBNull.Value;

		if (retrying)
		{
			// Tracer.Trace(GetType(), "RetrieveValue()", "Retrying");
			try
			{
				connection.Close();
				connection.Open();
			}
			catch { }
		}


		try
		{
			switch (name)
			{
				case SysConstants.C_KeyExDatasetKey:
					retval = _Csa.DatasetKey;
					break;
				case SysConstants.C_KeyExConnectionKey:
					retval = _Csa.ConnectionKey;
					break;
				case SysConstants.C_KeyExConnectionSource:
					retval = EnConnectionSource.ServerExplorer;
					break;
				case SysConstants.C_KeyDataSource:
					retval = connection.DataSource;
					break;
				case SysConstants.C_KeyExDataset:
					retval = _Csa.Dataset;
					break;
				case SysConstants.C_KeyDatabase:
					retval = connection.Database;
					break;
				case SysConstants.C_KeyExDatasetId:
					strval = _Csa.DatasetId;
					if (string.IsNullOrWhiteSpace(strval))
						strval = _Csa.Dataset;
					retval = strval;
					break;
				case SysConstants.C_KeyExDisplayName:
					strval = _Csa.DatasetId;
					if (string.IsNullOrWhiteSpace(strval))
						strval = _Csa.Dataset;
					if (!string.IsNullOrWhiteSpace(_Csa.ConnectionName))
						strval = _Csa.ConnectionName + " | " + strval;
					retval = strval;
					break;
				case SysConstants.C_KeyExClientVersion:
					retval = $"FirebirdSql {DbNative.ClientVersion}";
					break;
				case SysConstants.C_KeyExMemoryUsage:
					errval = -1;
					retval = SysConstants.C_DefaultExMemoryUsage;
					if ((connection.State & ConnectionState.Open) > 0)
					{
						NativeDatabaseInfoProxy info = new(connection);
						(strval, _) = ((long)info.GetCurrentMemory()).FmtByteSize();
						retval = strval;
					}
					break;
				case SysConstants.C_KeyExActiveUsers:
					errval = -1;
					retval = SysConstants.C_DefaultExActiveUsers;
					if ((connection.State & ConnectionState.Open) != 0)
					{
						NativeDatabaseInfoProxy info = new(connection);
						retval = info.GetActiveUsers().Count;
					}
					break;
				default:
					Describer describer = Csb.Describers[name];
					if (!_Csa.ContainsKey(describer.Name))
						retval = describer.DefaultValue ?? DBNull.Value;
					else if (describer.DataType == typeof(int))
						retval = Convert.ToInt32(_Csa[describer.Name]);
					else
						retval = _Csa[describer.Name];
					// Tracer.Trace(GetType(), "RetrieveValue()", "Name: {0}, CsbName: {1}, retval: {2}, ContainsKey(CsbName): {3}, _Csa[CsbName]: {4}.", name, describer.Name, retval, _Csa.ContainsKey(describer.Name), _Csa.ContainsKey(describer.Name) ? _Csa[describer.Name] : "NoExist");

					break;
			}
		}
		catch (Exception ex)
		{
			if (!retrying)
				return RetrieveValue(connection, name, true);

			Diag.Dug(ex, $"Error retrieving PropertyName: '{name}'");
			return errval;
		}

		retval ??= DBNull.Value;

		return retval;
	}


	#endregion Methods




	// =========================================================================================================
	#region Event handlers - TObjectSelectorRoot
	// =========================================================================================================


	protected override void OnSiteChanged(EventArgs e)
	{
		// Tracer.Trace(GetType(), "OnSiteChanged()");

		base.OnSiteChanged(e);
	}


	#endregion Event handlers


}
