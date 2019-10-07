using FluentAssertions;
using FluentAssertions.Equivalency;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Workstation.ServiceModel.Ua;
using Workstation.ServiceModel.Ua.Channels;
using Xunit;

namespace Workstation.UaClient.UnitTests.Channels
{
    public partial class BinaryEncoderTests
    {
        private static T EncodeDecode<T>(Action<BinaryEncoder> encode, Func<Opc.Ua.BinaryDecoder, T> decode)
        {
            using (var stream = new MemoryStream())
            {
                var encoder = new BinaryEncoder(stream);
                var decoder = new Opc.Ua.BinaryDecoder(stream, new Opc.Ua.ServiceMessageContext
                {

                });

                encode(encoder);
                stream.Position = 0;
                return decode(decoder);
            }
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        public void EncodeBoolean(bool val)
        {
            EncodeDecode(
                e => e.WriteBoolean(null, val),
                d => d.ReadBoolean(null))
                .Should().Be(val);
        }

        [InlineData(0)]
        [InlineData(43)]
        [InlineData(-43)]
        [InlineData(SByte.MinValue)]
        [InlineData(SByte.MaxValue)]
        [Theory]
        public void EncodeSByte(sbyte val)
        {
            EncodeDecode(
                e => e.WriteSByte(null, val),
                d => d.ReadSByte(null))
                .Should().Be(val);
        }

        [InlineData(0)]
        [InlineData(43)]
        [InlineData(Byte.MinValue)]
        [InlineData(Byte.MaxValue)]
        [Theory]
        public void EncodeByte(byte val)
        {
            EncodeDecode(
                e => e.WriteByte(null, val),
                d => d.ReadByte(null))
                .Should().Be(val);
        }

        [InlineData(0)]
        [InlineData(43)]
        [InlineData(-43)]
        [InlineData(Int16.MinValue)]
        [InlineData(Int16.MaxValue)]
        [Theory]
        public void EncodeInt16(short val)
        {
            EncodeDecode(
                e => e.WriteInt16(null, val),
                d => d.ReadInt16(null))
                .Should().Be(val);
        }

        [InlineData(0)]
        [InlineData(43)]
        [InlineData(UInt16.MinValue)]
        [InlineData(UInt16.MaxValue)]
        [Theory]
        public void EncodeUInt16(ushort val)
        {
            EncodeDecode(
                e => e.WriteUInt16(null, val),
                d => d.ReadUInt16(null))
                .Should().Be(val);
        }

        [InlineData(0)]
        [InlineData(123)]
        [InlineData(-123)]
        [InlineData(Int32.MinValue)]
        [InlineData(Int32.MaxValue)]
        [Theory]
        public void EncodeInt32(int val)
        {
            EncodeDecode(
                e => e.WriteInt32(null, val),
                d => d.ReadInt32(null))
                .Should().Be(val);
        }

        [InlineData(0)]
        [InlineData(123)]
        [InlineData(UInt32.MinValue)]
        [InlineData(UInt32.MaxValue)]
        [Theory]
        public void EncodeUInt32(uint val)
        {
            EncodeDecode(
                e => e.WriteUInt32(null, val),
                d => d.ReadUInt32(null))
                .Should().Be(val);
        }

        [InlineData(0)]
        [InlineData(123)]
        [InlineData(-123)]
        [InlineData(Int64.MinValue)]
        [InlineData(Int64.MaxValue)]
        [Theory]
        public void EncodeInt64(long val)
        {
            EncodeDecode(
                e => e.WriteInt64(null, val),
                d => d.ReadInt64(null))
                .Should().Be(val);
        }

        [InlineData(0)]
        [InlineData(123)]
        [InlineData(UInt64.MinValue)]
        [InlineData(UInt64.MaxValue)]
        [Theory]
        public void EncodeUInt64(ulong val)
        {
            EncodeDecode(
                e => e.WriteUInt64(null, val),
                d => d.ReadUInt64(null))
                .Should().Be(val);
        }

        [InlineData(0)]
        [InlineData(1.4)]
        [InlineData(-1.4)]
        [InlineData(Single.MinValue)]
        [InlineData(Single.MaxValue)]
        [InlineData(Single.NaN)]
        [InlineData(Single.Epsilon)]
        [InlineData(Single.NegativeInfinity)]
        [InlineData(Single.PositiveInfinity)]
        [Theory]
        public void EncodeFloat(float val)
        {
            EncodeDecode(
                e => e.WriteFloat(null, val),
                d => d.ReadFloat(null))
                .Should().Be(val);
        }

        [InlineData(0)]
        [InlineData(1.4)]
        [InlineData(-1.4)]
        [InlineData(Double.MinValue)]
        [InlineData(Double.MaxValue)]
        [InlineData(Double.NaN)]
        [InlineData(Double.Epsilon)]
        [InlineData(Double.NegativeInfinity)]
        [InlineData(Double.PositiveInfinity)]
        [Theory]
        public void EncodeDouble(double val)
        {
            EncodeDecode(
                e => e.WriteDouble(null, val),
                d => d.ReadDouble(null))
                .Should().Be(val);
        }

        [InlineData(null)]
        [InlineData("")]
        [InlineData("Test")]
        [InlineData("Umlaut Ä")]
        [InlineData("Euro €")]
        [Theory]
        public void EncodeString(string val)
        {
            EncodeDecode(
                e => e.WriteString(null, val),
                d => d.ReadString(null))
                .Should().Be(val);
        }

        public static IEnumerable<object[]> EncodeDateTimeData { get; } = new []
        {
            new DateTime(0),
            new DateTime(1601, 1, 1, 0, 0, 1),
            new DateTime(1990, 1, 1),
            DateTime.MinValue,
            DateTime.MaxValue,
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(EncodeDateTimeData))]
        [Theory]
        public void EncodeDateTime(DateTime val)
        {
            EncodeDecode(
                e => e.WriteDateTime(null, val),
                d => d.ReadDateTime(null))
                .Should().Be(val);
        }

        public static IEnumerable<object[]> EncodeGuidData { get; } = new []
        {
            Guid.Empty,
            Guid.NewGuid()
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(EncodeGuidData))]
        [Theory]
        public void EncodeGuid(Guid val)
        {
            EncodeDecode(
                e => e.WriteGuid(null, val),
                d => d.ReadGuid(null))
                .Should().Be(val);
        }

        public static IEnumerable<object[]> EncodeByteStringData { get; } = new []
        {
            null,
            new byte[] { },
            new byte[] { 0x0 },
            new byte[] { 0x45, 0xf3, 0x00, 0x34, 0xff, 0x01 }
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(EncodeByteStringData))]
        [Theory]
        public void EncodeByteString(byte[] val)
        {
            EncodeDecode(
                e => e.WriteByteString(null, val),
                d => d.ReadByteString(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> EncodeXElementData { get; } = new []
        {
             @"
                <Window x:Class=""WpfApplication1.Window1""
                        xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                        xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                        Title= ""Window1""
                        Height=""300""
                        Width=""300"">
                    <Grid>
                    </Grid>
                </Window>
            "
        }
        .Select(x => new object[] { XElement.Parse(x) });

        [MemberData(nameof(EncodeXElementData))]
        [Theory]
        public void EncodeXElement(XElement val)
        {
            EncodeDecode(
                e => e.WriteXElement(null, val),
                d => (object)d.ReadXmlElement(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> EncodeNodeIdData { get; } = new []
        {
            "ns=0;i=12",
            "ns=0;i=300",
            "ns=2;i=12",
            "ns=30;i=850000",
            "ns=300;i=12",
            "ns=300;i=850000",
            "ns=3;s=TestString",
            "ns=3;g=8994DA00-5CE1-461F-963C-43F7CFC6864E",
            "ns=3;b=Base64+Test="
        }
        .Select(x => new object[] { NodeId.Parse(x) });

        [MemberData(nameof(EncodeNodeIdData))]
        [Theory]
        public void EncodeNodeId(NodeId val)
        {
            EncodeDecode(
                e => e.WriteNodeId(null, val),
                d => d.ReadNodeId(null))
                .Should().BeEquivalentTo(val);
        }

        [Fact]
        public void EncodeNodeIdNull()
        {
            EncodeDecode(
                e => e.WriteNodeId(null, null),
                d => d.ReadNodeId(null))
                .Should().Be(new Opc.Ua.NodeId());
        }

        public static IEnumerable<object[]> EncodeExpandedNodeIdData { get; } = new []
        {
            "ns=0;i=12",
            "svr=1;ns=0;i=300",
            "svr=2;nsu=http://PLCopen.org/OpcUa/IEC61131-3;ns=2;i=12",
            "nsu=http://PLCopen.org/OpcUa/IEC61131-3;ns=30;i=850000",
            "svr=0;ns=300;i=12",
            "ns=300;i=850000",
            "svr=123;ns=3;s=TestString",
            "ns=3;g=8994DA00-5CE1-461F-963C-43F7CFC6864E",
            "svr=2;nsu=http://PLCopen.org/OpcUa/IEC61131-3;ns=3;b=Base64+Test="
        }
        .Select(x => new object[] { ExpandedNodeId.Parse(x) });

        [MemberData(nameof(EncodeExpandedNodeIdData))]
        [Theory]
        public void EncodeExpandedNodeId(ExpandedNodeId val)
        {
            EncodeDecode(
                e => e.WriteExpandedNodeId(null, val),
                d => d.ReadExpandedNodeId(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> EncodeStatusCodeData { get; } = new[]
        {
            StatusCodes.Good,
            StatusCodes.BadCertificateHostNameInvalid
        }
        .Select(x => new object[] { new StatusCode(x) });

        [MemberData(nameof(EncodeStatusCodeData))]
        [Theory]
        public void EncodeStatusCode(StatusCode val)
        {
            EncodeDecode(
                e => e.WriteStatusCode(null, val),
                d => d.ReadStatusCode(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> EncodeDiagnosticInfoData { get; } = new []
        {
            new DiagnosticInfo(),
            new DiagnosticInfo(2),
            new DiagnosticInfo(2, 3),
            new DiagnosticInfo(2, 3, 4),
            new DiagnosticInfo(2, 3, 4, 5),
            new DiagnosticInfo(2, 3, 4, 5, "Text text text."),
            new DiagnosticInfo(2, additionalInfo:"Test test test."),
            new DiagnosticInfo(2, locale:6, innerStatusCode: StatusCodes.BadSessionIdInvalid),
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(EncodeDiagnosticInfoData))]
        [Theory]
        public void EncodeDiagnosticInfo(DiagnosticInfo val)
        {
            EncodeDecode(
                e => e.WriteDiagnosticInfo(null, val),
                d => d.ReadDiagnosticInfo(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> EncodeQualifiedNameData { get; } = new []
        {
            new QualifiedName(null),
            QualifiedName.Parse("4:Test")
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(EncodeQualifiedNameData))]
        [Theory]
        public void EncodeQualifiedName(QualifiedName val)
        {
            EncodeDecode(
                e => e.WriteQualifiedName(null, val),
                d => d.ReadQualifiedName(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> EncodeLocalizedTextData { get; } = new []
        {
            new LocalizedText("Text", ""),
            new LocalizedText("Text", "de"),
            new LocalizedText("Text", null),
            new LocalizedText("", ""),
            new LocalizedText("", "de"),
            new LocalizedText("", null),
            new LocalizedText(null, ""),
            new LocalizedText(null, "de"),
            new LocalizedText(null, null)
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(EncodeLocalizedTextData))]
        [Theory]
        public void EncodeLocalizedText(LocalizedText val)
        {
            EncodeDecode(
                e => e.WriteLocalizedText(null, val),
                d => d.ReadLocalizedText(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> EncodeVariantData { get; } = new object[]
        {
            default,
            true,
            (sbyte)13,
            (byte)13,
            (short)13,
            (ushort)13,
            13,
            (uint)13,
            (long)13,
            (ulong)13,
            (float)13,
            (double)13,
            "13",
            new DateTime(0L),
            Guid.NewGuid(),
            new byte[] { 0x1, 0x3},
            NodeId.Parse("ns=3;s=Test.Node"),
            ExpandedNodeId.Parse("svr=2;nsu=http://PLCopen.org/OpcUa/IEC61131-3;ns=2;i=12"),
            QualifiedName.Parse("4:Test"),
            new LocalizedText("foo", "fr-FR"),
            XElement.Parse(@"<Item AttributeA=""A"" AttributeB=""B"" />"),
            new StatusCode(43)
        }
        .Select(x => new object[] { new Variant(x) });

        [MemberData(nameof(EncodeVariantData))]
        [Theory]
        public void EncodeVariant(Variant val)
        {
            EncodeDecode(
                e => e.WriteVariant(null, val),
                d => d.ReadVariant(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> EncodeDataValueData =>
            from value in new object[] { null, 54 }
            from status in new[] { StatusCodes.Good, StatusCodes.BadAttributeIdInvalid }
            from srcts in new[] { DateTime.MinValue, DateTime.UtcNow }
            from srcps in new ushort[] { 0, 212 }
            from svrts in new[] { DateTime.MinValue, DateTime.UtcNow }
            from svrps in new ushort[] { 0, 612 }
            select new object[] { new DataValue(value, status, srcts, srcps, svrts, svrps) };

        [MemberData(nameof(EncodeDataValueData))]
        [Theory]
        public void EncodeDataValue(DataValue val)
        {
            EncodeDecode(
                e => e.WriteDataValue(null, val),
                d => d.ReadDataValue(null))
                .Should().BeEquivalentTo(val);
        }

        [InlineData(TypeCode.Boolean)]
        [InlineData(TypeCode.Double)]
        [Theory]
        public void EncodeEnumeration(TypeCode val)
        {
            EncodeDecode(
                e => e.WriteEnumeration(null, val),
                d => d.ReadEnumerated(null, typeof(TypeCode)))
                .Should().Be(val);
        }

        public static IEnumerable<object[]> EncodeEncodableData { get; } = new[]
        {
            null,
            new TimeZoneDataType { },
            new TimeZoneDataType { Offset = 1, DaylightSavingInOffset = true },
            new TimeZoneDataType { Offset = 3, DaylightSavingInOffset = false }
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(EncodeEncodableData))]
        [Theory]
        public void EncodeEncodable(TimeZoneDataType val)
        {
            EncodeDecode(
                e => e.WriteEncodable(null, val),
                d => d.ReadEncodeable(null, typeof(Opc.Ua.TimeZoneDataType)))
                .Should().BeEquivalentTo(val ?? new TimeZoneDataType());
        }


        [InlineData(null)]
        [InlineData(new bool[] { })]
        [InlineData(new bool[] { true })]
        [InlineData(new bool[] { true, false})]
        [Theory]
        public void EncodeBooleanArray(bool[] val)
        {
            EncodeDecode(
                e => e.WriteBooleanArray(null, val),
                d => d.ReadBooleanArray(null))
                .Should().BeEquivalentTo(val);
        }

        [InlineData(null)]
        [InlineData(new sbyte[] { })]
        [InlineData(new sbyte[] { -1 })]
        [InlineData(new sbyte[] { -5, 6 })]
        [Theory]
        public void EncodeSByteArray(sbyte[] val)
        {
            EncodeDecode(
                e => e.WriteSByteArray(null, val),
                d => d.ReadSByteArray(null))
                .Should().BeEquivalentTo(val);
        }

        [InlineData(null)]
        [InlineData(new byte[] { })]
        [InlineData(new byte[] { 1 })]
        [InlineData(new byte[] { 5, 6 })]
        [Theory]
        public void EncodeByteArray(byte[] val)
        {
            EncodeDecode(
                e => e.WriteByteArray(null, val),
                d => d.ReadByteArray(null))
                .Should().BeEquivalentTo(val);
        }

        [InlineData(null)]
        [InlineData(new short[] { })]
        [InlineData(new short[] { 1 })]
        [InlineData(new short[] { -5, 6 })]
        [Theory]
        public void EncodeInt16Array(short[] val)
        {
            EncodeDecode(
                e => e.WriteInt16Array(null, val),
                d => d.ReadInt16Array(null))
                .Should().BeEquivalentTo(val);
        }

        [InlineData(null)]
        [InlineData(new ushort[] { })]
        [InlineData(new ushort[] { 1 })]
        [InlineData(new ushort[] { 5, 6 })]
        [Theory]
        public void EncodeUInt16Array(ushort[] val)
        {
            EncodeDecode(
                e => e.WriteUInt16Array(null, val),
                d => d.ReadUInt16Array(null))
                .Should().BeEquivalentTo(val);
        }

        [InlineData(null)]
        [InlineData(new int[] { })]
        [InlineData(new int[] { 1 })]
        [InlineData(new int[] { -5, 6 })]
        [Theory]
        public void EncodeInt32Array(int[] val)
        {
            EncodeDecode(
                e => e.WriteInt32Array(null, val),
                d => d.ReadInt32Array(null))
                .Should().BeEquivalentTo(val);
        }

        [InlineData(null)]
        [InlineData(new uint[] { })]
        [InlineData(new uint[] { 1 })]
        [InlineData(new uint[] { 5, UInt32.MaxValue })]
        [Theory]
        public void EncodeUInt32Array(uint[] val)
        {
            EncodeDecode(
                e => e.WriteUInt32Array(null, val),
                d => d.ReadUInt32Array(null))
                .Should().BeEquivalentTo(val);
        }

        [InlineData(null)]
        [InlineData(new long[] { })]
        [InlineData(new long[] { 1 })]
        [InlineData(new long[] { -5, 6 })]
        [Theory]
        public void EncodeInt64Array(long[] val)
        {
            EncodeDecode(
                e => e.WriteInt64Array(null, val),
                d => d.ReadInt64Array(null))
                .Should().BeEquivalentTo(val);
        }

        [InlineData(null)]
        [InlineData(new ulong[] { })]
        [InlineData(new ulong[] { 1 })]
        [InlineData(new ulong[] { 5, UInt64.MaxValue })]
        [Theory]
        public void EncodeUInt64Array(ulong[] val)
        {
            EncodeDecode(
                e => e.WriteUInt64Array(null, val),
                d => d.ReadUInt64Array(null))
                .Should().BeEquivalentTo(val);
        }

        [InlineData(null)]
        [InlineData(new float[] { })]
        [InlineData(new float[] { 0.1f })]
        [InlineData(new float[] { -12.2f, 123.5f })]
        [InlineData(new float[] { Single.NaN, Single.PositiveInfinity, Single.NegativeInfinity, Single.Epsilon, Single.MaxValue, Single.MinValue })]
        [Theory]
        public void EncodeFloatArray(float[] val)
        {
            EncodeDecode(
                e => e.WriteFloatArray(null, val),
                d => d.ReadFloatArray(null))
                .Should().BeEquivalentTo(val);
        }

        [InlineData(null)]
        [InlineData(new double[] { })]
        [InlineData(new double[] { 0.1 })]
        [InlineData(new double[] { -12.2, 123.5 })]
        [InlineData(new double[] { Double.NaN, Double.PositiveInfinity, Double.NegativeInfinity, Double.Epsilon, Double.MaxValue, Double.MinValue })]
        [Theory]
        public void EncodeDoubleArray(double[] val)
        {
            EncodeDecode(
                e => e.WriteDoubleArray(null, val),
                d => d.ReadDoubleArray(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> EncodeStringArrayData { get; } = new string[][]
        {
            null,
            new string[] { },
            new string[] { "" },
            new string[] { null },
            new string[] { "", null },
            new string[] { "Hello", "World" },
            new string[] { "Some Unicode characters", "Umlaut Ü", "Euro €", "Kanji 漢字" }
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(EncodeStringArrayData))]
        [Theory]
        public void EncodeStringArray(string[] val)
        {
            EncodeDecode(
                e => e.WriteStringArray(null, val),
                d => d.ReadStringArray(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> EncodeDateTimeArrayData { get; } = new DateTime[][]
        {
            null,
            new DateTime[] {},
            new DateTime[] { new DateTime(1990, 1, 1)},
            new DateTime[] { new DateTime(2001, 12, 1, 15, 10, 20), new DateTime(2100, 2, 3, 20, 0, 0) }
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(EncodeDateTimeArrayData))]
        [Theory]
        public void EncodeDateTimeArray(DateTime[] val)
        {
            EncodeDecode(
                e => e.WriteDateTimeArray(null, val),
                d => d.ReadDateTimeArray(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> EncodeGuidArrayData { get; } = new Guid[][]
        {
            null,
            new Guid[] {},
            new [] { Guid.Parse("a8e248bc-4de5-4d5a-ae67-c065cbe452f3") },
            new [] { Guid.Parse("3494ff88-e744-42b5-9aef-b72c677845fe"), Guid.Parse("82b5cc4f-bdc8-41d6-9e53-93b8e0539806") },
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(EncodeGuidArrayData))]
        [Theory]
        public void EncodeGuidArray(Guid[] val)
        {
            EncodeDecode(
                e => e.WriteGuidArray(null, val),
                d => d.ReadGuidArray(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> EncodeByteStringArrayData { get; } = new byte[][][]
        {
            null,
            new byte[][] {},
            new byte[][] { new byte[] { } },
            new byte[][] { new byte[] { 7 } },
            new byte[][] { new byte[] { 7, 0, 4 }, new byte[] { 255 } },
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(EncodeByteStringArrayData))]
        [Theory]
        public void EncodeByteStringArray(byte[][] val)
        {
            EncodeDecode(
                e => e.WriteByteStringArray(null, val),
                d => d.ReadByteStringArray(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> EncodeXElementArrayData { get; } = new string[][]
        {
            null,
            new string[] {},
            new string[] { null },
            new string[] { "<br />" },
            new string[] { "<h1 class=\"one\"><p>text</p></h1>" },
            new string[] { "<br />", "<h1 class=\"one\"><p>text</p></h1>" },
        }
        .Select(x => new object[] { x?.Select(s => s is null ? null : XElement.Parse(s)).ToArray() });

        [MemberData(nameof(EncodeXElementArrayData))]
        [Theory]
        public void EncodeXElementArray(XElement[] val)
        {
            EncodeDecode(
                e => e.WriteXElementArray(null, val),
                d => d.ReadXmlElementArray(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> EncodeNodeIdArrayData { get; } = new NodeId[][]
        {
            null,
            new NodeId[] {},
            new NodeId[] { new NodeId(4, 0) },
            new NodeId[] { new NodeId(234, 3), new NodeId("Text", 1), new NodeId(Guid.Parse("a8e248bc-4de5-4d5a-ae67-c065cbe452f3"), 8) },
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(EncodeNodeIdArrayData))]
        [Theory]
        public void EncodeNodeIdArray(NodeId[] val)
        {
            EncodeDecode(
                e => e.WriteNodeIdArray(null, val),
                d => d.ReadNodeIdArray(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> EncodeExpandedNodeIdArrayData { get; } = new ExpandedNodeId[][]
        {
            null,
            new ExpandedNodeId[] {},
            new ExpandedNodeId[] { new ExpandedNodeId(4) },
            new ExpandedNodeId[] { new ExpandedNodeId(234), new ExpandedNodeId("Text"), new ExpandedNodeId(Guid.Parse("a8e248bc-4de5-4d5a-ae67-c065cbe452f3")) },
            new ExpandedNodeId[] { ExpandedNodeId.Parse("ns=1;i=234"), ExpandedNodeId.Parse("ns=2;s=bla"), ExpandedNodeId.Parse("svr=2;nsu=http://PLCopen.org/OpcUa/IEC61131-3;ns=3;b=Base64+Test=") },
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(EncodeExpandedNodeIdArrayData))]
        [Theory]
        public void EncodeExpandedNodeIdArray(ExpandedNodeId[] val)
        {
            EncodeDecode(
                e => e.WriteExpandedNodeIdArray(null, val),
                d => d.ReadExpandedNodeIdArray(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> EncodeStatusCodeArrayData { get; } = new StatusCode[][]
            {
                null,
                new StatusCode[] {},
                new StatusCode[] { StatusCodes.BadAttributeIdInvalid, StatusCodes.BadMaxAgeInvalid, StatusCodes.Good, StatusCodes.GoodClamped },
                new StatusCode[] { StatusCodes.GoodNoData }
            }
            .Select(x => new object[] { x });

        [MemberData(nameof(EncodeStatusCodeArrayData))]
        [Theory]
        public void EncodeStatusCodeArray(StatusCode[] val)
        {
            EncodeDecode(
                e => e.WriteStatusCodeArray(null, val),
                d => d.ReadStatusCodeArray(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> EncodeQualifiedNameArrayData { get; } = new QualifiedName[][]
        {
                null,
                new QualifiedName[] {},
                new [] { new QualifiedName("Tests", 3), new QualifiedName("22", 34) },
                new [] { new QualifiedName("Name") }
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(EncodeQualifiedNameArrayData))]
        [Theory]
        public void EncodeQualifiedNameArray(QualifiedName[] val)
        {
            EncodeDecode(
                e => e.WriteQualifiedNameArray(null, val),
                d => d.ReadQualifiedNameArray(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> EncodeLocalizedTextArrayData { get; } = new object[][]
        {
            null,
            new LocalizedText[] {},
            new [] { new LocalizedText("Text", null)},
            new [] { new LocalizedText("", ""), new LocalizedText("", "de") , new LocalizedText("", null)},
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(EncodeLocalizedTextArrayData))]
        [Theory]
        public void EncodeLocalizedTextArray(LocalizedText[] val)
        {
            EncodeDecode(
                e => e.WriteLocalizedTextArray(null, val),
                d => d.ReadLocalizedTextArray(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> EncodeVariantArrayData { get; } = new Variant[][]
        {
            null,
            new Variant[] {},
            new [] { new Variant("Text")},
            new [] { new Variant(1), new Variant((object)null), new Variant(2.0)},
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(EncodeVariantArrayData))]
        [Theory]
        public void EncodeVariantArray(Variant[] val)
        {
            EncodeDecode(
                e => e.WriteVariantArray(null, val),
                d => d.ReadVariantArray(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> EncodeDiagnosticInfoArrayData { get; } = new DiagnosticInfo[][]
        {
            null,
            new DiagnosticInfo[] { },
            new DiagnosticInfo[] { new DiagnosticInfo(2) },
            new DiagnosticInfo[] { new DiagnosticInfo(2, 3, 4), new DiagnosticInfo(2, locale:6, innerStatusCode: StatusCodes.BadSessionIdInvalid) },
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(EncodeDiagnosticInfoArrayData))]
        [Theory]
        public void EncodeDiagnosticInfoArray(DiagnosticInfo[] val)
        {
            EncodeDecode(
                e => e.WriteDiagnosticInfoArray(null, val),
                d => d.ReadDiagnosticInfoArray(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> EncodeDataValueArrayData { get; } = new DataValue[][]
        {
            null,
            new DataValue[] { },
            new DataValue[] { new DataValue(23.0, StatusCodes.BadDataLost, new DateTime(1990,1,1), 12, DateTime.UtcNow, 14)},
            new DataValue[]
            {
                new DataValue(23.0, StatusCodes.BadDataLost, new DateTime(1990,1,1), 12, DateTime.UtcNow, 14),
                new DataValue(28, StatusCodes.GoodClamped, new DateTime(1990,12,1), 120, DateTime.UtcNow, 99),
            }
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(EncodeDataValueArrayData))]
        [Theory]
        public void EncodeDataValueArray(DataValue[] val)
        {
            EncodeDecode(
                e => e.WriteDataValueArray(null, val),
                d => d.ReadDataValueArray(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> EncodeEnumerationArrayData { get; } = new TypeCode[][]
        {
            null,
            new TypeCode[] { },
            new TypeCode[] { TypeCode.Boolean, TypeCode.Double }
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(EncodeEnumerationArrayData))]
        [Theory]
        public void EncodeEnumerationArray(TypeCode[] val)
        {
            EncodeDecode(
                e => e.WriteEnumerationArray(null, val),
                d => d.ReadEnumeratedArray(null, typeof(TypeCode)))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> EncodeEncodableArrayData { get; } = new TimeZoneDataType[][]
        {
            null,
            new TimeZoneDataType[] { },
            new TimeZoneDataType[]
            {
                new TimeZoneDataType { Offset = 1, DaylightSavingInOffset = true },
                new TimeZoneDataType { Offset = 3, DaylightSavingInOffset = false }
            }
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(EncodeEncodableArrayData))]
        [Theory]
        public void EncodeEncodableArray(TimeZoneDataType[] val)
        {
            EncodeDecode(
                e => e.WriteEncodableArray(null, val),
                d => d.ReadEncodeableArray(null, typeof(Opc.Ua.TimeZoneDataType)))
                .Should().BeEquivalentTo(val);
        }
    }
}
