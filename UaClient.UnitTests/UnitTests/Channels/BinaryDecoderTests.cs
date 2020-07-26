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
using Xunit.Abstractions;

namespace Workstation.UaClient.UnitTests.Channels
{
    public partial class BinaryDecoderTests
    {
        private readonly ITestOutputHelper output;


        public BinaryDecoderTests(ITestOutputHelper output)
        {
            this.output = output;
        }

            [Fact]
        public void LoadAssys()
        {
            foreach (var assy in AppDomain.CurrentDomain.GetAssemblies().Where(a=>!a.IsDynamic))
            {
                output.WriteLine(assy.FullName);
            }
        }
        private static T EncodeDecode<T>(Action<Opc.Ua.BinaryEncoder> encode, Func<BinaryDecoder, T> decode)
        {
            using (var stream = new MemoryStream())
            using (var encoder = new Opc.Ua.BinaryEncoder(stream, new Opc.Ua.ServiceMessageContext { }))
            using (var decoder = new BinaryDecoder(stream))
            {

                encode(encoder);
                stream.Position = 0;
                return decode(decoder);
            }
        }

        private static XmlElement XmlElementParse(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc.DocumentElement;
        }

        [Fact]
        public void CreateWithNullStream()
        {
            Stream stream = null;

            stream.Invoking(s => new BinaryDecoder(s))
                .Should().Throw<ArgumentNullException>();
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        public void DecodeBoolean(bool val)
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
        public void DecodeSByte(sbyte val)
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
        public void DecodeByte(byte val)
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
        public void DecodeInt16(short val)
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
        public void DecodeUInt16(ushort val)
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
        public void DecodeInt32(int val)
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
        public void DecodeUInt32(uint val)
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
        public void DecodeInt64(long val)
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
        public void DecodeUInt64(ulong val)
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
        public void DecodeFloat(float val)
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
        public void DecodeDouble(double val)
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
        public void DecodeString(string val)
        {
            EncodeDecode(
                e => e.WriteString(null, val),
                d => d.ReadString(null))
                .Should().Be(val);
        }

        // This is a valid UTF8 string
        [InlineData(new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x20 })]
        // A leading byte of [C0] or [C1] is not allowed
        [InlineData(new byte[] { 0x48, 0x65, 0x6c, 0xc0, 0xa0 })]
        // A leading byte of [E0] is not allowed to be followed by a 
        // continuation byte of [80] [9F]
        [InlineData(new byte[] { 0x48, 0x65, 0x6c, 0xe0, 0x81, 0xa0 })]
        // A leading byte of [F5] is not allowed
        [InlineData(new byte[] { 0x48, 0x65, 0x6c, 0xf5, 0x81, 0xa0, 0xaa })]
        [Theory]
        public void DecodeMalformedUtf8String(byte[] val)
        {
            var str = Encoding.UTF8.GetString(val);

            EncodeDecode(
                e => e.WriteByteString(null, val),
                d => d.ReadString(null))
                .Should().Be(str);
        }

        public static IEnumerable<object[]> DecodeDateTimeData { get; } = new[]
        {
            new DateTime(0),
            new DateTime(1601, 1, 1, 0, 0, 1),
            new DateTime(1990, 1, 1),
            DateTime.MinValue,
            DateTime.MaxValue,
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(DecodeDateTimeData))]
        [Theory]
        public void DecodeDateTime(DateTime val)
        {
            EncodeDecode(
                e => e.WriteDateTime(null, val),
                d => d.ReadDateTime(null))
                .Should().Be(val);
        }

        public static IEnumerable<object[]> DecodeGuidData { get; } = new[]
        {
            Guid.Empty,
            Guid.NewGuid()
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(DecodeGuidData))]
        [Theory]
        public void DecodeGuid(Guid val)
        {
            EncodeDecode(
                e => e.WriteGuid(null, val),
                d => d.ReadGuid(null))
                .Should().Be(val);
        }

        public static IEnumerable<object[]> DecodeByteStringData { get; } = new[]
        {
            null,
            new byte[] { },
            new byte[] { 0x0 },
            new byte[] { 0x45, 0xf3, 0x00, 0x34, 0xff, 0x01 }
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(DecodeByteStringData))]
        [Theory]
        public void DecodeByteString(byte[] val)
        {
            EncodeDecode(
                e => e.WriteByteString(null, val),
                d => d.ReadByteString(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> DecodeXElementData { get; } = new[]
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
        .Select(x => new object[] { XmlElementParse(x) });

        [MemberData(nameof(DecodeXElementData))]
        [Theory]
        public void DecodeXElement(XmlElement val)
        {
            EncodeDecode(
                e => e.WriteXmlElement(null, val),
                d => (object)d.ReadXElement(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> DecodeNodeIdData { get; } = new[]
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
        .Select(x => new object[] { Opc.Ua.NodeId.Parse(x) });

        [MemberData(nameof(DecodeNodeIdData))]
        [Theory]
        public void DecodeNodeId(Opc.Ua.NodeId val)
        {
            EncodeDecode(
                e => e.WriteNodeId(null, val),
                d => d.ReadNodeId(null))
                .Should().BeEquivalentTo(val);
        }

        [Fact]
        public void DecodeNodeIdNull()
        {
            EncodeDecode(
                e => e.WriteNodeId(null, null),
                d => d.ReadNodeId(null))
                .Should().Be(NodeId.Null);
        }

        public static IEnumerable<object[]> DecodeExpandedNodeIdData { get; } = new[]
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
        .Select(x => new object[] { Opc.Ua.ExpandedNodeId.Parse(x) });

        [MemberData(nameof(DecodeExpandedNodeIdData))]
        [Theory]
        public void DecodeExpandedNodeId(Opc.Ua.ExpandedNodeId val)
        {
            EncodeDecode(
                e => e.WriteExpandedNodeId(null, val),
                d => d.ReadExpandedNodeId(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> DecodeStatusCodeData { get; } = new[]
        {
            StatusCodes.Good,
            StatusCodes.BadCertificateHostNameInvalid
        }
        .Select(x => new object[] { new Opc.Ua.StatusCode(x) });

        [MemberData(nameof(DecodeStatusCodeData))]
        [Theory]
        public void DecodeStatusCode(Opc.Ua.StatusCode val)
        {
            EncodeDecode(
                e => e.WriteStatusCode(null, val),
                d => d.ReadStatusCode(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> DecodeDiagnosticInfoData { get; } = new[]
        {
            new Opc.Ua.DiagnosticInfo(),
            new Opc.Ua.DiagnosticInfo(0, 0, 0, 0, null),
            new Opc.Ua.DiagnosticInfo(2, 0, 0, 0, null),
            new Opc.Ua.DiagnosticInfo(2, 3, 0, 0, null),
            new Opc.Ua.DiagnosticInfo(2, 3, 4, 0, null),
            new Opc.Ua.DiagnosticInfo(2, 3, 4, 5, null),
            new Opc.Ua.DiagnosticInfo(2, 3, 4, 5, "Text text text."),
            new Opc.Ua.DiagnosticInfo(2, 0, 0, 0, "Test test test."),
            new Opc.Ua.DiagnosticInfo(2, 0, 6, 0, null),
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(DecodeDiagnosticInfoData))]
        [Theory]
        public void DecodeDiagnosticInfo(Opc.Ua.DiagnosticInfo val)
        {
            EncodeDecode(
                e => e.WriteDiagnosticInfo(null, val),
                d => d.ReadDiagnosticInfo(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> DecodeQualifiedNameData { get; } = new[]
        {
            new Opc.Ua.QualifiedName(null),
            Opc.Ua.QualifiedName.Parse("4:Test")
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(DecodeQualifiedNameData))]
        [Theory]
        public void DecodeQualifiedName(Opc.Ua.QualifiedName val)
        {
            EncodeDecode(
                e => e.WriteQualifiedName(null, val),
                d => d.ReadQualifiedName(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> DecodeLocalizedTextData { get; } = new[]
        {
            new Opc.Ua.LocalizedText("Text", ""),
            new Opc.Ua.LocalizedText("Text", "de"),
            new Opc.Ua.LocalizedText("Text", null),
            new Opc.Ua.LocalizedText("", ""),
            new Opc.Ua.LocalizedText("", "de"),
            new Opc.Ua.LocalizedText("", null),
            new Opc.Ua.LocalizedText(null, ""),
            new Opc.Ua.LocalizedText(null, "de"),
            new Opc.Ua.LocalizedText(null, null)
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(DecodeLocalizedTextData))]
        [Theory]
        public void DecodeLocalizedText(Opc.Ua.LocalizedText val)
        {
            EncodeDecode(
                e => e.WriteLocalizedText(null, val),
                d => d.ReadLocalizedText(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> DecodeVariantData { get; } = new object[]
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
            Opc.Ua.NodeId.Parse("ns=3;s=Test.Node"),
            Opc.Ua.ExpandedNodeId.Parse("svr=2;nsu=http://PLCopen.org/OpcUa/IEC61131-3;ns=2;i=12"),
            Opc.Ua.QualifiedName.Parse("4:Test"),
            new Opc.Ua.LocalizedText("foo", "fr-FR"),
            XmlElementParse(@"<Item AttributeA=""A"" AttributeB=""B"" />"),
            new Opc.Ua.StatusCode(43),
            new [] {true, false, true },
            new sbyte[] { 1, 2, 3},
            new short[] { 1, 2, 3},
            new ushort[] { 1, 2, 3},
            new int[] { 1, 2, 3},
            new uint[] { 1, 2, 3},
            new long[] { 1, 2, 3},
            new ulong[] { 1, 2, 3},
            new float[] { (float)1.3, (float)3.1, (float)4},
            new double[] { 1.3, 3.1, 4.0},
            new string[] { "a", "b", ""},
            new DateTime[] { DateTime.UtcNow },
            new Opc.Ua.Uuid[] { Opc.Ua.Uuid.Empty, new Opc.Ua.Uuid() },
            new byte[][] { new byte[] { }, new byte[] { 1, 2, 3} },
            new Opc.Ua.NodeId[] { new Opc.Ua.NodeId(5), new Opc.Ua.NodeId("b")},
            new Opc.Ua.ExpandedNodeId[] { new Opc.Ua.ExpandedNodeId(4), new Opc.Ua.ExpandedNodeId("ee")},
            new Opc.Ua.QualifiedName[] { Opc.Ua.QualifiedName.Parse("0:A"), Opc.Ua.QualifiedName.Parse("1:t") },
            new Opc.Ua.LocalizedText[] {new Opc.Ua.LocalizedText("Yes", "en-US"), new Opc.Ua.LocalizedText("Ja", "de-DE")},
            new [] { XmlElementParse(@"<Item AttributeA=""A"" AttributeB=""B"" />") },
            new Opc.Ua.StatusCode[] {42, 43},
            new Opc.Ua.Variant[] { new Opc.Ua.Variant(1)},
            new [,] { { true, false }, { true, true}, { false, false} },
            new byte[,] { { 1 }, { 2 }, { 3 } },
            new sbyte[,] { { 1 }, { 2 }, { 3 } },
            new short[,] { { 1, 2, 3 } },
            new ushort[,] { { 1, 2 }, { 3, 9 } },
            new int[,] { { 1, 2 }, { 3, 0 } },
            new uint[,] { { 1, 2, 3 }, { 6, 0, 0 } },
            new long[,] { { 1 } },
            new ulong[,,] { { { 1, 2 }, { 3, 5 } }, { { 8, 2 }, { 3, 7 } }},
            new float[,] { { (float)3.13456 } },
            new double[,,,] { { {{ double.PositiveInfinity }, { double.NaN } }, { { double.NegativeInfinity }, { 3.1 } } } },
            new byte[,][] { { new byte[] { 3, 4} }, { new byte[] { 5, 6, 7} } },
            new string[,] { { "a", null},{ "b", "" } },
            new DateTime[,] { { DateTime.MinValue } },
            new Opc.Ua.Uuid[,] { { Opc.Ua.Uuid.Empty, new Opc.Ua.Uuid() } },
            new Opc.Ua.NodeId[,] { { new Opc.Ua.NodeId(5) }, {new Opc.Ua.NodeId("b")} },
            new Opc.Ua.ExpandedNodeId[,] { { new Opc.Ua.ExpandedNodeId(4) }, { new Opc.Ua.ExpandedNodeId("ee") } },
            new Opc.Ua.QualifiedName[,,,,,,,] { { { { { { { { new Opc.Ua.QualifiedName("A") } } } } } } } },
            new Opc.Ua.LocalizedText[,] { { new Opc.Ua.LocalizedText("Yes", "en-US"), new Opc.Ua.LocalizedText("Ja", "de-DE") }, { new Opc.Ua.LocalizedText("No", "en-US"), new Opc.Ua.LocalizedText("Nein", "de-DE") } },
            new [,] { { XmlElementParse(@"<Item AttributeA=""A"" AttributeB=""B"" />") } },
            new Opc.Ua.StatusCode[,,] { {{ 42, 43 }, { 100, 102 }, {100, 234 }, { 239, 199} } },
            new Opc.Ua.Variant[,] { { new Opc.Ua.Variant(1), new Opc.Ua.Variant(2) }, { new Opc.Ua.Variant(3), new Opc.Ua.Variant(4)} },
        }
        .Select(x => new object[] { new Opc.Ua.Variant(x) });

        [MemberData(nameof(DecodeVariantData))]
        [Theory]
        public void DecodeVariant(Opc.Ua.Variant val)
        {
            EncodeDecode(
                e => e.WriteVariant(null, val),
                d => d.ReadVariant(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> DecodeDataValueData =>
            from value in new object[] { null, 54 }
            from status in new[] { StatusCodes.Good, StatusCodes.BadAttributeIdInvalid }
            from srcts in new[] { DateTime.MinValue, DateTime.UtcNow }
            from srcps in new ushort[] { 0, 212 }
            from svrts in new[] { DateTime.MinValue, DateTime.UtcNow }
            from svrps in new ushort[] { 0, 612 }
            select new object[]
            {
                new Opc.Ua.DataValue(new Opc.Ua.Variant(value), status, srcts, svrts)
                {
                    SourcePicoseconds = srcps,
                    ServerPicoseconds = svrps
                }
            };

        [MemberData(nameof(DecodeDataValueData))]
        [Theory]
        public void DecodeDataValue(Opc.Ua.DataValue val)
        {
            EncodeDecode(
                e => e.WriteDataValue(null, val),
                d => d.ReadDataValue(null))
                .Should().BeEquivalentTo(val);
        }

        [InlineData(TypeCode.Boolean)]
        [InlineData(TypeCode.Double)]
        [Theory]
        public void DecodeEnumeration(TypeCode val)
        {
            EncodeDecode(
                e => e.WriteEnumerated(null, val),
                d => d.ReadEnumeration<TypeCode>(null))
                .Should().Be(val);
        }

        public static IEnumerable<object[]> DecodeEncodableData { get; } = new[]
        {
            null,
            new Opc.Ua.TimeZoneDataType { },
            new Opc.Ua.TimeZoneDataType { Offset = 1, DaylightSavingInOffset = true },
            new Opc.Ua.TimeZoneDataType { Offset = 3, DaylightSavingInOffset = false }
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(DecodeEncodableData))]
        [Theory]
        public void DecodeEncodable(Opc.Ua.TimeZoneDataType val)
        {
            EncodeDecode(
                e => e.WriteEncodeable(null, val, typeof(Opc.Ua.TimeZoneDataType)),
                d => d.ReadEncodable<TimeZoneDataType>(null))
                .Should().BeEquivalentTo(val ?? new Opc.Ua.TimeZoneDataType());
        }


        [InlineData(null)]
        [InlineData(new bool[] { })]
        [InlineData(new bool[] { true })]
        [InlineData(new bool[] { true, false })]
        [Theory]
        public void DecodeBooleanArray(bool[] val)
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
        public void DecodeSByteArray(sbyte[] val)
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
        public void DecodeByteArray(byte[] val)
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
        public void DecodeInt16Array(short[] val)
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
        public void DecodeUInt16Array(ushort[] val)
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
        public void DecodeInt32Array(int[] val)
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
        public void DecodeUInt32Array(uint[] val)
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
        public void DecodeInt64Array(long[] val)
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
        public void DecodeUInt64Array(ulong[] val)
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
        public void DecodeFloatArray(float[] val)
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
        public void DecodeDoubleArray(double[] val)
        {
            EncodeDecode(
                e => e.WriteDoubleArray(null, val),
                d => d.ReadDoubleArray(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> DecodeStringArrayData { get; } = new string[][]
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

        [MemberData(nameof(DecodeStringArrayData))]
        [Theory]
        public void DecodeStringArray(string[] val)
        {
            EncodeDecode(
                e => e.WriteStringArray(null, val),
                d => d.ReadStringArray(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> DecodeMalformedUtf8StringArrayData { get; } = new byte[][]
        {
            // This is a valid UTF8 string
            new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x20 },
            // A leading byte of [C0] or [C1] is not allowed
            new byte[] { 0x48, 0x65, 0x6c, 0xc0, 0xa0 },
            // A leading byte of [E0] is not allowed to be followed by a 
            // continuation byte of [80] [9F]
            new byte[] { 0x48, 0x65, 0x6c, 0xe0, 0x81, 0xa0 },
            // A leading byte of [F5] is not allowed
            new byte[] { 0x48, 0x65, 0x6c, 0xf5, 0x81, 0xa0, 0xaa }
        }
        .Select(x => new object[] { new byte[][] { x, new byte[] { 0x20 } } });

        [MemberData(nameof(DecodeMalformedUtf8StringArrayData))]
        [Theory]
        public void DecodeMalformedUtf8StringArray(byte[][] val)
        {
            var str = val.Select(a => Encoding.UTF8.GetString(a));

            EncodeDecode(
                e => e.WriteByteStringArray(null, val),
                d => d.ReadStringArray(null))
                .Should().BeEquivalentTo(str);
        }

        public static IEnumerable<object[]> DecodeDateTimeArrayData { get; } = new DateTime[][]
        {
            null,
            new DateTime[] {},
            new DateTime[] { new DateTime(1990, 1, 1)},
            new DateTime[] { new DateTime(2001, 12, 1, 15, 10, 20), new DateTime(2100, 2, 3, 20, 0, 0) }
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(DecodeDateTimeArrayData))]
        [Theory]
        public void DecodeDateTimeArray(DateTime[] val)
        {
            EncodeDecode(
                e => e.WriteDateTimeArray(null, val),
                d => d.ReadDateTimeArray(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> DecodeGuidArrayData { get; } = new Guid[][]
        {
            null,
            new Guid[] {},
            new [] { Guid.Parse("a8e248bc-4de5-4d5a-ae67-c065cbe452f3") },
            new [] { Guid.Parse("3494ff88-e744-42b5-9aef-b72c677845fe"), Guid.Parse("82b5cc4f-bdc8-41d6-9e53-93b8e0539806") },
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(DecodeGuidArrayData))]
        [Theory]
        public void DecodeGuidArray(Guid[] val)
        {
            EncodeDecode(
                e => e.WriteGuidArray(null, val),
                d => d.ReadGuidArray(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> DecodeByteStringArrayData { get; } = new byte[][][]
        {
            null,
            new byte[][] {},
            new byte[][] { new byte[] { } },
            new byte[][] { new byte[] { 7 } },
            new byte[][] { new byte[] { 7, 0, 4 }, new byte[] { 255 } },
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(DecodeByteStringArrayData))]
        [Theory]
        public void DecodeByteStringArray(byte[][] val)
        {
            EncodeDecode(
                e => e.WriteByteStringArray(null, val),
                d => d.ReadByteStringArray(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> DecodeXElementArrayData { get; } = new string[][]
        {
            null,
            new string[] {},
            new string[] { null },
            new string[] { "<br />" },
            new string[] { "<h1 class=\"one\"><p>text</p></h1>" },
            new string[] { "<br />", "<h1 class=\"one\"><p>text</p></h1>" },
        }
        .Select(x => new object[] { x?.Select(s => s is null ? null : XmlElementParse(s)).ToArray() });

        [MemberData(nameof(DecodeXElementArrayData))]
        [Theory]
        public void DecodeXElementArray(XmlElement[] val)
        {
            EncodeDecode(
                e => e.WriteXmlElementArray(null, val),
                d => d.ReadXElementArray(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> DecodeNodeIdArrayData { get; } = new Opc.Ua.NodeId[][]
        {
            null,
            new Opc.Ua.NodeId[] {},
            new Opc.Ua.NodeId[] { new Opc.Ua.NodeId(4, 0) },
            new Opc.Ua.NodeId[] { new Opc.Ua.NodeId(234, 3), new Opc.Ua.NodeId("Text", 1), new Opc.Ua.NodeId(Guid.Parse("a8e248bc-4de5-4d5a-ae67-c065cbe452f3"), 8) },
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(DecodeNodeIdArrayData))]
        [Theory]
        public void DecodeNodeIdArray(Opc.Ua.NodeId[] val)
        {
            EncodeDecode(
                e => e.WriteNodeIdArray(null, val),
                d => d.ReadNodeIdArray(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> DecodeExpandedNodeIdArrayData { get; } = new Opc.Ua.ExpandedNodeId[][]
        {
            null,
            new Opc.Ua.ExpandedNodeId[] {},
            new Opc.Ua.ExpandedNodeId[] { new Opc.Ua.ExpandedNodeId(4) },
            new Opc.Ua.ExpandedNodeId[] { new Opc.Ua.ExpandedNodeId(234), new Opc.Ua.ExpandedNodeId("Text"), new Opc.Ua.ExpandedNodeId(Guid.Parse("a8e248bc-4de5-4d5a-ae67-c065cbe452f3")) },
            new Opc.Ua.ExpandedNodeId[] { Opc.Ua.ExpandedNodeId.Parse("ns=1;i=234"), Opc.Ua.ExpandedNodeId.Parse("ns=2;s=bla"), Opc.Ua.ExpandedNodeId.Parse("svr=2;nsu=http://PLCopen.org/OpcUa/IEC61131-3;ns=3;b=Base64+Test=") },
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(DecodeExpandedNodeIdArrayData))]
        [Theory]
        public void DecodeExpandedNodeIdArray(Opc.Ua.ExpandedNodeId[] val)
        {
            EncodeDecode(
                e => e.WriteExpandedNodeIdArray(null, val),
                d => d.ReadExpandedNodeIdArray(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> DecodeStatusCodeArrayData { get; } = new Opc.Ua.StatusCode[][]
            {
                null,
                new Opc.Ua.StatusCode[] {},
                new Opc.Ua.StatusCode[] { StatusCodes.BadAttributeIdInvalid, StatusCodes.BadMaxAgeInvalid, StatusCodes.Good, StatusCodes.GoodClamped },
                new Opc.Ua.StatusCode[] { StatusCodes.GoodNoData }
            }
            .Select(x => new object[] { x });

        [MemberData(nameof(DecodeStatusCodeArrayData))]
        [Theory]
        public void DecodeStatusCodeArray(Opc.Ua.StatusCode[] val)
        {
            EncodeDecode(
                e => e.WriteStatusCodeArray(null, val),
                d => d.ReadStatusCodeArray(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> DecodeQualifiedNameArrayData { get; } = new Opc.Ua.QualifiedName[][]
        {
                null,
                new Opc.Ua.QualifiedName[] {},
                new [] { new Opc.Ua.QualifiedName("Tests", 3), new Opc.Ua.QualifiedName("22", 34) },
                new [] { new Opc.Ua.QualifiedName("Name") }
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(DecodeQualifiedNameArrayData))]
        [Theory]
        public void DecodeQualifiedNameArray(Opc.Ua.QualifiedName[] val)
        {
            EncodeDecode(
                e => e.WriteQualifiedNameArray(null, val),
                d => d.ReadQualifiedNameArray(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> DecodeLocalizedTextArrayData { get; } = new object[][]
        {
            null,
            new Opc.Ua.LocalizedText[] {},
            new [] { new Opc.Ua.LocalizedText("Text", null)},
            new [] { new Opc.Ua.LocalizedText("", ""), new Opc.Ua.LocalizedText("", "de") , new Opc.Ua.LocalizedText("", null)},
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(DecodeLocalizedTextArrayData))]
        [Theory]
        public void DecodeLocalizedTextArray(Opc.Ua.LocalizedText[] val)
        {
            EncodeDecode(
                e => e.WriteLocalizedTextArray(null, val),
                d => d.ReadLocalizedTextArray(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> DecodeVariantArrayData { get; } = new Opc.Ua.Variant[][]
        {
            null,
            new Opc.Ua.Variant[] {},
            new [] { new Opc.Ua.Variant("Text")},
            new [] { new Opc.Ua.Variant(1), new Opc.Ua.Variant((object)null), new Opc.Ua.Variant(2.0)},
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(DecodeVariantArrayData))]
        [Theory]
        public void DecodeVariantArray(Opc.Ua.Variant[] val)
        {
            EncodeDecode(
                e => e.WriteVariantArray(null, val),
                d => d.ReadVariantArray(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> DecodeDiagnosticInfoArrayData { get; } = new Opc.Ua.DiagnosticInfo[][]
        {
            null,
            new Opc.Ua.DiagnosticInfo[] { },
            new Opc.Ua.DiagnosticInfo[] { new Opc.Ua.DiagnosticInfo(2, 0, 0, 0, null) },
            new Opc.Ua.DiagnosticInfo[] { new Opc.Ua.DiagnosticInfo(2, 3, 4, 0, null), new Opc.Ua.DiagnosticInfo(2, 0, 6, 0, null) },
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(DecodeDiagnosticInfoArrayData))]
        [Theory]
        public void DecodeDiagnosticInfoArray(Opc.Ua.DiagnosticInfo[] val)
        {
            EncodeDecode(
                e => e.WriteDiagnosticInfoArray(null, val),
                d => d.ReadDiagnosticInfoArray(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> DecodeDataValueArrayData { get; } = new Opc.Ua.DataValue[][]
        {
            null,
            new Opc.Ua.DataValue[] { },
            new Opc.Ua.DataValue[] { new Opc.Ua.DataValue(new Opc.Ua.Variant(23.0), StatusCodes.BadDataLost, new DateTime(1990,1,1), DateTime.UtcNow)},
            new Opc.Ua.DataValue[]
            {
                new Opc.Ua.DataValue(new Opc.Ua.Variant(23.0), StatusCodes.BadDataLost, new DateTime(1990,1,1), DateTime.UtcNow)
                {
                    SourcePicoseconds = 13,
                    ServerPicoseconds = 150
                },
                new Opc.Ua.DataValue(new Opc.Ua.Variant(28), StatusCodes.GoodClamped, new DateTime(1990,12,1), DateTime.UtcNow),
            }
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(DecodeDataValueArrayData))]
        [Theory]
        public void DecodeDataValueArray(Opc.Ua.DataValue[] val)
        {
            EncodeDecode(
                e => e.WriteDataValueArray(null, val),
                d => d.ReadDataValueArray(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> DecodeEnumerationArrayData { get; } = new TypeCode[][]
        {
            null,
            new TypeCode[] { },
            new TypeCode[] { TypeCode.Boolean, TypeCode.Double }
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(DecodeEnumerationArrayData))]
        [Theory]
        public void DecodeEnumerationArray(TypeCode[] val)
        {
            EncodeDecode(
                e => e.WriteEnumeratedArray(null, val, typeof(TypeCode)),
                d => d.ReadEnumerationArray<TypeCode>(null))
                .Should().BeEquivalentTo(val);
        }

        public static IEnumerable<object[]> DecodeEncodableArrayData { get; } = new Opc.Ua.TimeZoneDataType[][]
        {
            null,
            new Opc.Ua.TimeZoneDataType[] { },
            new Opc.Ua.TimeZoneDataType[]
            {
                new Opc.Ua.TimeZoneDataType { Offset = 1, DaylightSavingInOffset = true },
                new Opc.Ua.TimeZoneDataType { Offset = 3, DaylightSavingInOffset = false }
            }
        }
        .Select(x => new object[] { x });

        [MemberData(nameof(DecodeEncodableArrayData))]
        [Theory]
        public void DecodeEncodableArray(Opc.Ua.TimeZoneDataType[] val)
        {
            EncodeDecode(
                e => e.WriteEncodeableArray(null, val, typeof(Opc.Ua.TimeZoneDataType)),
                d => d.ReadEncodableArray<TimeZoneDataType>(null))
                .Should().BeEquivalentTo(val);
        }
    }
}
