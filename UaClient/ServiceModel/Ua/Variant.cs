// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace Workstation.ServiceModel.Ua
{
    public enum VariantType
    {
        Null,
        Boolean,
        SByte,
        Byte,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64,
        UInt64,
        Float,
        Double,
        String,
        DateTime,
        Guid,
        ByteString,
        XmlElement,
        NodeId,
        ExpandedNodeId,
        StatusCode,
        QualifiedName,
        LocalizedText,
        ExtensionObject,
        DataValue,
        Variant,
        DiagnosticInfo,
    }

    public struct Variant
    {
        public static Variant Null = default(Variant);

        private static Dictionary<Type, VariantType> typeMap = new Dictionary<Type, VariantType>()
        {
            [typeof(bool)] = VariantType.Boolean,
            [typeof(sbyte)] = VariantType.SByte,
            [typeof(byte)] = VariantType.Byte,
            [typeof(short)] = VariantType.Int16,
            [typeof(ushort)] = VariantType.UInt16,
            [typeof(int)] = VariantType.Int32,
            [typeof(uint)] = VariantType.UInt32,
            [typeof(long)] = VariantType.Int64,
            [typeof(ulong)] = VariantType.UInt64,
            [typeof(float)] = VariantType.Float,
            [typeof(double)] = VariantType.Double,
            [typeof(string)] = VariantType.String,
            [typeof(DateTime)] = VariantType.DateTime,
            [typeof(Guid)] = VariantType.Guid,
            [typeof(byte[])] = VariantType.ByteString,
            [typeof(XElement)] = VariantType.XmlElement,
            [typeof(NodeId)] = VariantType.NodeId,
            [typeof(ExpandedNodeId)] = VariantType.ExpandedNodeId,
            [typeof(StatusCode)] = VariantType.StatusCode,
            [typeof(QualifiedName)] = VariantType.QualifiedName,
            [typeof(LocalizedText)] = VariantType.LocalizedText,
            [typeof(ExtensionObject)] = VariantType.ExtensionObject,
            /*
            [typeof(DataValue)] = VariantType.DataValue,
            [typeof(Variant)] = VariantType.Variant,
            [typeof(DiagnosticInfo)] = VariantType.DiagnosticInfo,
            */
        };

        private static Dictionary<Type, VariantType> elemTypeMap = new Dictionary<Type, VariantType>()
        {
            [typeof(bool)] = VariantType.Boolean,
            [typeof(sbyte)] = VariantType.SByte,
            [typeof(byte)] = VariantType.Byte,
            [typeof(short)] = VariantType.Int16,
            [typeof(ushort)] = VariantType.UInt16,
            [typeof(int)] = VariantType.Int32,
            [typeof(uint)] = VariantType.UInt32,
            [typeof(long)] = VariantType.Int64,
            [typeof(ulong)] = VariantType.UInt64,
            [typeof(float)] = VariantType.Float,
            [typeof(double)] = VariantType.Double,
            [typeof(string)] = VariantType.String,
            [typeof(DateTime)] = VariantType.DateTime,
            [typeof(Guid)] = VariantType.Guid,
            [typeof(byte[])] = VariantType.ByteString,
            [typeof(XElement)] = VariantType.XmlElement,
            [typeof(NodeId)] = VariantType.NodeId,
            [typeof(ExpandedNodeId)] = VariantType.ExpandedNodeId,
            [typeof(StatusCode)] = VariantType.StatusCode,
            [typeof(QualifiedName)] = VariantType.QualifiedName,
            [typeof(LocalizedText)] = VariantType.LocalizedText,
            [typeof(ExtensionObject)] = VariantType.ExtensionObject,
            [typeof(Variant)] = VariantType.Variant,
            /*
            [typeof(DataValue)] = VariantType.DataValue,
            [typeof(DiagnosticInfo)] = VariantType.DiagnosticInfo,
            */
        };

        public Variant(object value)
        {
            Value = value;
            if (value == null)
            {
                Type = VariantType.Null;
                ArrayDimensions = null;
                return;
            }

            VariantType variantType;
            var array = value as Array;
            if (array != null)
            {
                Type elemType = value.GetType().GetElementType();
                if (elemType == null || !elemTypeMap.TryGetValue(elemType, out variantType))
                {
                    throw new ArgumentOutOfRangeException("value", elemType, "Array element Type is unsupported.");
                }

                Type = variantType;
                ArrayDimensions = new int[array.Rank];
                for (int i = 0; i < array.Rank; i++)
                {
                    ArrayDimensions[i] = array.GetLength(i);
                }

                return;
            }

            Type type = value.GetType();
            if (type == null || !elemTypeMap.TryGetValue(type, out variantType))
            {
                throw new ArgumentOutOfRangeException("value", type, "Type is unsupported.");
            }

            Type = variantType;
            ArrayDimensions = null;
        }

        public Variant(bool value)
        {
            Value = value;
            Type = VariantType.Boolean;
            ArrayDimensions = null;
        }

        public Variant(sbyte value)
        {
            Value = value;
            Type = VariantType.SByte;
            ArrayDimensions = null;
        }

        public Variant(byte value)
        {
            Value = value;
            Type = VariantType.Byte;
            ArrayDimensions = null;
        }

        public Variant(short value)
        {
            Value = value;
            Type = VariantType.Int16;
            ArrayDimensions = null;
        }

        public Variant(ushort value)
        {
            Value = value;
            Type = VariantType.UInt16;
            ArrayDimensions = null;
        }

        public Variant(int value)
        {
            Value = value;
            Type = VariantType.Int32;
            ArrayDimensions = null;
        }

        public Variant(uint value)
        {
            Value = value;
            Type = VariantType.UInt32;
            ArrayDimensions = null;
        }

        public Variant(long value)
        {
            Value = value;
            Type = VariantType.Int64;
            ArrayDimensions = null;
        }

        public Variant(ulong value)
        {
            Value = value;
            Type = VariantType.UInt64;
            ArrayDimensions = null;
        }

        public Variant(float value)
        {
            Value = value;
            Type = VariantType.Float;
            ArrayDimensions = null;
        }

        public Variant(double value)
        {
            Value = value;
            Type = VariantType.Double;
            ArrayDimensions = null;
        }

        public Variant(string value)
        {
            Value = value;
            Type = VariantType.String;
            ArrayDimensions = null;
        }

        public Variant(DateTime value)
        {
            Value = value;
            Type = VariantType.DateTime;
            ArrayDimensions = null;
        }

        public Variant(Guid value)
        {
            Value = value;
            Type = VariantType.Guid;
            ArrayDimensions = null;
        }

        public Variant(byte[] value)
        {
            Value = value;
            Type = VariantType.ByteString;
            ArrayDimensions = null;
        }

        public Variant(XElement value)
        {
            Value = value;
            Type = VariantType.XmlElement;
            ArrayDimensions = null;
        }

        public Variant(NodeId value)
        {
            Value = value;
            Type = VariantType.NodeId;
            ArrayDimensions = null;
        }

        public Variant(ExpandedNodeId value)
        {
            Value = value;
            Type = VariantType.ExpandedNodeId;
            ArrayDimensions = null;
        }

        public Variant(StatusCode value)
        {
            Value = value;
            Type = VariantType.StatusCode;
            ArrayDimensions = null;
        }

        public Variant(QualifiedName value)
        {
            Value = value;
            Type = VariantType.QualifiedName;
            ArrayDimensions = null;
        }

        public Variant(LocalizedText value)
        {
            Value = value;
            Type = VariantType.LocalizedText;
            ArrayDimensions = null;
        }

        public Variant(ExtensionObject value)
        {
            Value = value;
            Type = VariantType.ExtensionObject;
            ArrayDimensions = null;
        }

        public Variant(Enum value)
        {
            Value = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            Type = VariantType.Int32;
            ArrayDimensions = null;
        }

        public Variant(bool[] value)
        {
            Value = value;
            Type = VariantType.Boolean;
            ArrayDimensions = new int[value.Rank];
            for (int i = 0; i < value.Rank; i++)
            {
                ArrayDimensions[i] = value.GetLength(i);
            }
        }

        public Variant(sbyte[] value)
        {
            Value = value;
            Type = VariantType.SByte;
            ArrayDimensions = new int[value.Rank];
            for (int i = 0; i < value.Rank; i++)
            {
                ArrayDimensions[i] = value.GetLength(i);
            }
        }

        public Variant(short[] value)
        {
            Value = value;
            Type = VariantType.Int16;
            ArrayDimensions = new int[value.Rank];
            for (int i = 0; i < value.Rank; i++)
            {
                ArrayDimensions[i] = value.GetLength(i);
            }
        }

        public Variant(ushort[] value)
        {
            Value = value;
            Type = VariantType.UInt16;
            ArrayDimensions = new int[value.Rank];
            for (int i = 0; i < value.Rank; i++)
            {
                ArrayDimensions[i] = value.GetLength(i);
            }
        }

        public Variant(int[] value)
        {
            Value = value;
            Type = VariantType.Int32;
            ArrayDimensions = new int[value.Rank];
            for (int i = 0; i < value.Rank; i++)
            {
                ArrayDimensions[i] = value.GetLength(i);
            }
        }

        public Variant(uint[] value)
        {
            Value = value;
            Type = VariantType.UInt32;
            ArrayDimensions = new int[value.Rank];
            for (int i = 0; i < value.Rank; i++)
            {
                ArrayDimensions[i] = value.GetLength(i);
            }
        }

        public Variant(long[] value)
        {
            Value = value;
            Type = VariantType.Int64;
            ArrayDimensions = new int[value.Rank];
            for (int i = 0; i < value.Rank; i++)
            {
                ArrayDimensions[i] = value.GetLength(i);
            }
        }

        public Variant(ulong[] value)
        {
            Value = value;
            Type = VariantType.UInt64;
            ArrayDimensions = new int[value.Rank];
            for (int i = 0; i < value.Rank; i++)
            {
                ArrayDimensions[i] = value.GetLength(i);
            }
        }

        public Variant(float[] value)
        {
            Value = value;
            Type = VariantType.Float;
            ArrayDimensions = new int[value.Rank];
            for (int i = 0; i < value.Rank; i++)
            {
                ArrayDimensions[i] = value.GetLength(i);
            }
        }

        public Variant(double[] value)
        {
            Value = value;
            Type = VariantType.Double;
            ArrayDimensions = new int[value.Rank];
            for (int i = 0; i < value.Rank; i++)
            {
                ArrayDimensions[i] = value.GetLength(i);
            }
        }

        public Variant(string[] value)
        {
            Value = value;
            Type = VariantType.String;
            ArrayDimensions = new int[value.Rank];
            for (int i = 0; i < value.Rank; i++)
            {
                ArrayDimensions[i] = value.GetLength(i);
            }
        }

        public Variant(DateTime[] value)
        {
            Value = value;
            Type = VariantType.DateTime;
            ArrayDimensions = new int[value.Rank];
            for (int i = 0; i < value.Rank; i++)
            {
                ArrayDimensions[i] = value.GetLength(i);
            }
        }

        public Variant(Guid[] value)
        {
            Value = value;
            Type = VariantType.Guid;
            ArrayDimensions = new int[value.Rank];
            for (int i = 0; i < value.Rank; i++)
            {
                ArrayDimensions[i] = value.GetLength(i);
            }
        }

        public Variant(byte[][] value)
        {
            Value = value;
            Type = VariantType.ByteString;
            ArrayDimensions = new int[value.Rank];
            for (int i = 0; i < value.Rank; i++)
            {
                ArrayDimensions[i] = value.GetLength(i);
            }
        }

        public Variant(XElement[] value)
        {
            Value = value;
            Type = VariantType.XmlElement;
            ArrayDimensions = new int[value.Rank];
            for (int i = 0; i < value.Rank; i++)
            {
                ArrayDimensions[i] = value.GetLength(i);
            }
        }

        public Variant(NodeId[] value)
        {
            Value = value;
            Type = VariantType.NodeId;
            ArrayDimensions = new int[value.Rank];
            for (int i = 0; i < value.Rank; i++)
            {
                ArrayDimensions[i] = value.GetLength(i);
            }
        }

        public Variant(ExpandedNodeId[] value)
        {
            Value = value;
            Type = VariantType.ExpandedNodeId;
            ArrayDimensions = new int[value.Rank];
            for (int i = 0; i < value.Rank; i++)
            {
                ArrayDimensions[i] = value.GetLength(i);
            }
        }

        public Variant(StatusCode[] value)
        {
            Value = value;
            Type = VariantType.StatusCode;
            ArrayDimensions = new int[value.Rank];
            for (int i = 0; i < value.Rank; i++)
            {
                ArrayDimensions[i] = value.GetLength(i);
            }
        }

        public Variant(QualifiedName[] value)
        {
            Value = value;
            Type = VariantType.QualifiedName;
            ArrayDimensions = new int[value.Rank];
            for (int i = 0; i < value.Rank; i++)
            {
                ArrayDimensions[i] = value.GetLength(i);
            }
        }

        public Variant(LocalizedText[] value)
        {
            Value = value;
            Type = VariantType.LocalizedText;
            ArrayDimensions = new int[value.Rank];
            for (int i = 0; i < value.Rank; i++)
            {
                ArrayDimensions[i] = value.GetLength(i);
            }
        }

        public Variant(ExtensionObject[] value)
        {
            Value = value;
            Type = VariantType.ExtensionObject;
            ArrayDimensions = new int[value.Rank];
            for (int i = 0; i < value.Rank; i++)
            {
                ArrayDimensions[i] = value.GetLength(i);
            }
        }

        public Variant(Variant[] value)
        {
            Value = value;
            Type = VariantType.Variant;
            ArrayDimensions = new int[value.Rank];
            for (int i = 0; i < value.Rank; i++)
            {
                ArrayDimensions[i] = value.GetLength(i);
            }
        }

        public Variant(Enum[] value)
        {
            Value = value.Select(v => Convert.ToInt32(v, CultureInfo.InvariantCulture)).ToArray();
            Type = VariantType.Int32;
            ArrayDimensions = new int[value.Rank];
            for (int i = 0; i < value.Rank; i++)
            {
                ArrayDimensions[i] = value.GetLength(i);
            }
        }

        public Variant(Array value)
        {
            Value = value;
            VariantType varType;
            Type elemType = value.GetType().GetElementType();
            if (elemType == null || !elemTypeMap.TryGetValue(elemType, out varType))
            {
                throw new ArgumentOutOfRangeException("value", elemType, "Array element type is unsupported.");
            }

            Type = varType;
            ArrayDimensions = new int[value.Rank];
            for (int i = 0; i < value.Rank; i++)
            {
                ArrayDimensions[i] = value.GetLength(i);
            }
        }

        public object Value { get; }

        public VariantType Type { get; }

        public int[] ArrayDimensions { get; }

        public static implicit operator Variant(bool value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(sbyte value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(byte value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(short value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(ushort value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(int value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(uint value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(long value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(ulong value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(float value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(double value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(string value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(DateTime value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(Guid value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(byte[] value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(XElement value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(NodeId value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(ExpandedNodeId value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(StatusCode value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(QualifiedName value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(LocalizedText value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(ExtensionObject value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(bool[] value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(sbyte[] value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(short[] value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(ushort[] value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(int[] value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(uint[] value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(long[] value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(ulong[] value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(float[] value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(double[] value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(string[] value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(DateTime[] value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(Guid[] value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(byte[][] value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(XElement[] value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(NodeId[] value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(ExpandedNodeId[] value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(StatusCode[] value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(QualifiedName[] value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(LocalizedText[] value)
        {
            return new Variant(value);
        }

        public static implicit operator Variant(ExtensionObject[] value)
        {
            return new Variant(value);
        }

        public static explicit operator bool(Variant value)
        {
            return (bool)value.Value;
        }

        public static explicit operator sbyte(Variant value)
        {
            return (sbyte)value.Value;
        }

        public static explicit operator byte(Variant value)
        {
            return (byte)value.Value;
        }

        public static explicit operator short(Variant value)
        {
            return (short)value.Value;
        }

        public static explicit operator ushort(Variant value)
        {
            return (ushort)value.Value;
        }

        public static explicit operator int(Variant value)
        {
            return (int)value.Value;
        }

        public static explicit operator uint(Variant value)
        {
            return (uint)value.Value;
        }

        public static explicit operator long(Variant value)
        {
            return (long)value.Value;
        }

        public static explicit operator ulong(Variant value)
        {
            return (ulong)value.Value;
        }

        public static explicit operator float(Variant value)
        {
            return (float)value.Value;
        }

        public static explicit operator double(Variant value)
        {
            return (double)value.Value;
        }

        public static explicit operator string(Variant value)
        {
            return (string)value.Value;
        }

        public static explicit operator DateTime(Variant value)
        {
            return (DateTime)value.Value;
        }

        public static explicit operator Guid(Variant value)
        {
            return (Guid)value.Value;
        }

        public static explicit operator byte[](Variant value)
        {
            return (byte[])value.Value;
        }

        public static explicit operator XElement(Variant value)
        {
            return (XElement)value.Value;
        }

        public static explicit operator NodeId(Variant value)
        {
            return (NodeId)value.Value;
        }

        public static explicit operator ExpandedNodeId(Variant value)
        {
            return (ExpandedNodeId)value.Value;
        }

        public static explicit operator StatusCode(Variant value)
        {
            return (StatusCode)value.Value;
        }

        public static explicit operator QualifiedName(Variant value)
        {
            return (QualifiedName)value.Value;
        }

        public static explicit operator LocalizedText(Variant value)
        {
            return (LocalizedText)value.Value;
        }

        public static explicit operator ExtensionObject(Variant value)
        {
            return (ExtensionObject)value.Value;
        }

        public static explicit operator bool[](Variant value)
        {
            return (bool[])value.Value;
        }

        public static explicit operator sbyte[](Variant value)
        {
            return (sbyte[])value.Value;
        }

        public static explicit operator short[](Variant value)
        {
            return (short[])value.Value;
        }

        public static explicit operator ushort[](Variant value)
        {
            return (ushort[])value.Value;
        }

        public static explicit operator int[](Variant value)
        {
            return (int[])value.Value;
        }

        public static explicit operator uint[](Variant value)
        {
            return (uint[])value.Value;
        }

        public static explicit operator long[](Variant value)
        {
            return (long[])value.Value;
        }

        public static explicit operator ulong[](Variant value)
        {
            return (ulong[])value.Value;
        }

        public static explicit operator float[](Variant value)
        {
            return (float[])value.Value;
        }

        public static explicit operator double[](Variant value)
        {
            return (double[])value.Value;
        }

        public static explicit operator string[](Variant value)
        {
            return (string[])value.Value;
        }

        public static explicit operator DateTime[](Variant value)
        {
            return (DateTime[])value.Value;
        }

        public static explicit operator Guid[](Variant value)
        {
            return (Guid[])value.Value;
        }

        public static explicit operator byte[][](Variant value)
        {
            return (byte[][])value.Value;
        }

        public static explicit operator XElement[](Variant value)
        {
            return (XElement[])value.Value;
        }

        public static explicit operator NodeId[](Variant value)
        {
            return (NodeId[])value.Value;
        }

        public static explicit operator ExpandedNodeId[](Variant value)
        {
            return (ExpandedNodeId[])value.Value;
        }

        public static explicit operator StatusCode[](Variant value)
        {
            return (StatusCode[])value.Value;
        }

        public static explicit operator QualifiedName[](Variant value)
        {
            return (QualifiedName[])value.Value;
        }

        public static explicit operator LocalizedText[](Variant value)
        {
            return (LocalizedText[])value.Value;
        }

        public static explicit operator ExtensionObject[](Variant value)
        {
            return (ExtensionObject[])value.Value;
        }

        public static bool IsNull(Variant a)
        {
            return (a.Type == VariantType.Null) || (a.Value == null);
        }

        public override string ToString()
        {
            return Value?.ToString() ?? "{null}";
        }
    }
}