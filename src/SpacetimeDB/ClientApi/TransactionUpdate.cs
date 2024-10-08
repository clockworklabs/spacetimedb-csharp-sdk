// THIS FILE IS AUTOMATICALLY GENERATED BY SPACETIMEDB. EDITS TO THIS FILE
// WILL NOT BE SAVED. MODIFY TABLES IN RUST INSTEAD.
// <auto-generated />

#nullable enable

using System;
using SpacetimeDB;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SpacetimeDB.ClientApi
{
	[SpacetimeDB.Type]
	[DataContract]
	public partial class TransactionUpdate
	{
		[DataMember(Name = "status")]
		public SpacetimeDB.ClientApi.UpdateStatus Status;
		[DataMember(Name = "timestamp")]
		public SpacetimeDB.ClientApi.Timestamp Timestamp;
		[DataMember(Name = "caller_identity")]
		public SpacetimeDB.Identity CallerIdentity;
		[DataMember(Name = "caller_address")]
		public SpacetimeDB.Address CallerAddress;
		[DataMember(Name = "reducer_call")]
		public SpacetimeDB.ClientApi.ReducerCallInfo ReducerCall;
		[DataMember(Name = "energy_quanta_used")]
		public SpacetimeDB.ClientApi.EnergyQuanta EnergyQuantaUsed;
		[DataMember(Name = "host_execution_duration_micros")]
		public ulong HostExecutionDurationMicros;

		public TransactionUpdate(
			SpacetimeDB.ClientApi.UpdateStatus Status,
			SpacetimeDB.ClientApi.Timestamp Timestamp,
			SpacetimeDB.Identity CallerIdentity,
			SpacetimeDB.Address CallerAddress,
			SpacetimeDB.ClientApi.ReducerCallInfo ReducerCall,
			SpacetimeDB.ClientApi.EnergyQuanta EnergyQuantaUsed,
			ulong HostExecutionDurationMicros
		)
		{
			this.Status = Status;
			this.Timestamp = Timestamp;
			this.CallerIdentity = CallerIdentity;
			this.CallerAddress = CallerAddress;
			this.ReducerCall = ReducerCall;
			this.EnergyQuantaUsed = EnergyQuantaUsed;
			this.HostExecutionDurationMicros = HostExecutionDurationMicros;
		}

		public TransactionUpdate()
		{
			this.Status = null!;
			this.Timestamp = new();
			this.CallerIdentity = new();
			this.CallerAddress = new();
			this.ReducerCall = new();
			this.EnergyQuantaUsed = new();
		}

	}
}
