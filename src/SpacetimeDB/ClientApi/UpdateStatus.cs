// THIS FILE IS AUTOMATICALLY GENERATED BY SPACETIMEDB. EDITS TO THIS FILE
// WILL NOT BE SAVED. MODIFY TABLES IN RUST INSTEAD.
// <auto-generated />

#nullable enable

using System;
using SpacetimeDB;

namespace SpacetimeDB.ClientApi
{
	[SpacetimeDB.Type]
	public partial record UpdateStatus : SpacetimeDB.TaggedEnum<(
		SpacetimeDB.ClientApi.DatabaseUpdate Committed,
		string Failed,
		SpacetimeDB.Unit OutOfEnergy
	)>;
}
