using System;
using System.IO;
using System.Linq;
using SpacetimeDB.BSATN;

namespace SpacetimeDB
{
    public partial struct Address : IEquatable<Address>
    {
        public const int SIZE = 16;

        public byte[] Bytes;

        public static Address? From(byte[] bytes)
        {
            if (bytes.All(b => b == 0))
            {
                return null;
            }
            return new Address { Bytes = bytes };
        }

        public bool Equals(Address other) => ByteArrayComparer.Instance.Equals(Bytes, other.Bytes);

        public override bool Equals(object? o) => o is Address other && Equals(other);

        public static bool operator ==(Address a, Address b) => a.Equals(b);

        public static bool operator !=(Address a, Address b) => !a.Equals(b);

        public static Address Random()
        {
            var bytes = new byte[16];
            NetExtensions.Random.Shared.NextBytes(bytes);
            return new Address { Bytes = bytes };
        }

        public override int GetHashCode() => ByteArrayComparer.Instance.GetHashCode(Bytes);

        public override string ToString() => NetExtensions.Convert.ToHexString(Bytes);

        public readonly struct BSATN : IReadWrite<Address>
        {
            public Address Read(BinaryReader reader) =>
                new() { Bytes = ByteArray.Instance.Read(reader) };

            public void Write(BinaryWriter writer, Address value) =>
                ByteArray.Instance.Write(writer, value.Bytes);
        }
    }
}
