// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace Workstation.ServiceModel.Ua.Channels
{
    /// <summary>
    /// An encoder for the OPC UA Binary DataEncoding.
    /// </summary>
    /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.1/">OPC UA specification Part 6: Mappings, 5.2.1</seealso>
    public sealed class BinaryEncoder : IEncoder, IDisposable
    {
        private const long _minFileTime = 504911232000000000L;
        private readonly Stream _stream;
        private readonly IEncodingContext _context;
        private readonly Encoding _encoding;
        private readonly BinaryWriter _writer;

        public BinaryEncoder(Stream stream, IEncodingContext? context = null, bool keepStreamOpen = false)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            _stream = stream;
            _context = context ?? new DefaultEncodingContext();
            _encoding = new UTF8Encoding(false, false);
            _writer = new BinaryWriter(_stream, _encoding, keepStreamOpen);
        }

        public int Position
        {
            get { return (int)_stream.Position; }
            set { _stream.Position = value; }
        }

        public void Dispose()
        {
            if (_writer != null)
            {
                _writer.Dispose();
            }
        }

        public void PushNamespace(string namespaceUri)
        {
        }

        public void PopNamespace()
        {
        }

        /// <summary>
        /// Writes a boolean value to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.1">OPC UA specification Part 6: Mappings, 5.2.2.1</seealso>
        public void WriteBoolean(string? fieldName, bool value)
        {
            _writer.Write(value);
        }

        /// <summary>
        /// Writes a signed byte value to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        public void WriteSByte(string? fieldName, sbyte value)
        {
            _writer.Write(value);
        }

        /// <summary>
        /// Writes an unsigned byte value to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        public void WriteByte(string? fieldName, byte value)
        {
            _writer.Write(value);
        }

        /// <summary>
        /// Writes a signed short value to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        public void WriteInt16(string? fieldName, short value)
        {
            _writer.Write(value);
        }

        /// <summary>
        /// Writes a unsigned short value to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        public void WriteUInt16(string? fieldName, ushort value)
        {
            _writer.Write(value);
        }

        /// <summary>
        /// Writes a signed integer value to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        public void WriteInt32(string? fieldName, int value)
        {
            _writer.Write(value);
        }

        /// <summary>
        /// Writes an unsigned integer value to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        public void WriteUInt32(string? fieldName, uint value)
        {
            _writer.Write(value);
        }

        /// <summary>
        /// Writes a signed long integer value to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        public void WriteInt64(string? fieldName, long value)
        {
            _writer.Write(value);
        }

        /// <summary>
        /// Writes an unsigned long integer value to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        public void WriteUInt64(string? fieldName, ulong value)
        {
            _writer.Write(value);
        }

        /// <summary>
        /// Writes a floating point value to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.3">OPC UA specification Part 6: Mappings, 5.2.2.3</seealso>
        public void WriteFloat(string? fieldName, float value)
        {
            _writer.Write(value);
        }

        /// <summary>
        /// Writes a double precision floating point value to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.3">OPC UA specification Part 6: Mappings, 5.2.2.3</seealso>
        public void WriteDouble(string? fieldName, double value)
        {
            _writer.Write(value);
        }

        /// <summary>
        /// Writes a string to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.4">OPC UA specification Part 6: Mappings, 5.2.2.4</seealso>
        public void WriteString(string? fieldName, string? value)
        {
            if (value == null)
            {
                _writer.Write(-1);
                return;
            }

            WriteByteString(null, _encoding.GetBytes(value));
        }

        /// <summary>
        /// Writes a date time value to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.5">OPC UA specification Part 6: Mappings, 5.2.2.5</seealso>
        public void WriteDateTime(string? fieldName, DateTime value)
        {
            if (value.Kind == DateTimeKind.Local)
            {
                value = value.ToUniversalTime();
            }

            if (value.Ticks < _minFileTime)
            {
                _writer.Write(0L);
                return;
            }

            _writer.Write(value.ToFileTimeUtc());
        }

        /// <summary>
        /// Writes a GUID to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.6">OPC UA specification Part 6: Mappings, 5.2.2.6</seealso>
        public void WriteGuid(string? fieldName, Guid value)
        {
            _writer.Write(value.ToByteArray());
        }

        /// <summary>
        /// Writes a byte string to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.7">OPC UA specification Part 6: Mappings, 5.2.2.7</seealso>
        public void WriteByteString(string? fieldName, byte[]? value)
        {
            if (value == null)
            {
                _writer.Write(-1);
                return;
            }

            _writer.Write(value.Length);
            _writer.Write(value);
        }

        /// <summary>
        /// Writes a XML element to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.8">OPC UA specification Part 6: Mappings, 5.2.2.8</seealso>
        public void WriteXElement(string? fieldName, XElement? value)
        {
            if (value == null)
            {
                _writer.Write(-1);
                return;
            }

            WriteByteString(null, _encoding.GetBytes(value.ToString()));
        }

        /// <summary>
        /// Writes a node id to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.9">OPC UA specification Part 6: Mappings, 5.2.2.9</seealso>
        public void WriteNodeId(string? fieldName, NodeId? value)
        {
            if (value == null)
            {
                WriteUInt16(null, 0);
                return;
            }

            ushort ns = value.NamespaceIndex;
            switch (value.IdType)
            {
                case IdType.Numeric:
                    var id = (uint)value.Identifier;
                    if (id <= 255u && ns == 0u)
                    {
                        WriteByte(null, 0x00);
                        WriteByte(null, (byte)id);
                        break;
                    }
                    else if (id <= 65535u && ns <= 255u)
                    {
                        WriteByte(null, 0x01);
                        WriteByte(null, (byte)ns);
                        WriteUInt16(null, (ushort)id);
                        break;
                    }
                    else
                    {
                        WriteByte(null, 0x02);
                        WriteUInt16(null, ns);
                        WriteUInt32(null, id);
                        break;
                    }

                case IdType.String:
                    WriteByte(null, 0x03);
                    WriteUInt16(null, ns);
                    WriteString(null, (string)value.Identifier);
                    break;

                case IdType.Guid:
                    WriteByte(null, 0x04);
                    WriteUInt16(null, ns);
                    WriteGuid(null, (Guid)value.Identifier);
                    break;

                case IdType.Opaque:
                    WriteByte(null, 0x05);
                    WriteUInt16(null, ns);
                    WriteByteString(null, (byte[])value.Identifier);
                    break;

                default:
                    throw new ServiceResultException(StatusCodes.BadEncodingError);
            }
        }

        /// <summary>
        /// Writes an expanded node id to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.10">OPC UA specification Part 6: Mappings, 5.2.2.10</seealso>
        public void WriteExpandedNodeId(string? fieldName, ExpandedNodeId? value)
        {
            if (value == null)
            {
                WriteUInt16(null, 0);
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
                        WriteByte(null, b);
                        WriteByte(null, (byte)id);
                        break;
                    }
                    else if (id <= 65535u && ns <= 255u)
                    {
                        b |= 0x01;
                        WriteByte(null, b);
                        WriteByte(null, (byte)ns);
                        WriteUInt16(null, (ushort)id);
                        break;
                    }
                    else
                    {
                        b |= 0x02;
                        WriteByte(null, b);
                        WriteUInt16(null, ns);
                        WriteUInt32(null, id);
                        break;
                    }

                case IdType.String:
                    b |= 0x03;
                    WriteByte(null, b);
                    WriteUInt16(null, ns);
                    WriteString(null, (string)value.NodeId.Identifier);
                    break;

                case IdType.Guid:
                    b |= 0x04;
                    WriteByte(null, b);
                    WriteUInt16(null, ns);
                    WriteGuid(null, (Guid)value.NodeId.Identifier);
                    break;

                case IdType.Opaque:
                    b |= 0x05;
                    WriteByte(null, b);
                    WriteUInt16(null, ns);
                    WriteByteString(null, (byte[])value.NodeId.Identifier);
                    break;

                default:
                    throw new ServiceResultException(StatusCodes.BadEncodingError);
            }

            if ((b & 0x80) != 0)
            {
                WriteString(null, value.NamespaceUri);
            }

            if ((b & 0x40) != 0)
            {
                WriteUInt32(null, svr);
            }
        }

        /// <summary>
        /// Writes a status code to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.11">OPC UA specification Part 6: Mappings, 5.2.2.11</seealso>
        public void WriteStatusCode(string? fieldName, StatusCode value)
        {
            WriteUInt32(null, value.Value);
        }

        /// <summary>
        /// Writes a <see cref="DiagnosticInfo"/> object to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.12">OPC UA specification Part 6: Mappings, 5.2.2.12</seealso>
        public void WriteDiagnosticInfo(string? fieldName, DiagnosticInfo? value)
        {
            if (value == null)
            {
                WriteByte(null, 0);
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

            WriteByte(null, b);
            if ((b & 1) != 0)
            {
                WriteInt32(null, value.SymbolicId);
            }

            if ((b & 2) != 0)
            {
                WriteInt32(null, value.NamespaceUri);
            }

            if ((b & 8) != 0)
            {
                WriteInt32(null, value.Locale);
            }

            if ((b & 4) != 0)
            {
                WriteInt32(null, value.LocalizedText);
            }

            if ((b & 16) != 0)
            {
                WriteString(null, value.AdditionalInfo);
            }

            if ((b & 32) != 0)
            {
                WriteStatusCode(null, value.InnerStatusCode);
            }

            if ((b & 64) != 0)
            {
                WriteDiagnosticInfo(null, value.InnerDiagnosticInfo);
            }
        }

        /// <summary>
        /// Writes a <see cref="QualifiedName"/> object to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.13">OPC UA specification Part 6: Mappings, 5.2.2.13</seealso>
        public void WriteQualifiedName(string? fieldName, QualifiedName? value)
        {
            if (value == null)
            {
                WriteUInt16(null, 0);
                WriteString(null, null);
                return;
            }

            WriteUInt16(null, value.NamespaceIndex);
            WriteString(null, value.Name);
        }

        /// <summary>
        /// Writes a <see cref="LocalizedText"/> to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.14">OPC UA specification Part 6: Mappings, 5.2.2.14</seealso>
        public void WriteLocalizedText(string? fieldName, LocalizedText? value)
        {
            if (value == null)
            {
                WriteByte(null, 0);
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

            WriteByte(null, b);
            if ((b & 1) != 0)
            {
                WriteString(null, value.Locale);
            }

            if ((b & 2) != 0)
            {
                WriteString(null, value.Text);
            }
        }

        /// <summary>
        /// Writes a <see cref="Variant"/> value to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.16">OPC UA specification Part 6: Mappings, 5.2.2.16</seealso>
        public void WriteVariant(string? fieldName, Variant value)
        {
            var obj = value.Value;
            if (obj is null || Variant.IsNull(value))
            {
                WriteByte(null, 0);
                return;
            }

            byte b = (byte)value.Type;
            int[]? dims = value.ArrayDimensions;
            if (dims == null)
            {
                WriteByte(null, b);
                switch (value.Type)
                {
                    case VariantType.Boolean:
                        WriteBoolean(null, (bool)obj);
                        break;

                    case VariantType.SByte:
                        WriteSByte(null, (sbyte)obj);
                        break;

                    case VariantType.Byte:
                        WriteByte(null, (byte)obj);
                        break;

                    case VariantType.Int16:
                        WriteInt16(null, (short)obj);
                        break;

                    case VariantType.UInt16:
                        WriteUInt16(null, (ushort)obj);
                        break;

                    case VariantType.Int32:
                        WriteInt32(null, (int)obj);
                        break;

                    case VariantType.UInt32:
                        WriteUInt32(null, (uint)obj);
                        break;

                    case VariantType.Int64:
                        WriteInt64(null, (long)obj);
                        break;

                    case VariantType.UInt64:
                        WriteUInt64(null, (ulong)obj);
                        break;

                    case VariantType.Float:
                        WriteFloat(null, (float)obj);
                        break;

                    case VariantType.Double:
                        WriteDouble(null, (double)obj);
                        break;

                    case VariantType.String:
                        WriteString(null, (string)obj);
                        break;

                    case VariantType.DateTime:
                        WriteDateTime(null, (DateTime)obj);
                        break;

                    case VariantType.Guid:
                        WriteGuid(null, (Guid)obj);
                        break;

                    case VariantType.ByteString:
                        WriteByteString(null, (byte[])obj);
                        break;

                    case VariantType.XmlElement:
                        WriteXElement(null, (XElement)obj);
                        break;

                    case VariantType.NodeId:
                        WriteNodeId(null, (NodeId)obj);
                        break;

                    case VariantType.ExpandedNodeId:
                        WriteExpandedNodeId(null, (ExpandedNodeId)obj);
                        break;

                    case VariantType.StatusCode:
                        WriteStatusCode(null, (StatusCode)obj);
                        break;

                    case VariantType.QualifiedName:
                        WriteQualifiedName(null, (QualifiedName)obj);
                        break;

                    case VariantType.LocalizedText:
                        WriteLocalizedText(null, (LocalizedText)obj);
                        break;

                    case VariantType.ExtensionObject:
                        WriteExtensionObject(null, (ExtensionObject)obj);
                        break;

                    default:
                        throw new ServiceResultException(StatusCodes.BadEncodingError);
                }

                return;
            }

            b |= 128; // an array
            if (dims.Length == 1)
            {
                WriteByte(null, b);
                switch (value.Type)
                {
                    case VariantType.Boolean:
                        WriteBooleanArray(null, (bool[])obj);
                        break;

                    case VariantType.SByte:
                        WriteSByteArray(null, (sbyte[])obj);
                        break;

                    case VariantType.Byte:
                        WriteByteArray(null, (byte[])obj);
                        break;

                    case VariantType.Int16:
                        WriteInt16Array(null, (short[])obj);
                        break;

                    case VariantType.UInt16:
                        WriteUInt16Array(null, (ushort[])obj);
                        break;

                    case VariantType.Int32:
                        WriteInt32Array(null, (int[])obj);
                        break;

                    case VariantType.UInt32:
                        WriteUInt32Array(null, (uint[])obj);
                        break;

                    case VariantType.Int64:
                        WriteInt64Array(null, (long[])obj);
                        break;

                    case VariantType.UInt64:
                        WriteUInt64Array(null, (ulong[])obj);
                        break;

                    case VariantType.Float:
                        WriteFloatArray(null, (float[])obj);
                        break;

                    case VariantType.Double:
                        WriteDoubleArray(null, (double[])obj);
                        break;

                    case VariantType.String:
                        WriteStringArray(null, (string[])obj);
                        break;

                    case VariantType.DateTime:
                        WriteDateTimeArray(null, (DateTime[])obj);
                        break;

                    case VariantType.Guid:
                        WriteGuidArray(null, (Guid[])obj);
                        break;

                    case VariantType.ByteString:
                        WriteByteStringArray(null, (byte[][])obj);
                        break;

                    case VariantType.XmlElement:
                        WriteXElementArray(null, (XElement[])obj);
                        break;

                    case VariantType.NodeId:
                        WriteNodeIdArray(null, (NodeId[])obj);
                        break;

                    case VariantType.ExpandedNodeId:
                        WriteExpandedNodeIdArray(null, (ExpandedNodeId[])obj);
                        break;

                    case VariantType.StatusCode:
                        WriteStatusCodeArray(null, (StatusCode[])obj);
                        break;

                    case VariantType.QualifiedName:
                        WriteQualifiedNameArray(null, (QualifiedName[])obj);
                        break;

                    case VariantType.LocalizedText:
                        WriteLocalizedTextArray(null, (LocalizedText[])obj);
                        break;

                    case VariantType.ExtensionObject:
                        WriteExtensionObjectArray(null, (ExtensionObject[])obj);
                        break;

                    case VariantType.Variant:
                        WriteVariantArray(null, (Variant[])obj);
                        break;

                    default:
                        throw new ServiceResultException(StatusCodes.BadEncodingError);
                }

                return;
            }

            var a1 = (Array)obj;
            b |= 64;
            WriteByte(null, b);
            switch (value.Type)
            {
                case VariantType.Boolean:
                    WriteBooleanArray(null, FlattenArray<bool>(a1));
                    break;

                case VariantType.SByte:
                    WriteSByteArray(null, FlattenArray<sbyte>(a1));
                    break;

                case VariantType.Byte:
                    WriteByteArray(null, FlattenArray<byte>(a1));
                    break;

                case VariantType.Int16:
                    WriteInt16Array(null, FlattenArray<short>(a1));
                    break;

                case VariantType.UInt16:
                    WriteUInt16Array(null, FlattenArray<ushort>(a1));
                    break;

                case VariantType.Int32:
                    WriteInt32Array(null, FlattenArray<int>(a1));
                    break;

                case VariantType.UInt32:
                    WriteUInt32Array(null, FlattenArray<uint>(a1));
                    break;

                case VariantType.Int64:
                    WriteInt64Array(null, FlattenArray<long>(a1));
                    break;

                case VariantType.UInt64:
                    WriteUInt64Array(null, FlattenArray<ulong>(a1));
                    break;

                case VariantType.Float:
                    WriteFloatArray(null, FlattenArray<float>(a1));
                    break;

                case VariantType.Double:
                    WriteDoubleArray(null, FlattenArray<double>(a1));
                    break;

                case VariantType.String:
                    WriteStringArray(null, FlattenArray<string>(a1));
                    break;

                case VariantType.DateTime:
                    WriteDateTimeArray(null, FlattenArray<DateTime>(a1));
                    break;

                case VariantType.Guid:
                    WriteGuidArray(null, FlattenArray<Guid>(a1));
                    break;

                case VariantType.ByteString:
                    WriteByteStringArray(null, FlattenArray<byte[]>(a1));
                    break;

                case VariantType.XmlElement:
                    WriteXElementArray(null, FlattenArray<XElement>(a1));
                    break;

                case VariantType.NodeId:
                    WriteNodeIdArray(null, FlattenArray<NodeId>(a1));
                    break;

                case VariantType.ExpandedNodeId:
                    WriteExpandedNodeIdArray(null, FlattenArray<ExpandedNodeId>(a1));
                    break;

                case VariantType.StatusCode:
                    WriteStatusCodeArray(null, FlattenArray<StatusCode>(a1));
                    break;

                case VariantType.QualifiedName:
                    WriteQualifiedNameArray(null, FlattenArray<QualifiedName>(a1));
                    break;

                case VariantType.LocalizedText:
                    WriteLocalizedTextArray(null, FlattenArray<LocalizedText>(a1));
                    break;

                case VariantType.ExtensionObject:
                    WriteExtensionObjectArray(null, FlattenArray<ExtensionObject>(a1));
                    break;

                case VariantType.Variant:
                    WriteVariantArray(null, FlattenArray<Variant>(a1));
                    break;

                default:
                    throw new ServiceResultException(StatusCodes.BadEncodingError);
            }

            WriteInt32Array(null, dims);
        }

        /// <summary>
        /// Writes a <see cref="DataValue"/> to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.17">OPC UA specification Part 6: Mappings, 5.2.2.17</seealso>
        public void WriteDataValue(string? fieldName, DataValue? value)
        {
            if (value == null)
            {
                WriteByte(null, 0);
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

            WriteByte(null, b);
            if ((b & 1) != 0)
            {
                WriteVariant(null, value.Variant);
            }

            if ((b & 2) != 0)
            {
                WriteStatusCode(null, value.StatusCode);
            }

            if ((b & 4) != 0)
            {
                WriteDateTime(null, value.SourceTimestamp);
            }

            if ((b & 16) != 0)
            {
                WriteUInt16(null, value.SourcePicoseconds);
            }

            if ((b & 8) != 0)
            {
                WriteDateTime(null, value.ServerTimestamp);
            }

            if ((b & 32) != 0)
            {
                WriteUInt16(null, value.ServerPicoseconds);
            }
        }

        /// <summary>
        /// Writes an <see cref="ExtensionObject"/> to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.15">OPC UA specification Part 6: Mappings, 5.2.2.15</seealso>
        public void WriteExtensionObject(string? fieldName, ExtensionObject? value)
        {
            try
            {
                if (value == null || value.BodyType == BodyType.None)
                {
                    WriteNodeId(null, NodeId.Null);
                    WriteByte(null, 0x00);
                    return;
                }

                // If the body type is not none, than the type id
                // is guaranteed to be not null
                var typeId = value.TypeId!;

                if (value.BodyType == BodyType.ByteString)
                {
                    WriteNodeId(null, ExpandedNodeId.ToNodeId(typeId, _context.NamespaceUris));
                    WriteByte(null, 0x01);
                    WriteByteString(null, (byte[]?)value.Body);
                    return;
                }

                if (value.BodyType == BodyType.XmlElement)
                {
                    WriteNodeId(null, ExpandedNodeId.ToNodeId(typeId, _context.NamespaceUris));
                    WriteByte(null, 0x02);
                    WriteXElement(null, (XElement?)value.Body);
                    return;
                }

                if (value.BodyType == BodyType.Encodable)
                {
                    WriteNodeId(null, ExpandedNodeId.ToNodeId(typeId, _context.NamespaceUris));
                    WriteByte(null, 0x01); // BodyType Encodable is encoded as ByteString.
                    var pos0 = _writer.BaseStream.Position;
                    WriteInt32(null, -1);
                    var pos1 = _writer.BaseStream.Position;
                    ((IEncodable)value.Body!).Encode(this);
                    var pos2 = _writer.BaseStream.Position;
                    _writer.Seek((int)pos0, SeekOrigin.Begin);
                    WriteInt32(null, (int)(pos2 - pos1));
                    _writer.Seek((int)pos2, SeekOrigin.Begin);
                    return;
                }
            }
            catch
            {
                throw new ServiceResultException(StatusCodes.BadEncodingError);
            }
        }

        /// <summary>
        /// Writes an <see cref="ExtensionObject"/> to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.15">OPC UA specification Part 6: Mappings, 5.2.2.15</seealso>
        public void WriteExtensionObject<T>(string? fieldName, T? value)
            where T : class, IEncodable
        {
            try
            {
                if (value == null)
                {
                    WriteNodeId(null, NodeId.Null);
                    WriteByte(null, 0x00);
                    return;
                }

                var type = value.GetType();
                if (!TypeLibrary.TryGetBinaryEncodingIdFromType(type, out var binaryEncodingId))
                {
                    throw new ServiceResultException(StatusCodes.BadEncodingError);
                }

                WriteNodeId(null, ExpandedNodeId.ToNodeId(binaryEncodingId, _context.NamespaceUris));
                WriteByte(null, 0x01);
                var pos0 = _writer.BaseStream.Position;
                WriteInt32(null, -1);
                var pos1 = _writer.BaseStream.Position;
                value.Encode(this);
                var pos2 = _writer.BaseStream.Position;
                _writer.Seek((int)pos0, SeekOrigin.Begin);
                WriteInt32(null, (int)(pos2 - pos1));
                _writer.Seek((int)pos2, SeekOrigin.Begin);
                return;
            }
            catch
            {
                throw new ServiceResultException(StatusCodes.BadEncodingError);
            }
        }

        /// <summary>
        /// Writes an <see cref="IEncodable"/> object to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.6/">OPC UA specification Part 6: Mappings, 5.2.6</seealso>
        public void WriteEncodable<T>(string? fieldName, T? value)
            where T : class, IEncodable
        {
            if (value == null)
            {
                value = Activator.CreateInstance<T>();
            }

            value.Encode(this);
        }

        public void WriteMessage(IEncodable value)
        {
            try
            {
                if (!TypeLibrary.TryGetBinaryEncodingIdFromType(value.GetType(), out var id))
                {
                    throw new ServiceResultException(StatusCodes.BadEncodingError);
                }
                WriteNodeId(null, ExpandedNodeId.ToNodeId(id, _context.NamespaceUris));
                value.Encode(this);
            }
            catch
            {
                throw new ServiceResultException(StatusCodes.BadEncodingError);
            }
        }

        /// <summary>
        /// Writes an enumeration value to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="value">The value.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.4/">OPC UA specification Part 6: Mappings, 5.2.4</seealso>
        public void WriteEnumeration<T>(string? fieldName, T value)
            where T : struct, IConvertible
        {
            WriteInt32(null, Convert.ToInt32(value, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Writes a boolean array to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="values">The array.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.1">OPC UA specification Part 6: Mappings, 5.2.2.1</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public void WriteBooleanArray(string? fieldName, bool[]? values)
        {
            if (TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    WriteBoolean(null, values[i]);
                }
            }
        }

        /// <summary>
        /// Writes a signed byte array to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="values">The array.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public void WriteSByteArray(string? fieldName, sbyte[]? values)
        {
            if (TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    WriteSByte(null, values[i]);
                }
            }
        }

        /// <summary>
        /// Writes an unsigned byte array to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="values">The array.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public void WriteByteArray(string? fieldName, byte[]? values)
        {
            if (TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    WriteByte(null, values[i]);
                }
            }
        }

        /// <summary>
        /// Writes a signed short array to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="values">The array.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public void WriteInt16Array(string? fieldName, short[]? values)
        {
            if (TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    WriteInt16(null, values[i]);
                }
            }
        }

        /// <summary>
        /// Writes an unsigned short array to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="values">The array.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public void WriteUInt16Array(string? fieldName, ushort[]? values)
        {
            if (TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    WriteUInt16(null, values[i]);
                }
            }
        }

        /// <summary>
        /// Writes a signed integer array to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="values">The array.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public void WriteInt32Array(string? fieldName, int[]? values)
        {
            if (TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    WriteInt32(null, values[i]);
                }
            }
        }

        /// <summary>
        /// Writes an unsigned integer array to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="values">The array.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public void WriteUInt32Array(string? fieldName, uint[]? values)
        {
            if (TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    WriteUInt32(null, values[i]);
                }
            }
        }

        /// <summary>
        /// Writes a signed long integer array to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="values">The array.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public void WriteInt64Array(string? fieldName, long[]? values)
        {
            if (TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    WriteInt64(null, values[i]);
                }
            }
        }

        /// <summary>
        /// Writes an unsigned long integer array to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="values">The array.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public void WriteUInt64Array(string? fieldName, ulong[]? values)
        {
            if (TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    WriteUInt64(null, values[i]);
                }
            }
        }

        /// <summary>
        /// Writes a floating point array to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="values">The array.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.3">OPC UA specification Part 6: Mappings, 5.2.2.3</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public void WriteFloatArray(string? fieldName, float[]? values)
        {
            if (TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    WriteFloat(null, values[i]);
                }
            }
        }

        /// <summary>
        /// Writes a double precision floating point array to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="values">The array.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.3">OPC UA specification Part 6: Mappings, 5.2.2.3</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public void WriteDoubleArray(string? fieldName, double[]? values)
        {
            if (TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    WriteDouble(null, values[i]);
                }
            }
        }

        /// <summary>
        /// Writes a string array to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="values">The array.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.4">OPC UA specification Part 6: Mappings, 5.2.2.4</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public void WriteStringArray(string? fieldName, string?[]? values)
        {
            if (TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    WriteString(null, values[i]);
                }
            }
        }

        /// <summary>
        /// Writes a date time array to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="values">The array.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.5">OPC UA specification Part 6: Mappings, 5.2.2.5</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public void WriteDateTimeArray(string? fieldName, DateTime[]? values)
        {
            if (TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    WriteDateTime(null, values[i]);
                }
            }
        }

        /// <summary>
        /// Writes a GUID array to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="values">The array.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.6">OPC UA specification Part 6: Mappings, 5.2.2.6</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public void WriteGuidArray(string? fieldName, Guid[]? values)
        {
            if (TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    WriteGuid(null, values[i]);
                }
            }
        }

        /// <summary>
        /// Writes a byte string array to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="values">The array.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.7">OPC UA specification Part 6: Mappings, 5.2.2.7</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public void WriteByteStringArray(string? fieldName, byte[]?[]? values)
        {
            if (TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    WriteByteString(null, values[i]);
                }
            }
        }

        /// <summary>
        /// Writes a XML element array to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="values">The array.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.8">OPC UA specification Part 6: Mappings, 5.2.2.8</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public void WriteXElementArray(string? fieldName, XElement?[]? values)
        {
            if (TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    WriteXElement(null, values[i]);
                }
            }
        }

        /// <summary>
        /// Writes a node id array to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="values">The array.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.9">OPC UA specification Part 6: Mappings, 5.2.2.9</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public void WriteNodeIdArray(string? fieldName, NodeId?[]? values)
        {
            if (TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    WriteNodeId(null, values[i]);
                }
            }
        }

        /// <summary>
        /// Writes an expanded node id array to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="values">The array.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.10">OPC UA specification Part 6: Mappings, 5.2.2.10</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public void WriteExpandedNodeIdArray(string? fieldName, ExpandedNodeId?[]? values)
        {
            if (TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    WriteExpandedNodeId(null, values[i]);
                }
            }
        }

        /// <summary>
        /// Writes a status code array to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="values">The array.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.11">OPC UA specification Part 6: Mappings, 5.2.2.11</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public void WriteStatusCodeArray(string? fieldName, StatusCode[]? values)
        {
            if (TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    WriteStatusCode(null, values[i]);
                }
            }
        }

        /// <summary>
        /// Writes a <see cref="DiagnosticInfo"/> object array to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="values">The array.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.12">OPC UA specification Part 6: Mappings, 5.2.2.12</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public void WriteDiagnosticInfoArray(string? fieldName, DiagnosticInfo?[]? values)
        {
            if (TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    WriteDiagnosticInfo(null, values[i]);
                }
            }
        }

        /// <summary>
        /// Writes a <see cref="QualifiedName"/> array to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="values">The array.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.13">OPC UA specification Part 6: Mappings, 5.2.2.13</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public void WriteQualifiedNameArray(string? fieldName, QualifiedName?[]? values)
        {
            if (TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    WriteQualifiedName(null, values[i]);
                }
            }
        }

        /// <summary>
        /// Writes a <see cref="LocalizedText"/> array to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="values">The array.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.14">OPC UA specification Part 6: Mappings, 5.2.2.14</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public void WriteLocalizedTextArray(string? fieldName, LocalizedText?[]? values)
        {
            if (TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    WriteLocalizedText(null, values[i]);
                }
            }
        }

        /// <summary>
        /// Writes a <see cref="Variant"/> array to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="values">The array.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.16">OPC UA specification Part 6: Mappings, 5.2.2.16</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public void WriteVariantArray(string? fieldName, Variant[]? values)
        {
            if (TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    WriteVariant(null, values[i]);
                }
            }
        }

        /// <summary>
        /// Writes a <see cref="DataValue"/> array to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="values">The array.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.17">OPC UA specification Part 6: Mappings, 5.2.2.17</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public void WriteDataValueArray(string? fieldName, DataValue?[]? values)
        {
            if (TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    WriteDataValue(null, values[i]);
                }
            }
        }

        /// <summary>
        /// Writes an <see cref="ExtensionObject"/> array to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="values">The array.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.15">OPC UA specification Part 6: Mappings, 5.2.2.15</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public void WriteExtensionObjectArray(string? fieldName, ExtensionObject?[]? values)
        {
            if (TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    WriteExtensionObject(null, values[i]);
                }
            }
        }

        /// <summary>
        /// Writes an <see cref="ExtensionObject"/> array to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="values">The array.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.15">OPC UA specification Part 6: Mappings, 5.2.2.15</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public void WriteExtensionObjectArray<T>(string? fieldName, T?[]? values)
            where T : class?, IEncodable
        {
            if (TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    WriteExtensionObject(null, values[i]);
                }
            }
        }

        /// <summary>
        /// Writes an <see cref="IEncodable"/> array to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="values">The array.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.6/">OPC UA specification Part 6: Mappings, 5.2.6</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public void WriteEncodableArray<T>(string? fieldName, T?[]? values)
            where T : class?, IEncodable
        {
            if (TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    WriteEncodable(null, values[i]);
                }
            }
        }

        /// <summary>
        /// Writes an enumeration array to the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <param name="values">The array.</param>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.4/">OPC UA specification Part 6: Mappings, 5.2.4</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public void WriteEnumerationArray<T>(string? fieldName, T[]? values)
            where T : struct, IConvertible
        {
            if (TryWriteArrayLength(values))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    WriteEnumeration(null, values[i]);
                }
            }
        }

        public void Write(byte[] buffer, int index, int count)
        {
            _writer.Write(buffer, index, count);
        }

        private bool TryWriteArrayLength<T>([NotNullWhen(returnValue: true)] T[]? values)
        {
            if (values == null)
            {
                WriteInt32(null, -1);
                return false;
            }

            WriteInt32(null, values.Length);
            return true;
        }

        private T[] FlattenArray<T>(Array a1)
        {
            return a1.Cast<T>().ToArray();
        }
    }
}