// THIS FILE IS AUTOMATICALLY GENERATED BY SPACETIMEDB. EDITS TO THIS FILE
// WILL NOT BE SAVED. MODIFY TABLES IN RUST INSTEAD.

using System;
using System.Collections.Generic;
using SpacetimeDB;

namespace SpacetimeDB.Types
{
	public partial class Message : IDatabaseTable
	{
		[Newtonsoft.Json.JsonProperty("sender")]
		public SpacetimeDB.Identity Sender;
		[Newtonsoft.Json.JsonProperty("sent")]
		public ulong Sent;
		[Newtonsoft.Json.JsonProperty("text")]
		public string Text;


		private static void InternalOnValueInserted(object insertedValue)
		{
			var val = (Message)insertedValue;
		}

		private static void InternalOnValueDeleted(object deletedValue)
		{
			var val = (Message)deletedValue;
		}

		public static SpacetimeDB.SATS.AlgebraicType GetAlgebraicType()
		{
			return SpacetimeDB.SATS.AlgebraicType.CreateProductType(new SpacetimeDB.SATS.ProductTypeElement[]
			{
				new SpacetimeDB.SATS.ProductTypeElement("sender", SpacetimeDB.SATS.AlgebraicType.CreateProductType(new SpacetimeDB.SATS.ProductTypeElement[]
			{
				new SpacetimeDB.SATS.ProductTypeElement("__identity_bytes", SpacetimeDB.SATS.AlgebraicType.CreateBytesType()),
			})),
				new SpacetimeDB.SATS.ProductTypeElement("sent", SpacetimeDB.SATS.AlgebraicType.CreateU64Type()),
				new SpacetimeDB.SATS.ProductTypeElement("text", SpacetimeDB.SATS.AlgebraicType.CreateStringType()),
			});
		}

		public static explicit operator Message(SpacetimeDB.SATS.AlgebraicValue value)
		{
			if (value == null) {
				return null;
			}
			var productValue = value.AsProductValue();
			return new Message
			{
				Sender = SpacetimeDB.Identity.From(productValue.elements[0].AsProductValue().elements[0].AsBytes()),
				Sent = productValue.elements[1].AsU64(),
				Text = productValue.elements[2].AsString(),
			};
		}

		public static System.Collections.Generic.IEnumerable<Message> Iter()
		{
			foreach(var entry in SpacetimeDBClient.clientDB.GetEntries("Message"))
			{
				yield return (Message)entry.Item2;
			}
		}
		public static int Count()
		{
			return SpacetimeDBClient.clientDB.Count("Message");
		}
		public static System.Collections.Generic.IEnumerable<Message> FilterBySender(SpacetimeDB.Identity value)
		{
			foreach(var entry in SpacetimeDBClient.clientDB.GetEntries("Message"))
			{
				var productValue = entry.Item1.AsProductValue();
				var compareValue = Identity.From(productValue.elements[0].AsProductValue().elements[0].AsBytes());
				if (compareValue == value) {
					yield return (Message)entry.Item2;
				}
			}
		}

		public static System.Collections.Generic.IEnumerable<Message> FilterBySent(ulong value)
		{
			foreach(var entry in SpacetimeDBClient.clientDB.GetEntries("Message"))
			{
				var productValue = entry.Item1.AsProductValue();
				var compareValue = (ulong)productValue.elements[1].AsU64();
				if (compareValue == value) {
					yield return (Message)entry.Item2;
				}
			}
		}

		public static System.Collections.Generic.IEnumerable<Message> FilterByText(string value)
		{
			foreach(var entry in SpacetimeDBClient.clientDB.GetEntries("Message"))
			{
				var productValue = entry.Item1.AsProductValue();
				var compareValue = (string)productValue.elements[2].AsString();
				if (compareValue == value) {
					yield return (Message)entry.Item2;
				}
			}
		}

		public static bool ComparePrimaryKey(SpacetimeDB.SATS.AlgebraicType t, SpacetimeDB.SATS.AlgebraicValue _v1, SpacetimeDB.SATS.AlgebraicValue _v2)
		{
			return false;
		}

		public delegate void InsertEventHandler(Message insertedValue, SpacetimeDB.Types.ReducerEvent dbEvent);
		public delegate void UpdateEventHandler(Message oldValue, Message newValue, SpacetimeDB.Types.ReducerEvent dbEvent);
		public delegate void DeleteEventHandler(Message deletedValue, SpacetimeDB.Types.ReducerEvent dbEvent);
		public delegate void RowUpdateEventHandler(SpacetimeDBClient.TableOp op, Message oldValue, Message newValue, SpacetimeDB.Types.ReducerEvent dbEvent);
		public static event InsertEventHandler OnInsert;
		public static event UpdateEventHandler OnUpdate;
		public static event DeleteEventHandler OnBeforeDelete;
		public static event DeleteEventHandler OnDelete;
		public static event RowUpdateEventHandler OnRowUpdate;

		public static void OnInsertEvent(object newValue, ClientApi.Event dbEvent)
		{
			OnInsert?.Invoke((Message)newValue,(ReducerEvent)dbEvent?.FunctionCall.CallInfo);
		}

		public static void OnUpdateEvent(object oldValue, object newValue, ClientApi.Event dbEvent)
		{
			OnUpdate?.Invoke((Message)oldValue,(Message)newValue,(ReducerEvent)dbEvent?.FunctionCall.CallInfo);
		}

		public static void OnBeforeDeleteEvent(object oldValue, ClientApi.Event dbEvent)
		{
			OnBeforeDelete?.Invoke((Message)oldValue,(ReducerEvent)dbEvent?.FunctionCall.CallInfo);
		}

		public static void OnDeleteEvent(object oldValue, ClientApi.Event dbEvent)
		{
			OnDelete?.Invoke((Message)oldValue,(ReducerEvent)dbEvent?.FunctionCall.CallInfo);
		}

		public static void OnRowUpdateEvent(SpacetimeDBClient.TableOp op, object oldValue, object newValue, ClientApi.Event dbEvent)
		{
			OnRowUpdate?.Invoke(op, (Message)oldValue,(Message)newValue,(ReducerEvent)dbEvent?.FunctionCall.CallInfo);
		}
	}
}
