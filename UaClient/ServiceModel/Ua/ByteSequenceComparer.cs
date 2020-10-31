// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Workstation.ServiceModel.Ua
{
    public sealed class ByteSequenceComparer : IEqualityComparer<byte[]>
    {
        public static readonly ByteSequenceComparer Instance = new ByteSequenceComparer();

        private ByteSequenceComparer()
        {
        }

        public static bool Equals(byte[]? left, byte[]? right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            for (var i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static int GetHashCode(byte[] x)
        {
            // Compute the FNV-1a hash of a sequence of bytes
            // See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
            int hashCode = unchecked((int)2166136261);

            for (int i = 0; i < x.Length; i++)
            {
                hashCode = unchecked((hashCode ^ x[i]) * 16777619);
            }

            return hashCode;
        }

        bool IEqualityComparer<byte[]>.Equals(byte[]? x, byte[]? y)
        {
            return Equals(x, y);
        }

        int IEqualityComparer<byte[]>.GetHashCode(byte[] x)
        {
            return GetHashCode(x);
        }
    }
}
