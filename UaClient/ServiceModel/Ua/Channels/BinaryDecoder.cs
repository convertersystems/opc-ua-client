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
            encoding = new UTF8Encoding(false, true);
            reader = new BinaryReader(this.stream, encoding, keepStreamOpen);
        }

        public int Position
        {
            get { return (int)stream.Position; }
            set { stream.Position = value; }
        }

        public void Dispose()
        {
            if (reader != null)
            {
                reader.Dispose();
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
            return reader.ReadBoolean();
        }

        public sbyte ReadSByte(string fieldName)
        {
            return reader.ReadSByte();
        }

        public byte ReadByte(string fieldName)
        {
            return reader.ReadByte();
        }

        public short ReadInt16(string fieldName)
        {
            return reader.ReadInt16();
        }

        public ushort ReadUInt16(string fieldName)
        {
            return reader.ReadUInt16();
        }

        public int ReadInt32(string fieldName)
        {
            return reader.ReadInt32();
        }

        public uint ReadUInt32(string fieldName)
        {
            return reader.ReadUInt32();
        }

        public long ReadInt64(string fieldName)
        {
            return reader.ReadInt64();
        }

        public ulong ReadUInt64(string fieldName)
        {
            return reader.ReadUInt64();
        }

        public float ReadFloat(string fieldName)
        {
            return reader.ReadSingle();
        }

        public double ReadDouble(string fieldName)
        {
            return reader.ReadDouble();
        }

        public string ReadString(string fieldName)
        {
            byte[] array = ReadByteString(fieldName);
            if (array == null || array.Length == 0)
            {
                return null;
            }

            return encoding.GetString(array, 0, array.Length);
        }

        public DateTime ReadDateTime(string fieldName)
        {
            long num = reader.ReadInt64();
            return DateTime.FromFileTimeUtc(num);
        }

        public Guid ReadGuid(string fieldName)
        {
            byte[] b = reader.ReadBytes(16);
            return new Guid(b);
        }

        public byte[] ReadByteString(string fieldName)
        {
            int num = reader.ReadInt32();
            if (num == -1)
            {
                return null;
            }

            return reader.ReadBytes(num);
        }

        public XElement ReadXElement(string fieldName)
        {
            byte[] array = ReadByteString(fieldName);
            if (array == null || array.Length == 0)
            {
                return null;
            }

            return XElement.Parse(encoding.GetString(array, 0, array.Length));
        }

        public NodeId ReadNodeId(string fieldName)
        {
            ushort ns = 0;
            byte b = reader.ReadByte();
            switch (b)
            {
                case 0x00:
                    return new NodeId(reader.ReadByte(), ns);

                case 0x01:
                    ns = reader.ReadByte();
                    return new NodeId(reader.ReadUInt16(), ns);

                case 0x02:
                    ns = reader.ReadUInt16();
                    return new NodeId(reader.ReadUInt32(), ns);

                case 0x03:
                    ns = reader.ReadUInt16();
                    return new NodeId(ReadString(null), ns);

                case 0x04:
                    ns = reader.ReadUInt16();
                    return new NodeId(ReadGuid(null), ns);

                case 0x05:
                    ns = reader.ReadUInt16();
                    return new NodeId(ReadByteString(null), ns);

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
            byte b = reader.ReadByte();
            switch (b)
            {
                case 0x00:
                    nodeId = new NodeId(reader.ReadByte());
                    break;

                case 0x01:
                    ns = reader.ReadByte();
                    nodeId = new NodeId(reader.ReadUInt16(), ns);
                    break;

                case 0x02:
                    ns = reader.ReadUInt16();
                    nodeId = new NodeId(reader.ReadUInt32(), ns);
                    break;

                case 0x03:
                    ns = reader.ReadUInt16();
                    nodeId = new NodeId(ReadString(null), ns);
                    break;

                case 0x04:
                    ns = reader.ReadUInt16();
                    nodeId = new NodeId(ReadGuid(null), ns);
                    break;

                case 0x05:
                    ns = reader.ReadUInt16();
                    nodeId = new NodeId(ReadByteString(null), ns);
                    break;

                default:
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

        public StatusCode ReadStatusCode(string fieldName)
        {
            return ReadUInt32(fieldName);
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
            byte b = reader.ReadByte();
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

        public QualifiedName ReadQualifiedName(string fieldName)
        {
            ushort ns = ReadUInt16(null);
            string name = ReadString(null);
            return new QualifiedName(name, ns);
        }

        public LocalizedText ReadLocalizedText(string fieldName)
        {
            string text = null;
            string locale = null;
            byte b = reader.ReadByte();
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

        public Variant ReadVariant(string fieldName)
        {
            byte b = reader.ReadByte();
            if ((b & 0x80) == 0)
            {
                switch (b & 0x3F)
                {
                    case 0:
                        return Variant.Null;

                    case 1:
                        return new Variant(ReadBoolean(null));

                    case 2:
                        return new Variant(ReadSByte(null));

                    case 3:
                        return new Variant(ReadByte(null));

                    case 4:
                        return new Variant(ReadInt16(null));

                    case 5:
                        return new Variant(ReadUInt16(null));

                    case 6:
                        return new Variant(ReadInt32(null));

                    case 7:
                        return new Variant(ReadUInt32(null));

                    case 8:
                        return new Variant(ReadInt64(null));

                    case 9:
                        return new Variant(ReadUInt64(null));

                    case 10:
                        return new Variant(ReadFloat(null));

                    case 11:
                        return new Variant(ReadDouble(null));

                    case 12:
                        return new Variant(ReadString(null));

                    case 13:
                        return new Variant(ReadDateTime(null));

                    case 14:
                        return new Variant(ReadGuid(null));

                    case 15:
                        return new Variant(ReadByteString(null));

                    case 16:
                        return new Variant(ReadXElement(null));

                    case 17:
                        return new Variant(ReadNodeId(null));

                    case 18:
                        return new Variant(ReadExpandedNodeId(null));

                    case 19:
                        return new Variant(ReadStatusCode(null));

                    case 20:
                        return new Variant(ReadQualifiedName(null));

                    case 21:
                        return new Variant(ReadLocalizedText(null));

                    case 22:
                        return new Variant(ReadExtensionObject(null));

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
                        return new Variant(ReadBooleanArray(null));

                    case 2:
                        return new Variant(ReadSByteArray(null));

                    case 3:
                        return new Variant(ReadByteArray(null));

                    case 4:
                        return new Variant(ReadInt16Array(null));

                    case 5:
                        return new Variant(ReadUInt16Array(null));

                    case 6:
                        return new Variant(ReadInt32Array(null));

                    case 7:
                        return new Variant(ReadUInt32Array(null));

                    case 8:
                        return new Variant(ReadInt64Array(null));

                    case 9:
                        return new Variant(ReadUInt64Array(null));

                    case 10:
                        return new Variant(ReadFloatArray(null));

                    case 11:
                        return new Variant(ReadDoubleArray(null));

                    case 12:
                        return new Variant(ReadStringArray(null));

                    case 13:
                        return new Variant(ReadDateTimeArray(null));

                    case 14:
                        return new Variant(ReadGuidArray(null));

                    case 15:
                        return new Variant(ReadByteStringArray(null));

                    case 16:
                        return new Variant(ReadXElementArray(null));

                    case 17:
                        return new Variant(ReadNodeIdArray(null));

                    case 18:
                        return new Variant(ReadExpandedNodeIdArray(null));

                    case 19:
                        return new Variant(ReadStatusCodeArray(null));

                    case 20:
                        return new Variant(ReadQualifiedNameArray(null));

                    case 21:
                        return new Variant(ReadLocalizedTextArray(null));

                    case 22:
                        return new Variant(ReadExtensionObjectArray(null));

                    case 24:
                        return new Variant(ReadVariantArray(null));

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
                        return new Variant(BuildArray(ReadBooleanArray(null), ReadInt32Array(null)));

                    case 2:
                        return new Variant(BuildArray(ReadSByteArray(null), ReadInt32Array(null)));

                    case 3:
                        return new Variant(BuildArray(ReadByteArray(null), ReadInt32Array(null)));

                    case 4:
                        return new Variant(BuildArray(ReadInt16Array(null), ReadInt32Array(null)));

                    case 5:
                        return new Variant(BuildArray(ReadUInt16Array(null), ReadInt32Array(null)));

                    case 6:
                        return new Variant(BuildArray(ReadInt32Array(null), ReadInt32Array(null)));

                    case 7:
                        return new Variant(BuildArray(ReadUInt32Array(null), ReadInt32Array(null)));

                    case 8:
                        return new Variant(BuildArray(ReadInt64Array(null), ReadInt32Array(null)));

                    case 9:
                        return new Variant(BuildArray(ReadUInt64Array(null), ReadInt32Array(null)));

                    case 10:
                        return new Variant(BuildArray(ReadFloatArray(null), ReadInt32Array(null)));

                    case 11:
                        return new Variant(BuildArray(ReadDoubleArray(null), ReadInt32Array(null)));

                    case 12:
                        return new Variant(BuildArray(ReadStringArray(null), ReadInt32Array(null)));

                    case 13:
                        return new Variant(BuildArray(ReadDateTimeArray(null), ReadInt32Array(null)));

                    case 14:
                        return new Variant(BuildArray(ReadGuidArray(null), ReadInt32Array(null)));

                    case 15:
                        return new Variant(BuildArray(ReadByteStringArray(null), ReadInt32Array(null)));

                    case 16:
                        return new Variant(BuildArray(ReadXElementArray(null), ReadInt32Array(null)));

                    case 17:
                        return new Variant(BuildArray(ReadNodeIdArray(null), ReadInt32Array(null)));

                    case 18:
                        return new Variant(BuildArray(ReadExpandedNodeIdArray(null), ReadInt32Array(null)));

                    case 19:
                        return new Variant(BuildArray(ReadStatusCodeArray(null), ReadInt32Array(null)));

                    case 20:
                        return new Variant(BuildArray(ReadQualifiedNameArray(null), ReadInt32Array(null)));

                    case 21:
                        return new Variant(BuildArray(ReadLocalizedTextArray(null), ReadInt32Array(null)));

                    case 22:
                        return new Variant(BuildArray(ReadExtensionObjectArray(null), ReadInt32Array(null)));

                    case 24:
                        return new Variant(BuildArray(ReadVariantArray(null), ReadInt32Array(null)));

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
            byte b = reader.ReadByte();
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

        public ExtensionObject ReadExtensionObject(string fieldName)
        {
            NodeId nodeId = ReadNodeId(null);
            byte b = reader.ReadByte();
            if (b == 1)
            {
                ExpandedNodeId binaryEncodingId = NodeId.ToExpandedNodeId(nodeId, channel?.NamespaceUris);
                Type type2;
                if (UaTcpSecureChannel.BinaryEncodingIdToTypeDictionary.TryGetValue(binaryEncodingId, out type2))
                {
                    var len = ReadInt32(null);
                    var encodable = Activator.CreateInstance(type2) as IEncodable;
                    encodable.Decode(this);
                    return new ExtensionObject(encodable);
                }

                return new ExtensionObject(ReadByteString(null), binaryEncodingId);
            }
            else if (b == 2)
            {
                ExpandedNodeId xmlEncodingId = NodeId.ToExpandedNodeId(nodeId, channel?.NamespaceUris);
                return new ExtensionObject(ReadXElement(null), xmlEncodingId);
            }

            return null;
        }

        public T ReadExtensionObject<T>(string fieldName)
            where T : IEncodable
        {
            NodeId nodeId = ReadNodeId(null);
            byte b = reader.ReadByte();
            if (b == 1)
            {
                ExpandedNodeId binaryEncodingId = NodeId.ToExpandedNodeId(nodeId, channel?.NamespaceUris);
                Type type2;
                if (!UaTcpSecureChannel.BinaryEncodingIdToTypeDictionary.TryGetValue(binaryEncodingId, out type2))
                {
                    throw new ServiceResultException(StatusCodes.BadDataTypeIdUnknown);
                }

                if (!typeof(T).GetTypeInfo().IsAssignableFrom(type2.GetTypeInfo()))
                {
                    throw new ServiceResultException(StatusCodes.BadDecodingError);
                }

                var len = ReadInt32(null);
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
            return (T)Enum.ToObject(typeof(T), ReadInt32(null));
        }

        public bool[] ReadBooleanArray(string fieldNames)
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

        public sbyte[] ReadSByteArray(string fieldName)
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

        public byte[] ReadByteArray(string fieldName)
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

        public short[] ReadInt16Array(string fieldName)
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

        public ushort[] ReadUInt16Array(string fieldName)
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

        public int[] ReadInt32Array(string fieldName)
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

        public uint[] ReadUInt32Array(string fieldName)
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

        public long[] ReadInt64Array(string fieldName)
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

        public ulong[] ReadUInt64Array(string fieldName)
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

        public float[] ReadFloatArray(string fieldName)
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

        public double[] ReadDoubleArray(string fieldName)
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

        public string[] ReadStringArray(string fieldName)
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new string[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadString(null);
            }

            return list;
        }

        public DateTime[] ReadDateTimeArray(string fieldName)
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

        public Guid[] ReadGuidArray(string fieldName)
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

        public byte[][] ReadByteStringArray(string fieldName)
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new byte[num][];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadByteString(null);
            }

            return list;
        }

        public XElement[] ReadXElementArray(string fieldName)
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new XElement[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadXElement(null);
            }

            return list;
        }

        public NodeId[] ReadNodeIdArray(string fieldName)
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

        public ExpandedNodeId[] ReadExpandedNodeIdArray(string fieldName)
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

        public StatusCode[] ReadStatusCodeArray(string fieldName)
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

        public DiagnosticInfo[] ReadDiagnosticInfoArray(string fieldName)
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

        public QualifiedName[] ReadQualifiedNameArray(string fieldName)
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

        public LocalizedText[] ReadLocalizedTextArray(string fieldName)
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

        public Variant[] ReadVariantArray(string fieldName)
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

        public DataValue[] ReadDataValueArray(string fieldName)
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

        public ExtensionObject[] ReadExtensionObjectArray(string fieldName)
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new ExtensionObject[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadExtensionObject(null);
            }

            return list;
        }

        public T[] ReadExtensionObjectArray<T>(string fieldName)
            where T : IEncodable
        {
            int num = ReadArrayLength();
            if (num == -1)
            {
                return null;
            }

            var list = new T[num];
            for (int i = 0; i < num; i++)
            {
                list[i] = ReadExtensionObject<T>(null);
            }

            return list;
        }

        public T[] ReadEncodableArray<T>(string fieldName)
            where T : IEncodable
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

        public T[] ReadEnumerationArray<T>(string fieldName)
            where T : IConvertible
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
            return reader.Read(buffer, offset, count);
        }

        private int ReadArrayLength()
        {
            int num = reader.ReadInt32();
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