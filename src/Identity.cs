using System;
using System.IO;
using SpacetimeDB.BSATN;

namespace SpacetimeDB
{
    public partial struct Identity : IEquatable<Identity>
    {
        public const int SIZE = 32;

        public byte[] Bytes;

        public static Identity From(byte[] bytes) =>
            // TODO: should we validate length here?
            new Identity { Bytes = bytes, };

        public bool Equals(Identity other) => ByteArrayComparer.Instance.Equals(Bytes, other.Bytes);

        public override bool Equals(object? o) => o is Identity other && Equals(other);

        public static bool operator ==(Identity a, Identity b) => a.Equals(b);

        public static bool operator !=(Identity a, Identity b) => !a.Equals(b);

        public override int GetHashCode() => ByteArrayComparer.Instance.GetHashCode(Bytes);

        public override string ToString() => NetExtensions.Convert.ToHexString(Bytes);

        public readonly struct BSATN : IReadWrite<Identity>
        {
            public Identity Read(BinaryReader reader) => From(ByteArray.Instance.Read(reader));
            public void Write(BinaryWriter writer, Identity value) => ByteArray.Instance.Write(writer, value.Bytes);
        }
    }
}
