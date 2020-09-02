// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
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
        private const long MinFileTime =  504911232000000000L;
        private const long MaxFileTime = 3155378975990000000L;
        private readonly Stream stream;
        private readonly UaTcpSecureChannel? channel;
        private readonly Encoding encoding;
        private readonly BinaryReader reader;

        public BinaryDecoder(Stream stream, UaTcpSecureChannel? channel = null, bool keepStreamOpen = false)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            this.stream = stream;
            this.channel = channel;
            this.encoding = new UTF8Encoding(false, false);
            this.reader = new BinaryReader(this.stream, this.encoding, keepStreamOpen);
        }

        public int Position
        {
            get { return (int)this.stream.Position; }
            set { this.stream.Position = value; }
        }

        public void Dispose()
        {
            if (this.reader != null)
            {
                this.reader.Dispose();
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
            return this.reader.ReadBoolean();
        }

        /// <summary>
        /// Reads a signed byte value from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        public sbyte ReadSByte(string? fieldName)
        {
            return this.reader.ReadSByte();
        }

        /// <summary>
        /// Reads an unsigned byte value from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        public byte ReadByte(string? fieldName)
        {
            return this.reader.ReadByte();
        }

        /// <summary>
        /// Reads a signed short value from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        public short ReadInt16(string? fieldName)
        {
            return this.reader.ReadInt16();
        }

        /// <summary>
        /// Reads an unsigned short value from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        public ushort ReadUInt16(string? fieldName)
        {
            return this.reader.ReadUInt16();
        }

        /// <summary>
        /// Reads a signed integer value from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        public int ReadInt32(string? fieldName)
        {
            return this.reader.ReadInt32();
        }

        /// <summary>
        /// Reads an unsigned integer value from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        public uint ReadUInt32(string? fieldName)
        {
            return this.reader.ReadUInt32();
        }

        /// <summary>
        /// Reads a signed long integer value from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        public long ReadInt64(string? fieldName)
        {
            return this.reader.ReadInt64();
        }

        /// <summary>
        /// Reads an unsigned long integer value from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.2">OPC UA specification Part 6: Mappings, 5.2.2.2</seealso>
        public ulong ReadUInt64(string? fieldName)
        {
            return this.reader.ReadUInt64();
        }

        /// <summary>
        /// Reads a floating point value from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.3">OPC UA specification Part 6: Mappings, 5.2.2.3</seealso>
        public float ReadFloat(string? fieldName)
        {
            return this.reader.ReadSingle();
        }

        /// <summary>
        /// Reads a double precision floating point value from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.3">OPC UA specification Part 6: Mappings, 5.2.2.3</seealso>
        public double ReadDouble(string? fieldName)
        {
            return this.reader.ReadDouble();
        }

        /// <summary>
        /// Reads a string from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.4">OPC UA specification Part 6: Mappings, 5.2.2.4</seealso>
        public string? ReadString(string? fieldName)
        {
            var array = this.ReadByteString(fieldName);
            if (array == null)
            {
                return null;
            }

            return this.encoding.GetString(array, 0, array.Length);
        }

        /// <summary>
        /// Reads a date time value from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.5">OPC UA specification Part 6: Mappings, 5.2.2.5</seealso>
        public DateTime ReadDateTime(string? fieldName)
        {
            long num = this.reader.ReadInt64();

            if (num <= 0)
            {
                return DateTime.MinValue;
            }
            else if (num >= MaxFileTime - MinFileTime)
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
            byte[] b = this.reader.ReadBytes(16);
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
            int num = this.reader.ReadInt32();
            if (num == -1)
            {
                return null;
            }

            return this.reader.ReadBytes(num);
        }

        /// <summary>
        /// Reads a XML element from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.2/#5.2.2.8">OPC UA specification Part 6: Mappings, 5.2.2.8</seealso>
        public XElement? ReadXElement(string? fieldName)
        {
            var array = this.ReadByteString(fieldName);
            if (array == null || array.Length == 0)
            {
                return null;
            }

            try
            {
                return XElement.Parse(this.encoding.GetString(array, 0, array.Length).TrimEnd('\0'));
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
            byte b = this.reader.ReadByte();
            switch (b)
            {
                case 0x00:
                    return new NodeId(this.reader.ReadByte(), ns);

                case 0x01:
                    ns = this.reader.ReadByte();
                    return new NodeId(this.reader.ReadUInt16(), ns);

                case 0x02:
                    ns = this.reader.ReadUInt16();
                    return new NodeId(this.reader.ReadUInt32(), ns);

                case 0x03:
                    ns = this.reader.ReadUInt16();
                    var str = this.ReadString(null);
                    if (str is null)
                    {
                        break;
                    }

                    return new NodeId(str, ns);

                case 0x04:
                    ns = this.reader.ReadUInt16();
                    return new NodeId(this.ReadGuid(null), ns);

                case 0x05:
                    ns = this.reader.ReadUInt16();
                    var bstr = this.ReadByteString(null);
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
            byte b = this.reader.ReadByte();
            switch (b & 0x0F)
            {
                case 0x00:
                    nodeId = new NodeId(this.reader.ReadByte(), ns);
                    break;

                case 0x01:
                    ns = this.reader.ReadByte();
                    nodeId = new NodeId(this.reader.ReadUInt16(), ns);
                    break;

                case 0x02:
                    ns = this.reader.ReadUInt16();
                    nodeId = new NodeId(this.reader.ReadUInt32(), ns);
                    break;

                case 0x03:
                    ns = this.reader.ReadUInt16();
                    if (this.ReadString(null) is { } str)
                    {
                        nodeId = new NodeId(str, ns);
                    }
                    break;

                case 0x04:
                    ns = this.reader.ReadUInt16();
                    nodeId = new NodeId(this.ReadGuid(null), ns);
                    break;

                case 0x05:
                    ns = this.reader.ReadUInt16();
                    if (this.ReadByteString(null) is { } bstr)
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
                nsu = this.ReadString(null);
            }

            if ((b & 0x40) != 0)
            {
                svr = this.ReadUInt32(null);
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
            return this.ReadUInt32(fieldName);
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
            byte b = this.reader.ReadByte();
            if ((b & 1) != 0)
            {
                symbolicId = this.ReadInt32(null);
            }

            if ((b & 2) != 0)
            {
                namespaceUri = this.ReadInt32(null);
            }

            if ((b & 8) != 0)
            {
                locale = this.ReadInt32(null);
            }

            if ((b & 4) != 0)
            {
                localizedText = this.ReadInt32(null);
            }

            if ((b & 16) != 0)
            {
                additionalInfo = this.ReadString(null);
            }

            if ((b & 32) != 0)
            {
                innerStatusCode = this.ReadStatusCode(null);
            }

            if ((b & 64) != 0)
            {
                innerDiagnosticInfo = this.ReadDiagnosticInfo(null);
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
            ushort ns = this.ReadUInt16(null);
            string? name = this.ReadString(null);
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
            byte b = this.reader.ReadByte();
            if ((b & 1) != 0)
            {
                locale = this.ReadString(null);
            }

            if ((b & 2) != 0)
            {
                text = this.ReadString(null);
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
            byte b = this.reader.ReadByte();
            var type = (VariantType)(b & 0x3F);

            if ((b & 0x80) == 0)
            {
                switch (type)
                {
                    case VariantType.Null:
                        return Variant.Null;

                    case VariantType.Boolean:
                        return new Variant(this.ReadBoolean(null));

                    case VariantType.SByte:
                        return new Variant(this.ReadSByte(null));

                    case VariantType.Byte:
                        return new Variant(this.ReadByte(null));

                    case VariantType.Int16:
                        return new Variant(this.ReadInt16(null));

                    case VariantType.UInt16:
                        return new Variant(this.ReadUInt16(null));

                    case VariantType.Int32:
                        return new Variant(this.ReadInt32(null));

                    case VariantType.UInt32:
                        return new Variant(this.ReadUInt32(null));

                    case VariantType.Int64:
                        return new Variant(this.ReadInt64(null));

                    case VariantType.UInt64:
                        return new Variant(this.ReadUInt64(null));

                    case VariantType.Float:
                        return new Variant(this.ReadFloat(null));

                    case VariantType.Double:
                        return new Variant(this.ReadDouble(null));

                    case VariantType.String:
                        return new Variant(this.ReadString(null));

                    case VariantType.DateTime:
                        return new Variant(this.ReadDateTime(null));

                    case VariantType.Guid:
                        return new Variant(this.ReadGuid(null));

                    case VariantType.ByteString:
                        return new Variant(this.ReadByteString(null));

                    case VariantType.XmlElement:
                        return new Variant(this.ReadXElement(null));

                    case VariantType.NodeId:
                        return new Variant(this.ReadNodeId(null));

                    case VariantType.ExpandedNodeId:
                        return new Variant(this.ReadExpandedNodeId(null));

                    case VariantType.StatusCode:
                        return new Variant(this.ReadStatusCode(null));

                    case VariantType.QualifiedName:
                        return new Variant(this.ReadQualifiedName(null));

                    case VariantType.LocalizedText:
                        return new Variant(this.ReadLocalizedText(null));

                    case VariantType.ExtensionObject:
                        return new Variant(this.ReadExtensionObject(null));

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
                        return new Variant(this.ReadBooleanArray(null));

                    case VariantType.SByte:
                        return new Variant(this.ReadSByteArray(null));

                    case VariantType.Byte:
                        return new Variant(this.ReadByteArray(null));

                    case VariantType.Int16:
                        return new Variant(this.ReadInt16Array(null));

                    case VariantType.UInt16:
                        return new Variant(this.ReadUInt16Array(null));

                    case VariantType.Int32:
                        return new Variant(this.ReadInt32Array(null));

                    case VariantType.UInt32:
                        return new Variant(this.ReadUInt32Array(null));

                    case VariantType.Int64:
                        return new Variant(this.ReadInt64Array(null));

                    case VariantType.UInt64:
                        return new Variant(this.ReadUInt64Array(null));

                    case VariantType.Float:
                        return new Variant(this.ReadFloatArray(null));

                    case VariantType.Double:
                        return new Variant(this.ReadDoubleArray(null));

                    case VariantType.String:
                        return new Variant(this.ReadStringArray(null));

                    case VariantType.DateTime:
                        return new Variant(this.ReadDateTimeArray(null));

                    case VariantType.Guid:
                        return new Variant(this.ReadGuidArray(null));

                    case VariantType.ByteString:
                        return new Variant(this.ReadByteStringArray(null));

                    case VariantType.XmlElement:
                        return new Variant(this.ReadXElementArray(null));

                    case VariantType.NodeId:
                        return new Variant(this.ReadNodeIdArray(null));

                    case VariantType.ExpandedNodeId:
                        return new Variant(this.ReadExpandedNodeIdArray(null));

                    case VariantType.StatusCode:
                        return new Variant(this.ReadStatusCodeArray(null));

                    case VariantType.QualifiedName:
                        return new Variant(this.ReadQualifiedNameArray(null));

                    case VariantType.LocalizedText:
                        return new Variant(this.ReadLocalizedTextArray(null));

                    case VariantType.ExtensionObject:
                        return new Variant(this.ReadExtensionObjectArray(null));

                    case VariantType.Variant:
                        return new Variant(this.ReadVariantArray(null));

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
                        return new Variant(this.BuildArray(this.ReadBooleanArray(null), this.ReadInt32Array(null)));

                    case VariantType.SByte:
                        return new Variant(this.BuildArray(this.ReadSByteArray(null), this.ReadInt32Array(null)));

                    case VariantType.Byte:
                        return new Variant(this.BuildArray(this.ReadByteArray(null), this.ReadInt32Array(null)));

                    case VariantType.Int16:
                        return new Variant(this.BuildArray(this.ReadInt16Array(null), this.ReadInt32Array(null)));

                    case VariantType.UInt16:
                        return new Variant(this.BuildArray(this.ReadUInt16Array(null), this.ReadInt32Array(null)));

                    case VariantType.Int32:
                        return new Variant(this.BuildArray(this.ReadInt32Array(null), this.ReadInt32Array(null)));

                    case VariantType.UInt32:
                        return new Variant(this.BuildArray(this.ReadUInt32Array(null), this.ReadInt32Array(null)));

                    case VariantType.Int64:
                        return new Variant(this.BuildArray(this.ReadInt64Array(null), this.ReadInt32Array(null)));

                    case VariantType.UInt64:
                        return new Variant(this.BuildArray(this.ReadUInt64Array(null), this.ReadInt32Array(null)));

                    case VariantType.Float:
                        return new Variant(this.BuildArray(this.ReadFloatArray(null), this.ReadInt32Array(null)));

                    case VariantType.Double:
                        return new Variant(this.BuildArray(this.ReadDoubleArray(null), this.ReadInt32Array(null)));

                    case VariantType.String:
                        return new Variant(this.BuildArray(this.ReadStringArray(null), this.ReadInt32Array(null)));

                    case VariantType.DateTime:
                        return new Variant(this.BuildArray(this.ReadDateTimeArray(null), this.ReadInt32Array(null)));

                    case VariantType.Guid:
                        return new Variant(this.BuildArray(this.ReadGuidArray(null), this.ReadInt32Array(null)));

                    case VariantType.ByteString:
                        return new Variant(this.BuildArray(this.ReadByteStringArray(null), this.ReadInt32Array(null)));

                    case VariantType.XmlElement:
                        return new Variant(this.BuildArray(this.ReadXElementArray(null), this.ReadInt32Array(null)));

                    case VariantType.NodeId:
                        return new Variant(this.BuildArray(this.ReadNodeIdArray(null), this.ReadInt32Array(null)));

                    case VariantType.ExpandedNodeId:
                        return new Variant(this.BuildArray(this.ReadExpandedNodeIdArray(null), this.ReadInt32Array(null)));

                    case VariantType.StatusCode:
                        return new Variant(this.BuildArray(this.ReadStatusCodeArray(null), this.ReadInt32Array(null)));

                    case VariantType.QualifiedName:
                        return new Variant(this.BuildArray(this.ReadQualifiedNameArray(null), this.ReadInt32Array(null)));

                    case VariantType.LocalizedText:
                        return new Variant(this.BuildArray(this.ReadLocalizedTextArray(null), this.ReadInt32Array(null)));

                    case VariantType.ExtensionObject:
                        return new Variant(this.BuildArray(this.ReadExtensionObjectArray(null), this.ReadInt32Array(null)));

                    case VariantType.Variant:
                        return new Variant(this.BuildArray(this.ReadVariantArray(null), this.ReadInt32Array(null)));

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
            byte b = this.reader.ReadByte();
            if ((b & 1) != 0)
            {
                variant = this.ReadVariant(null);
            }

            if ((b & 2) != 0)
            {
                statusCode = this.ReadStatusCode(null);
            }

            if ((b & 4) != 0)
            {
                sourceTimestamp = this.ReadDateTime(null);
            }

            if ((b & 16) != 0)
            {
                sourcePicoseconds = this.ReadUInt16(null);
            }

            if ((b & 8) != 0)
            {
                serverTimestamp = this.ReadDateTime(null);
            }

            if ((b & 32) != 0)
            {
                serverPicoseconds = this.ReadUInt16(null);
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
            NodeId nodeId = this.ReadNodeId(null);
            byte b = this.reader.ReadByte();
            if (b == (byte)BodyType.ByteString) // BodyType Encodable is encoded as ByteString.
            {
                ExpandedNodeId binaryEncodingId = NodeId.ToExpandedNodeId(nodeId, this.channel?.NamespaceUris);

                if (this.channel != null && this.channel.TryGetTypeFromEncodingId(nodeId, out var type))
                {
                    var len = this.ReadInt32(null);
                    var encodable = (IEncodable)Activator.CreateInstance(type)!;
                    encodable.Decode(this);
                    return new ExtensionObject(encodable, binaryEncodingId);
                }

                return new ExtensionObject(this.ReadByteString(null), binaryEncodingId);
            }

            if (b == (byte)BodyType.XmlElement)
            {
                ExpandedNodeId xmlEncodingId = NodeId.ToExpandedNodeId(nodeId, this.channel?.NamespaceUris);
                return new ExtensionObject(this.ReadXElement(null), xmlEncodingId);
            }

            return null;
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
            NodeId nodeId = this.ReadNodeId(null);
            byte b = this.reader.ReadByte();
            if (b == (byte)BodyType.ByteString)
            {
                if (this.channel == null || !this.channel.TryGetTypeFromEncodingId(nodeId, out var type))
                {
                    throw new ServiceResultException(StatusCodes.BadDecodingError);
                }

                var len = this.ReadInt32(null);
                var encodable = (IEncodable)Activator.CreateInstance(type)!;
                encodable.Decode(this);
                return (T)encodable;
            }

            // TODO: else if (b = 2) use XmlDecoder

            return default(T);
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
        /// Reads an enumeration value from the stream.
        /// </summary>
        /// <param name="fieldName">The field name.</param>
        /// <returns>The value.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/5.2.4/">OPC UA specification Part 6: Mappings, 5.2.4</seealso>
        public T ReadEnumeration<T>(string? fieldName)
            where T : struct, IConvertible
        {
            return (T)Enum.ToObject(typeof(T), this.ReadInt32(null));
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
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new bool[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadBoolean(null);
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
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new sbyte[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadSByte(null);
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
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new byte[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadByte(null);
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
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new short[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadInt16(null);
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
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new ushort[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadUInt16(null);
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
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new int[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadInt32(null);
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
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new uint[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadUInt32(null);
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
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new long[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadInt64(null);
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
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new ulong[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadUInt64(null);
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
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new float[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadFloat(null);
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
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new double[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadDouble(null);
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
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new string?[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadString(null);
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
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new DateTime[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadDateTime(null);
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
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new Guid[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadGuid(null);
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
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            byte[]?[] list = new byte[num][];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadByteString(null);
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
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new XElement?[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadXElement(null);
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
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new NodeId[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadNodeId(null);
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
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new ExpandedNodeId[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadExpandedNodeId(null);
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
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new StatusCode[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadStatusCode(null);
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
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new DiagnosticInfo[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadDiagnosticInfo(null);
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
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new QualifiedName[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadQualifiedName(null);
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
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new LocalizedText[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadLocalizedText(null);
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
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new Variant[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadVariant(null);
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
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new DataValue[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadDataValue(null);
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
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new ExtensionObject?[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadExtensionObject(null);
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
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new T?[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadExtensionObject<T>(null);
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
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new T[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadEncodable<T>(null);
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
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new T[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadEnumeration<T>(null);
            }

            return list;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return this.reader.Read(buffer, offset, count);
        }

        private int ReadArrayLength()
        {
            int num = this.reader.ReadInt32();
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