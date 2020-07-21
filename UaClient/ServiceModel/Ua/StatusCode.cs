// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Workstation.ServiceModel.Ua
{
    public readonly struct StatusCode : IEquatable<StatusCode>
    {
        private const uint SeverityMask = 0xC0000000u;
        private const uint SeverityGood = 0x00000000u;
        private const uint SeverityUncertain = 0x40000000u;
        private const uint SeverityBad = 0x80000000u;
        private const uint SubCodeMask = 0x0FFF0000u;
        private const uint StructureChanged = 0x00008000u;
        private const uint SemanticsChanged = 0x00004000u;
        private const uint InfoTypeMask = 0x00000C00u;
        private const uint InfoTypeDataValue = 0x00000400u;
        private const uint InfoBitsMask = 0x000003FFu;
        private const uint LimitBitsMask = 0x00000300u;
        private const uint LimitBitsNone = 0x00000000u;
        private const uint LimitBitsLow = 0x00000100u;
        private const uint LimitBitsHigh = 0x00000200u;
        private const uint LimitBitsConstant = 0x00000300u;
        private const uint Overflow = 0x0080u;

        public StatusCode(uint value)
        {
            this.Value = value;
        }

        public uint Value { get; }

        public static implicit operator StatusCode(uint a)
        {
            return new StatusCode(a);
        }

        public static implicit operator uint(StatusCode a)
        {
            return a.Value;
        }

        public static bool operator ==(StatusCode left, StatusCode right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StatusCode left, StatusCode right)
        {
            return !(left == right);
        }

        public static bool IsGood(StatusCode a)
        {
            return (a.Value & SeverityMask) == SeverityGood;
        }

        public static bool IsBad(StatusCode a)
        {
            return (a.Value & SeverityMask) == SeverityBad;
        }

        public static bool IsUncertain(StatusCode a)
        {
            return (a.Value & SeverityMask) == SeverityUncertain;
        }

        public static bool IsStructureChanged(StatusCode a)
        {
            return (a.Value & StructureChanged) == StructureChanged;
        }

        public static bool IsSemanticsChanged(StatusCode a)
        {
            return (a.Value & SemanticsChanged) == SemanticsChanged;
        }

        public static bool IsOverflow(StatusCode a)
        {
            return ((a.Value & InfoTypeMask) == InfoTypeDataValue) && ((a.Value & Overflow) == Overflow);
        }

        public override string ToString()
        {
            return $"0x{this.Value:X8}";
        }

        public override bool Equals(object? obj)
        {
            return obj is StatusCode code && Equals(code);
        }

        public bool Equals(StatusCode other)
        {
            return Value == other.Value;
        }

        public override int GetHashCode()
        {
            return -1937169414 + Value.GetHashCode();
        }
    }
}