// THIS FILE IS AUTOMATICALLY GENERATED BY SPACETIMEDB. EDITS TO THIS FILE
// WILL NOT BE SAVED. MODIFY TABLES IN RUST INSTEAD.
// <auto-generated />

#nullable enable

using System;
using SpacetimeDB;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SpacetimeDB.Types
{
	[SpacetimeDB.Type]
	[DataContract]
	public partial class User : IDatabaseRow
	{
		[DataMember(Name = "identity")]
		public SpacetimeDB.Identity Identity;
		[DataMember(Name = "name")]
		public string? Name;
		[DataMember(Name = "online")]
		public bool Online;

		public User(
			SpacetimeDB.Identity Identity,
			string? Name,
			bool Online
		)
		{
			this.Identity = Identity;
			this.Name = Name;
			this.Online = Online;
		}

		public User()
		{
			this.Identity = new();
		}

	}
}
