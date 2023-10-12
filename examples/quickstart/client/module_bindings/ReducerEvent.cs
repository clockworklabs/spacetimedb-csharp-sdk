// THIS FILE IS AUTOMATICALLY GENERATED BY SPACETIMEDB. EDITS TO THIS FILE
// WILL NOT BE SAVED. MODIFY TABLES IN RUST INSTEAD.

using System;
using ClientApi;
using Newtonsoft.Json.Linq;
using SpacetimeDB;

namespace SpacetimeDB.Types
{
	public enum ReducerType
	{
		None,
		SendMessage,
		SetName,
	}

	public partial class ReducerEvent : ReducerEventBase
	{
		public ReducerType Reducer { get; private set; }

		public ReducerEvent(ReducerType reducer, string reducerName, ulong timestamp, SpacetimeDB.Identity identity, SpacetimeDB.Address? callerAddress, string errMessage, ClientApi.Event.Types.Status status, object args)
			: base(reducerName, timestamp, identity, callerAddress, errMessage, status, args)
		{
			Reducer = reducer;
		}

		public SendMessageArgsStruct SendMessageArgs
		{
			get
			{
				if (Reducer != ReducerType.SendMessage) throw new SpacetimeDB.ReducerMismatchException(Reducer.ToString(), "SendMessage");
				return (SendMessageArgsStruct)Args;
			}
		}
		public SetNameArgsStruct SetNameArgs
		{
			get
			{
				if (Reducer != ReducerType.SetName) throw new SpacetimeDB.ReducerMismatchException(Reducer.ToString(), "SetName");
				return (SetNameArgsStruct)Args;
			}
		}

		public object[] GetArgsAsObjectArray()
		{
			switch (Reducer)
			{
				case ReducerType.SendMessage:
				{
					var args = SendMessageArgs;
					return new object[] {
						args.Text,
					};
				}
				case ReducerType.SetName:
				{
					var args = SetNameArgs;
					return new object[] {
						args.Name,
					};
				}
				default: throw new System.Exception($"Unhandled reducer case: {Reducer}. Please run SpacetimeDB code generator");
			}
		}
	}
}
