// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml.Linq;

namespace Workstation.ServiceModel.Ua
{
    public interface IEncoder
    {
        void PushNamespace(string namespaceUri);

        void PopNamespace();

        void WriteBoolean(string fieldName, bool value);

        void WriteSByte(string fieldName, sbyte value);

        void WriteByte(string fieldName, byte value);

        void WriteInt16(string fieldName, short value);

        void WriteUInt16(string fieldName, ushort value);

        void WriteInt32(string fieldName, int value);

        void WriteUInt32(string fieldName, uint value);

        void WriteInt64(string fieldName, long value);

        void WriteUInt64(string fieldName, ulong value);

        void WriteFloat(string fieldName, float value);

        void WriteDouble(string fieldName, double value);

        void WriteString(string fieldName, string value);

        void WriteDateTime(string fieldName, DateTime value);

        void WriteGuid(string fieldName, Guid value);

        void WriteByteString(string fieldName, byte[] value);

        void WriteXElement(string fieldName, XElement value);

        void WriteNodeId(string fieldName, NodeId value);

        void WriteExpandedNodeId(string fieldName, ExpandedNodeId value);

        void WriteStatusCode(string fieldName, StatusCode value);

        void WriteQualifiedName(string fieldName, QualifiedName value);

        void WriteLocalizedText(string fieldName, LocalizedText value);

        void WriteExtensionObject(string fieldName, ExtensionObject value);

        void WriteExtensionObject<T>(string fieldName, T value)
            where T : IEncodable;

        void WriteDataValue(string fieldName, DataValue value);

        void WriteVariant(string fieldName, Variant value);

        void WriteDiagnosticInfo(string fieldName, DiagnosticInfo value);

        void WriteEncodable<T>(string fieldName, T value)
            where T : IEncodable;

        void WriteEnumeration<T>(string fieldName, T value)
            where T : IConvertible;

        void WriteBooleanArray(string fieldName, bool[] values);

        void WriteSByteArray(string fieldName, sbyte[] values);

        void WriteByteArray(string fieldName, byte[] values);

        void WriteInt16Array(string fieldName, short[] values);

        void WriteUInt16Array(string fieldName, ushort[] values);

        void WriteInt32Array(string fieldName, int[] values);

        void WriteUInt32Array(string fieldName, uint[] values);

        void WriteInt64Array(string fieldName, long[] values);

        void WriteUInt64Array(string fieldName, ulong[] values);

        void WriteFloatArray(string fieldName, float[] values);

        void WriteDoubleArray(string fieldName, double[] values);

        void WriteStringArray(string fieldName, string[] values);

        void WriteDateTimeArray(string fieldName, DateTime[] values);

        void WriteGuidArray(string fieldName, Guid[] values);

        void WriteByteStringArray(string fieldName, byte[][] values);

        void WriteXElementArray(string fieldName, XElement[] values);

        void WriteNodeIdArray(string fieldName, NodeId[] values);

        void WriteExpandedNodeIdArray(string fieldName, ExpandedNodeId[] values);

        void WriteStatusCodeArray(string fieldName, StatusCode[] values);

        void WriteQualifiedNameArray(string fieldName, QualifiedName[] values);

        void WriteLocalizedTextArray(string fieldName, LocalizedText[] values);

        void WriteExtensionObjectArray(string fieldName, ExtensionObject[] values);

        void WriteExtensionObjectArray<T>(string fieldName, T[] values)
            where T : IEncodable;

        void WriteDataValueArray(string fieldName, DataValue[] values);

        void WriteVariantArray(string fieldName, Variant[] values);

        void WriteDiagnosticInfoArray(string fieldName, DiagnosticInfo[] values);

        void WriteEncodableArray<T>(string fieldName, T[] values)
            where T : IEncodable;

        void WriteEnumerationArray<T>(string fieldName, T[] values)
            where T : IConvertible;
    }
}