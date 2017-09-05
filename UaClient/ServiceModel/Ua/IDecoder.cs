// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml.Linq;

namespace Workstation.ServiceModel.Ua
{
    public interface IDecoder
    {
        void PushNamespace(string namespaceUri);

        void PopNamespace();

        bool ReadBoolean(string fieldName);

        sbyte ReadSByte(string fieldName);

        byte ReadByte(string fieldName);

        short ReadInt16(string fieldName);

        ushort ReadUInt16(string fieldName);

        int ReadInt32(string fieldName);

        uint ReadUInt32(string fieldName);

        long ReadInt64(string fieldName);

        ulong ReadUInt64(string fieldName);

        float ReadFloat(string fieldName);

        double ReadDouble(string fieldName);

        string ReadString(string fieldName);

        DateTime ReadDateTime(string fieldName);

        Guid ReadGuid(string fieldName);

        byte[] ReadByteString(string fieldName);

        XElement ReadXElement(string fieldName);

        NodeId ReadNodeId(string fieldName);

        ExpandedNodeId ReadExpandedNodeId(string fieldName);

        StatusCode ReadStatusCode(string fieldName);

        QualifiedName ReadQualifiedName(string fieldName);

        LocalizedText ReadLocalizedText(string fieldName);

        ExtensionObject ReadExtensionObject(string fieldName);

        T ReadExtensionObject<T>(string fieldName)
            where T : class, IEncodable;

        DataValue ReadDataValue(string fieldName);

        Variant ReadVariant(string fieldName);

        DiagnosticInfo ReadDiagnosticInfo(string fieldName);

        T ReadEncodable<T>(string fieldName)
            where T : class, IEncodable;

        T ReadEnumeration<T>(string fieldName)
            where T : struct, IConvertible;

        bool[] ReadBooleanArray(string fieldName);

        sbyte[] ReadSByteArray(string fieldName);

        byte[] ReadByteArray(string fieldName);

        short[] ReadInt16Array(string fieldName);

        ushort[] ReadUInt16Array(string fieldName);

        int[] ReadInt32Array(string fieldName);

        uint[] ReadUInt32Array(string fieldName);

        long[] ReadInt64Array(string fieldName);

        ulong[] ReadUInt64Array(string fieldName);

        float[] ReadFloatArray(string fieldName);

        double[] ReadDoubleArray(string fieldName);

        string[] ReadStringArray(string fieldName);

        DateTime[] ReadDateTimeArray(string fieldName);

        Guid[] ReadGuidArray(string fieldName);

        byte[][] ReadByteStringArray(string fieldName);

        XElement[] ReadXElementArray(string fieldName);

        NodeId[] ReadNodeIdArray(string fieldName);

        ExpandedNodeId[] ReadExpandedNodeIdArray(string fieldName);

        StatusCode[] ReadStatusCodeArray(string fieldName);

        QualifiedName[] ReadQualifiedNameArray(string fieldName);

        LocalizedText[] ReadLocalizedTextArray(string fieldName);

        ExtensionObject[] ReadExtensionObjectArray(string fieldName);

        T[] ReadExtensionObjectArray<T>(string fieldName)
            where T : class, IEncodable;

        DataValue[] ReadDataValueArray(string fieldName);

        Variant[] ReadVariantArray(string fieldName);

        DiagnosticInfo[] ReadDiagnosticInfoArray(string fieldName);

        T[] ReadEncodableArray<T>(string fieldName)
            where T : class, IEncodable;

        T[] ReadEnumerationArray<T>(string fieldName)
            where T : struct, IConvertible;
    }
}