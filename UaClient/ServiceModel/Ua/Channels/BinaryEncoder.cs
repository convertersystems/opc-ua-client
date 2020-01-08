// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

#nullable enable

namespace Workstation.ServiceModel.Ua.Channels
{
    public sealed class BinaryEncoder : IEncoder, IDisposable
    {
        private const long MinFileTime = 504911232000000000L;
        private readonly Stream stream;
        private readonly UaTcpSecureChannel? channel;
        private readonly Encoding encoding;
        private readonly BinaryWriter writer;

        public BinaryEncoder(Stream stream, UaTcpSecureChannel? channel = null, bool keepStreamOpen = false)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            this.stream = stream;
            this.channel = channel;
            this.encoding = new UTF8Encoding(false, true);
            this.writer = new BinaryWriter(this.stream, this.encoding, keepStreamOpen);
        }

        public int Position
        {
            get { return (int)this.stream.Position; }
            set { this.stream.Position = value; }
        }

        public void Dispose()
        {
            if (this.writer != null)
            {
                this.writer.Dispose();
            }
        }

        public void PushNamespace(string namespaceUri)
        {
        }

        public void PopNamespace()
        {
        }

        public void WriteBoolean(string? fieldName, bool value)
        {
            this.writer.Write(value);
        }

        public void WriteSByte(string? fieldName, sbyte value)
        {
            this.writer.Write(value);
        }

        public void WriteByte(string? fieldName, byte value)
        {
            this.writer.Write(value);
        }

        public void WriteInt16(string? fieldName, short value)
        {
            this.writer.Write(value);
        }

        public void WriteUInt16(string? fieldName, ushort value)
        {
            this.writer.Write(value);
        }

        public void WriteInt32(string? fieldName, int value)
        {
            this.writer.Write(value);
        }

        public void WriteUInt32(string? fieldName, uint value)
        {
            this.writer.Write(value);
        }

        public void WriteInt64(string? fieldName, long value)
        {
            this.writer.Write(value);
        }

        public void WriteUInt64(string? fieldName, ulong value)
        {
            this.writer.Write(value);
        }

        public void WriteFloat(string? fieldName, float value)
        {
            this.writer.Write(value);
        }

        public void WriteDouble(string? fieldName, double value)
        {
            this.writer.Write(value);
        }

        public void WriteString(string? fieldName, string? value)
        {
            if (value == null)
            {
                this.writer.Write(-1);
                return;
            }

            this.WriteByteString(null, this.encoding.GetBytes(value));
        }

        public void WriteDateTime(string? fieldName, DateTime value)
        {
            if (value.Kind == DateTimeKind.Local)
            {
                value = value.ToUniversalTime();
            }

            if (value.Ticks < MinFileTime)
            {
                this.writer.Write(0L);
                return;
            }

            this.writer.Write(value.ToFileTimeUtc());
        }

        public void WriteGuid(string? fieldName, Guid value)
        {
            this.writer.Write(value.ToByteArray());
        }

        public void WriteByteString(string? fieldName, byte[]? value)
        {
            if (value == null)
            {
                this.writer.Write(-1);
                return;
            }

            this.writer.Write(value.Length);
            this.writer.Write(value);
        }

        public void WriteXElement(string? fieldName, XElement? value)
        {
            if (value == null)
            {
                this.writer.Write(-1);
                return;
            }

            this.WriteByteString(null, this.encoding.GetBytes(value.ToString()));
        }

        public void WriteNodeId(string? fieldName, NodeId? value)
        {
            if (value == null)
            {
                this.WriteUInt16(null, 0);
                return;
            }

            ushort ns = value.NamespaceIndex;
            switch (value.IdType)
            {
                case IdType.Numeric:
                    var id = (uint)value.Identifier;
                    if (id <= 255u && ns == 0u)
                    {
                        this.WriteByte(null, 0x00);
                        this.WriteByte(null, (byte)id);
                        break;
                    }
                    else if (id <= 65535u && ns <= 255u)
                    {
                        this.WriteByte(null, 0x01);
                        this.WriteByte(null, (byte)ns);
                        this.WriteUInt16(null, (ushort)id);
                        break;
                    }
                    else
                    {
                        this.WriteByte(null, 0x02);
                        this.WriteUInt16(null, ns);
                        this.WriteUInt32(null, id);
                        break;
                    }

                case IdType.String:
                    this.WriteByte(null, 0x03);
                    this.WriteUInt16(null, ns);
                    this.WriteString(null, (string)value.Identifier);
                    break;

                case IdType.Guid:
                    this.WriteByte(null, 0x04);
                    this.WriteUInt16(null, ns);
                    this.WriteGuid(null, (Guid)value.Identifier);
                    break;

                case IdType.Opaque:
                    this.WriteByte(null, 0x05);
                    this.WriteUInt16(null, ns);
                    this.WriteByteString(null, (byte[])value.Identifier);
                    break;

                default:
                    throw new ServiceResultException(StatusCodes.BadEncodingError);
            }
        }

        public void WriteExpandedNodeId(string? fieldName, ExpandedNodeId? value)
        {
            if (value == null)
            {
                this.WriteUInt16(null, 0);
                return;
            }

            ushort ns = value.NodeId.NamespaceIndex;
            uint svr = value.ServerIndex;
            byte b = 0x00;
            if (!string.IsNullOrEmpty(value.NamespaceUri))
            {
                b |= 0x80;
                ns = 0;
            }

            if (svr > 0u)
            {
                b |= 0x40;
            }

            switch (value.NodeId.IdType)
            {
                case IdType.Numeric:
                    var id = (uint)value.NodeId.Identifier;
                    if (id <= 255u && ns == 0u)
                    {
                        this.WriteByte(null, b);
                        this.WriteByte(null, (byte)id);
                        break;
                    }
                    else if (id <= 65535u && ns <= 255u)
                    {
                        b |= 0x01;
                        this.WriteByte(null, b);
                        this.WriteByte(null, (byte)ns);
                        this.WriteUInt16(null, (ushort)id);
                        break;
                    }
                    else
                    {
                        b |= 0x02;
                        this.WriteByte(null, b);
                        this.WriteUInt16(null, ns);
                        this.WriteUInt32(null, id);
                        break;
                    }

                case IdType.String:
                    b |= 0x03;
                    this.WriteByte(null, b);
                    this.WriteUInt16(null, ns);
                    this.WriteString(null, (string)value.NodeId.Identifier);
                    break;

                case IdType.Guid:
                    b |= 0x04;
                    this.WriteByte(null, b);
                    this.WriteUInt16(null, ns);
                    this.WriteGuid(null, (Guid)value.NodeId.Identifier);
                    break;

                case IdType.Opaque:
                    b |= 0x05;
                    this.WriteByte(null, b);
                    this.WriteUInt16(null, ns);
                    this.WriteByteString(null, (byte[])value.NodeId.Identifier);
                    break;

                default:
                    throw new ServiceResultException(StatusCodes.BadEncodingError);
            }

            if ((b & 0x80) != 0)
            {
                this.WriteString(null, value.NamespaceUri);
            }

            if ((b & 0x40) != 0)
            {
                this.WriteUInt32(null, svr);
            }
        }

        public void WriteStatusCode(string? fieldName, StatusCode value)
        {
            this.WriteUInt32(null, value.Value);
        }

        public void WriteDiagnosticInfo(string? fieldName, DiagnosticInfo? value)
        {
            if (value == null)
            {
                this.WriteByte(null, 0);
                return;
            }

            byte b = 0;
            if (value.SymbolicId >= 0)
            {
                b |= 1;
            }

            if (value.NamespaceUri >= 0)
            {
                b |= 2;
            }

            if (value.Locale >= 0)
            {
                b |= 8;
            }

            if (value.LocalizedText >= 0)
            {
                b |= 4;
            }

            if (value.AdditionalInfo != null)
            {
                b |= 16;
            }

            if (value.InnerStatusCode != 0u)
            {
                b |= 32;
            }

            if (value.InnerDiagnosticInfo != null)
            {
                b |= 64;
            }

            this.WriteByte(null, b);
            if ((b & 1) != 0)
            {
                this.WriteInt32(null, value.SymbolicId);
            }

            if ((b & 2) != 0)
            {
                this.WriteInt32(null, value.NamespaceUri);
            }

            if ((b & 8) != 0)
            {
                this.WriteInt32(null, value.Locale);
            }

            if ((b & 4) != 0)
            {
                this.WriteInt32(null, value.LocalizedText);
            }

            if ((b & 16) != 0)
            {
                this.WriteString(null, value.AdditionalInfo);
            }

            if ((b & 32) != 0)
            {
                this.WriteStatusCode(null, value.InnerStatusCode);
            }

            if ((b & 64) != 0)
            {
                this.WriteDiagnosticInfo(null, value.InnerDiagnosticInfo);
            }
        }

        public void WriteQualifiedName(string? fieldName, QualifiedName? value)
        {
            if (value == null)
            {
                this.WriteUInt16(null, 0);
                this.WriteString(null, null);
                return;
            }

            this.WriteUInt16(null, value.NamespaceIndex);
            this.WriteString(null, value.Name);
        }

        public void WriteLocalizedText(string? fieldName, LocalizedText? value)
        {
            if (value == null)
            {
                this.WriteByte(null, 0);
                return;
            }

            byte b = 0;
            if (value.Locale != null)
            {
                b |= 1;
            }

            if (value.Text != null)
            {
                b |= 2;
            }

            this.WriteByte(null, b);
            if ((b & 1) != 0)
            {
                this.WriteString(null, value.Locale);
            }

            if ((b & 2) != 0)
            {
                this.WriteString(null, value.Text);
            }
        }

        public void WriteVariant(string? fieldName, Variant value)
        {
            var obj = value.Value;
            if (obj is null || Variant.IsNull(value))
            {
                this.WriteByte(null, 0);
                return;
            }

            byte b = (byte)value.Type;
            int[]? dims = value.ArrayDimensions;
            if (dims == null)
            {
                this.WriteByte(null, b);
                switch (value.Type)
                {
                    case VariantType.Boolean:
                        this.WriteBoolean(null, (bool)obj);
                        break;

                    case VariantType.SByte:
                        this.WriteSByte(null, (sbyte)obj);
                        break;

                    case VariantType.Byte:
                        this.WriteByte(null, (byte)obj);
                        break;

                    case VariantType.Int16:
                        this.WriteInt16(null, (short)obj);
                        break;

                    case VariantType.UInt16:
                        this.WriteUInt16(null, (ushort)obj);
                        break;

                    case VariantType.Int32:
                        this.WriteInt32(null, (int)obj);
                        break;

                    case VariantType.UInt32:
                        this.WriteUInt32(null, (uint)obj);
                        break;

                    case VariantType.Int64:
                        this.WriteInt64(null, (long)obj);
                        break;

                    case VariantType.UInt64:
                        this.WriteUInt64(null, (ulong)obj);
                        break;

                    case VariantType.Float:
                        this.WriteFloat(null, (float)obj);
                        break;

                    case VariantType.Double:
                        this.WriteDouble(null, (double)obj);
                        break;

                    case VariantType.String:
                        this.WriteString(null, (string)obj);
                        break;

                    case VariantType.DateTime:
                        this.WriteDateTime(null, (DateTime)obj);
                        break;

                    case VariantType.Guid:
                        this.WriteGuid(null, (Guid)obj);
                        break;

                    case VariantType.ByteString:
                        this.WriteByteString(null, (byte[])obj);
                        break;

                    case VariantType.XmlElement:
                        this.WriteXElement(null, (XElement)obj);
                        break;

                    case VariantType.NodeId:
                        this.WriteNodeId(null, (NodeId)obj);
                        break;

                    case VariantType.ExpandedNodeId:
                        this.WriteExpandedNodeId(null, (ExpandedNodeId)obj);
                        break;

                    case VariantType.StatusCode:
                        this.WriteStatusCode(null, (StatusCode)obj);
                        break;

                    case VariantType.QualifiedName:
                        this.WriteQualifiedName(null, (QualifiedName)obj);
                        break;

                    case VariantType.LocalizedText:
                        this.WriteLocalizedText(null, (LocalizedText)obj);
                        break;

                    case VariantType.ExtensionObject:
                        this.WriteExtensionObject(null, (ExtensionObject)obj);
                        break;

                    default:
                        throw new ServiceResultException(StatusCodes.BadEncodingError);
                }

                return;
            }

            b |= 128; // an array
            if (dims.Length == 1)
            {
                this.WriteByte(null, b);
                switch (value.Type)
                {
                    case VariantType.Boolean:
                        this.WriteBooleanArray(null, (bool[])obj);
                        break;

                    case VariantType.SByte:
                        this.WriteSByteArray(null, (sbyte[])obj);
                        break;

                    case VariantType.Byte:
                        this.WriteByteArray(null, (byte[])obj);
                        break;

                    case VariantType.Int16:
                        this.WriteInt16Array(null, (short[])obj);
                        break;

                    case VariantType.UInt16:
                        this.WriteUInt16Array(null, (ushort[])obj);
                        break;

                    case VariantType.Int32:
                        this.WriteInt32Array(null, (int[])obj);
                        break;

                    case VariantType.UInt32:
                        this.WriteUInt32Array(null, (uint[])obj);
                        break;

                    case VariantType.Int64:
                        this.WriteInt64Array(null, (long[])obj);
                        break;

                    case VariantType.UInt64:
                        this.WriteUInt64Array(null, (ulong[])obj);
                        break;

                    case VariantType.Float:
                        this.WriteFloatArray(null, (float[])obj);
                        break;

                    case VariantType.Double:
                        this.WriteDoubleArray(null, (double[])obj);
                        break;

                    case VariantType.String:
                        this.WriteStringArray(null, (string[])obj);
                        break;

                    case VariantType.DateTime:
                        this.WriteDateTimeArray(null, (DateTime[])obj);
                        break;

                    case VariantType.Guid:
                        this.WriteGuidArray(null, (Guid[])obj);
                        break;

                    case VariantType.ByteString:
                        this.WriteByteStringArray(null, (byte[][])obj);
                        break;

                    case VariantType.XmlElement:
                        this.WriteXElementArray(null, (XElement[])obj);
                        break;

                    case VariantType.NodeId:
                        this.WriteNodeIdArray(null, (NodeId[])obj);
                        break;

                    case VariantType.ExpandedNodeId:
                        this.WriteExpandedNodeIdArray(null, (ExpandedNodeId[])obj);
                        break;

                    case VariantType.StatusCode:
                        this.WriteStatusCodeArray(null, (StatusCode[])obj);
                        break;

                    case VariantType.QualifiedName:
                        this.WriteQualifiedNameArray(null, (QualifiedName[])obj);
                        break;

                    case VariantType.LocalizedText:
                        this.WriteLocalizedTextArray(null, (LocalizedText[])obj);
                        break;

                    case VariantType.ExtensionObject:
                        this.WriteExtensionObjectArray(null, (ExtensionObject[])obj);
                        break;

                    case VariantType.Variant:
                        this.WriteVariantArray(null, (Variant[])obj);
                        break;

                    default:
                        throw new ServiceResultException(StatusCodes.BadEncodingError);
                }

                return;
            }

            var a1 = (Array)obj;
            b |= 64;
            this.WriteByte(null, b);
            switch (value.Type)
            {
                case VariantType.Boolean:
                    this.WriteBooleanArray(null, this.FlattenArray<bool>(a1));
                    break;

                case VariantType.SByte:
                    this.WriteSByteArray(null, this.FlattenArray<sbyte>(a1));
                    break;

                case VariantType.Byte:
                    this.WriteByteArray(null, this.FlattenArray<byte>(a1));
                    break;

                case VariantType.Int16:
                    this.WriteInt16Array(null, this.FlattenArray<short>(a1));
                    break;

                case VariantType.UInt16:
                    this.WriteUInt16Array(null, this.FlattenArray<ushort>(a1));
                    break;

                case VariantType.Int32:
                    this.WriteInt32Array(null, this.FlattenArray<int>(a1));
                    break;

                case VariantType.UInt32:
                    this.WriteUInt32Array(null, this.FlattenArray<uint>(a1));
                    break;

                case VariantType.Int64:
                    this.WriteInt64Array(null, this.FlattenArray<long>(a1));
                    break;

                case VariantType.UInt64:
                    this.WriteUInt64Array(null, this.FlattenArray<ulong>(a1));
                    break;

                case VariantType.Float:
                    this.WriteFloatArray(null, this.FlattenArray<float>(a1));
                    break;

                case VariantType.Double:
                    this.WriteDoubleArray(null, this.FlattenArray<double>(a1));
                    break;

                case VariantType.String:
                    this.WriteStringArray(null, this.FlattenArray<string>(a1));
                    break;

                case VariantType.DateTime:
                    this.WriteDateTimeArray(null, this.FlattenArray<DateTime>(a1));
                    break;

                case VariantType.Guid:
                    this.WriteGuidArray(null, this.FlattenArray<Guid>(a1));
                    break;

                case VariantType.ByteString:
                    this.WriteByteStringArray(null, this.FlattenArray<byte[]>(a1));
                    break;

                case VariantType.XmlElement:
                    this.WriteXElementArray(null, this.FlattenArray<XElement>(a1));
                    break;

                case VariantType.NodeId:
                    this.WriteNodeIdArray(null, this.FlattenArray<NodeId>(a1));
                    break;

                case VariantType.ExpandedNodeId:
                    this.WriteExpandedNodeIdArray(null, this.FlattenArray<ExpandedNodeId>(a1));
                    break;

                case VariantType.StatusCode:
                    this.WriteStatusCodeArray(null, this.FlattenArray<StatusCode>(a1));
                    break;

                case VariantType.QualifiedName:
                    this.WriteQualifiedNameArray(null, this.FlattenArray<QualifiedName>(a1));
                    break;

                case VariantType.LocalizedText:
                    this.WriteLocalizedTextArray(null, this.FlattenArray<LocalizedText>(a1));
                    break;

                case VariantType.ExtensionObject:
                    this.WriteExtensionObjectArray(null, this.FlattenArray<ExtensionObject>(a1));
                    break;

                case VariantType.Variant:
                    this.WriteVariantArray(null, this.FlattenArray<Variant>(a1));
                    break;

                default:
                    throw new ServiceResultException(StatusCodes.BadEncodingError);
            }

            this.WriteInt32Array(null, dims);
        }

        public void WriteDataValue(string? fieldName, DataValue? value)
        {
            if (value == null)
            {
                this.WriteByte(null, 0);
                return;
            }

            byte b = 0;
            if (!Variant.IsNull(value.Variant))
            {
                b |= 1;
            }

            if (value.StatusCode != 0u)
            {
                b |= 2;
            }

            if (value.SourceTimestamp != DateTime.MinValue)
            {
                b |= 4;
            }

            if (value.SourcePicoseconds != 0)
            {
                b |= 16;
            }

            if (value.ServerTimestamp != DateTime.MinValue)
            {
                b |= 8;
            }

            if (value.ServerPicoseconds != 0)
            {
                b |= 32;
            }

            this.WriteByte(null, b);
            if ((b & 1) != 0)
            {
                this.WriteVariant(null, value.Variant);
            }

            if ((b & 2) != 0)
            {
                this.WriteStatusCode(null, value.StatusCode);
            }

            if ((b & 4) != 0)
            {
                this.WriteDateTime(null, value.SourceTimestamp);
            }

            if ((b & 16) != 0)
            {
                this.WriteUInt16(null, value.SourcePicoseconds);
            }

            if ((b & 8) != 0)
            {
                this.WriteDateTime(null, value.ServerTimestamp);
            }

            if ((b & 32) != 0)
            {
                this.WriteUInt16(null, value.ServerPicoseconds);
            }
        }

        public void WriteExtensionObject(string? fieldName, ExtensionObject? value)
        {
            if (value == null || value.BodyType == BodyType.None)
            {
                this.WriteNodeId(null, NodeId.Null);
                this.WriteByte(null, 0x00);
                return;
            }

            // If the body type is not none, than the type id
            // is guaranteed to be not null
            var typeId = value.TypeId!;

            if (value.BodyType == BodyType.ByteString)
            {
                this.WriteNodeId(null, ExpandedNodeId.ToNodeId(typeId, this.channel?.NamespaceUris));
                this.WriteByte(null, 0x01);
                this.WriteByteString(null, (byte[]?)value.Body);
                return;
            }

            if (value.BodyType == BodyType.XmlElement)
            {
                this.WriteNodeId(null, ExpandedNodeId.ToNodeId(typeId, this.channel?.NamespaceUris));
                this.WriteByte(null, 0x02);
                this.WriteXElement(null, (XElement?)value.Body);
                return;
            }

            if (value.BodyType == BodyType.Encodable)
            {
                this.WriteNodeId(null, ExpandedNodeId.ToNodeId(typeId, this.channel?.NamespaceUris));
                this.WriteByte(null, 0x01); // BodyType Encodable is encoded as ByteString.
                var pos0 = this.writer.BaseStream.Position;
                this.WriteInt32(null, -1);
                var pos1 = this.writer.BaseStream.Position;
                ((IEncodable)value.Body!).Encode(this);
                var pos2 = this.writer.BaseStream.Position;
                this.writer.Seek((int)pos0, SeekOrigin.Begin);
                this.WriteInt32(null, (int)(pos2 - pos1));
                this.writer.Seek((int)pos2, SeekOrigin.Begin);
                return;
            }
        }

        public void WriteExtensionObject<T>(string? fieldName, T? value)
            where T : class, IEncodable
        {
            if (value == null)
            {
                this.WriteNodeId(null, NodeId.Null);
                this.WriteByte(null, 0x00);
                return;
            }

            var type = value.GetType();
            if (this.channel is null || !this.channel.TryGetBinaryEncodingIdFromType(type, out NodeId binaryEncodingId))
            {
                throw new ServiceResultException(StatusCodes.BadEncodingError);
            }

            this.WriteNodeId(null, binaryEncodingId);
            this.WriteByte(null, 0x01);
            var pos0 = this.writer.BaseStream.Position;
            this.WriteInt32(null, -1);
            var pos1 = this.writer.BaseStream.Position;
            value.Encode(this);
            var pos2 = this.writer.BaseStream.Position;
            this.writer.Seek((int)pos0, SeekOrigin.Begin);
            this.WriteInt32(null, (int)(pos2 - pos1));
            this.writer.Seek((int)pos2, SeekOrigin.Begin);
            return;
        }

        public void WriteEncodable<T>(string? fieldName, T? value)
            where T : class, IEncodable
        {
            if (value == null)
            {
                value = Activator.CreateInstance<T>();
            }

            value.Encode(this);
        }

        public void WriteEnumeration<T>(string? fieldName, T value)
            where T : struct, IConvertible
        {
            this.WriteInt32(null, Convert.ToInt32(value, CultureInfo.InvariantCulture));
        }

        public void WriteBooleanArray(string? fieldName, bool[]? values)
        {
            if (this.TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this.WriteBoolean(null, values[i]);
                }
            }
        }

        public void WriteSByteArray(string? fieldName, sbyte[]? values)
        {
            if (this.TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this.WriteSByte(null, values[i]);
                }
            }
        }

        public void WriteByteArray(string? fieldName, byte[]? values)
        {
            if (this.TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this.WriteByte(null, values[i]);
                }
            }
        }

        public void WriteInt16Array(string? fieldName, short[]? values)
        {
            if (this.TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this.WriteInt16(null, values[i]);
                }
            }
        }

        public void WriteUInt16Array(string? fieldName, ushort[]? values)
        {
            if (this.TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this.WriteUInt16(null, values[i]);
                }
            }
        }

        public void WriteInt32Array(string? fieldName, int[]? values)
        {
            if (this.TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this.WriteInt32(null, values[i]);
                }
            }
        }

        public void WriteUInt32Array(string? fieldName, uint[]? values)
        {
            if (this.TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this.WriteUInt32(null, values[i]);
                }
            }
        }

        public void WriteInt64Array(string? fieldName, long[]? values)
        {
            if (this.TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this.WriteInt64(null, values[i]);
                }
            }
        }

        public void WriteUInt64Array(string? fieldName, ulong[]? values)
        {
            if (this.TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this.WriteUInt64(null, values[i]);
                }
            }
        }

        public void WriteFloatArray(string? fieldName, float[]? values)
        {
            if (this.TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this.WriteFloat(null, values[i]);
                }
            }
        }

        public void WriteDoubleArray(string? fieldName, double[]? values)
        {
            if (this.TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this.WriteDouble(null, values[i]);
                }
            }
        }

        public void WriteStringArray(string? fieldName, string?[]? values)
        {
            if (this.TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this.WriteString(null, values[i]);
                }
            }
        }

        public void WriteDateTimeArray(string? fieldName, DateTime[]? values)
        {
            if (this.TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this.WriteDateTime(null, values[i]);
                }
            }
        }

        public void WriteGuidArray(string? fieldName, Guid[]? values)
        {
            if (this.TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this.WriteGuid(null, values[i]);
                }
            }
        }

        public void WriteByteStringArray(string? fieldName, byte[]?[]? values)
        {
            if (this.TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this.WriteByteString(null, values[i]);
                }
            }
        }

        public void WriteXElementArray(string? fieldName, XElement?[]? values)
        {
            if (this.TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this.WriteXElement(null, values[i]);
                }
            }
        }

        public void WriteNodeIdArray(string? fieldName, NodeId?[]? values)
        {
            if (this.TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this.WriteNodeId(null, values[i]);
                }
            }
        }

        public void WriteExpandedNodeIdArray(string? fieldName, ExpandedNodeId?[]? values)
        {
            if (this.TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this.WriteExpandedNodeId(null, values[i]);
                }
            }
        }

        public void WriteStatusCodeArray(string? fieldName, StatusCode[]? values)
        {
            if (this.TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this.WriteStatusCode(null, values[i]);
                }
            }
        }

        public void WriteDiagnosticInfoArray(string? fieldName, DiagnosticInfo?[]? values)
        {
            if (this.TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this.WriteDiagnosticInfo(null, values[i]);
                }
            }
        }

        public void WriteQualifiedNameArray(string? fieldName, QualifiedName?[]? values)
        {
            if (this.TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this.WriteQualifiedName(null, values[i]);
                }
            }
        }

        public void WriteLocalizedTextArray(string? fieldName, LocalizedText?[]? values)
        {
            if (this.TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this.WriteLocalizedText(null, values[i]);
                }
            }
        }

        public void WriteVariantArray(string? fieldName, Variant[]? values)
        {
            if (this.TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this.WriteVariant(null, values[i]);
                }
            }
        }

        public void WriteDataValueArray(string? fieldName, DataValue?[]? values)
        {
            if (this.TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this.WriteDataValue(null, values[i]);
                }
            }
        }

        public void WriteExtensionObjectArray(string? fieldName, ExtensionObject?[]? values)
        {
            if (this.TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this.WriteExtensionObject(null, values[i]);
                }
            }
        }

        public void WriteExtensionObjectArray<T>(string? fieldName, T?[]? values)
            where T : class?, IEncodable
        {
            if (this.TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this.WriteExtensionObject(null, values[i]);
                }
            }
        }

        public void WriteEncodableArray<T>(string? fieldName, T?[]? values)
            where T : class?, IEncodable
        {
            if (this.TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this.WriteEncodable(null, values[i]);
                }
            }
        }

        public void WriteEnumerationArray<T>(string? fieldName, T[]? values)
            where T : struct, IConvertible
        {
            if (this.TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    this.WriteEnumeration(null, values[i]);
                }
            }
        }

        public void Write(byte[] buffer, int index, int count)
        {
            this.writer.Write(buffer, index, count);
        }

        private bool TryWriteArrayLength<T>([NotNullWhen(returnValue: true)] T[]? values)
        {
            if (values == null)
            {
                this.WriteInt32(null, -1);
                return false;
            }

            this.WriteInt32(null, values.Length);
            return true;
        }

        private T[] FlattenArray<T>(Array a1)
        {
            return a1.Cast<T>().ToArray();
        }
    }
}