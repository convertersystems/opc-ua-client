using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Workstation.ServiceModel.Ua;
using Xunit;

namespace Workstation.UaClient.UnitTests
{
    public class VariantTests
    {
        [Fact]
        public void CreateBoolean()
        {
            var val = true;
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.Boolean);
            v.ArrayDimensions
                .Should().BeNull();
        }

        [Fact]
        public void CreateSByte()
        {
            var val = (sbyte)2;
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.SByte);
            v.ArrayDimensions
                .Should().BeNull();
        }

        [Fact]
        public void CreateByte()
        {
            var val = (byte)2;
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.Byte);
            v.ArrayDimensions
                .Should().BeNull();
        }

        [Fact]
        public void CreateShort()
        {
            var val = (short)2;
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.Int16);
            v.ArrayDimensions
                .Should().BeNull();
        }

        [Fact]
        public void CreateUShort()
        {
            var val = (ushort)2;
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.UInt16);
            v.ArrayDimensions
                .Should().BeNull();
        }

        [Fact]
        public void CreateInt()
        {
            var val = 2;
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.Int32);
            v.ArrayDimensions
                .Should().BeNull();
        }

        [Fact]
        public void CreateUInt()
        {
            var val = (uint)2;
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.UInt32);
            v.ArrayDimensions
                .Should().BeNull();
        }

        [Fact]
        public void CreateLong()
        {
            var val = (long)2;
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.Int64);
            v.ArrayDimensions
                .Should().BeNull();
        }

        [Fact]
        public void CreateULong()
        {
            var val = (ulong)2;
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.UInt64);
            v.ArrayDimensions
                .Should().BeNull();
        }

        [Fact]
        public void CreateFloat()
        {
            var val = (float)2;
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.Float);
            v.ArrayDimensions
                .Should().BeNull();
        }

        [Fact]
        public void CreateDouble()
        {
            var val = (double)2;
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.Double);
            v.ArrayDimensions
                .Should().BeNull();
        }

        [Fact]
        public void CreateString()
        {
            var val = "Test string";
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.String);
            v.ArrayDimensions
                .Should().BeNull();
        }

        [Fact]
        public void CreateDateTime()
        {
            var val = DateTime.Parse("2012-12-24");
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.DateTime);
            v.ArrayDimensions
                .Should().BeNull();
        }

        [Fact]
        public void CreateGuid()
        {
            var val = Guid.NewGuid();
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.Guid);
            v.ArrayDimensions
                .Should().BeNull();
        }

        [Fact]
        public void CreateByteString()
        {
            var val = new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f };
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.ByteString);
            v.ArrayDimensions
                .Should().BeNull();
        }

        [Fact]
        public void CreateXElement()
        {
            var val = XElement.Parse(@"<Item AttributeA=""A"" AttributeB=""B"" />");
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.XmlElement);
            v.ArrayDimensions
                .Should().BeNull();
        }

        [Fact]
        public void CreateNodeId()
        {
            var val = new NodeId(Guid.NewGuid(), 2);
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.NodeId);
            v.ArrayDimensions
                .Should().BeNull();
        }

        [Fact]
        public void CreateExpandedNodeId()
        {
            var val = new ExpandedNodeId(5);
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.ExpandedNodeId);
            v.ArrayDimensions
                .Should().BeNull();
        }

        [Fact]
        public void CreateStatusCode()
        {
            var val = new StatusCode(2);
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.StatusCode);
            v.ArrayDimensions
                .Should().BeNull();
        }

        [Fact]
        public void CreateQualifiedName()
        {
            var val = new QualifiedName("name");
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.QualifiedName);
            v.ArrayDimensions
                .Should().BeNull();
        }

        [Fact]
        public void CreateLocalizedText()
        {
            var val = new LocalizedText("Text");
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.LocalizedText);
            v.ArrayDimensions
                .Should().BeNull();
        }

        enum TestEnumeration
        {
            A,
            B
        }

        [Fact]
        public void CreateEnum()
        {
            var val = TestEnumeration.B;
            var v = new Variant(val);

            v.Value
                .Should().Be((int)val);
            v.Type
                .Should().Be(VariantType.Int32);
            v.ArrayDimensions
                .Should().BeNull();
        }
    }
}
