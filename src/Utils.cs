﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    public static class IsExternalInit { } // https://stackoverflow.com/a/64749403/1484415
}
#endif

namespace SpacetimeDB
{
    // Note: this utility struct is used by an autogenerated C# code so it needs to be public.
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

        // Same as `Convert.ToHexString`, but that method is not available in .NET Standard
        // which we need to target for Unity support.
        public static string ToHexString(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", "");
    }
}