// THIS FILE IS AUTOMATICALLY GENERATED BY SPACETIMEDB. EDITS TO THIS FILE
// WILL NOT BE SAVED. MODIFY TABLES IN RUST INSTEAD.
// <auto-generated />

#nullable enable

using System;
using SpacetimeDB;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace SpacetimeDB.ClientApi
{
	[SpacetimeDB.Type]
	[DataContract]
	public partial class OneOffTable
	{
		[DataMember(Name = "table_name")]
		public string TableName = "";
		[DataMember(Name = "rows")]
		public System.Collections.Generic.List<SpacetimeDB.ClientApi.EncodedValue> Rows = new();

	}
}
