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
	public partial class IdentityToken
	{
		[DataMember(Name = "identity")]
		public SpacetimeDB.Identity Identity = new();
		[DataMember(Name = "token")]
		public string Token = "";
		[DataMember(Name = "address")]
		public SpacetimeDB.Address Address = new();

	}
}
