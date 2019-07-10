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
        public static IEnumerable<object[]> CreateObjectData { get; } = new object[][]
        {
            new object[] { default, VariantType.Null },
            new object[] { true, VariantType.Boolean },
            new object[] { (sbyte)13, VariantType.SByte },
            new object[] { (byte)13, VariantType.Byte },
            new object[] { (short)13, VariantType.Int16 },
            new object[] { (ushort)13, VariantType.UInt16 },
            new object[] { 13, VariantType.Int32 },
            new object[] { (uint)13, VariantType.UInt32 },
            new object[] { (long)13, VariantType.Int64 },
            new object[] { (ulong)13, VariantType.UInt64 },
            new object[] { (float)13, VariantType.Float },
            new object[] { (double)13, VariantType.Double },
            new object[] { "13", VariantType.String },
            new object[] { new DateTime(0L), VariantType.DateTime },
            new object[] { Guid.NewGuid(), VariantType.Guid},
            new object[] { new byte[] { 0x1, 0x3}, VariantType.ByteString },
            new object[] { XElement.Parse(@"<Item AttributeA=""A"" AttributeB=""B"" />"), VariantType.XmlElement },
            new object[] { new NodeId(42), VariantType.NodeId },
            new object[] { new ExpandedNodeId(new NodeId(42)), VariantType.ExpandedNodeId },
            new object[] { new StatusCode(43), VariantType.StatusCode },
            new object[] { new QualifiedName("foo"), VariantType.QualifiedName },
            new object[] { new LocalizedText("foo"), VariantType.LocalizedText },
            new object[] { new ExtensionObject(null), VariantType.ExtensionObject },
        };

        [MemberData(nameof(CreateObjectData))]
        [Theory]
        public void CreateObject(object val, VariantType type)
        {
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(type);
            v.ArrayDimensions
                .Should().BeNull();
        }

        public static IEnumerable<object[]> CreateArrayObjectData { get; } = new object[][]
        {
            new object[] { 2, new[] { true, false }, VariantType.Boolean },
            new object[] { 1, new sbyte[] { 13 }, VariantType.SByte },
            new object[] { 2, new short[] { 13, 15 }, VariantType.Int16 },
            new object[] { 2, new ushort[] { 13, 14 }, VariantType.UInt16 },
            new object[] { 1, new[] { 13 }, VariantType.Int32 },
            new object[] { 0, new int[] {}, VariantType.Int32 },
            new object[] { 3, new uint[] { 13, 14, 15 }, VariantType.UInt32 },
            new object[] { 3, new long[] { 13, 17, 19 }, VariantType.Int64 },
            new object[] { 3, new ulong[] { 13, 4, 1 }, VariantType.UInt64 },
            new object[] { 2, new[] { 13.0f, 4.1f }, VariantType.Float },
            new object[] { 2, new[] { 13.0, 5 }, VariantType.Double },
            new object[] { 2, new[] { "13", "A" }, VariantType.String },
            new object[] { 1, new[] { new DateTime(0L) }, VariantType.DateTime },
            new object[] { 1, new[] { Guid.NewGuid() }, VariantType.Guid},
            new object[] { 1, new byte[][] { new byte[]{ 0x1, 0x3 } }, VariantType.ByteString },
            new object[] { 1, new[] { XElement.Parse(@"<Item AttributeA=""A"" AttributeB=""B"" />") }, VariantType.XmlElement },
            new object[] { 2, new[] { new NodeId(42), new NodeId(43) }, VariantType.NodeId },
            new object[] { 1, new[] { new ExpandedNodeId(new NodeId(42)) }, VariantType.ExpandedNodeId },
            new object[] { 1, new[] { new StatusCode(43) }, VariantType.StatusCode },
            new object[] { 1, new[] { new QualifiedName("foo") }, VariantType.QualifiedName },
            new object[] { 1, new[] { new LocalizedText("foo") }, VariantType.LocalizedText },
            new object[] { 1, new[] { new ExtensionObject(null) }, VariantType.ExtensionObject },
        };

        [MemberData(nameof(CreateArrayObjectData))]
        [Theory]
        public void CreateArrayObject(int length, object val, VariantType type)
        {
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(type);
            v.ArrayDimensions
                .Should().ContainSingle()
                .Which
                .Should().Be(length);
        }

        [MemberData(nameof(CreateArrayObjectData))]
        [Theory]
        public void CreateArray(int length, Array val, VariantType type)
        {
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(type);
            v.ArrayDimensions
                .Should().ContainSingle()
                .Which
                .Should().Be(length);
        }

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
        public void CreateBooleanArray()
        {
            var val = new [] { true, false, true };
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.Boolean);
            v.ArrayDimensions
                .Should().ContainSingle()
                .Which
                .Should().Be(val.Length);
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
        public void CreateSByteArray()
        {
            var val = new sbyte[] { 2, 3, 4, 0 };
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.SByte);
            v.ArrayDimensions
                .Should().ContainSingle()
                .Which
                .Should().Be(val.Length);
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
        public void CreateShortArray()
        {
            var val = new short[]{ 2 };
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.Int16);
            v.ArrayDimensions
                .Should().ContainSingle()
                .Which
                .Should().Be(val.Length);
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
        public void CreateUShortArray()
        {
            var val = new ushort[] { };
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.UInt16);
            v.ArrayDimensions
                .Should().ContainSingle()
                .Which
                .Should().Be(val.Length);
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
        public void CreateIntArray()
        {
            var val = new[] { 2, 4, 1, 3 };
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.Int32);
            v.ArrayDimensions
                .Should().ContainSingle()
                .Which
                .Should().Be(val.Length);
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
        public void CreateUIntArray()
        {
            var val = new uint[] { 2 };
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.UInt32);
            v.ArrayDimensions
                .Should().ContainSingle()
                .Which
                .Should().Be(val.Length);
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
        public void CreateLongArray()
        {
            var val = new long[] { 2, 0 };
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.Int64);
            v.ArrayDimensions
                .Should().ContainSingle()
                .Which
                .Should().Be(val.Length);
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
        public void CreateULongArray()
        {
            var val = new ulong[] { 2, 0 };
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.UInt64);
            v.ArrayDimensions
                .Should().ContainSingle()
                .Which
                .Should().Be(val.Length);
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
        public void CreateFloatArray()
        {
            var val = new float[] { 2 };
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.Float);
            v.ArrayDimensions
                .Should().ContainSingle()
                .Which
                .Should().Be(val.Length);
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
        public void CreateDoubleArray()
        {
            var val = new double[] { 2.0, 6.7 };
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.Double);
            v.ArrayDimensions
                .Should().ContainSingle()
                .Which
                .Should().Be(val.Length);
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
        public void CreateStringArray()
        {
            var val = new[] { "Test string", "Test" };
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.String);
            v.ArrayDimensions
                .Should().ContainSingle()
                .Which
                .Should().Be(val.Length);
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
        public void CreateDateTimeArray()
        {
            var val = new[] { DateTime.Parse("2012-12-24") };
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.DateTime);
            v.ArrayDimensions
                .Should().ContainSingle()
                .Which
                .Should().Be(val.Length);
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
        public void CreateGuidArray()
        {
            var val = new[] { Guid.NewGuid(), Guid.NewGuid() };
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.Guid);
            v.ArrayDimensions
                .Should().ContainSingle()
                .Which
                .Should().Be(val.Length);
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
        public void CreateByteStringArray()
        {
            var val = new byte[][] { new byte[]{ 0x48, 0x65, 0x6c, 0x6c, 0x6f } };
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.ByteString);
            v.ArrayDimensions
                .Should().ContainSingle()
                .Which
                .Should().Be(val.Length);
        }

        [Fact]
        public void CreateXElementArray()
        {
            var val = new[] { XElement.Parse(@"<Item AttributeA=""A"" AttributeB=""B"" />") };
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.XmlElement);
            v.ArrayDimensions
                .Should().ContainSingle()
                .Which
                .Should().Be(val.Length);
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
        public void CreateNodeIdArray()
        {
            var val = new[] { new NodeId(Guid.NewGuid(), 2) };
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.NodeId);
            v.ArrayDimensions
                .Should().ContainSingle()
                .Which
                .Should().Be(val.Length);
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
        public void CreateExpandedNodeIdArray()
        {
            var val = new[] { new ExpandedNodeId(5), new ExpandedNodeId(7) };
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.ExpandedNodeId);
            v.ArrayDimensions
                .Should().ContainSingle()
                .Which
                .Should().Be(val.Length);
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
        public void CreateStatusCodeArray()
        {
            var val = new[] { new StatusCode(2), new StatusCode(3) };
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.StatusCode);
            v.ArrayDimensions
                .Should().ContainSingle()
                .Which
                .Should().Be(val.Length);
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
        public void CreateQualifiedNameArray()
        {
            var val = new[] { new QualifiedName("name") };
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.QualifiedName);
            v.ArrayDimensions
                .Should().ContainSingle()
                .Which
                .Should().Be(val.Length);
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

        [Fact]
        public void CreateLocalizedTextArray()
        {
            var val = new[] { new LocalizedText("Text"), new LocalizedText("Test") };
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.LocalizedText);
            v.ArrayDimensions
                .Should().ContainSingle()
                .Which
                .Should().Be(val.Length);
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

        [Fact]
        public void CreateEnumArray()
        {
            var val = new Enum[] { TestEnumeration.B, TestEnumeration.A };
            var v = new Variant(val);

            v.Value
                .Should().BeEquivalentTo(new[] { (int)TestEnumeration.B, (int)TestEnumeration.A });
            v.Type
                .Should().Be(VariantType.Int32);
            v.ArrayDimensions
                .Should().ContainSingle()
                .Which
                .Should().Be(val.Length);
        }
    }
}
