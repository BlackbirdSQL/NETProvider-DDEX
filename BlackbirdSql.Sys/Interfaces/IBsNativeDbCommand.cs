// $License = https://github.com/BlackbirdSQL/NETProvider-DDEX/blob/master/Docs/license.txt
// $Authors = GA Christos (greg@blackbirdsql.org)

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Runtime.InteropServices;



namespace BlackbirdSql.Sys;

[Guid(LibraryData.NativeDbCommandServiceGuid)]


// =========================================================================================================
//										IBsNativeDbCommand Interface
/// <summary>
/// Interface for native DbException extension methods service.
/// </summary>
// =========================================================================================================
public interface IBsNativeDbCommand
{
	int AddParameter(DbCommand @this, string name, int index, object value);
	DbDataAdapter CreateDbDataAdapter_(DbCommand @this);
}