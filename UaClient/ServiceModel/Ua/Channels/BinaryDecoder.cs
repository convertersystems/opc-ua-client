// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Workstation.ServiceModel.Ua.Channels
{
    /// <summary>
    /// A decoder for the OPC UA Binary DataEncoding.
    /// </summary>
    /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.1/">OPC UA specification Part 6: Mappings, 5.2.1</seealso>
    public sealed class BinaryDecoder : IDecoder, IDisposable
    {
        private const long _minFileTime = 504911232000000000L;
        private const long _maxFileTime = 3155378975990000000L;
        private static readonly NodeId _readResponseNodeId = NodeId.Parse(ObjectIds.ReadResponse_Encoding_DefaultBinary);
        private static readonly NodeId _publishResponseNodeId = NodeId.Parse(ObjectIds.PublishResponse_Encoding_DefaultBinary);
        private readonly Stream _stream;
        private readonly IEncodingContext _context;
        private readonly Encoding _encoding;
        private readonly BinaryReader _reader;

        public BinaryDecoder(Stream stream, IEncodingContext? context = null, bool keepStreamOpen = false)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            _stream = stream;
            _context = context ?? new DefaultEncodingContext();
            _encoding = new UTF8Encoding(false, false);
            _reader = new BinaryReader(_stream, _encoding, keepStreamOpen);
        }

        public int Position
        {
            get { return (int)_stream.Position; }
            set { _stream.Position = value; }
        }

        public void Dispose()
        {
            if (_reader != null)
            {
                _reader.Dispose();
            }
        }

        public void PushNamespace(string namespaceUri)
        {
        }

        public void PopNamespace()
        {
        }

        /// <summary>
        /// Reads a boolean value from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.1">OPC UA specification Part 6: Mappings, 5.2.2.1</seealso>
        public bool ReadBoolean(string? fieldName)
        {
            return _reader.ReadBoolean();
        }

        /// <summary>
        /// Reads a signed byte value from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        public sbyte ReadSByte(string? fieldName)
        {
            return _reader.ReadSByte();
        }

        /// <summary>
        /// Reads an unsigned byte value from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        public byte ReadByte(string? fieldName)
        {
            return _reader.ReadByte();
        }

        /// <summary>
        /// Reads a signed short value from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        public short ReadInt16(string? fieldName)
        {
            return _reader.ReadInt16();
        }

        /// <summary>
        /// Reads an unsigned short value from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        public ushort ReadUInt16(string? fieldName)
        {
            return _reader.ReadUInt16();
        }

        /// <summary>
        /// Reads a signed integer value from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        public int ReadInt32(string? fieldName)
        {
            return _reader.ReadInt32();
        }

        /// <summary>
        /// Reads an unsigned integer value from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        public uint ReadUInt32(string? fieldName)
        {
            return _reader.ReadUInt32();
        }

        /// <summary>
        /// Reads a signed long integer value from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        public long ReadInt64(string? fieldName)
        {
            return _reader.ReadInt64();
        }

        /// <summary>
        /// Reads an unsigned long integer value from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        public ulong ReadUInt64(string? fieldName)
        {
            return _reader.ReadUInt64();
        }

        /// <summary>
        /// Reads a floating point value from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.3">OPC UA specification Part 6: Mappings, 5.2.2.3</seealso>
        public float ReadFloat(string? fieldName)
        {
            return _reader.ReadSingle();
        }

        /// <summary>
        /// Reads a double precision floating point value from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.3">OPC UA specification Part 6: Mappings, 5.2.2.3</seealso>
        public double ReadDouble(string? fieldName)
        {
            return _reader.ReadDouble();
        }

        /// <summary>
        /// Reads a string from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.4">OPC UA specification Part 6: Mappings, 5.2.2.4</seealso>
        public string? ReadString(string? fieldName)
        {
            var array = ReadByteString(fieldName);
            if (array == null)
            {
                return null;
            }

            return _encoding.GetString(array, 0, array.Length);
        }

        /// <summary>
        /// Reads a date time value from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.5">OPC UA specification Part 6: Mappings, 5.2.2.5</seealso>
        public DateTime ReadDateTime(string? fieldName)
        {
            long num = _reader.ReadInt64();

            if (num <= 0)
            {
                return DateTime.MinValue;
            }
            else if (num >= _maxFileTime - _minFileTime)
            {
                return DateTime.MaxValue;
            }

            return DateTime.FromFileTimeUtc(num);
        }

        /// <summary>
        /// Reads a GUID from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.6">OPC UA specification Part 6: Mappings, 5.2.2.6</seealso>
        public Guid ReadGuid(string? fieldName)
        {
            byte[] b = _reader.ReadBytes(16);
            return new Guid(b);
        }

        /// <summary>
        /// Reads a byte string from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.7">OPC UA specification Part 6: Mappings, 5.2.2.7</seealso>
        public byte[]? ReadByteString(string? fieldName)
        {
            int num = _reader.ReadInt32();
            if (num == -1)
            {
                return null;
            }

            return _reader.ReadBytes(num);
        }

        /// <summary>
        /// Reads a XML element from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.8">OPC UA specification Part 6: Mappings, 5.2.2.8</seealso>
        public XElement? ReadXElement(string? fieldName)
        {
            var array = ReadByteString(fieldName);
            if (array == null || array.Length == 0)
            {
                return null;
            }

            try
            {
                return XElement.Parse(_encoding.GetString(array, 0, array.Length).TrimEnd('\0'));
            }
            catch (XmlException)
            {
                return null;
            }
        }

        /// <summary>
        /// Reads a node id from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.9">OPC UA specification Part 6: Mappings, 5.2.2.9</seealso>
        public NodeId ReadNodeId(string? fieldName)
        {
            ushort ns = 0;
            byte b = _reader.ReadByte();
            switch (b)
            {
                case 0x00:
                    return new NodeId(_reader.ReadByte(), ns);

                case 0x01:
                    ns = _reader.ReadByte();
                    return new NodeId(_reader.ReadUInt16(), ns);

                case 0x02:
                    ns = _reader.ReadUInt16();
                    return new NodeId(_reader.ReadUInt32(), ns);

                case 0x03:
                    ns = _reader.ReadUInt16();
                    var str = ReadString(null);
                    if (str is null)
                    {
                        break;
                    }

                    return new NodeId(str, ns);

                case 0x04:
                    ns = _reader.ReadUInt16();
                    return new NodeId(ReadGuid(null), ns);

                case 0x05:
                    ns = _reader.ReadUInt16();
                    var bstr = ReadByteString(null);
                    if (bstr is null)
                    {
                        break;
                    }
                    return new NodeId(bstr, ns);

                default:
                    break;
            }

            throw new ServiceResultException(StatusCodes.BadDecodingError);
        }

        /// <summary>
        /// Reads an expanded node id from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.10">OPC UA specification Part 6: Mappings, 5.2.2.10</seealso>
        public ExpandedNodeId ReadExpandedNodeId(string? fieldName)
        {
            ushort ns = 0;
            NodeId? nodeId = null;
            string? nsu = null;
            uint svr = 0;
            byte b = _reader.ReadByte();
            switch (b & 0x0F)
            {
                case 0x00:
                    nodeId = new NodeId(_reader.ReadByte(), ns);
                    break;

                case 0x01:
                    ns = _reader.ReadByte();
                    nodeId = new NodeId(_reader.ReadUInt16(), ns);
                    break;

                case 0x02:
                    ns = _reader.ReadUInt16();
                    nodeId = new NodeId(_reader.ReadUInt32(), ns);
                    break;

                case 0x03:
                    ns = _reader.ReadUInt16();
                    if (ReadString(null) is { } str)
                    {
                        nodeId = new NodeId(str, ns);
                    }
                    break;

                case 0x04:
                    ns = _reader.ReadUInt16();
                    nodeId = new NodeId(ReadGuid(null), ns);
                    break;

                case 0x05:
                    ns = _reader.ReadUInt16();
                    if (ReadByteString(null) is { } bstr)
                    {
                        nodeId = new NodeId(bstr, ns);
                    }
                    break;

                default:
                    break;
            }

            if (nodeId is null)
            {
                throw new ServiceResultException(StatusCodes.BadDecodingError);
            }

            if ((b & 0x80) != 0)
            {
                nsu = ReadString(null);
            }

            if ((b & 0x40) != 0)
            {
                svr = ReadUInt32(null);
            }

            return new ExpandedNodeId(nodeId, nsu, svr);
        }

        /// <summary>
        /// Reads a status code from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.11">OPC UA specification Part 6: Mappings, 5.2.2.11</seealso>
        public StatusCode ReadStatusCode(string? fieldName)
        {
            return ReadUInt32(fieldName);
        }

        /// <summary>
        /// Reads a <see cref="DiagnosticInfo"/> object from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.12">OPC UA specification Part 6: Mappings, 5.2.2.12</seealso>
        public DiagnosticInfo ReadDiagnosticInfo(string? fieldName)
        {
            int symbolicId = -1;
            int namespaceUri = -1;
            int locale = -1;
            int localizedText = -1;
            string? additionalInfo = null;
            StatusCode innerStatusCode = default(StatusCode);
            DiagnosticInfo? innerDiagnosticInfo = default(DiagnosticInfo);
            byte b = _reader.ReadByte();
            if ((b & 1) != 0)
            {
                symbolicId = ReadInt32(null);
            }

            if ((b & 2) != 0)
            {
                namespaceUri = ReadInt32(null);
            }

            if ((b & 8) != 0)
            {
                locale = ReadInt32(null);
            }

            if ((b & 4) != 0)
            {
                localizedText = ReadInt32(null);
            }

            if ((b & 16) != 0)
            {
                additionalInfo = ReadString(null);
            }

            if ((b & 32) != 0)
            {
                innerStatusCode = ReadStatusCode(null);
            }

            if ((b & 64) != 0)
            {
                innerDiagnosticInfo = ReadDiagnosticInfo(null);
            }

            return new DiagnosticInfo(namespaceUri, symbolicId, locale, localizedText, additionalInfo, innerStatusCode, innerDiagnosticInfo);
        }

        /// <summary>
        /// Reads a <see cref="QualifiedName"/> object from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.13">OPC UA specification Part 6: Mappings, 5.2.2.13</seealso>
        public QualifiedName ReadQualifiedName(string? fieldName)
        {
            ushort ns = ReadUInt16(null);
            string? name = ReadString(null);
            return new QualifiedName(name, ns);
        }

        /// <summary>
        /// Reads a <see cref="LocalizedText"/> from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.14">OPC UA specification Part 6: Mappings, 5.2.2.14</seealso>
        public LocalizedText ReadLocalizedText(string? fieldName)
        {
            string? text = null;
            string? locale = null;
            byte b = _reader.ReadByte();
            if ((b & 1) != 0)
            {
                locale = ReadString(null);
            }

            if ((b & 2) != 0)
            {
                text = ReadString(null);
            }

            return new LocalizedText(text, locale);
        }

        /// <summary>
        /// Reads a <see cref="Variant"/> value from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.16">OPC UA specification Part 6: Mappings, 5.2.2.16</seealso>
        public Variant ReadVariant(string? fieldName)
        {
            byte b = _reader.ReadByte();
            var type = (VariantType)(b & 0x3F);

            if ((b & 0x80) == 0)
            {
                switch (type)
                {
                    case VariantType.Null:
                        return Variant.Null;

                    case VariantType.Boolean:
                        return new Variant(ReadBoolean(null));

                    case VariantType.SByte:
                        return new Variant(ReadSByte(null));

                    case VariantType.Byte:
                        return new Variant(ReadByte(null));

                    case VariantType.Int16:
                        return new Variant(ReadInt16(null));

                    case VariantType.UInt16:
                        return new Variant(ReadUInt16(null));

                    case VariantType.Int32:
                        return new Variant(ReadInt32(null));

                    case VariantType.UInt32:
                        return new Variant(ReadUInt32(null));

                    case VariantType.Int64:
                        return new Variant(ReadInt64(null));

                    case VariantType.UInt64:
                        return new Variant(ReadUInt64(null));

                    case VariantType.Float:
                        return new Variant(ReadFloat(null));

                    case VariantType.Double:
                        return new Variant(ReadDouble(null));

                    case VariantType.String:
                        return new Variant(ReadString(null));

                    case VariantType.DateTime:
                        return new Variant(ReadDateTime(null));

                    case VariantType.Guid:
                        return new Variant(ReadGuid(null));

                    case VariantType.ByteString:
                        return new Variant(ReadByteString(null));

                    case VariantType.XmlElement:
                        return new Variant(ReadXElement(null));

                    case VariantType.NodeId:
                        return new Variant(ReadNodeId(null));

                    case VariantType.ExpandedNodeId:
                        return new Variant(ReadExpandedNodeId(null));

                    case VariantType.StatusCode:
                        return new Variant(ReadStatusCode(null));

                    case VariantType.QualifiedName:
                        return new Variant(ReadQualifiedName(null));

                    case VariantType.LocalizedText:
                        return new Variant(ReadLocalizedText(null));

                    case VariantType.ExtensionObject:
                        return new Variant(ReadExtensionObject(null));

                    default:
                        throw new ServiceResultException(StatusCodes.BadDecodingError);
                }
            }

            if ((b & 0x40) == 0)
            {
                switch (type)
                {
                    case VariantType.Null:
                        return Variant.Null;

                    case VariantType.Boolean:
                        return new Variant(ReadBooleanArray(null));

                    case VariantType.SByte:
                        return new Variant(ReadSByteArray(null));

                    case VariantType.Byte:
                        return new Variant(ReadByteArray(null));

                    case VariantType.Int16:
                        return new Variant(ReadInt16Array(null));

                    case VariantType.UInt16:
                        return new Variant(ReadUInt16Array(null));

                    case VariantType.Int32:
                        return new Variant(ReadInt32Array(null));

                    case VariantType.UInt32:
                        return new Variant(ReadUInt32Array(null));

                    case VariantType.Int64:
                        return new Variant(ReadInt64Array(null));

                    case VariantType.UInt64:
                        return new Variant(ReadUInt64Array(null));

                    case VariantType.Float:
                        return new Variant(ReadFloatArray(null));

                    case VariantType.Double:
                        return new Variant(ReadDoubleArray(null));

                    case VariantType.String:
                        return new Variant(ReadStringArray(null));

                    case VariantType.DateTime:
                        return new Variant(ReadDateTimeArray(null));

                    case VariantType.Guid:
                        return new Variant(ReadGuidArray(null));

                    case VariantType.ByteString:
                        return new Variant(ReadByteStringArray(null));

                    case VariantType.XmlElement:
                        return new Variant(ReadXElementArray(null));

                    case VariantType.NodeId:
                        return new Variant(ReadNodeIdArray(null));

                    case VariantType.ExpandedNodeId:
                        return new Variant(ReadExpandedNodeIdArray(null));

                    case VariantType.StatusCode:
                        return new Variant(ReadStatusCodeArray(null));

                    case VariantType.QualifiedName:
                        return new Variant(ReadQualifiedNameArray(null));

                    case VariantType.LocalizedText:
                        return new Variant(ReadLocalizedTextArray(null));

                    case VariantType.ExtensionObject:
                        return new Variant(ReadExtensionObjectArray(null));

                    case VariantType.Variant:
                        return new Variant(ReadVariantArray(null));

                    default:
                        throw new ServiceResultException(StatusCodes.BadDecodingError);
                }
            }
            else
            {
                switch (type)
                {
                    case VariantType.Null:
                        return Variant.Null;

                    case VariantType.Boolean:
                        return new Variant(BuildArray(ReadBooleanArray(null), ReadInt32Array(null)));

                    case VariantType.SByte:
                        return new Variant(BuildArray(ReadSByteArray(null), ReadInt32Array(null)));

                    case VariantType.Byte:
                        return new Variant(BuildArray(ReadByteArray(null), ReadInt32Array(null)));

                    case VariantType.Int16:
                        return new Variant(BuildArray(ReadInt16Array(null), ReadInt32Array(null)));

                    case VariantType.UInt16:
                        return new Variant(BuildArray(ReadUInt16Array(null), ReadInt32Array(null)));

                    case VariantType.Int32:
                        return new Variant(BuildArray(ReadInt32Array(null), ReadInt32Array(null)));

                    case VariantType.UInt32:
                        return new Variant(BuildArray(ReadUInt32Array(null), ReadInt32Array(null)));

                    case VariantType.Int64:
                        return new Variant(BuildArray(ReadInt64Array(null), ReadInt32Array(null)));

                    case VariantType.UInt64:
                        return new Variant(BuildArray(ReadUInt64Array(null), ReadInt32Array(null)));

                    case VariantType.Float:
                        return new Variant(BuildArray(ReadFloatArray(null), ReadInt32Array(null)));

                    case VariantType.Double:
                        return new Variant(BuildArray(ReadDoubleArray(null), ReadInt32Array(null)));

                    case VariantType.String:
                        return new Variant(BuildArray(ReadStringArray(null), ReadInt32Array(null)));

                    case VariantType.DateTime:
                        return new Variant(BuildArray(ReadDateTimeArray(null), ReadInt32Array(null)));

                    case VariantType.Guid:
                        return new Variant(BuildArray(ReadGuidArray(null), ReadInt32Array(null)));

                    case VariantType.ByteString:
                        return new Variant(BuildArray(ReadByteStringArray(null), ReadInt32Array(null)));

                    case VariantType.XmlElement:
                        return new Variant(BuildArray(ReadXElementArray(null), ReadInt32Array(null)));

                    case VariantType.NodeId:
                        return new Variant(BuildArray(ReadNodeIdArray(null), ReadInt32Array(null)));

                    case VariantType.ExpandedNodeId:
                        return new Variant(BuildArray(ReadExpandedNodeIdArray(null), ReadInt32Array(null)));

                    case VariantType.StatusCode:
                        return new Variant(BuildArray(ReadStatusCodeArray(null), ReadInt32Array(null)));

                    case VariantType.QualifiedName:
                        return new Variant(BuildArray(ReadQualifiedNameArray(null), ReadInt32Array(null)));

                    case VariantType.LocalizedText:
                        return new Variant(BuildArray(ReadLocalizedTextArray(null), ReadInt32Array(null)));

                    case VariantType.ExtensionObject:
                        return new Variant(BuildArray(ReadExtensionObjectArray(null), ReadInt32Array(null)));

                    case VariantType.Variant:
                        return new Variant(BuildArray(ReadVariantArray(null), ReadInt32Array(null)));

                    default:
                        throw new ServiceResultException(StatusCodes.BadDecodingError);
                }
            }
        }

        /// <summary>
        /// Reads a <see cref="DataValue"/> from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.17">OPC UA specification Part 6: Mappings, 5.2.2.17</seealso>
        public DataValue ReadDataValue(string? fieldName)
        {
            Variant variant = Variant.Null;
            StatusCode statusCode = 0;
            DateTime sourceTimestamp = DateTime.MinValue;
            ushort sourcePicoseconds = 0;
            DateTime serverTimestamp = DateTime.MinValue;
            ushort serverPicoseconds = 0;
            byte b = _reader.ReadByte();
            if ((b & 1) != 0)
            {
                variant = ReadVariant(null);
            }

            if ((b & 2) != 0)
            {
                statusCode = ReadStatusCode(null);
            }

            if ((b & 4) != 0)
            {
                sourceTimestamp = ReadDateTime(null);
            }

            if ((b & 16) != 0)
            {
                sourcePicoseconds = ReadUInt16(null);
            }

            if ((b & 8) != 0)
            {
                serverTimestamp = ReadDateTime(null);
            }

            if ((b & 32) != 0)
            {
                serverPicoseconds = ReadUInt16(null);
            }

            return new DataValue(variant, statusCode, sourceTimestamp, sourcePicoseconds, serverTimestamp, serverPicoseconds);
        }

        /// <summary>
        /// Reads a <see cref="ExtensionObject"/> from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.15">OPC UA specification Part 6: Mappings, 5.2.2.15</seealso>
        public ExtensionObject? ReadExtensionObject(string? fieldName)
        {
            try
            {
                NodeId nodeId = ReadNodeId(null);
                byte b = _reader.ReadByte();
                if (b == (byte)BodyType.ByteString) // BodyType Encodable is encoded as ByteString.
                {
                    ExpandedNodeId binaryEncodingId = NodeId.ToExpandedNodeId(nodeId, _context.NamespaceUris);

                    if (TypeLibrary.TryGetTypeFromBinaryEncodingId(binaryEncodingId, out var type))
                    {
                        _ = ReadInt32(null);
                        var encodable = (IEncodable)Activator.CreateInstance(type)!;
                        encodable.Decode(this);
                        return new ExtensionObject(encodable, binaryEncodingId);
                    }

                    return new ExtensionObject(ReadByteString(null), binaryEncodingId);
                }

                if (b == (byte)BodyType.XmlElement)
                {
                    ExpandedNodeId xmlEncodingId = NodeId.ToExpandedNodeId(nodeId, _context.NamespaceUris);
                    return new ExtensionObject(ReadXElement(null), xmlEncodingId);
                }

                return null;

            }
            catch
            {
                throw new ServiceResultException(StatusCodes.BadDecodingError);
            }
        }

        /// <summary>
        /// Reads a <see cref="ExtensionObject"/> from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.15">OPC UA specification Part 6: Mappings, 5.2.2.15</seealso>
        public T? ReadExtensionObject<T>(string? fieldName)
            where T : class, IEncodable
        {
            try
            {
                NodeId nodeId = ReadNodeId(null);
                byte b = _reader.ReadByte();
                if (b == (byte)BodyType.ByteString)
                {
                    ExpandedNodeId binaryEncodingId = NodeId.ToExpandedNodeId(nodeId, _context.NamespaceUris);

                    if (!TypeLibrary.TryGetTypeFromBinaryEncodingId(binaryEncodingId, out var type))
                    {
                        throw new ServiceResultException(StatusCodes.BadDecodingError);
                    }

                    _ = ReadInt32(null);
                    var encodable = (IEncodable)Activator.CreateInstance(type)!;
                    encodable.Decode(this);
                    return (T)encodable;
                }

                // TODO: else if (b = 2) use XmlDecoder

                return default(T);

            }
            catch
            {
                throw new ServiceResultException(StatusCodes.BadDecodingError);
            }
        }

        /// <summary>
        /// Reads a <see cref="IEncodable"/> object from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.6/">OPC UA specification Part 6: Mappings, 5.2.6</seealso>
        public T ReadEncodable<T>(string? fieldName)
            where T : class, IEncodable
        {
            var value = Activator.CreateInstance<T>();
            value.Decode(this);
            return value;
        }

        /// <summary>
        /// Reads a <see cref="IServiceResponse"/> from the stream.
        /// </summary>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.9/">OPC UA specification Part 6: Mappings, 5.2.9</seealso>
        public IServiceResponse ReadResponse()
        {
            try
            {
                IServiceResponse value;
                NodeId nodeId = ReadNodeId(null);
                // fast path
                if (nodeId == _publishResponseNodeId)
                {
                    value = new PublishResponse();
                }
                else if (nodeId == _readResponseNodeId)
                {
                    value = new ReadResponse();
                }
                else
                {
                    if (!TypeLibrary.TryGetTypeFromBinaryEncodingId(NodeId.ToExpandedNodeId(nodeId, _context.NamespaceUris), out var type))
                    {
                        throw new ServiceResultException(StatusCodes.BadEncodingError);
                    }
                    value = (IServiceResponse)Activator.CreateInstance(type)!;
                }
                value.Decode(this);
                return value;
            }
            catch
            {
                throw new ServiceResultException(StatusCodes.BadEncodingError);
            }        
        }


        /// <summary>
        /// Reads an enumeration value from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.4/">OPC UA specification Part 6: Mappings, 5.2.4</seealso>
        public T ReadEnumeration<T>(string? fieldName)
            where T : struct, IConvertible
        {
            return (T)Enum.ToObject(typeof(T), ReadInt32(null));
        }

        /// <summary>
        /// Reads a boolean value array from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.1">OPC UA specification Part 6: Mappings, 5.2.2.1</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public bool[]? ReadBooleanArray(string? fieldNames)
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new bool[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadBoolean(null);
            }

            return list;
        }

        /// <summary>
        /// Reads a signed byte array from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public sbyte[]? ReadSByteArray(string? fieldName)
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new sbyte[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadSByte(null);
            }

            return list;
        }

        /// <summary>
        /// Reads an unsigned byte array from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public byte[]? ReadByteArray(string? fieldName)
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new byte[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadByte(null);
            }

            return list;
        }

        /// <summary>
        /// Reads a signed short array from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public short[]? ReadInt16Array(string? fieldName)
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new short[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadInt16(null);
            }

            return list;
        }

        /// <summary>
        /// Reads an unsigned short array from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public ushort[]? ReadUInt16Array(string? fieldName)
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new ushort[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadUInt16(null);
            }

            return list;
        }

        /// <summary>
        /// Reads a signed integer array from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public int[]? ReadInt32Array(string? fieldName)
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new int[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadInt32(null);
            }

            return list;
        }

        /// <summary>
        /// Reads an unsigned integer array from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public uint[]? ReadUInt32Array(string? fieldName)
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new uint[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadUInt32(null);
            }

            return list;
        }

        /// <summary>
        /// Reads a signed long integer array from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public long[]? ReadInt64Array(string? fieldName)
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new long[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadInt64(null);
            }

            return list;
        }

        /// <summary>
        /// Reads an unsigned long integer array from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public ulong[]? ReadUInt64Array(string? fieldName)
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new ulong[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadUInt64(null);
            }

            return list;
        }

        /// <summary>
        /// Reads a floating point array from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.3">OPC UA specification Part 6: Mappings, 5.2.2.3</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public float[]? ReadFloatArray(string? fieldName)
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new float[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadFloat(null);
            }

            return list;
        }

        /// <summary>
        /// Reads a double percision floating point array from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.3">OPC UA specification Part 6: Mappings, 5.2.2.3</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public double[]? ReadDoubleArray(string? fieldName)
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new double[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadDouble(null);
            }

            return list;
        }

        /// <summary>
        /// Reads a string array from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.4">OPC UA specification Part 6: Mappings, 5.2.2.4</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public string?[]? ReadStringArray(string? fieldName)
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new string?[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadString(null);
            }

            return list;
        }

        /// <summary>
        /// Reads a date time array from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.5">OPC UA specification Part 6: Mappings, 5.2.2.5</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public DateTime[]? ReadDateTimeArray(string? fieldName)
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new DateTime[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadDateTime(null);
            }

            return list;
        }

        /// <summary>
        /// Reads a GUID array from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.6">OPC UA specification Part 6: Mappings, 5.2.2.6</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public Guid[]? ReadGuidArray(string? fieldName)
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new Guid[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadGuid(null);
            }

            return list;
        }

        /// <summary>
        /// Reads a byte string array from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.7">OPC UA specification Part 6: Mappings, 5.2.2.7</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public byte[]?[]? ReadByteStringArray(string? fieldName)
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            byte[]?[] list = new byte[num][];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadByteString(null);
            }

            return list;
        }

        /// <summary>
        /// Reads a XML element array from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.8">OPC UA specification Part 6: Mappings, 5.2.2.8</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public XElement?[]? ReadXElementArray(string? fieldName)
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new XElement?[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadXElement(null);
            }

            return list;
        }

        /// <summary>
        /// Reads a node id array from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.9">OPC UA specification Part 6: Mappings, 5.2.2.9</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public NodeId[]? ReadNodeIdArray(string? fieldName)
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new NodeId[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadNodeId(null);
            }

            return list;
        }

        /// <summary>
        /// Reads an expanded node id array from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.10">OPC UA specification Part 6: Mappings, 5.2.2.10</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public ExpandedNodeId[]? ReadExpandedNodeIdArray(string? fieldName)
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new ExpandedNodeId[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadExpandedNodeId(null);
            }

            return list;
        }

        /// <summary>
        /// Reads a status code array from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.11">OPC UA specification Part 6: Mappings, 5.2.2.11</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public StatusCode[]? ReadStatusCodeArray(string? fieldName)
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new StatusCode[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadStatusCode(null);
            }

            return list;
        }

        /// <summary>
        /// Reads a <see cref="DiagnosticInfo"/> object array from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.12">OPC UA specification Part 6: Mappings, 5.2.2.12</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public DiagnosticInfo[]? ReadDiagnosticInfoArray(string? fieldName)
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new DiagnosticInfo[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadDiagnosticInfo(null);
            }

            return list;
        }

        /// <summary>
        /// Reads a <see cref="QualifiedName"/> array from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.13">OPC UA specification Part 6: Mappings, 5.2.2.13</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public QualifiedName[]? ReadQualifiedNameArray(string? fieldName)
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new QualifiedName[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadQualifiedName(null);
            }

            return list;
        }

        /// <summary>
        /// Reads a <see cref="LocalizedText"/> array from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.14">OPC UA specification Part 6: Mappings, 5.2.2.14</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public LocalizedText[]? ReadLocalizedTextArray(string? fieldName)
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new LocalizedText[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadLocalizedText(null);
            }

            return list;
        }

        /// <summary>
        /// Reads a <see cref="Variant"/> array from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.16">OPC UA specification Part 6: Mappings, 5.2.2.16</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public Variant[]? ReadVariantArray(string? fieldName)
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new Variant[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadVariant(null);
            }

            return list;
        }

        /// <summary>
        /// Reads a <see cref="DataValue"/> array from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.17">OPC UA specification Part 6: Mappings, 5.2.2.17</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public DataValue[]? ReadDataValueArray(string? fieldName)
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new DataValue[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadDataValue(null);
            }

            return list;
        }

        /// <summary>
        /// Reads an <see cref="ExtensionObject"/> array from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.15">OPC UA specification Part 6: Mappings, 5.2.2.15</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public ExtensionObject?[]? ReadExtensionObjectArray(string? fieldName)
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new ExtensionObject?[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadExtensionObject(null);
            }

            return list;
        }

        /// <summary>
        /// Reads an <see cref="ExtensionObject"/> array from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.15">OPC UA specification Part 6: Mappings, 5.2.2.15</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public T?[]? ReadExtensionObjectArray<T>(string? fieldName)
            where T : class, IEncodable
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new T?[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadExtensionObject<T>(null);
            }

            return list;
        }

        /// <summary>
        /// Reads an <see cref="IEncodable"/> array from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.6/">OPC UA specification Part 6: Mappings, 5.2.6</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public T[]? ReadEncodableArray<T>(string? fieldName)
            where T : class, IEncodable
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new T[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadEncodable<T>(null);
            }

            return list;
        }

        /// <summary>
        /// Reads an enumeration array from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.4/">OPC UA specification Part 6: Mappings, 5.2.4</seealso>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.5/">OPC UA specification Part 6: Mappings, 5.2.5</seealso>
        public T[]? ReadEnumerationArray<T>(string? fieldName)
            where T : struct, IConvertible
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new T[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadEnumeration<T>(null);
            }

            return list;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return _reader.Read(buffer, offset, count);
        }

        private int ReadArrayLength()
        {
            int num = _reader.ReadInt32();
            return num;
        }

        private Array? BuildArray<T>(T[]? a1, int[]? dims)
        {
            if (a1 is null || dims is null)
            {
                return default(Array);
            }

            var indices = new int[dims.Length];
            var a2 = Array.CreateInstance(typeof(T), dims);
            foreach (var value in a1)
            {
                a2.SetValue(value, indices);
                for (int i = indices.Length - 1; i >= 0; i--)
                {
                    indices[i]++;
                    if (indices[i] == dims[i])
                    {
                        indices[i] = 0;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return a2;
        }
    }
}