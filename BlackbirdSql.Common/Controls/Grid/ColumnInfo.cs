﻿#region Assembly Microsoft.SqlServer.DataStorage, Version=16.200.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91
// C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\Extensions\Microsoft\SQLCommon\Microsoft.SqlServer.DataStorage.dll
// Decompiled with ICSharpCode.Decompiler 7.1.0.6543
#endregion

using System;
using System.Collections.Generic;
using System.Data;
using BlackbirdSql.Common.Interfaces;
using BlackbirdSql.Common.Model;
using BlackbirdSql.Core;

// namespace Microsoft.SqlServer.Management.UI.Grid
namespace BlackbirdSql.Common.Controls.Grid;

public class ColumnInfo : IColumnInfo
{
	protected string _ColumnName;

	protected string _DataTypeName;

	protected string _ProviderSpecificDataTypeName;

	protected Type _FieldType;

	protected int _MaxLength;

	protected int _Precision;

	public const int C_ColumnSizeIndex = 2;

	public const int C_PrecisionIndex = 4;

	private static readonly Dictionary<string, bool> _AllServerDataTypes;

	public const int C_UdtAssemblyQualifiedNameIndex = 28;

	private static readonly StorageDataEntity _SColumnDataEntity;

	private bool _IsUdtField;

	protected bool _IsBlobField;

	protected bool _IsCharsField;

	protected bool _IsBytesField;

	protected bool _IsXml;

	protected bool _IsSqlVariant;




	public bool IsUdtField => _IsUdtField;


	public bool IsBlobField => _IsBlobField;


	public bool IsCharsField => _IsCharsField;

	public bool IsBytesField => _IsBytesField;

	public bool IsXml => _IsXml;


	public bool IsSqlVariant => _IsSqlVariant;


	public int MaxLength => _MaxLength;


	public int Precision => _Precision;


	public string DataTypeName => _DataTypeName;


	public string ProviderSpecificDataTypeName => _ProviderSpecificDataTypeName;


	public string ColumnName => _ColumnName;


	public Type FieldType => _FieldType;





	static ColumnInfo()
	{
		_SColumnDataEntity = new StorageDataEntity();
		_AllServerDataTypes = new Dictionary<string, bool>(29);
		_AllServerDataTypes.Add("bigint", value: false);
		_AllServerDataTypes.Add("binary", value: false);
		_AllServerDataTypes.Add("boolean", value: false);
		_AllServerDataTypes.Add("char", value: false);
		_AllServerDataTypes.Add("datetime", value: false);
		_AllServerDataTypes.Add("decimal", value: false);
		_AllServerDataTypes.Add("float", value: false);
		_AllServerDataTypes.Add("image", value: false);
		_AllServerDataTypes.Add("integer", value: false);
		_AllServerDataTypes.Add("double", value: false);
		_AllServerDataTypes.Add("dec16", value: false);
		_AllServerDataTypes.Add("dec34", value: false);
		_AllServerDataTypes.Add("null", value: false);
		_AllServerDataTypes.Add("int128", value: false);
		_AllServerDataTypes.Add("guid", value: false);
		_AllServerDataTypes.Add("timetz", value: false);
		_AllServerDataTypes.Add("smallint", value: false);
		_AllServerDataTypes.Add("numeric", value: false);
		_AllServerDataTypes.Add("text", value: false);
		_AllServerDataTypes.Add("timestamp", value: false);
		_AllServerDataTypes.Add("tinyint", value: false);
		_AllServerDataTypes.Add("varbinary", value: false);
		_AllServerDataTypes.Add("varchar", value: false);
		_AllServerDataTypes.Add("array", value: false);
		_AllServerDataTypes.Add("timetzex", value: false);
		_AllServerDataTypes.Add("date", value: false);
		_AllServerDataTypes.Add("time", value: false);
		_AllServerDataTypes.Add("timestamptz", value: false);
		_AllServerDataTypes.Add("timestamptzex", value: false);
	}

	public ColumnInfo(string name, string serverDataTypeName, string providerSpecificDataTypeName, Type fieldType, int maxLength)
	{
		_ColumnName = name;
		_DataTypeName = serverDataTypeName;
		_ProviderSpecificDataTypeName = providerSpecificDataTypeName;
		_FieldType = fieldType;
		_MaxLength = maxLength;
		_Precision = 0;
		InitFieldTypes();
	}

	public ColumnInfo(StorageDataReader reader, int colIndex)
	{
		_ColumnName = reader.GetName(colIndex);
		_DataTypeName = reader.GetDataTypeName(colIndex);
		DataTable schemaTable = reader.GetSchemaTable();
		_MaxLength = (int)schemaTable.Rows[colIndex][C_ColumnSizeIndex];
		if (!DBNull.Value.Equals(schemaTable.Rows[colIndex][C_PrecisionIndex]))
		{
			_Precision = Convert.ToInt32(schemaTable.Rows[colIndex][C_PrecisionIndex]);
		}

		InitFieldTypes();
		if (!_IsUdtField)
		{
			_ProviderSpecificDataTypeName = reader.GetProviderSpecificDataTypeName(colIndex);
			_FieldType = reader.GetFieldType(colIndex);
			return;
		}

		object obj = schemaTable.Rows[colIndex][C_UdtAssemblyQualifiedNameIndex];
		string text = "MICROSOFT.SQLSERVER.TYPES.SQLHIERARCHYID";
		if (obj != null && string.Compare(obj.ToString(), 0, text, 0, text.Length, StringComparison.OrdinalIgnoreCase) == 0)
		{
			_ProviderSpecificDataTypeName = "System.Data.SqlTypes.SqlBinary";
			_FieldType = _SColumnDataEntity.TypeSqlBinary;
		}
		else
		{
			_ProviderSpecificDataTypeName = "System.Byte[]";
			_FieldType = _SColumnDataEntity.TypeBytes;
			_MaxLength = int.MaxValue;
		}
	}

	public ColumnInfo(string name)
	{
		_ColumnName = name;
	}

	public void InitFieldTypes()
	{
		string text = DataTypeName.ToLowerInvariant();
		switch (text)
		{
			case "varchar":
			case "nvarchar":
				_IsCharsField = true;
				if (MaxLength == int.MaxValue)
				{
					if ("Microsoft SQL Server 2005 XML Showplan" == ColumnName)
					{
						_IsXml = true;
					}

					_IsBlobField = true;
				}

				break;
			case "text":
			case "ntext":
				_IsCharsField = true;
				_IsBlobField = true;
				break;
			case "xml":
				_IsXml = true;
				_IsBlobField = true;
				break;
			case "array":
			case "guid":
			case "binary":
			case "image":
				_IsBytesField = true;
				_IsBlobField = true;
				break;
			case "varbinary":
			case "rowversion":
			case "timestamp":
			case "timestamptz":
			case "timestamptzex":
			case "timetz":
			case "timetzex":
				_IsBytesField = true;
				if (MaxLength == int.MaxValue)
				{
					_IsBlobField = true;
				}

				break;
			case "sql_variant":
				_IsSqlVariant = true;
				break;
			default:
				if (!_AllServerDataTypes.ContainsKey(text))
				{
					Diag.Dug(true, "Invalid text _FieldType: " + text);
					_IsUdtField = true;
					_IsBytesField = true;
					_IsBlobField = true;
				}

				break;
		}
	}
}