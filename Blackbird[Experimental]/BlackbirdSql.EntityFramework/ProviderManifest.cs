﻿/*
 *    The contents of this file are subject to the Initial
 *    Developer's Public License Version 1.0 (the "License");
 *    you may not use this file except in compliance with the
 *    License. You may obtain a copy of the License at
 *    https://github.com/BlackbirdSQL/NETProvider/master/license.txt.
 *
 *    Software distributed under the License is distributed on
 *    an "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either
 *    express or implied. See the License for the specific
 *    language governing rights and limitations under the License.
 *
 *    All Rights Reserved.
 */

//$OriginalAuthors = Jiri Cincura (jiri@cincura.net)

using System;

#if EF6 || NET
using System.Data.Entity.Core;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Core.Metadata.Edm;
#else
using System.Data;
using System.Data.Common;
using System.Data.Metadata.Edm;
#endif

using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Xml;
using BlackbirdSql.Common;

#if EF6 || NET
using BlackbirdSql.Data.Entity.Helpers;
using System.Net.NetworkInformation;
#else
using BlackbirdSql.Data.Helpers;
#endif



#if EF6 || NET
namespace BlackbirdSql.Data.Entity;
#else
namespace BlackbirdSql.Data;
#endif




public class ProviderManifest : DbXmlEnabledProviderManifest
{
	#region Private Fields

	internal const int BinaryMaxSize = int.MaxValue;
	internal const int AsciiVarcharMaxSize = 32765;
	internal const int UnicodeVarcharMaxSize = AsciiVarcharMaxSize / 4;
	internal const char LikeEscapeCharacter = '\\';

	private System.Collections.ObjectModel.ReadOnlyCollection<PrimitiveType> _primitiveTypes = null;
	private System.Collections.ObjectModel.ReadOnlyCollection<EdmFunction> _functions = null;

	#endregion

	#region Constructors

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="manifestToken">A token used to infer the capabilities of the store</param>
	public ProviderManifest(string manifestToken) : base(GetProviderManifest())
	{
		Diag.Trace();
	}

	#endregion

	#region Properties
	#endregion

	internal static XmlReader GetProviderManifest()
	{
		Diag.Trace();
		return GetXmlResource(GetManifestResourceName());
	}

	/// <summary>
	/// Providers should override this to return information specific to their provider.
	///
	/// This method should never return null.
	/// </summary>
	/// <param name="informationType">The name of the information to be retrieved.</param>
	/// <returns>An XmlReader at the begining of the information requested.</returns>
	protected override XmlReader GetDbInformation(string informationType)
	{
		Diag.Trace();
		if (informationType == StoreSchemaDefinition || informationType == StoreSchemaDefinitionVersion3)
		{
			return GetStoreSchemaDescription(informationType);
		}
		if (informationType == StoreSchemaMapping || informationType == StoreSchemaMappingVersion3)
		{
			return GetStoreSchemaMapping(informationType);
		}
		if (informationType == ConceptualSchemaDefinition || informationType == ConceptualSchemaDefinitionVersion3)
		{
			return null;
		}

		Diag.Dug(true, string.Format("The provider returned null for the informationType '{0}'.", informationType));
		throw new ProviderIncompatibleException(string.Format("The provider returned null for the informationType '{0}'.", informationType));
	}

	public override System.Collections.ObjectModel.ReadOnlyCollection<PrimitiveType> GetStoreTypes()
	{
		Diag.Trace();
		_primitiveTypes ??= base.GetStoreTypes();

		return _primitiveTypes;
	}

	public override System.Collections.ObjectModel.ReadOnlyCollection<EdmFunction> GetStoreFunctions()
	{
		Diag.Trace();
		_functions ??= base.GetStoreFunctions();

		return _functions;
	}

	/// <summary>
	/// This method takes a type and a set of facets and returns the best mapped equivalent type
	/// in EDM.
	/// </summary>
	/// <param name="storeType">A TypeUsage encapsulating a store type and a set of facets</param>
	/// <returns>A TypeUsage encapsulating an EDM type and a set of facets</returns>
	public override TypeUsage GetEdmType(TypeUsage storeType)
	{
		if (storeType == null)
		{
			Diag.Dug(true, "Null argument: storeType");
			throw new ArgumentNullException("storeType");
		}

		var storeTypeName = storeType.EdmType.Name.ToLowerInvariant();

		if (!StoreTypeNameToEdmPrimitiveType.ContainsKey(storeTypeName))
		{
			Diag.Dug(true, string.Format("The underlying provider does not support the type '{0}'.", storeTypeName));
			throw new ArgumentException(string.Format("The underlying provider does not support the type '{0}'.", storeTypeName));
		}

		var edmPrimitiveType = StoreTypeNameToEdmPrimitiveType[storeTypeName];

		int maxLength = 0;
		bool isUnicode = true;
		bool isFixedLen;
		bool isUnbounded;

		PrimitiveTypeKind newPrimitiveTypeKind;

		switch (storeTypeName)
		{
			// for some types we just go with simple type usage with no facets
			case "smallint":
			case "int":
			case "bigint":
			case "smallint_bool":
			case "float":
			case "double":
			case "guid":
				return TypeUsage.CreateDefaultTypeUsage(edmPrimitiveType);

			case "decimal":
			case "numeric":
				if (TypeHelpers.TryGetPrecision(storeType, out var precision) && TypeHelpers.TryGetScale(storeType, out var scale))
				{
					return TypeUsage.CreateDecimalTypeUsage(edmPrimitiveType, precision, scale);
				}
				else
				{
					return TypeUsage.CreateDecimalTypeUsage(edmPrimitiveType);
				}

			case "varchar":
				newPrimitiveTypeKind = PrimitiveTypeKind.String;
				isUnbounded = !TypeHelpers.TryGetMaxLength(storeType, out maxLength);
				isUnicode = true; //hardcoded
				isFixedLen = false;
				break;

			case "char":
				newPrimitiveTypeKind = PrimitiveTypeKind.String;
				isUnbounded = !TypeHelpers.TryGetMaxLength(storeType, out maxLength);
				isUnicode = true; //hardcoded
				isFixedLen = true;
				break;

			case "timestamp":
				return TypeUsage.CreateDateTimeTypeUsage(edmPrimitiveType, null);
			case "date":
				return TypeUsage.CreateDateTimeTypeUsage(edmPrimitiveType, null);
			case "time":
				return TypeUsage.CreateTimeTypeUsage(edmPrimitiveType, null);

			case "blob":
				newPrimitiveTypeKind = PrimitiveTypeKind.Binary;
				isUnbounded = true;
				isFixedLen = false;
				break;

			case "clob":
				newPrimitiveTypeKind = PrimitiveTypeKind.String;
				isUnbounded = true;
				isUnicode = true; //hardcoded
				isFixedLen = false;
				break;

			default:
				Diag.Dug(true, string.Format("The underlying provider does not support the type '{0}'.", storeTypeName));
				throw new NotSupportedException(string.Format("The underlying provider does not support the type '{0}'.", storeTypeName));
		}

		Debug.Assert(newPrimitiveTypeKind == PrimitiveTypeKind.String || newPrimitiveTypeKind == PrimitiveTypeKind.Binary, "at this point only string and binary types should be present");

		switch (newPrimitiveTypeKind)
		{
			case PrimitiveTypeKind.String:
				if (!isUnbounded)
				{
					return TypeUsage.CreateStringTypeUsage(edmPrimitiveType, isUnicode, isFixedLen, maxLength);
				}
				else
				{
					return TypeUsage.CreateStringTypeUsage(edmPrimitiveType, isUnicode, isFixedLen);
				}
			case PrimitiveTypeKind.Binary:
				if (!isUnbounded)
				{
					return TypeUsage.CreateBinaryTypeUsage(edmPrimitiveType, isFixedLen, maxLength);
				}
				else
				{
					return TypeUsage.CreateBinaryTypeUsage(edmPrimitiveType, isFixedLen);
				}
			default:
				Diag.Dug(true, string.Format("The underlying provider does not support the type '{0}'.", storeTypeName));
				throw new NotSupportedException(string.Format("The underlying provider does not support the type '{0}'.", storeTypeName));
		}
	}

	/// <summary>
	/// This method takes a type and a set of facets and returns the best mapped equivalent type
	/// in SQL Server, taking the store version into consideration.
	/// </summary>
	/// <param name="storeType">A TypeUsage encapsulating an EDM type and a set of facets</param>
	/// <returns>A TypeUsage encapsulating a store type and a set of facets</returns>
	public override TypeUsage GetStoreType(TypeUsage edmType)
	{
		Diag.Trace();
		if (edmType == null)
		{
			Diag.Dug(true, "Null argument: edmType");
			throw new ArgumentNullException("""edmType""");
		}
		Debug.Assert(edmType.EdmType.BuiltInTypeKind == BuiltInTypeKind.PrimitiveType);

		if (edmType.EdmType is not PrimitiveType primitiveType)
		{
			Diag.Dug(true, string.Format("The underlying provider does not support the type '{0}'.", edmType));
			throw new ArgumentException(string.Format("The underlying provider does not support the type '{0}'.", edmType));
		}

		var facets = edmType.Facets;

		switch (primitiveType.PrimitiveTypeKind)
		{
			case PrimitiveTypeKind.Boolean:
				return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["smallint_bool"]);

			case PrimitiveTypeKind.Int16:
				return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["smallint"]);

			case PrimitiveTypeKind.Int32:
				return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["int"]);

			case PrimitiveTypeKind.Int64:
				return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["bigint"]);

			case PrimitiveTypeKind.Double:
				return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["double"]);

			case PrimitiveTypeKind.Single:
				return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["float"]);

			case PrimitiveTypeKind.Decimal: // decimal, numeric
				{
					if (!TypeHelpers.TryGetPrecision(edmType, out var precision))
					{
						precision = 9;
					}

					if (!TypeHelpers.TryGetScale(edmType, out var scale))
					{
						scale = 0;
					}

					return TypeUsage.CreateDecimalTypeUsage(StoreTypeNameToStorePrimitiveType["decimal"], precision, scale);
				}

			case PrimitiveTypeKind.Binary: // blob
				{
					var isFixedLength = null != facets[MetadataHelpers.FixedLengthFacetName].Value && (bool)facets[MetadataHelpers.FixedLengthFacetName].Value;
					var f = facets[MetadataHelpers.MaxLengthFacetName];

					var isMaxLength = f.IsUnbounded || null == f.Value || (int)f.Value > BinaryMaxSize;
					var maxLength = !isMaxLength ? (int)f.Value : int.MinValue;

					TypeUsage tu;
					if (isFixedLength)
					{
						tu = TypeUsage.CreateBinaryTypeUsage(StoreTypeNameToStorePrimitiveType["blob"], true, maxLength);
					}
					else
					{
						if (isMaxLength)
						{
							tu = TypeUsage.CreateBinaryTypeUsage(StoreTypeNameToStorePrimitiveType["blob"], false);
							Debug.Assert(tu.Facets["MaxLength"].Description.IsConstant, "blob is not constant!");
						}
						else
						{
							tu = TypeUsage.CreateBinaryTypeUsage(StoreTypeNameToStorePrimitiveType["blob"], false, maxLength);
						}
					}
					return tu;
				}

			case PrimitiveTypeKind.String: // char, varchar, text blob
				{
					var isUnicode = null == facets[MetadataHelpers.UnicodeFacetName].Value || (bool)facets[MetadataHelpers.UnicodeFacetName].Value;
					var isFixedLength = null != facets[MetadataHelpers.FixedLengthFacetName].Value && (bool)facets[MetadataHelpers.FixedLengthFacetName].Value;
					var f = facets[MetadataHelpers.MaxLengthFacetName];
					// maxlen is true if facet value is unbounded, the value is bigger than the limited string sizes *or* the facet
					// value is null. this is needed since functions still have maxlength facet value as null
					var isMaxLength = f.IsUnbounded || null == f.Value || (int)f.Value > (isUnicode ? UnicodeVarcharMaxSize : AsciiVarcharMaxSize);
					var maxLength = !isMaxLength ? (int)f.Value : int.MinValue;

					TypeUsage tu;

					if (isUnicode)
					{
						if (isFixedLength)
						{
							tu = TypeUsage.CreateStringTypeUsage(StoreTypeNameToStorePrimitiveType["char"], true, true, maxLength);
						}
						else
						{
							if (isMaxLength)
							{
								tu = TypeUsage.CreateStringTypeUsage(StoreTypeNameToStorePrimitiveType["clob"], true, false);
							}
							else
							{
								tu = TypeUsage.CreateStringTypeUsage(StoreTypeNameToStorePrimitiveType["varchar"], true, false, maxLength);
							}
						}
					}
					else
					{
						if (isFixedLength)
						{
							tu = TypeUsage.CreateStringTypeUsage(StoreTypeNameToStorePrimitiveType["char"], false, true, maxLength);
						}
						else
						{
							if (isMaxLength)
							{
								tu = TypeUsage.CreateStringTypeUsage(StoreTypeNameToStorePrimitiveType["clob"], false, false);
							}
							else
							{
								tu = TypeUsage.CreateStringTypeUsage(StoreTypeNameToStorePrimitiveType["varchar"], false, false, maxLength);
							}
						}
					}
					return tu;
				}

			case PrimitiveTypeKind.DateTime: // datetime, date
				{
					bool useTimestamp;
					if (TypeHelpers.TryGetPrecision(edmType, out var precision))
					{
						if (precision == 0)
							useTimestamp = false;
						else
							useTimestamp = true;
					}
					else
					{
						useTimestamp = true;
					}

					return TypeUsage.CreateDefaultTypeUsage(useTimestamp ? StoreTypeNameToStorePrimitiveType["timestamp"] : StoreTypeNameToStorePrimitiveType["date"]);
				}

			case PrimitiveTypeKind.Time:
				return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["time"]);

			case PrimitiveTypeKind.Guid:
				return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["guid"]);

			default:
				Diag.Dug(true, String.Format("There is no store type corresponding to the EDM type '{0}' of primitive type '{1}'.", edmType, primitiveType.PrimitiveTypeKind));
				throw new NotSupportedException(string.Format("There is no store type corresponding to the EDM type '{0}' of primitive type '{1}'.", edmType, primitiveType.PrimitiveTypeKind));
		}
	}

	private XmlReader GetStoreSchemaMapping(string mslName)
	{
		Diag.Trace();
		return GetXmlResource(GetStoreSchemaResourceName(mslName, "msl"));
	}

	private XmlReader GetStoreSchemaDescription(string ssdlName)
	{
		Diag.Trace();
		return GetXmlResource(GetStoreSchemaResourceName(ssdlName, "ssdl"));
	}

	private static XmlReader GetXmlResource(string resourceName)
	{
		Diag.Trace();
		var executingAssembly = Assembly.GetExecutingAssembly();
		var stream = executingAssembly.GetManifestResourceStream(resourceName);
		return XmlReader.Create(stream);
	}

	private static string GetManifestResourceName()
	{
		Diag.Trace();
#if EF6
		return "BlackbirdSql.Data.Entity.Resources.ProviderManifest.xml";
#else
		return "BlackbirdSql.Data.Resources.ProviderManifest.xml";
#endif
	}

	private static string GetStoreSchemaResourceName(string name, string type)
	{
		Diag.Trace();
#if EF6
		return string.Format("BlackbirdSql.Data.Entity.Resources.{0}.{1}", name, type);
#else
		return string.Format("BlackbirdSql.Data.Resources.{0}.{1}", name, type);
#endif
	}

	public override bool SupportsEscapingLikeArgument(out char escapeCharacter)
	{
		Diag.Trace();
		escapeCharacter = LikeEscapeCharacter;
		return true;
	}

	public override string EscapeLikeArgument(string argument)
	{
		Diag.Trace();
		var sb = new StringBuilder(argument);
		sb.Replace(LikeEscapeCharacter.ToString(), LikeEscapeCharacter.ToString() + LikeEscapeCharacter.ToString());
		sb.Replace("%", LikeEscapeCharacter + "%");
		sb.Replace("_", LikeEscapeCharacter + "_");
		return sb.ToString();
	}

#if EF6 || NET
	public override bool SupportsInExpression()
	{
		Diag.Trace();
		return true;
	}

	public override bool SupportsParameterOptimizationInSchemaQueries()
	{
		Diag.Trace();
		return true;
	}
#endif
}