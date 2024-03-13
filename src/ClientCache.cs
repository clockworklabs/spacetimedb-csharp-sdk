using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SpacetimeDB.BSATN;
using Google.Protobuf;

namespace SpacetimeDB
{
    public class ClientCache
    {
        // (Ab)using generic instantiation for type-based lookup instead of hashmap in static contexts.
        internal static class TableEntries<T>
            where T: IDatabaseTable
        {
            public static readonly Dictionary<byte[], IDatabaseTable> Entries = new (new ByteArrayComparer());
        }

        public class TableCache
        {
            // The function to use for decoding a type value
            public readonly Func<ByteString, IDatabaseTable> SetAndForgetDecodedValue;

            // Maps from primary key to type value
            public readonly Dictionary<byte[], IDatabaseTable> entries;

            public Type ClientTableType { get; }

            public string Name => ClientTableType.Name;

            private TableCache(
                Type clientTableType,
                Func<ByteString, IDatabaseTable> decoderFunc,
                Dictionary<byte[], IDatabaseTable> entries
            )
            {
                ClientTableType = clientTableType;
                SetAndForgetDecodedValue = decoderFunc;
                this.entries = entries;
            }

            public static TableCache Create<T>()
                where T: IDatabaseTable, IStructuralReadWrite, new()
            {
                return new TableCache(
                    clientTableType: typeof(T),
                    decoderFunc: bytes => BSATNHelpers.FromProtoBytes<T>(bytes),
                    entries: TableEntries<T>.Entries
                );
            }

            /// <summary>
            /// Inserts the value into the table. There can be no existing value with the provided BSATN bytes.
            /// </summary>
            /// <param name="rowBytes">The BSATN encoded bytes of the row to retrieve.</param>
            /// <param name="value">The parsed AlgebraicValue of the row encoded by the <paramref>rowBytes</paramref>.</param>
            /// <returns>True if the row was inserted, false if the row wasn't inserted because it was a duplicate.</returns>
            public bool InsertEntry(byte[] rowBytes, IDatabaseTable value) => entries.TryAdd(rowBytes, value);

            /// <summary>
            /// Deletes a value from the table.
            /// </summary>
            /// <param name="rowBytes">The BSATN encoded bytes of the row to remove.</param>
            /// <returns>True if and only if the value was previously resident and has been deleted.</returns>
            public bool DeleteEntry(byte[] rowBytes)
            {
                if (entries.Remove(rowBytes))
                {
                    return true;
                }

                SpacetimeDBClient.instance.Logger.LogWarning("Deleting value that we don't have (no cached value available)");
                return false;
            }
        }

        private readonly Dictionary<string, TableCache> tables = new();

        public void AddTable<T>()
            where T: IDatabaseTable, IStructuralReadWrite, new()
        {
            string name = typeof(T).Name;

            if (!tables.TryAdd(name, TableCache.Create<T>()))
            {
                SpacetimeDBClient.instance.Logger.LogError($"Table with name already exists: {name}");
            }
        }

        public TableCache? GetTable(string name)
        {
            if (tables.TryGetValue(name, out var table))
            {
                return table;
            }

            SpacetimeDBClient.instance.Logger.LogError($"We don't know that this table is: {name}");
            return null;
        }

        public IEnumerable<T> GetObjects<T>() where T: IDatabaseTable => TableEntries<T>.Entries.Values.Cast<T>();

        public int Count<T>() where T: IDatabaseTable => TableEntries<T>.Entries.Count;

        public IEnumerable<string> GetTableNames() => tables.Keys;

        public IEnumerable<TableCache> GetTables() => tables.Values;
    }
}
