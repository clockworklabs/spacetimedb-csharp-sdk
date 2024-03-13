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
        public object Args { get; }

        public ReducerEventBase(ClientApi.Event dbEvent, object args)
        {
            ReducerName = dbEvent.FunctionCall.Reducer;
            Timestamp = dbEvent.Timestamp;
            if (dbEvent.CallerIdentity != null)
            {
                Identity = Identity.From(dbEvent.CallerIdentity.ToByteArray());
            }

            if (dbEvent.CallerAddress != null)
            {
                CallerAddress = Address.From(dbEvent.CallerAddress.ToByteArray());
            }

            ErrMessage = dbEvent.Message;
            Status = dbEvent.Status;
            Args = args;
        }

        public abstract bool InvokeHandler();
    }
}
