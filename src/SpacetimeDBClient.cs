#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ClientApi;
using SpacetimeDB.BSATN;
using Channel = System.Threading.Channels.Channel;
using Thread = System.Threading.Thread;
using Google.Protobuf;
using Event = ClientApi.Event;

namespace SpacetimeDB
{
    public class SpacetimeDBClient
    {
        public enum TableOp
        {
            Insert,
            Delete,
            Update,
            NoChange,
        }

        public struct DbOp
        {
            public ClientCache.ITableCache table;
            public TableOp op;
            public IDatabaseTable newValue;
            public IDatabaseTable oldValue;
            public byte[] deletedBytes;
            public byte[] insertedBytes;
            public object primaryKeyValue;
        }

        /// <summary>
        /// Called when a connection is established to a spacetimedb instance.
        /// </summary>
        public event Action onConnect;

        /// <summary>
        /// Called when a connection attempt fails.
        /// </summary>
        public event Action<WebSocketError?, string> onConnectError;

        /// <summary>
        /// Called when an exception occurs when sending a message.
        /// </summary>
        public event Action<Exception> onSendError;

        /// <summary>
        /// Called when a connection that was established has disconnected.
        /// </summary>
        public event Action<WebSocketCloseStatus?, WebSocketError?> onDisconnect;

        /// <summary>
        /// Invoked when a subscription is about to start being processed. This is called even before OnBeforeDelete.
        /// </summary>
        public event Action onBeforeSubscriptionApplied;

        /// <summary>
        /// Invoked when the local client cache is updated as a result of changes made to the subscription queries.
        /// </summary>
        public event Action onSubscriptionApplied;

        /// <summary>
        /// Invoked when a reducer is returned with an error and has no client-side handler.
        /// </summary>
        public event Action<ReducerEventBase> onUnhandledReducerError;

        /// <summary>
        /// Called when we receive an identity from the server
        /// </summary>
        public event Action<string, Identity, Address> onIdentityReceived;

        /// <summary>
        /// Invoked when an event message is received or at the end of a transaction update.
        /// </summary>
        public event Action<ClientApi.Event> onEvent;

        public readonly Address clientAddress = Address.Random();

        private SpacetimeDB.WebSocket webSocket;
        private bool connectionClosed;
        public static ClientCache clientDB = new();

        private static Func<ClientApi.Event, ReducerEventBase> reducerEventFromDbEvent;

        public static void SetReducerEventFromDbEvent(Func<ClientApi.Event, ReducerEventBase> reducerEventFromDbEvent_)
        {
            if (reducerEventFromDbEvent != null)
            {
                throw new Exception("Another module has already called SetReducerEventFromDbEvent!");
            }
            reducerEventFromDbEvent = reducerEventFromDbEvent_;
        }

        private readonly Dictionary<Guid, Channel<OneOffQueryResponse>> waitingOneOffQueries = new();

        private bool isClosing;
        private Thread networkMessageProcessThread;
        private Thread stateDiffProcessThread;

        public static SpacetimeDBClient instance;

        public ISpacetimeDBLogger Logger => logger;
        private ISpacetimeDBLogger logger;
        private Stats stats = new();

        public static void CreateInstance(ISpacetimeDBLogger loggerToUse)
        {
            if (instance == null)
            {
                new SpacetimeDBClient(loggerToUse);
            }
            else
            {
                loggerToUse.LogError($"Instance already created.");
            }
        }

        protected SpacetimeDBClient(ISpacetimeDBLogger loggerToUse)
        {
            if (instance != null)
            {
                loggerToUse.LogError($"There is more than one {nameof(SpacetimeDBClient)}");
                return;
            }

            instance = this;
            logger = loggerToUse;

            var options = new SpacetimeDB.ConnectOptions
            {
                //v1.bin.spacetimedb
                //v1.text.spacetimedb
                Protocol = "v1.bin.spacetimedb",
            };
            webSocket = new SpacetimeDB.WebSocket(logger, options);
            webSocket.OnMessage += OnMessageReceived;
            webSocket.OnClose += (code, error) => onDisconnect?.Invoke(code, error);
            webSocket.OnConnect += () => onConnect?.Invoke();
            webSocket.OnConnectError += (a, b) => onConnectError?.Invoke(a, b);
            webSocket.OnSendError += a => onSendError?.Invoke(a);

            if (reducerEventFromDbEvent == null)
            {
                loggerToUse.LogError($"Could not find reducer event type. Have you run spacetime generate?");
            }

            _preProcessCancellationToken = _preProcessCancellationTokenSource.Token;
            networkMessageProcessThread = new Thread(PreProcessMessages);
            networkMessageProcessThread.Start();

            _stateDiffCancellationToken = _stateDiffCancellationTokenSource.Token;
            stateDiffProcessThread = new Thread(ExecuteStateDiff);
            stateDiffProcessThread.Start();
        }

        struct PreProcessedMessage
        {
            public Message message;
            public List<DbOp> dbOps;
            public Dictionary<System.Type, HashSet<byte[]>> inserts;
        }

        private readonly BlockingCollection<byte[]> _messageQueue =
            new BlockingCollection<byte[]>(new ConcurrentQueue<byte[]>());

        private readonly BlockingCollection<PreProcessedMessage> _preProcessedNetworkMessages =
            new BlockingCollection<PreProcessedMessage>(new ConcurrentQueue<PreProcessedMessage>());

        private CancellationTokenSource _preProcessCancellationTokenSource = new CancellationTokenSource();
        private CancellationToken _preProcessCancellationToken;

        void PreProcessMessages()
        {
            while (!isClosing)
            {
                try
                {
                    var bytes = _messageQueue.Take(_preProcessCancellationToken);
                    var preprocessedMessage = PreProcessMessage(bytes);
                    _preProcessedNetworkMessages.Add(preprocessedMessage, _preProcessCancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Normal shutdown
                    return;
                }
            }

            PreProcessedMessage PreProcessMessage(byte[] bytes)
            {
                var dbOps = new List<DbOp>();
                var message = Message.Parser.ParseFrom(bytes);

                // This is all of the inserts
                Dictionary<System.Type, HashSet<byte[]>> subscriptionInserts = null;
                // All row updates that have a primary key, this contains inserts, deletes and updates.
                var primaryKeyChanges = new Dictionary<(System.Type tableType, object primaryKeyValue), DbOp>();

                HashSet<byte[]> GetInsertHashSet(System.Type tableType, int tableSize)
                {
                    if (!subscriptionInserts.TryGetValue(tableType, out var hashSet))
                    {
                        hashSet = new HashSet<byte[]>(capacity: tableSize, comparer: ByteArrayComparer.Instance);
                        subscriptionInserts[tableType] = hashSet;
                    }

                    return hashSet;
                }

                switch (message)
                {
                    case { TypeCase: Message.TypeOneofCase.SubscriptionUpdate, SubscriptionUpdate: var subscriptionUpdate }:
                        subscriptionInserts = new Dictionary<System.Type, HashSet<byte[]>>(
                            capacity: subscriptionUpdate.TableUpdates.Sum(a => a.TableRowOperations.Count));
                        // First apply all of the state
                        foreach (var update in subscriptionUpdate.TableUpdates)
                        {
                            var tableName = update.TableName;
                            var table = clientDB.GetTable(tableName);
                            var hashSet = GetInsertHashSet(table.ClientTableType, subscriptionUpdate.TableUpdates.Count);
                            if (table == null)
                            {
                                logger.LogError($"Unknown table name: {tableName}");
                                continue;
                            }

                            foreach (var row in update.TableRowOperations)
                            {
                                var rowBytes = row.Row.ToByteArray();

                                if (row.Op != TableRowOperation.Types.OperationType.Insert)
                                {
                                    logger.LogWarning("Non-insert during a subscription update!");
                                    continue;
                                }

                                var obj = table.SetAndForgetDecodedValue(row.Row);
                                var op = new DbOp
                                {
                                    table = table,
                                    deletedBytes = null,
                                    insertedBytes = rowBytes,
                                    op = TableOp.Insert,
                                    newValue = obj,
                                    oldValue = null,
                                    primaryKeyValue = null,
                                };

                                if (!hashSet.Add(rowBytes))
                                {
                                    logger.LogError($"Multiple of the same insert in the same subscription update: table={table.ClientTableType.Name} rowBytes={rowBytes}");
                                }
                                else
                                {
                                    dbOps.Add(op);
                                }
                            }
                        }

                        break;

                    case { TypeCase: Message.TypeOneofCase.TransactionUpdate, TransactionUpdate: var transactionUpdate }:
                        // First apply all of the state
                        foreach (var update in transactionUpdate.SubscriptionUpdate.TableUpdates)
                        {
                            var tableName = update.TableName;
                            var table = clientDB.GetTable(tableName);
                            if (table == null)
                            {
                                logger.LogError($"Unknown table name: {tableName}");
                                continue;
                            }

                            foreach (var row in update.TableRowOperations)
                            {
                                var rowBytes = row.Row.ToByteArray();

                                var obj = table.SetAndForgetDecodedValue(row.Row);

                                var op = new DbOp
                                {
                                    table = table,
                                    deletedBytes =
                                        row.Op == TableRowOperation.Types.OperationType.Delete ? rowBytes : null,
                                    insertedBytes =
                                        row.Op == TableRowOperation.Types.OperationType.Delete ? null : rowBytes,
                                    op = row.Op == TableRowOperation.Types.OperationType.Delete
                                        ? TableOp.Delete
                                        : TableOp.Insert,
                                    newValue = row.Op == TableRowOperation.Types.OperationType.Delete ? null : obj,
                                    oldValue = row.Op == TableRowOperation.Types.OperationType.Delete ? obj : null,
                                };

                                if (obj is IDatabaseTableWithPrimaryKey objWithPk)
                                {
                                    op.primaryKeyValue = objWithPk.GetPrimaryKeyValue();

                                    var key = (table.ClientTableType, op.primaryKeyValue);

                                    if (primaryKeyChanges.TryGetValue(key, out var oldOp))
                                    {
                                        if (oldOp.op == op.op || oldOp.op == TableOp.Update)
                                        {
                                            logger.LogWarning($"Update with the same primary key was " +
                                                              $"applied multiple times! tableName={tableName}");
                                            // TODO(jdetter): Is this a correctable error? This would be a major error on the
                                            // SpacetimeDB side.
                                            continue;
                                        }

                                        var insertOp = op;
                                        var deleteOp = oldOp;
                                        if (op.op == TableOp.Delete)
                                        {
                                            insertOp = oldOp;
                                            deleteOp = op;
                                        }

                                        op = new DbOp
                                        {
                                            table = insertOp.table,
                                            op = TableOp.Update,
                                            newValue = insertOp.newValue,
                                            oldValue = deleteOp.oldValue,
                                            deletedBytes = deleteOp.deletedBytes,
                                            insertedBytes = insertOp.insertedBytes,
                                            primaryKeyValue = insertOp.primaryKeyValue,
                                        };
                                    }

                                    primaryKeyChanges[key] = op;
                                }
                                else
                                {
                                    dbOps.Add(op);
                                }
                            }
                        }

                        // Combine primary key updates and non-primary key updates
                        dbOps.AddRange(primaryKeyChanges.Values);

                        // Convert the generic event arguments in to a domain specific event object, this gets fed back into
                        // the message.TransactionUpdate.Event.FunctionCall.CallInfo field.
                        var dbEvent = message.TransactionUpdate.Event;
                        dbEvent.FunctionCall.CallInfo = reducerEventFromDbEvent(dbEvent);

                        break;
                    case { TypeCase: Message.TypeOneofCase.OneOffQueryResponse, OneOffQueryResponse: var resp }:
                        /// This case does NOT produce a list of DBOps, because it should not modify the client cache state!
                        Guid messageId = new Guid(resp.MessageId.Span);

                        if (!waitingOneOffQueries.ContainsKey(messageId))
                        {
                            logger.LogError("Response to unknown one-off-query: " + messageId);
                            break;
                        }

                        waitingOneOffQueries[messageId].Writer.TryWrite(resp);
                        waitingOneOffQueries.Remove(messageId);
                        break;
                }


                // logger.LogWarning($"Total Updates preprocessed: {totalUpdateCount}");
                return new PreProcessedMessage { message = message, dbOps = dbOps, inserts = subscriptionInserts };
            }
        }

        struct ProcessedMessage
        {
            public Message message;
            public List<DbOp> dbOps;
        }

        // The message that has been preprocessed and has had its state diff calculated

        private BlockingCollection<ProcessedMessage> _stateDiffMessages = new BlockingCollection<ProcessedMessage>();
        private CancellationTokenSource _stateDiffCancellationTokenSource = new CancellationTokenSource();
        private CancellationToken _stateDiffCancellationToken;

        void ExecuteStateDiff()
        {
            while (!isClosing)
            {
                try
                {
                    var message = _preProcessedNetworkMessages.Take(_stateDiffCancellationToken);
                    var (m, events) = CalculateStateDiff(message);
                    _stateDiffMessages.Add(new ProcessedMessage { dbOps = events, message = m, });
                }
                catch (OperationCanceledException)
                {
                    // Normal shutdown
                    return;
                }
            }

            (Message, List<DbOp>) CalculateStateDiff(PreProcessedMessage preProcessedMessage)
            {
                var message = preProcessedMessage.message;
                var dbOps = preProcessedMessage.dbOps;
                // Perform the state diff, this has to be done on the main thread because we have to touch
                // the client cache.
                if (message.TypeCase == Message.TypeOneofCase.SubscriptionUpdate)
                {
                    foreach (var table in clientDB.GetTables())
                    {
                        if (!preProcessedMessage.inserts.TryGetValue(table.ClientTableType, out var hashSet))
                        {
                            continue;
                        }

                        foreach (var (rowBytes, oldValue) in table.Where(kv => !hashSet.Contains(kv.Key)))
                        {
                            // This is a row that we had before, but we do not have it now.
                            // This must have been a delete.
                            dbOps.Add(new DbOp
                            {
                                table = table,
                                op = TableOp.Delete,
                                newValue = null,
                                oldValue = oldValue,
                                deletedBytes = rowBytes,
                                insertedBytes = null,
                                primaryKeyValue = null
                            });
                        }
                    }
                }

                return (message, dbOps);
            }
        }

        public void Close()
        {
            isClosing = true;
            connectionClosed = true;
            webSocket.Close();
            _preProcessCancellationTokenSource.Cancel();
            _stateDiffCancellationTokenSource.Cancel();

            webSocket = null;
        }

        /// <summary>
        /// Connect to a remote spacetime instance.
        /// </summary>
        /// <param name="uri"> URI of the SpacetimeDB server (ex: https://testnet.spacetimedb.com)
        /// <param name="addressOrName">The name or address of the database to connect to</param>
        public void Connect(string token, string uri, string addressOrName)
        {
            isClosing = false;

            uri = uri.Replace("http://", "ws://");
            uri = uri.Replace("https://", "wss://");
            if (!uri.StartsWith("ws://") && !uri.StartsWith("wss://"))
            {
                uri = $"ws://{uri}";
            }

            logger.Log($"SpacetimeDBClient: Connecting to {uri} {addressOrName}");
            Task.Run(async () =>
            {
                try
                {
                    await webSocket.Connect(token, uri, addressOrName, clientAddress);
                }
                catch (Exception e)
                {
                    if (connectionClosed)
                    {
                        logger.Log("Connection closed gracefully.");
                        return;
                    }

                    logger.LogException(e);
                }
            });
        }


        private void OnMessageProcessCompleteUpdate(List<DbOp> dbOps, Event transactionEvent)
        {
            // First trigger OnBeforeDelete
            foreach (var update in dbOps)
            {
                if (update.op == TableOp.Delete)
                {
                    try
                    {
                        update.oldValue.OnBeforeDeleteEvent(transactionEvent);
                    }
                    catch (Exception e)
                    {
                        logger.LogException(e);
                    }
                }
            }

            void InternalDeleteCallback(DbOp op)
            {
                if (op.oldValue != null)
                {
                    op.oldValue.InternalOnValueDeleted();
                }
                else
                {
                    logger.LogError("Delete issued, but no value was present!");
                }
            }

            void InternalInsertCallback(DbOp op)
            {
                if (op.newValue != null)
                {
                    op.newValue.InternalOnValueInserted();
                }
                else
                {
                    logger.LogError("Insert issued, but no value was present!");
                }
            }

            // Apply all of the state
            for (var i = 0; i < dbOps.Count; i++)
            {
                // TODO: Reimplement updates when we add support for primary keys
                var update = dbOps[i];
                switch (update.op)
                {
                    case TableOp.Delete:
                        if (dbOps[i].table.DeleteEntry(update.deletedBytes))
                        {
                            InternalDeleteCallback(update);
                        }
                        else
                        {
                            var op = dbOps[i];
                            op.op = TableOp.NoChange;
                            dbOps[i] = op;
                        }
                        break;
                    case TableOp.Insert:
                        if (dbOps[i].table.InsertEntry(update.insertedBytes, update.newValue))
                        {
                            InternalInsertCallback(update);
                        }
                        else
                        {
                            var op = dbOps[i];
                            op.op = TableOp.NoChange;
                            dbOps[i] = op;
                        }
                        break;
                    case TableOp.Update:
                        if (dbOps[i].table.DeleteEntry(update.deletedBytes))
                        {
                            InternalDeleteCallback(update);
                        }
                        else
                        {
                            var op = dbOps[i];
                            op.op = TableOp.NoChange;
                            dbOps[i] = op;
                        }

                        if (dbOps[i].table.InsertEntry(update.insertedBytes, update.newValue))
                        {
                            InternalInsertCallback(update);
                        }
                        else
                        {
                            var op = dbOps[i];
                            op.op = TableOp.NoChange;
                            dbOps[i] = op;
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // Send out events
            var updateCount = dbOps.Count;
            for (var i = 0; i < updateCount; i++)
            {
                var tableOp = dbOps[i].op;
                var oldValue = dbOps[i].oldValue;
                var newValue = dbOps[i].newValue;

                switch (tableOp)
                {
                    case TableOp.Insert:
                        if (oldValue == null && newValue != null)
                        {
                            try
                            {
                                newValue.OnInsertEvent(transactionEvent);
                            }
                            catch (Exception e)
                            {
                                logger.LogException(e);
                            }
                        }
                        else
                        {
                            logger.LogError("Failed to send callback: invalid insert!");
                        }

                        break;
                    case TableOp.Delete:
                        {
                            if (oldValue != null && newValue == null)
                            {
                                try
                                {
                                    oldValue.OnDeleteEvent(transactionEvent);
                                }
                                catch (Exception e)
                                {
                                    logger.LogException(e);
                                }
                            }
                            else
                            {
                                logger.LogError("Failed to send callback: invalid delete");
                            }

                            break;
                        }
                    case TableOp.Update:
                        {
                            if (oldValue != null && newValue != null)
                            {
                                var oldValue_ = (IDatabaseTableWithPrimaryKey)oldValue;
                                var newValue_ = (IDatabaseTableWithPrimaryKey)newValue;

                                try
                                {
                                    oldValue_.OnUpdateEvent(newValue_, transactionEvent);
                                }
                                catch (Exception e)
                                {
                                    logger.LogException(e);
                                }
                            }
                            else
                            {
                                logger.LogError("Failed to send callback: invalid update");
                            }

                            break;
                        }
                    case TableOp.NoChange:
                        // noop
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void OnMessageProcessComplete(Message message, List<DbOp> dbOps)
        {
            switch (message)
            {
                case { TypeCase: Message.TypeOneofCase.SubscriptionUpdate }:
                    onBeforeSubscriptionApplied?.Invoke();
                    OnMessageProcessCompleteUpdate(dbOps, null);
                    try
                    {
                        onSubscriptionApplied?.Invoke();
                    }
                    catch (Exception e)
                    {
                        logger.LogException(e);
                    }
                    break;
                case { TypeCase: Message.TypeOneofCase.TransactionUpdate, TransactionUpdate: { Event: var transactionEvent } }:
                    OnMessageProcessCompleteUpdate(dbOps, transactionEvent);
                    try
                    {
                        onEvent?.Invoke(transactionEvent);
                    }
                    catch (Exception e)
                    {
                        logger.LogException(e);
                    }

                    bool reducerFound = false;
                    try
                    {
                        reducerFound = transactionEvent.FunctionCall.CallInfo.InvokeHandler();
                    }
                    catch (Exception e)
                    {
                        logger.LogException(e);
                    }

                    if (!reducerFound && transactionEvent.Status == Event.Types.Status.Failed)
                    {
                        try
                        {
                            onUnhandledReducerError?.Invoke(transactionEvent.FunctionCall
                                .CallInfo);
                        }
                        catch (Exception e)
                        {
                            logger.LogException(e);
                        }
                    }
                    break;
                case { TypeCase: Message.TypeOneofCase.IdentityToken, IdentityToken: var identityToken }:
                    try
                    {
                        onIdentityReceived?.Invoke(identityToken.Token,
                            Identity.From(identityToken.Identity.ToByteArray()),
                            (Address)Address.From(identityToken.Address.ToByteArray()));
                    }
                    catch (Exception e)
                    {
                        logger.LogException(e);
                    }
                    break;
                case { TypeCase: Message.TypeOneofCase.Event, Event: var event_ }:
                    try
                    {
                        onEvent?.Invoke(event_);
                    }
                    catch (Exception e)
                    {
                        logger.LogException(e);
                    }

                    break;
            }
        }

        private void OnMessageReceived(byte[] bytes) => _messageQueue.Add(bytes);

        public void InternalCallReducer<T>(T args)
            where T : IReducerArgsBase, new()
        {
            if (!webSocket.IsConnected)
            {
                logger.LogError("Cannot call reducer, not connected to server!");
                return;
            }

            var requestId = stats.ReducerRequestTracker.StartTrackingRequest();

            webSocket.Send(new Message
            {
                FunctionCall = new FunctionCall
                {
                    Reducer = args.ReducerName,
                    ArgBytes = args.ToProtoBytes(),
                    RequestId = requestId,
                }
            });
        }

        public void Subscribe(List<string> queries)
        {
            if (!webSocket.IsConnected)
            {
                logger.LogError("Cannot subscribe, not connected to server!");
                return;
            }

            var request = new ClientApi.Subscribe();
            request.QueryStrings.AddRange(queries);
            request.RequestId = stats.SubscriptionRequestTracker.StartTrackingRequest();
            webSocket.Send(new Message { Subscribe = request, });
        }

        /// Usage: SpacetimeDBClient.instance.OneOffQuery<Message>("WHERE sender = \"bob\"");
        public async Task<T[]> OneOffQuery<T>(string query)
            where T : IDatabaseTable, IStructuralReadWrite, new()
        {
            var requestId = stats.OneOffQueryRequestTracker.StartTrackingRequest();
            var messageId = Guid.NewGuid();
            Type type = typeof(T);
            Channel<OneOffQueryResponse> resultChannel = Channel.CreateBounded<OneOffQueryResponse>(1);
            waitingOneOffQueries[messageId] = resultChannel;

            // unsanitized here, but writes will be prevented serverside.
            // the best they can do is send multiple selects, which will just result in them getting no data back.
            string queryString = "SELECT * FROM " + type.Name + " " + query;

            var serializedQuery = new ClientApi.OneOffQuery
            {
                MessageId = UnsafeByteOperations.UnsafeWrap(messageId.ToByteArray()),
                QueryString = queryString,
            };
            webSocket.Send(new Message { OneOffQuery = serializedQuery });

            // Suspend for an arbitrary amount of time
            var result = await resultChannel.Reader.ReadAsync();
            stats.OneOffQueryRequestTracker.FinishTrackingRequest(requestId);

            T[] LogAndThrow(string error)
            {
                error = "While processing one-off-query `" + queryString + "`, ID " + messageId + ": " + error;
                logger.LogError(error);
                throw new Exception(error);
            }

            // The server got back to us
            if (result.Error != null && result.Error != "")
            {
                return LogAndThrow("Server error: " + result.Error);
            }

            if (result.Tables.Count != 1)
            {
                return LogAndThrow("Expected a single table, but got " + result.Tables.Count);
            }

            var resultTable = result.Tables[0];
            var cacheTable = clientDB.GetTable(resultTable.TableName);

            if (cacheTable.ClientTableType != type)
            {
                return LogAndThrow("Mismatched result type, expected " + type + " but got " + resultTable.TableName);
            }

            return resultTable.Row.Select(row => BSATNHelpers.FromProtoBytes<T>(row)).ToArray();
        }

        public bool IsConnected() => webSocket != null && webSocket.IsConnected;

        public void Update()
        {
            webSocket.Update();
            while (_stateDiffMessages.TryTake(out var stateDiffMessage))
            {
                OnMessageProcessComplete(stateDiffMessage.message, stateDiffMessage.dbOps);
            }
        }
    }
}
