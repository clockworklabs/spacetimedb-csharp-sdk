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
	public partial class InitialSubscription
	{
		[DataMember(Name = "database_update")]
		public SpacetimeDB.ClientApi.DatabaseUpdate DatabaseUpdate = new();

		[DataMember(Name = "request_id")]
		public uint RequestId;

		[DataMember(Name = "total_host_execution_duration_micros")]
		public ulong TotalHostExecutionDurationMicros;
	}
}
