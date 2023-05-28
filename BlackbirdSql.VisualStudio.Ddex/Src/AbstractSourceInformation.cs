// $License = https://github.com/BlackbirdSQL/NETProvider-DDEX/blob/master/Docs/license.txt
// $Authors = GA Christos (greg@blackbirdsql.org)


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;

using Microsoft.VisualStudio.Data.Core;
using Microsoft.VisualStudio.Data.Framework;
using Microsoft.VisualStudio.Data.Services;
using Microsoft.VisualStudio.Data.Services.SupportEntities;

using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Data.Services;

using BlackbirdSql.Common;
using BlackbirdSql.Common.Extensions;
using BlackbirdSql.Common.Providers;
using BlackbirdSql.VisualStudio.Ddex.Schema;
using BlackbirdSql.VisualStudio.Ddex.Extensions;
namespace BlackbirdSql.VisualStudio.Ddex;


// =========================================================================================================
//										AbstractSourceInformation Class
//
/// <summary>
/// Replacement for <see cref="Microsoft.VisualStudio.Data.Framework.AdoDotNet.AdoDotNetSourceInformation"/>
/// </summary>
// =========================================================================================================
public abstract class AbstractSourceInformation : DataSourceInformation, IVsDataSourceInformation
{
	private DataTable _SourceInformation;



	// ---------------------------------------------------------------------------------
	#region Property Accessors - TSourceInformation
	// ---------------------------------------------------------------------------------


	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Gets a data source information property with the specified name.
	/// </summary>
	// ---------------------------------------------------------------------------------
	object IVsDataSourceInformation.this[string propertyName]
	{
		get
		{
			object value;

			if (SourceInformation != null && SourceInformation.Columns.Contains(propertyName))
			{
				DataColumn col = SourceInformation.Columns[propertyName];
				value = SourceInformation.Rows[0][col.Ordinal];


				if (value == null || value == DBNull.Value)
					value = RetrieveValue(propertyName);
			}
			else
			{
				value = base[propertyName];
			}

			return value;
		}
	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Creates or returns the data source information table.
	/// </summary>
	// ---------------------------------------------------------------------------------
	internal DataTable SourceInformation
	{
		get
		{
			if (_SourceInformation == null && Connection != null)
			{
				LinkageParser parser = LinkageParser.Instance((FbConnection)Connection, false);
				parser?.SyncEnter();

				try
				{

					Site.EnsureConnected();

					_SourceInformation = CreateSourceInformationSchema();

					_SourceInformation ??= new DataTable
					{
						Locale = CultureInfo.InvariantCulture
					};

				}
				catch (Exception ex)
				{
					Diag.Dug(ex);
					throw ex;
				}
				finally
				{
					parser?.SyncExit();
				}
			}


			return _SourceInformation;
		}
	}


	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Return the undelying db connection.
	/// </summary>
	// ---------------------------------------------------------------------------------
	protected DbConnection Connection
	{
		get
		{
			if (Site != null)
			{
				if (Site.GetService(typeof(IVsDataConnectionSupport)) is IVsDataConnectionSupport vsDataConnectionSupport)
					return vsDataConnectionSupport.ProviderObject as DbConnection;
			}

			return null;
		}
	}


	#endregion Property Accessors





	// =========================================================================================================
	#region Constructors / Destructors - AbstractSourceInformation
	// =========================================================================================================


	public AbstractSourceInformation() : base()
	{
		// Diag.Trace();
		AddStandardProperties();
	}

	public AbstractSourceInformation(IVsDataConnection connection) : base(connection)
	{
		// Diag.Trace();
		AddStandardProperties();
	}


	#endregion Constructors / Destructors





	// =========================================================================================================
	#region Methods - AbstractSourceInformation
	// =========================================================================================================





	private void AddStandardProperties()
	{
		foreach (KeyValuePair<string, object> pair in DslProperties.DslDefaultValues)
		{
			object value = pair.Value;

			if (value != null && DslProperties.GetDslType(pair.Key) == typeof(int) && (int)value == int.MinValue)
			{
				value = null;
			}

			if (value == null)
				AddProperty(pair.Key);
			else
				AddProperty(pair.Key, pair.Value);
		}


		AddProperty(CommandPrepareSupport, 1.ToString(CultureInfo.InvariantCulture));
		AddProperty(CommandDeriveParametersSupport, 4.ToString(CultureInfo.InvariantCulture));
		AddProperty(CommandDeriveSchemaSupport, 1.ToString(CultureInfo.InvariantCulture));
		AddProperty(CommandExecuteSupport, 1.ToString(CultureInfo.InvariantCulture));
		AddProperty(CommandParameterSupport, 7);
		AddProperty(IdentifierPartsCaseSensitive);
		AddProperty(ReservedWords);
		AddProperty(SupportsNestedTransactions, false);

	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Returns a Boolean value indicating whether the specified property is contained
	/// in the data source information instance.
	/// </summary>
	// ---------------------------------------------------------------------------------
	bool IVsDataSourceInformation.Contains(string propertyName)
	{
		if (Contains(propertyName)
			|| (SourceInformation != null && SourceInformation.Columns.Contains(propertyName)))
		{
			return true;
		}

		return false;
	}


	protected DataTable CreateSourceInformationSchema()
	{
		object value;
		DataTable schema;


		// Diag.Trace();

		schema = DslSchemaFactory.GetSchema((FbConnection)Connection, "DataSourceInformation", null);

		// Rename each column in the schema to it's AdoDotNet root column name
		foreach (DataColumn col in schema.Columns)
		{
			if (DslProperties.DslSynonyms.TryGetValue(col.ColumnName, out string key))
			{
				if (key != col.ColumnName)
					col.ColumnName = key;
			}
		}


		schema.AcceptChanges();


		// Add in the root types
		foreach (KeyValuePair<string, Type> pair in DslProperties.DslTypes)
		{
			if (!schema.Columns.Contains(pair.Key))
				schema.Columns.Add(pair.Key, pair.Value);
		}

		schema.AcceptChanges();


		IVsDataConnectionProperties connectionProperties = GetConnectionProperties();
		PropertyDescriptorCollection descriptors = GetDescriptors(connectionProperties);

		schema.BeginLoadData();


		DataRow row = schema.Rows[0];
		PropertyDescriptor descriptor;

		/* Descriptor dump
			ParallelWorkers[], IsolationLevel[], Password[], ApplicationName[], FetchSize[], DbCachePages[], Charset[UTF8], ReturnRecordsAffected[], Values[], IsFixedSize[], Role[], initial catalog[C:\Server\Data\smartitplus_databases\MMEINT_SI_DB.FDB], NoGarbageCollect[], Dialect[], DataSource[], MaxPoolSize[], BrowsableConnectionString[], WireCrypt[], port number[55504], ConnectionTimeout[], Port[], ConnectionLifeTime[], Compression[], MinPoolSize[], Keys[], Pooling[], UserID[sysdba], CryptKey[], ClientLibrary[], PacketSize[], CommandTimeout[], Database[], NoDatabaseTriggers[], Count[], Enlist[], ServerType[], IsReadOnly[], data source[MMEI-LT01], ConnectionString[data source=MMEI-LT01;port number=55504;initial catalog=C:\Server\Data\smartitplus_databases\MMEINT_SI_DB.FDB;character set=UTF8;user id=sysdba]
		*/

		// Update the row values for each descriptor
		if (descriptors != null)
		{
			foreach (DataColumn col in schema.Columns)
			{
				descriptor = FindDescriptor(col.ColumnName);

				if (descriptor != null)
				{
					value = descriptor.GetValueX(connectionProperties);

					if (value != null)
						row[col.ColumnName] = value;
				}
			}
		}



		schema.EndLoadData();
		schema.AcceptChanges();


        /*
		string txt = "";
		foreach (DataColumn col in schema.Columns)
		{
			txt += col.ColumnName + ":" + (schema.Rows[0][col.Ordinal] == null ? "null" : schema.Rows[0][col.Ordinal].ToString()) + ", ";
		}

		// Diag.Trace(txt);
		*/

		/*
		LinkageParser parser = LinkageParser.Instance(Site);

		if (parser.ClearToLoadAsync)
			parser.AsyncExecute(50, 20);
		*/

		return schema;
	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Finds the connection string descriptor for a given connection string
	/// parameter or data source information property, if it exists.
	/// </summary>
	// ---------------------------------------------------------------------------------
	protected PropertyDescriptor FindDescriptor(string name)
	{
		PropertyDescriptor descriptor = null;
		string descriptorName;

		if (DslProperties.Synonyms.TryGetValue(name, out string key)
			&& (descriptorName = DslProperties.Descriptor(key)) != null)
		{
			name = descriptorName;
		}


		PropertyDescriptorCollection descriptors = GetDescriptors();

		if (descriptors != null)
			descriptor = descriptors.Find(name, true);

		/*
		if (descriptor == null)
		{
			key ??= "null";
			descriptorName ??= "null";

			ObjectNotFoundException ex = new($"Descriptor {descriptorName} for {name}->{key} not found");
			Diag.Dug(ex);
		}
		*/

		return descriptor;
	}



	internal object GetAdoProperty(string propertyName)
	{
		object result = null;
		if (SourceInformation != null && SourceInformation.Columns.Contains(propertyName))
		{
			result = SourceInformation.Rows[0][propertyName];
		}

		return result;
	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Gets the connection properties object of the current connection string.
	/// </summary>
	/// <returns>
	/// The <see cref="IVsDataConnectionProperties"/> object associated with this root node.
	/// </returns>
	// ---------------------------------------------------------------------------------
	protected IVsDataConnectionProperties GetConnectionProperties()
	{
		IVsDataConnectionProperties connectionProperties;

		IServiceProvider serviceProvider = Site.GetService(typeof(IServiceProvider)) as IServiceProvider;


		Hostess host = new(serviceProvider);

		connectionProperties = host.GetService<IVsDataProviderManager>().Providers[Site.Provider].TryCreateObject<IVsDataConnectionUIProperties>(Site.Source);
		connectionProperties ??= host.GetService<IVsDataProviderManager>().Providers[Site.Provider].TryCreateObject<IVsDataConnectionProperties>(Site.Source);

		return connectionProperties;
	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Gets the property descriptor collection for the current connection string.
	/// </summary>
	/// <returns>The property descriptor collection.</returns>
	// ---------------------------------------------------------------------------------
	protected PropertyDescriptorCollection GetDescriptors(IVsDataConnectionProperties connectionProperties = null)
	{
		PropertyDescriptorCollection descriptors = null;

		connectionProperties ??= GetConnectionProperties();

		if (connectionProperties != null)
		{
			connectionProperties.Parse(Site.SafeConnectionString);

			descriptors = TypeDescriptor.GetProperties(connectionProperties);
		}

		if (descriptors == null)
		{
			DataException ex = new("Connection descriptors is null");
			Diag.Dug(ex);
			throw ex;
		}

		return descriptors;
	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Retrieves the System.Type value indicating the type of a specified property,
	/// thus enabling appropriate conversion of a retrieved value to the correct type.
	/// </summary>
	/// <param name="propertyName">
	/// The name of the property for which to get the type.
	/// </param>
	/// <returns>
	/// A System.Type value indicating the type of a specified property.
	/// </returns>
	/// <exception cref="ArgumentNullException"></exception>
	// ---------------------------------------------------------------------------------
	protected override Type GetType(string propertyName)
	{
		if (propertyName == null)
		{
			ArgumentNullException ex = new(propertyName);
			Diag.Dug(ex);
			throw ex;
		}

		Type type = DslProperties.GetDslType(propertyName) ?? base.GetType(propertyName);


		if (type == null)
		{
			ArgumentException ex = new(propertyName);
			Diag.Dug(ex);
			throw ex;
		}


		return type;
	}




	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Retrieves a value for a specified data source information property.
	/// </summary>
	/// <param name="propertyName"></param>
	/// <returns></returns>
	// ---------------------------------------------------------------------------------
	public object RetrieveSourceInformationValue(string propertyName)
	{
		return RetrieveValue(propertyName);
	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Retrieves a value for a specified data source information property.
	/// </summary>
	/// <param name="propertyName"></param>
	/// <returns></returns>
	// ---------------------------------------------------------------------------------
	protected override object RetrieveValue(string propertyName)
	{

		object retval = null;


		try
		{
			switch (propertyName)
			{
				case DataSourceName:
					retval = Connection.DataSource;
					break;
				case DataSourceProduct:
					retval = GetAdoProperty(propertyName);
					break;
				case DataSourceVersion:
					retval = "Firebird " + FbServerProperties.ParseServerVersion(Connection.ServerVersion).ToString();
					break;
				case DefaultCatalog:
					if (Connection.Database != null && Connection.Database.Length > 0)
						retval = Connection.Database;
					break;
				case ReservedWords:
					DataTable dataTable = null;
					if (Connection != null)
					{
						base.Site.EnsureConnected();
						try
						{
							dataTable = Connection.GetSchema(DbMetaDataCollectionNames.ReservedWords);
						}
						catch
						{
						}
					}

					if (dataTable != null)
					{
						using (dataTable)
						{
							StringBuilder stringBuilder = new StringBuilder();
							foreach (DataRow row in dataTable.Rows)
							{
								stringBuilder.Append(row[0]);
								stringBuilder.Append(",");
							}

							retval = stringBuilder.ToString().TrimEnd(',');
						}
					}
					break;
				case SupportsAnsi92Sql:
					retval = false;
					object adoProperty = GetAdoProperty(DbMetaDataColumnNames.SupportedJoinOperators);
					if (adoProperty is SupportedJoinOperators supportedJoinOperators)
					{
						if ((supportedJoinOperators & SupportedJoinOperators.LeftOuter) > SupportedJoinOperators.None
							|| (supportedJoinOperators & SupportedJoinOperators.RightOuter) > SupportedJoinOperators.None
							|| (supportedJoinOperators & SupportedJoinOperators.FullOuter) > SupportedJoinOperators.None)
						{
							retval = true;
						}
					}
					break;

				case IdentifierPartsCaseSensitive:
					retval = false;
					object adoProperty2 = GetAdoProperty(DbMetaDataColumnNames.IdentifierCase);
					if (adoProperty2 is int)
					{
						IdentifierCase identifierCase = (IdentifierCase)adoProperty2;
						if (identifierCase == IdentifierCase.Sensitive)
						{
							retval = true;
						}

						_ = 1;
					}

					break;

				case QuotedIdentifierPartsCaseSensitive:
					retval = true;
					object adoProperty3 = GetAdoProperty(DbMetaDataColumnNames.QuotedIdentifierCase);
					if (adoProperty3 is int)
					{
						switch ((IdentifierCase)adoProperty3)
						{
							case IdentifierCase.Sensitive:
								retval = true;
								break;
							case IdentifierCase.Insensitive:
								retval = false;
								break;
						}
					}

					break;
				default:
					retval = null;
					break;
			}
		}
		catch (Exception ex)
		{
			Diag.Dug(ex);
			return null;
		}

		retval ??= base.RetrieveValue(propertyName);

		return retval;
	}


	#endregion Methods





	// =========================================================================================================
	#region Event handlers - AbstractSourceInformation
	// =========================================================================================================





	protected override void OnSiteChanged(EventArgs e)
	{
		base.OnSiteChanged(e);

		if (Site == null && _SourceInformation != null)
		{
			_SourceInformation.Dispose();
			_SourceInformation = null;
		}

		/*
		if (Connection != null && (Connection.State & ConnectionState.Open) != 0 && _SourceInformation != null)
		{

			LinkageParser parser = LinkageParser.Instance((FbConnection)Connection);

			if (parser.ClearToLoadAsync)
				parser.AsyncExecute(10, 5);
		}
		*/
	}


	#endregion Event handlers

}
