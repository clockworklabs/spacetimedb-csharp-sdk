namespace SpacetimeDB
{
    public abstract class ReducerEventBase
    {
        public string ReducerName { get; }
        public ulong Timestamp { get; }
        public SpacetimeDB.Identity Identity { get; }
        public SpacetimeDB.Address? CallerAddress { get; }
        public string ErrMessage { get; }
        public ClientApi.Event.Types.Status Status { get; }
        protected object Args;

        public ReducerEventBase(ClientApi.Event dbEvent, object args)
        {
            ReducerName = dbEvent.FunctionCall.Reducer;
            Timestamp = dbEvent.Timestamp;
            Identity = Identity.From(dbEvent.CallerIdentity.ToByteArray());
            CallerAddress = Address.From(dbEvent.CallerAddress.ToByteArray());
            ErrMessage = dbEvent.Message;
            Status = dbEvent.Status;
            Args = args;
        }
    }
}
