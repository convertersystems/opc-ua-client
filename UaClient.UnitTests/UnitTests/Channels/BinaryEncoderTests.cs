using FluentAssertions;
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
    public class BinaryEncoderTests
    {
        private T EncodeDecode<T>(Action<BinaryEncoder> encode, Func<Opc.Ua.BinaryDecoder, T> decode)
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

        [Fact]
        public void EncodeBoolean()
        {
            var val = true;

            EncodeDecode(
                e => e.WriteBoolean(null, val),
                d => d.ReadBoolean(null))
                .Should().Be(val);
        }

        [Fact]
        public void EncodeSByte()
        {
            var val = (sbyte)43;

            EncodeDecode(
                e => e.WriteSByte(null, val),
                d => d.ReadSByte(null))
                .Should().Be(val);
        }

        [Fact]
        public void EncodeByte()
        {
            var val = (byte)43;

            EncodeDecode(
                e => e.WriteByte(null, val),
                d => d.ReadByte(null))
                .Should().Be(val);
        }

        [Fact]
        public void EncodeInt16()
        {
            var val = (short)43;

            EncodeDecode(
                e => e.WriteInt16(null, val),
                d => d.ReadInt16(null))
                .Should().Be(val);
        }

        [Fact]
        public void EncodeUInt16()
        {
            var val = (ushort)43;

            EncodeDecode(
                e => e.WriteUInt16(null, val),
                d => d.ReadUInt16(null))
                .Should().Be(val);
        }

        [Fact]
        public void EncodeInt32()
        {
            var val = 123;

            EncodeDecode(
                e => e.WriteInt32(null, val),
                d => d.ReadInt32(null))
                .Should().Be(val);
        }

        [Fact]
        public void EncodeUInt32()
        {
            var val = (uint)123;

            EncodeDecode(
                e => e.WriteUInt32(null, val),
                d => d.ReadUInt32(null))
                .Should().Be(val);
        }

        [Fact]
        public void EncodeInt64()
        {
            var val = (long)123;

            EncodeDecode(
                e => e.WriteInt64(null, val),
                d => d.ReadInt64(null))
                .Should().Be(val);
        }

        [Fact]
        public void EncodeUInt64()
        {
            var val = (ulong)123;

            EncodeDecode(
                e => e.WriteUInt64(null, val),
                d => d.ReadUInt64(null))
                .Should().Be(val);
        }

        [Fact]
        public void EncodeFloat()
        {
            var val = 4.13f;

            EncodeDecode(
                e => e.WriteFloat(null, val),
                d => d.ReadFloat(null))
                .Should().Be(val);
        }

        [Fact]
        public void EncodeDouble()
        {
            var val = 4.13;

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

        public static IEnumerable<object[]> EncodeDateTimeData { get; } = new object[][]
        {
            new object[] { new DateTime(0), new DateTime(0) },
            new object[] { new DateTime(1601, 1, 1, 0, 0, 1), new DateTime(1601,1,1,0,0,1) },
            new object[] { new DateTime(1990, 1, 1), new DateTime(1990, 1, 1) },
            new object[] { DateTime.MinValue, DateTime.MinValue },
            new object[] { DateTime.MaxValue, DateTime.MaxValue },
        };

        [MemberData(nameof(EncodeDateTimeData))]
        [Theory]
        public void EncodeDateTime(DateTime input, DateTime expected)
        {
            EncodeDecode(
                e => e.WriteDateTime(null, input),
                d => d.ReadDateTime(null))
                .Should().Be(expected);
        }

        [Fact]
        public void EncodeGuid()
        {
            var val = Guid.NewGuid();
            EncodeDecode(
                e => e.WriteGuid(null, val),
                d => d.ReadGuid(null))
                .Should().Be(val);
        }

        [Fact]
        public void EncodeByteString()
        {
            var val = new byte[] { 0x45, 0xf3, 0x00, 0x34, 0xff, 0x01 };
            EncodeDecode(
                e => e.WriteByteString(null, val),
                d => d.ReadByteString(null))
                .Should().BeEquivalentTo(val);
        }

        [Fact]
        public void EncodeXElement()
        {
            string xml = @"
                <Window x:Class=""WpfApplication1.Window1""
                        xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                        xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                        Title= ""Window1""
                        Height=""300""
                        Width=""300"">
                    <Grid>
                    </Grid>
                </Window>
            ";

            var xelem = XElement.Parse(xml);
            var xnode = ToXmlNode(xelem);

            EncodeDecode(
                e => e.WriteXElement(null, xelem),
                d => d.ReadXmlElement(null))
                .Should().BeEquivalentTo(xnode);
        }

        private static XmlNode ToXmlNode(XElement element)
        {
            using (XmlReader reader = element.CreateReader())
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(reader);
                return doc;
            }
        }

        [InlineData("ns=0;i=12")]
        [InlineData("ns=0;i=300")]
        [InlineData("ns=2;i=12")]
        [InlineData("ns=30;i=850000")]
        [InlineData("ns=300;i=12")]
        [InlineData("ns=300;i=850000")]
        [InlineData("ns=3;s=TestString")]
        [InlineData("ns=3;g=8994DA00-5CE1-461F-963C-43F7CFC6864E")]
        [InlineData("ns=3;b=Base64+Test=")]
        [Theory]
        public void EncodeNodeId(string id)
        {
            var input = NodeId.Parse(id);
            var expected = Opc.Ua.NodeId.Parse(id);

            EncodeDecode(
                e => e.WriteNodeId(null, input),
                d => d.ReadNodeId(null))
                .Should().Be(expected);
        }

        [InlineData("ns=0;i=12")]
        [InlineData("svr=1;ns=0;i=300", Skip = "Because of a possible bug in the reference implementation")]
        [InlineData("svr=2;nsu=http://PLCopen.org/OpcUa/IEC61131-3;ns=2;i=12")]
        [InlineData("nsu=http://PLCopen.org/OpcUa/IEC61131-3;ns=30;i=850000")]
        [InlineData("svr=0;ns=300;i=12")]
        [InlineData("ns=300;i=850000")]
        [InlineData("svr=123;ns=3;s=TestString", Skip = "Because of a possible bug in the reference implementation")]
        [InlineData("ns=3;g=8994DA00-5CE1-461F-963C-43F7CFC6864E")]
        [InlineData("svr=2;nsu=http://PLCopen.org/OpcUa/IEC61131-3;ns=3;b=Base64+Test=")]
        [Theory]
        public void EncodeExpandedNodeId(string id)
        {
            var input = ExpandedNodeId.Parse(id);
            var expected = Opc.Ua.ExpandedNodeId.Parse(id);

            EncodeDecode(
                e => e.WriteExpandedNodeId(null, input),
                d => d.ReadExpandedNodeId(null))
                .Should().Be(expected);
        }

        [Fact]
        public void EncodeStatusCode()
        {
            var val = StatusCodes.BadCertificateHostNameInvalid;

            EncodeDecode(
                e => e.WriteStatusCode(null, val),
                d => d.ReadStatusCode(null))
                .Should().Be(val);
        }

        public static IEnumerable<object[]> EncodeDiagnosticInfoData { get; } = new object[][]
        {
            new object[] { new DiagnosticInfo() },
            new object[] { new DiagnosticInfo(2) },
            new object[] { new DiagnosticInfo(2, 3) },
            new object[] { new DiagnosticInfo(2, 3, 4) },
            new object[] { new DiagnosticInfo(2, 3, 4, 5) },
            new object[] { new DiagnosticInfo(2, 3, 4, 5, "Text text text.") },
            new object[] { new DiagnosticInfo(2, additionalInfo:"Test test test.") },
            new object[] { new DiagnosticInfo(2, locale:6, innerStatusCode: StatusCodes.BadSessionIdInvalid) },
        };

        [MemberData(nameof(EncodeDiagnosticInfoData))]
        [Theory]
        public void EncodeDiagnosticInfo(DiagnosticInfo input)
        {
            var output = EncodeDecode(
                e => e.WriteDiagnosticInfo(null, input),
                d => d.ReadDiagnosticInfo(null));

            output.AdditionalInfo
                .Should().Be(input.AdditionalInfo);

            output.InnerDiagnosticInfo
                .Should().Be(input.InnerDiagnosticInfo);

            output.InnerStatusCode.Code
                .Should().Be(input.InnerStatusCode);

            output.Locale
                .Should().Be(input.Locale);

            output.LocalizedText
                .Should().Be(input.LocalizedText);

            output.NamespaceUri
                .Should().Be(input.NamespaceUri);

            output.SymbolicId
                .Should().Be(input.SymbolicId);
        }

        [Fact]
        public void EncodeQualifiedName()
        {
            var val = QualifiedName.Parse("4:Test");

            EncodeDecode(
                e => e.WriteQualifiedName(null, val),
                d => d.ReadQualifiedName(null))
                .Should().BeEquivalentTo(val, options => options.ComparingByMembers<QualifiedName>());
        }

        public static IEnumerable<object[]> EncodeLocalizedTextData { get; } = new object[][]
        {
            new object[] { new LocalizedText("Text", "")},
            new object[] { new LocalizedText("Text", "de")},
            new object[] { new LocalizedText("Text", null)},
            new object[] { new LocalizedText("", "")},
            new object[] { new LocalizedText("", "de")},
            new object[] { new LocalizedText("", null)},
            new object[] { new LocalizedText(null, "")},
            new object[] { new LocalizedText(null, "de")},
            new object[] { new LocalizedText(null, null)},
        };

        [MemberData(nameof(EncodeLocalizedTextData))]
        [Theory]
        public void EncodeLocalizedText(LocalizedText val)
        {
            EncodeDecode(
                e => e.WriteLocalizedText(null, val),
                d => d.ReadLocalizedText(null))
                .Should().BeEquivalentTo(val, options => options.ComparingByMembers<LocalizedText>());
        }

        public static IEnumerable<object[]> EncodeVariantData { get; } = new object[][]
        {
            new object[] { default },
            new object[] { true },
            new object[] { (sbyte)13 },
            new object[] { (byte)13 },
            new object[] { (short)13 },
            new object[] { (ushort)13 },
            new object[] { 13 },
            new object[] { (uint)13 },
            new object[] { (long)13 },
            new object[] { (ulong)13 },
            new object[] { (float)13 },
            new object[] { (double)13 },
            new object[] { "13" },
            new object[] { new DateTime(0L) },
            new object[] { Guid.NewGuid() },
            new object[] { new byte[] { 0x1, 0x3} },
        };

        [MemberData(nameof(EncodeVariantData))]
        [Theory]
        public void EncodeVariant(object obj)
        {
            var input = new Variant(obj);

            var output = EncodeDecode(
                e => e.WriteVariant(null, input),
                d => d.ReadVariant(null));

            output.Value
                .Should().BeEquivalentTo(input.Value);

            ((int)output.TypeInfo.BuiltInType)
                .Should().Be((int)input.Type);
        }

        [Fact]
        public void EncodeXElementVariant()
        {
            string xml = @"<Item AttributeA=""A"" AttributeB=""B"" />";

            var xelem = XElement.Parse(xml);
            var xnode = ToXmlNode(xelem);

            var input = new Variant(xelem);

            var output = EncodeDecode(
                e => e.WriteVariant(null, input),
                d => d.ReadVariant(null));

            (output.Value as XmlNode)
                .Should().BeEquivalentTo(xnode);

            ((int)output.TypeInfo.BuiltInType)
                .Should().Be((int)input.Type);
        }

        [Fact]
        public void EncodeNodeIdVariant()
        {
            var id = "ns=3;s=Test.Node";
            var input = new Variant(NodeId.Parse(id));
            var expected = Opc.Ua.NodeId.Parse(id);

            var output = EncodeDecode(
                e => e.WriteVariant(null, input),
                d => d.ReadVariant(null));

            output.Value
                .Should().Be(expected);

            ((int)output.TypeInfo.BuiltInType)
                .Should().Be((int)input.Type);
        }

        [Fact]
        public void EncodeExpandedNodeIdVariant()
        {
            var id = "svr=2;nsu=http://PLCopen.org/OpcUa/IEC61131-3;ns=2;i=12";
            var input = new Variant(ExpandedNodeId.Parse(id));
            var expected = Opc.Ua.ExpandedNodeId.Parse(id);

            var output = EncodeDecode(
                e => e.WriteVariant(null, input),
                d => d.ReadVariant(null));

            output.Value
                .Should().Be(expected);

            ((int)output.TypeInfo.BuiltInType)
                .Should().Be((int)input.Type);
        }

        [Fact]
        public void EncodeStatusCodeVariant()
        {
            var input = new Variant(new StatusCode(43));
            var expected = new Opc.Ua.StatusCode(43);

            var output = EncodeDecode(
                e => e.WriteVariant(null, input),
                d => d.ReadVariant(null));

            output.Value
                .Should().Be(expected);

            ((int)output.TypeInfo.BuiltInType)
                .Should().Be((int)input.Type);
        }

        [Fact]
        public void EncodeQualifiedNameVariant()
        {
            var input = new Variant(QualifiedName.Parse("4:Test"));

            var output = EncodeDecode(
                e => e.WriteVariant(null, input),
                d => d.ReadVariant(null));

            output.Value
                .Should().BeEquivalentTo(input.Value, options => options.ComparingByMembers<QualifiedName>());

            ((int)output.TypeInfo.BuiltInType)
                .Should().Be((int)input.Type);
        }

        [Fact]
        public void EncodeLocalizedTextVariant()
        {
            var input = new Variant(new LocalizedText("foo", "fr-FR"));

            var output = EncodeDecode(
                e => e.WriteVariant(null, input),
                d => d.ReadVariant(null));

            output.Value
                .Should().BeEquivalentTo(input.Value, options => options.ComparingByMembers<LocalizedText>());

            ((int)output.TypeInfo.BuiltInType)
                .Should().Be((int)input.Type);
        }
    }
}
