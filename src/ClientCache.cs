using System;
using System.Collections;
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
            public static readonly Dictionary<byte[], T> Entries = new (new ByteArrayComparer());
        }

        public interface ITableCache : IEnumerable<KeyValuePair<byte[], IDatabaseTable>>
        {
            Type ClientTableType { get; }
            bool InsertEntry(byte[] rowBytes, IDatabaseTable value);
            bool DeleteEntry(byte[] rowBytes);
            IDatabaseTable SetAndForgetDecodedValue(ByteString bytes);
        }

        public class TableCache<T> : ITableCache
            where T: IDatabaseTable, IStructuralReadWrite, new()
        {
            public Type ClientTableType => typeof(T);

            /// <summary>
            /// Inserts the value into the table. There can be no existing value with the provided BSATN bytes.
            /// </summary>
            /// <param name="rowBytes">The BSATN encoded bytes of the row to retrieve.</param>
            /// <param name="value">The parsed AlgebraicValue of the row encoded by the <paramref>rowBytes</paramref>.</param>
            /// <returns>True if the row was inserted, false if the row wasn't inserted because it was a duplicate.</returns>
            public bool InsertEntry(byte[] rowBytes, IDatabaseTable value) => TableEntries<T>.Entries.TryAdd(rowBytes, (T)value);

            /// <summary>
            /// Deletes a value from the table.
            /// </summary>
            /// <param name="rowBytes">The BSATN encoded bytes of the row to remove.</param>
            /// <returns>True if and only if the value was previously resident and has been deleted.</returns>
            public bool DeleteEntry(byte[] rowBytes)
            {
                if (TableEntries<T>.Entries.Remove(rowBytes))
                {
                    return true;
                }

                SpacetimeDBClient.instance.Logger.LogWarning("Deleting value that we don't have (no cached value available)");
                return false;
            }

            // The function to use for decoding a type value.
            public IDatabaseTable SetAndForgetDecodedValue(ByteString bytes) => BSATNHelpers.FromProtoBytes<T>(bytes);

            public IEnumerator<KeyValuePair<byte[], IDatabaseTable>> GetEnumerator() => TableEntries<T>.Entries.Select(kv => new KeyValuePair<byte[], IDatabaseTable>(kv.Key, kv.Value)).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private readonly Dictionary<string, ITableCache> tables = new();

        public void AddTable<T>()
            where T: IDatabaseTable, IStructuralReadWrite, new()
        {
            string name = typeof(T).Name;

            if (!tables.TryAdd(name, new TableCache<T>()))
            {
                SpacetimeDBClient.instance.Logger.LogError($"Table with name already exists: {name}");
            }
        }

        public ITableCache? GetTable(string name)
        {
            if (tables.TryGetValue(name, out var table))
            {
                return table;
            }

            SpacetimeDBClient.instance.Logger.LogError($"We don't know that this table is: {name}");
            return null;
        }

        public IEnumerable<T> GetObjects<T>() where T: IDatabaseTable => TableEntries<T>.Entries.Values;

        public int Count<T>() where T: IDatabaseTable => TableEntries<T>.Entries.Count;

        public IEnumerable<ITableCache> GetTables() => tables.Values;
    }
}
