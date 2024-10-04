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
	public partial class Message : IDatabaseRow
	{
		[DataMember(Name = "sender")]
		public SpacetimeDB.Identity Sender;
		[DataMember(Name = "sent")]
		public ulong Sent;
		[DataMember(Name = "text")]
		public string Text;

		public Message(
			SpacetimeDB.Identity Sender,
			ulong Sent,
			string Text
		)
		{
			this.Sender = Sender;
			this.Sent = Sent;
			this.Text = Text;
		}

		public Message()
		{
			this.Sender = new();
			this.Text = "";
		}

	}
}
