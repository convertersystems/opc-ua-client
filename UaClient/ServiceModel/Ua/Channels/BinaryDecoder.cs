// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace Workstation.ServiceModel.Ua.Channels
{
    public sealed class BinaryDecoder : IDecoder, IDisposable
    {
        private Stream stream;
        private UaTcpSecureChannel channel;
        private Encoding encoding;
        private BinaryReader reader;

        public BinaryDecoder(Stream stream, UaTcpSecureChannel channel = null, bool keepStreamOpen = false)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            this.stream = stream;
            this.channel = channel;
            this.encoding = new UTF8Encoding(false, true);
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

        public bool ReadBoolean(string fieldName)
        {
            return this.reader.ReadBoolean();
        }

        public sbyte ReadSByte(string fieldName)
        {
            return this.reader.ReadSByte();
        }

        public byte ReadByte(string fieldName)
        {
            return this.reader.ReadByte();
        }

        public short ReadInt16(string fieldName)
        {
            return this.reader.ReadInt16();
        }

        public ushort ReadUInt16(string fieldName)
        {
            return this.reader.ReadUInt16();
        }

        public int ReadInt32(string fieldName)
        {
            return this.reader.ReadInt32();
        }

        public uint ReadUInt32(string fieldName)
        {
            return this.reader.ReadUInt32();
        }

        public long ReadInt64(string fieldName)
        {
            return this.reader.ReadInt64();
        }

        public ulong ReadUInt64(string fieldName)
        {
            return this.reader.ReadUInt64();
        }

        public float ReadFloat(string fieldName)
        {
            return this.reader.ReadSingle();
        }

        public double ReadDouble(string fieldName)
        {
            return this.reader.ReadDouble();
        }

        public string ReadString(string fieldName)
        {
            byte[] array = this.ReadByteString(fieldName);
            if (array == null || array.Length == 0)
            {
                return null;
            }

            return this.encoding.GetString(array, 0, array.Length);
        }

        public DateTime ReadDateTime(string fieldName)
        {
            long num = this.reader.ReadInt64();
            return DateTime.FromFileTimeUtc(num);
        }

        public Guid ReadGuid(string fieldName)
        {
            byte[] b = this.reader.ReadBytes(16);
            return new Guid(b);
        }

        public byte[] ReadByteString(string fieldName)
        {
            int num = this.reader.ReadInt32();
            if (num == -1)
            {
                return null;
            }

            return this.reader.ReadBytes(num);
        }

        public XElement ReadXElement(string fieldName)
        {
            byte[] array = this.ReadByteString(fieldName);
            if (array == null || array.Length == 0)
            {
                return null;
            }

            return XElement.Parse(this.encoding.GetString(array, 0, array.Length));
        }

        public NodeId ReadNodeId(string fieldName)
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
                    return new NodeId(this.ReadString(null), ns);

                case 0x04:
                    ns = this.reader.ReadUInt16();
                    return new NodeId(this.ReadGuid(null), ns);

                case 0x05:
                    ns = this.reader.ReadUInt16();
                    return new NodeId(this.ReadByteString(null), ns);

                default:
                    throw new ServiceResultException(StatusCodes.BadDecodingError);
            }
        }

        public ExpandedNodeId ReadExpandedNodeId(string fieldName)
        {
            ushort ns = 0;
            NodeId nodeId = null;
            string nsu = null;
            uint svr = 0;
            byte b = this.reader.ReadByte();
            switch (b)
            {
                case 0x00:
                    nodeId = new NodeId(this.reader.ReadByte());
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
                    nodeId = new NodeId(this.ReadString(null), ns);
                    break;

                case 0x04:
                    ns = this.reader.ReadUInt16();
                    nodeId = new NodeId(this.ReadGuid(null), ns);
                    break;

                case 0x05:
                    ns = this.reader.ReadUInt16();
                    nodeId = new NodeId(this.ReadByteString(null), ns);
                    break;

                default:
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

        public StatusCode ReadStatusCode(string fieldName)
        {
            return this.ReadUInt32(fieldName);
        }

        public DiagnosticInfo ReadDiagnosticInfo(string fieldName)
        {
            int symbolicId = -1;
            int namespaceUri = -1;
            int locale = -1;
            int localizedText = -1;
            string additionalInfo = null;
            StatusCode innerStatusCode = default(StatusCode);
            DiagnosticInfo innerDiagnosticInfo = default(DiagnosticInfo);
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

        public QualifiedName ReadQualifiedName(string fieldName)
        {
            ushort ns = this.ReadUInt16(null);
            string name = this.ReadString(null);
            return new QualifiedName(name, ns);
        }

        public LocalizedText ReadLocalizedText(string fieldName)
        {
            string text = null;
            string locale = null;
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

        public Variant ReadVariant(string fieldName)
        {
            byte b = this.reader.ReadByte();
            if ((b & 0x80) == 0)
            {
                switch (b & 0x3F)
                {
                    case 0:
                        return Variant.Null;

                    case 1:
                        return new Variant(this.ReadBoolean(null));

                    case 2:
                        return new Variant(this.ReadSByte(null));

                    case 3:
                        return new Variant(this.ReadByte(null));

                    case 4:
                        return new Variant(this.ReadInt16(null));

                    case 5:
                        return new Variant(this.ReadUInt16(null));

                    case 6:
                        return new Variant(this.ReadInt32(null));

                    case 7:
                        return new Variant(this.ReadUInt32(null));

                    case 8:
                        return new Variant(this.ReadInt64(null));

                    case 9:
                        return new Variant(this.ReadUInt64(null));

                    case 10:
                        return new Variant(this.ReadFloat(null));

                    case 11:
                        return new Variant(this.ReadDouble(null));

                    case 12:
                        return new Variant(this.ReadString(null));

                    case 13:
                        return new Variant(this.ReadDateTime(null));

                    case 14:
                        return new Variant(this.ReadGuid(null));

                    case 15:
                        return new Variant(this.ReadByteString(null));

                    case 16:
                        return new Variant(this.ReadXElement(null));

                    case 17:
                        return new Variant(this.ReadNodeId(null));

                    case 18:
                        return new Variant(this.ReadExpandedNodeId(null));

                    case 19:
                        return new Variant(this.ReadStatusCode(null));

                    case 20:
                        return new Variant(this.ReadQualifiedName(null));

                    case 21:
                        return new Variant(this.ReadLocalizedText(null));

                    case 22:
                        return new Variant(this.ReadExtensionObject(null));

                    default:
                        throw new ServiceResultException(StatusCodes.BadDecodingError);
                }
            }

            if ((b & 0x40) == 0)
            {
                switch (b & 0x3F)
                {
                    case 0:
                        return Variant.Null;

                    case 1:
                        return new Variant(this.ReadBooleanArray(null));

                    case 2:
                        return new Variant(this.ReadSByteArray(null));

                    case 3:
                        return new Variant(this.ReadByteArray(null));

                    case 4:
                        return new Variant(this.ReadInt16Array(null));

                    case 5:
                        return new Variant(this.ReadUInt16Array(null));

                    case 6:
                        return new Variant(this.ReadInt32Array(null));

                    case 7:
                        return new Variant(this.ReadUInt32Array(null));

                    case 8:
                        return new Variant(this.ReadInt64Array(null));

                    case 9:
                        return new Variant(this.ReadUInt64Array(null));

                    case 10:
                        return new Variant(this.ReadFloatArray(null));

                    case 11:
                        return new Variant(this.ReadDoubleArray(null));

                    case 12:
                        return new Variant(this.ReadStringArray(null));

                    case 13:
                        return new Variant(this.ReadDateTimeArray(null));

                    case 14:
                        return new Variant(this.ReadGuidArray(null));

                    case 15:
                        return new Variant(this.ReadByteStringArray(null));

                    case 16:
                        return new Variant(this.ReadXElementArray(null));

                    case 17:
                        return new Variant(this.ReadNodeIdArray(null));

                    case 18:
                        return new Variant(this.ReadExpandedNodeIdArray(null));

                    case 19:
                        return new Variant(this.ReadStatusCodeArray(null));

                    case 20:
                        return new Variant(this.ReadQualifiedNameArray(null));

                    case 21:
                        return new Variant(this.ReadLocalizedTextArray(null));

                    case 22:
                        return new Variant(this.ReadExtensionObjectArray(null));

                    case 24:
                        return new Variant(this.ReadVariantArray(null));

                    default:
                        throw new ServiceResultException(StatusCodes.BadDecodingError);
                }
            }
            else
            {
                switch (b & 0x3F)
                {
                    case 0:
                        return Variant.Null;

                    case 1:
                        return new Variant(this.BuildArray(this.ReadBooleanArray(null), this.ReadInt32Array(null)));

                    case 2:
                        return new Variant(this.BuildArray(this.ReadSByteArray(null), this.ReadInt32Array(null)));

                    case 3:
                        return new Variant(this.BuildArray(this.ReadByteArray(null), this.ReadInt32Array(null)));

                    case 4:
                        return new Variant(this.BuildArray(this.ReadInt16Array(null), this.ReadInt32Array(null)));

                    case 5:
                        return new Variant(this.BuildArray(this.ReadUInt16Array(null), this.ReadInt32Array(null)));

                    case 6:
                        return new Variant(this.BuildArray(this.ReadInt32Array(null), this.ReadInt32Array(null)));

                    case 7:
                        return new Variant(this.BuildArray(this.ReadUInt32Array(null), this.ReadInt32Array(null)));

                    case 8:
                        return new Variant(this.BuildArray(this.ReadInt64Array(null), this.ReadInt32Array(null)));

                    case 9:
                        return new Variant(this.BuildArray(this.ReadUInt64Array(null), this.ReadInt32Array(null)));

                    case 10:
                        return new Variant(this.BuildArray(this.ReadFloatArray(null), this.ReadInt32Array(null)));

                    case 11:
                        return new Variant(this.BuildArray(this.ReadDoubleArray(null), this.ReadInt32Array(null)));

                    case 12:
                        return new Variant(this.BuildArray(this.ReadStringArray(null), this.ReadInt32Array(null)));

                    case 13:
                        return new Variant(this.BuildArray(this.ReadDateTimeArray(null), this.ReadInt32Array(null)));

                    case 14:
                        return new Variant(this.BuildArray(this.ReadGuidArray(null), this.ReadInt32Array(null)));

                    case 15:
                        return new Variant(this.BuildArray(this.ReadByteStringArray(null), this.ReadInt32Array(null)));

                    case 16:
                        return new Variant(this.BuildArray(this.ReadXElementArray(null), this.ReadInt32Array(null)));

                    case 17:
                        return new Variant(this.BuildArray(this.ReadNodeIdArray(null), this.ReadInt32Array(null)));

                    case 18:
                        return new Variant(this.BuildArray(this.ReadExpandedNodeIdArray(null), this.ReadInt32Array(null)));

                    case 19:
                        return new Variant(this.BuildArray(this.ReadStatusCodeArray(null), this.ReadInt32Array(null)));

                    case 20:
                        return new Variant(this.BuildArray(this.ReadQualifiedNameArray(null), this.ReadInt32Array(null)));

                    case 21:
                        return new Variant(this.BuildArray(this.ReadLocalizedTextArray(null), this.ReadInt32Array(null)));

                    case 22:
                        return new Variant(this.BuildArray(this.ReadExtensionObjectArray(null), this.ReadInt32Array(null)));

                    case 24:
                        return new Variant(this.BuildArray(this.ReadVariantArray(null), this.ReadInt32Array(null)));

                    default:
                        throw new ServiceResultException(StatusCodes.BadDecodingError);
                }
            }
        }

        public DataValue ReadDataValue(string fieldName)
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

        public ExtensionObject ReadExtensionObject(string fieldName)
        {
            NodeId nodeId = this.ReadNodeId(null);
            byte b = this.reader.ReadByte();
            if (b == 1)
            {
                ExpandedNodeId binaryEncodingId = NodeId.ToExpandedNodeId(nodeId, this.channel?.NamespaceUris);
                Type type2;
                if (UaTcpSecureChannel.BinaryEncodingIdToTypeDictionary.TryGetValue(binaryEncodingId, out type2))
                {
                    var len = this.ReadInt32(null);
                    var encodable = Activator.CreateInstance(type2) as IEncodable;
                    encodable.Decode(this);
                    return new ExtensionObject(encodable);
                }

                return new ExtensionObject(this.ReadByteString(null), binaryEncodingId);
            }
            else if (b == 2)
            {
                ExpandedNodeId xmlEncodingId = NodeId.ToExpandedNodeId(nodeId, this.channel?.NamespaceUris);
                return new ExtensionObject(this.ReadXElement(null), xmlEncodingId);
            }

            return null;
        }

        public T ReadExtensionObject<T>(string fieldName)
            where T : IEncodable
        {
            NodeId nodeId = this.ReadNodeId(null);
            byte b = this.reader.ReadByte();
            if (b == 1)
            {
                ExpandedNodeId binaryEncodingId = NodeId.ToExpandedNodeId(nodeId, this.channel?.NamespaceUris);
                Type type2;
                if (!UaTcpSecureChannel.BinaryEncodingIdToTypeDictionary.TryGetValue(binaryEncodingId, out type2))
                {
                    throw new ServiceResultException(StatusCodes.BadDataTypeIdUnknown);
                }

                if (!typeof(T).GetTypeInfo().IsAssignableFrom(type2.GetTypeInfo()))
                {
                    throw new ServiceResultException(StatusCodes.BadDecodingError);
                }

                var len = this.ReadInt32(null);
                var encodable = Activator.CreateInstance(type2) as IEncodable;
                encodable.Decode(this);
                return (T)encodable;
            }

            // TODO: else if (b = 2) use XmlDecoder

            return default(T);
        }

        public T ReadEncodable<T>(string fieldName)
            where T : IEncodable
        {
            var value = Activator.CreateInstance<T>();
            value.Decode(this);
            return value;
        }

        public T ReadEnumeration<T>(string fieldName)
            where T : IConvertible
        {
            return (T)Enum.ToObject(typeof(T), this.ReadInt32(null));
        }

        public bool[] ReadBooleanArray(string fieldNames)
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

        public sbyte[] ReadSByteArray(string fieldName)
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

        public byte[] ReadByteArray(string fieldName)
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

        public short[] ReadInt16Array(string fieldName)
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

        public ushort[] ReadUInt16Array(string fieldName)
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

        public int[] ReadInt32Array(string fieldName)
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

        public uint[] ReadUInt32Array(string fieldName)
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

        public long[] ReadInt64Array(string fieldName)
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

        public ulong[] ReadUInt64Array(string fieldName)
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

        public float[] ReadFloatArray(string fieldName)
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

        public double[] ReadDoubleArray(string fieldName)
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

        public string[] ReadStringArray(string fieldName)
        {
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new string[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadString(null);
            }

            return list;
        }

        public DateTime[] ReadDateTimeArray(string fieldName)
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

        public Guid[] ReadGuidArray(string fieldName)
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

        public byte[][] ReadByteStringArray(string fieldName)
        {
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new byte[num][];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadByteString(null);
            }

            return list;
        }

        public XElement[] ReadXElementArray(string fieldName)
        {
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new XElement[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadXElement(null);
            }

            return list;
        }

        public NodeId[] ReadNodeIdArray(string fieldName)
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

        public ExpandedNodeId[] ReadExpandedNodeIdArray(string fieldName)
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

        public StatusCode[] ReadStatusCodeArray(string fieldName)
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

        public DiagnosticInfo[] ReadDiagnosticInfoArray(string fieldName)
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

        public QualifiedName[] ReadQualifiedNameArray(string fieldName)
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

        public LocalizedText[] ReadLocalizedTextArray(string fieldName)
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

        public Variant[] ReadVariantArray(string fieldName)
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

        public DataValue[] ReadDataValueArray(string fieldName)
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

        public ExtensionObject[] ReadExtensionObjectArray(string fieldName)
        {
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new ExtensionObject[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadExtensionObject(null);
            }

            return list;
        }

        public T[] ReadExtensionObjectArray<T>(string fieldName)
            where T : IEncodable
        {
            int num = this.ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new T[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = this.ReadExtensionObject<T>(null);
            }

            return list;
        }

        public T[] ReadEncodableArray<T>(string fieldName)
            where T : IEncodable
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

        public T[] ReadEnumerationArray<T>(string fieldName)
            where T : IConvertible
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

        private Array BuildArray<T>(T[] a1, int[] dims)
        {
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