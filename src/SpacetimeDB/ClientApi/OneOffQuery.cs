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
	[DataContract]
	[SpacetimeDB.Type]
	public partial class OneOffQuery
	{
		[DataMember(Name = "message_id")]
		public byte[] MessageId = Array.Empty<byte>();

		[DataMember(Name = "query_string")]
		public string QueryString = "";
	}
}
