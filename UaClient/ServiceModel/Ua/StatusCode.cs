// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Workstation.ServiceModel.Ua
{
    public readonly struct StatusCode : IEquatable<StatusCode>
    {
        private const uint _severityMask = 0xC0000000u;
        private const uint _severityGood = 0x00000000u;
        private const uint _severityUncertain = 0x40000000u;
        private const uint _severityBad = 0x80000000u;
        private const uint _subCodeMask = 0x0FFF0000u;
        private const uint _structureChanged = 0x00008000u;
        private const uint _semanticsChanged = 0x00004000u;
        private const uint _infoTypeMask = 0x00000C00u;
        private const uint _infoTypeDataValue = 0x00000400u;
        private const uint _infoBitsMask = 0x000003FFu;
        private const uint _limitBitsMask = 0x00000300u;
        private const uint _limitBitsNone = 0x00000000u;
        private const uint _limitBitsLow = 0x00000100u;
        private const uint _limitBitsHigh = 0x00000200u;
        private const uint _limitBitsConstant = 0x00000300u;
        private const uint _overflow = 0x0080u;

        public StatusCode(uint value)
        {
            Value = value;
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
            return (a.Value & _severityMask) == _severityGood;
        }

        public static bool IsBad(StatusCode a)
        {
            return (a.Value & _severityMask) == _severityBad;
        }

        public static bool IsUncertain(StatusCode a)
        {
            return (a.Value & _severityMask) == _severityUncertain;
        }

        public static bool IsStructureChanged(StatusCode a)
        {
            return (a.Value & _structureChanged) == _structureChanged;
        }

        public static bool IsSemanticsChanged(StatusCode a)
        {
            return (a.Value & _semanticsChanged) == _semanticsChanged;
        }

        public static bool IsOverflow(StatusCode a)
        {
            return ((a.Value & _infoTypeMask) == _infoTypeDataValue) && ((a.Value & _overflow) == _overflow);
        }

        public override string ToString()
        {
            return $"0x{Value:X8}";
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