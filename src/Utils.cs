using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SpacetimeDB
{
    // Helpful utilities from .NET that don't exist in .NET standard.
    internal static class NetExtensions
    {
        public static T CreateDelegate<T>(this System.Reflection.MethodInfo methodInfo) where T : Delegate
        {
            return (T)methodInfo.CreateDelegate(typeof(T));
        }

        public static void AddBytes(this HashCode hashCode, ReadOnlySpan<byte> value)
        {
            foreach (var b in value)
            {
                hashCode.Add(b);
            }
        }

        public static class Convert
        {
            public static string ToHexString(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", "");
        }

        public static class Random
        {
            public static System.Random Shared = new();
        }
    }

    public readonly struct ByteArrayComparer : IEqualityComparer<byte[]>
    {
        public static readonly ByteArrayComparer Instance = new();

        public bool Equals(byte[]? left, byte[]? right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left is null || right is null || left.Length != right.Length)
            {
                return false;
            }

            return EqualsUnvectorized(left, right);

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool EqualsUnvectorized(byte[] left, byte[] right)
        {
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(byte[] obj)
        {
            var hash = new HashCode();
            hash.AddBytes(obj);
            return hash.ToHashCode();
        }
    }
}
