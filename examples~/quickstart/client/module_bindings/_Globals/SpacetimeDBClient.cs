// THIS FILE IS AUTOMATICALLY GENERATED BY SPACETIMEDB. EDITS TO THIS FILE
// WILL NOT BE SAVED. MODIFY TABLES IN RUST INSTEAD.
// <auto-generated />

#nullable enable

using System;
using SpacetimeDB;
using SpacetimeDB.ClientApi;

namespace SpacetimeDB.Types
{
	public enum ReducerType
	{
		None,
		SendMessage,
		SetName,
	}

	public interface IReducerArgs : IReducerArgsBase
	{
		ReducerType ReducerType { get; }
		bool InvokeHandler(ReducerEvent reducerEvent);
	}

	public partial class ReducerEvent : ReducerEventBase
	{
		public IReducerArgs? Args { get; }

		public string ReducerName => Args?.ReducerName ?? "<none>";

		[Obsolete("ReducerType is deprecated, please match directly on type of .Args instead.")]
		public ReducerType Reducer => Args?.ReducerType ?? ReducerType.None;

		public ReducerEvent(IReducerArgs? args) : base() => Args = args;
		public ReducerEvent(TransactionUpdate dbEvent, IReducerArgs? args) : base(dbEvent) => Args = args;

		[Obsolete("Accessors that implicitly cast `Args` are deprecated, please match `Args` against the desired type explicitly instead.")]
		public SendMessageArgsStruct SendMessageArgs => (SendMessageArgsStruct)Args!;
		[Obsolete("Accessors that implicitly cast `Args` are deprecated, please match `Args` against the desired type explicitly instead.")]
		public SetNameArgsStruct SetNameArgs => (SetNameArgsStruct)Args!;

		public override bool InvokeHandler() => Args?.InvokeHandler(this) ?? false;
	}

	public class SpacetimeDBClient : SpacetimeDBClientBase<ReducerEvent>
	{
		protected SpacetimeDBClient()
		{
			clientDB.AddTable<Message>();
			clientDB.AddTable<User>();
		}

		public static readonly SpacetimeDBClient instance = new();

		protected override ReducerEvent ReducerEventFromDbEvent(TransactionUpdate update)
		{
			var argBytes = update.ReducerCall.Args;
			IReducerArgs? args = update.ReducerCall.ReducerName switch {
				"send_message" => BSATNHelpers.Decode<SendMessageArgsStruct>(argBytes),
				"set_name" => BSATNHelpers.Decode<SetNameArgsStruct>(argBytes),
				"<none>" => null,
				var reducer => throw new ArgumentOutOfRangeException("Reducer", $"Unknown reducer {reducer}")
			};
			return new ReducerEvent(update, args);
		}
	}
}