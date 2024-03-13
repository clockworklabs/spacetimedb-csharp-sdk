using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#if UNITY_5_3_OR_NEWER
// This attribute is recognised by C# compilers either way but doesn't exist in Unity runtime.
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ModuleInitializerAttribute : Attribute { }
}
#endif

namespace SpacetimeDB
{
    // Helpful utilities from .NET that don't exist in .NET standard.
    internal static class NetExtensions
    {
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
            int hash = 17;
            foreach (byte b in obj)
            {
                hash = hash * 31 + b;
            }
            return hash;
        }
    }
}
