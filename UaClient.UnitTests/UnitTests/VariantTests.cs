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
        
        [Fact]
        public void CreateObjectEncodeable()
        {
            object val = new ReadRequest();
            var v = new Variant(val);

            ((ExtensionObject)v.Value).Body
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.ExtensionObject);
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

        [Fact]
        public void CreateArrayObjectEncodeable()
        {
            object val = new[] { new ReadRequest() };
            var v = new Variant(val);

            ((ExtensionObject[])v.Value)
                .Select(eo => eo.Body)
                .Should().BeEquivalentTo((object[])val);

            v.Type
                .Should().Be(VariantType.ExtensionObject);
            v.ArrayDimensions
                .Should().ContainSingle()
                .Which
                .Should().Be(1);
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
        public void CreateArrayUnsupported()
        {
            Array val = new DateTimeOffset[] { };

            val.Invoking(v => new Variant(v))
                .Should().Throw<ArgumentOutOfRangeException>();
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
        public void ImplicitCreateBoolean()
        {
            var val = true;
            Variant v1 = val;
            var v2 = new Variant(val);

            v1
                .Should().Be(v2);
        }
        
        [Fact]
        public void ExplicitConvertToBoolean()
        {
            var v1 = true;
            var val = new Variant(v1);
            var v2 = (bool)val;

            v1
                .Should().Be(v2);
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
        public void ImplicitCreateBooleanArray()
        {
            var val = new[] { true, false, true };
            Variant v1 = val;
            var v2 = new Variant(val);

            v1.Value
                .Should().Be(v2.Value);
        }
        
        [Fact]
        public void ExplicitConvertToBooleanArray()
        {
            var v1 = new[] { true, false, true };
            var val = new Variant(v1);
            var v2 = (bool[])val;

            v1
                .Should().BeEquivalentTo(v2);
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
        public void ImplicitCreateSByte()
        {
            var val = (sbyte)2;
            Variant v1 = val;
            var v2 = new Variant(val);

            v1
                .Should().Be(v2);
        }
        
        [Fact]
        public void ExplicitConvertToSByte()
        {
            var v1 = (sbyte)2;
            var val = new Variant(v1);
            var v2 = (sbyte)val;

            v1
                .Should().Be(v2);
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
        public void ImplicitCreateSByteArray()
        {
            var val = new sbyte[] { 2, 3, 4, 0 };
            Variant v1 = val;
            var v2 = new Variant(val);

            v1.Value
                .Should().Be(v2.Value);
        }
        
        [Fact]
        public void ExplicitConvertToSByteArray()
        {
            var v1 = new sbyte[] { 2, 3, 4, 0 };
            var val = new Variant(v1);
            var v2 = (sbyte[])val;

            v1
                .Should().BeEquivalentTo(v2);
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
        public void ImplicitCreateByte()
        {
            var val = (byte)2;
            Variant v1 = val;
            var v2 = new Variant(val);

            v1
                .Should().Be(v2);
        }
        
        [Fact]
        public void ExplicitConvertToByte()
        {
            var v1 = (byte)2;
            var val = new Variant(v1);
            var v2 = (byte)val;

            v1
                .Should().Be(v2);
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
        public void ImplicitCreateShort()
        {
            var val = (short)2;
            Variant v1 = val;
            var v2 = new Variant(val);

            v1
                .Should().Be(v2);
        }

        [Fact]
        public void ExplicitConvertToShort()
        {
            var v1 = (short)2;
            var val = new Variant(v1);
            var v2 = (short)val;

            v1
                .Should().Be(v2);
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
        public void ImplicitCreateShortArray()
        {
            var val = new short[] { 2, 3, 4, 0 };
            Variant v1 = val;
            var v2 = new Variant(val);

            v1.Value
                .Should().Be(v2.Value);
        }
        
        [Fact]
        public void ExplicitConvertToShortArray()
        {
            var v1 = new short[] { 2, 3, 4, 0 };
            var val = new Variant(v1);
            var v2 = (short[])val;

            v1
                .Should().BeEquivalentTo(v2);
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
        public void ImplicitCreateUShort()
        {
            var val = (ushort)2;
            Variant v1 = val;
            var v2 = new Variant(val);

            v1
                .Should().Be(v2);
        }

        [Fact]
        public void ExplicitConvertToUShort()
        {
            var v1 = (ushort)2;
            var val = new Variant(v1);
            var v2 = (ushort)val;

            v1
                .Should().Be(v2);
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
        public void ImplicitCreateUShortArray()
        {
            var val = new ushort[] { 2, 3, 4, 0 };
            Variant v1 = val;
            var v2 = new Variant(val);

            v1.Value
                .Should().Be(v2.Value);
        }
        
        [Fact]
        public void ExplicitConvertToUShortArray()
        {
            var v1 = new ushort[] { 2, 3, 4, 0 };
            var val = new Variant(v1);
            var v2 = (ushort[])val;

            v1
                .Should().BeEquivalentTo(v2);
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
        public void ImplicitCreateInt()
        {
            var val = 2;
            Variant v1 = val;
            var v2 = new Variant(val);

            v1
                .Should().Be(v2);
        }

        [Fact]
        public void ExplicitConvertToInt()
        {
            var v1 = 2;
            var val = new Variant(v1);
            var v2 = (int)val;

            v1
                .Should().Be(v2);
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
        public void ImplicitCreateIntArray()
        {
            var val = new int[] { 2, 3, 4, 0 };
            Variant v1 = val;
            var v2 = new Variant(val);

            v1.Value
                .Should().Be(v2.Value);
        }
        
        [Fact]
        public void ExplicitConvertToIntArray()
        {
            var v1 = new int[] { 2, 3, 4, 0 };
            var val = new Variant(v1);
            var v2 = (int[])val;

            v1
                .Should().BeEquivalentTo(v2);
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
        public void ImplicitCreateUInt()
        {
            var val = (uint)2;
            Variant v1 = val;
            var v2 = new Variant(val);

            v1
                .Should().Be(v2);
        }

        [Fact]
        public void ExplicitConvertToUInt()
        {
            var v1 = (uint)2;
            var val = new Variant(v1);
            var v2 = (uint)val;

            v1
                .Should().Be(v2);
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
        public void ImplicitCreateUIntArray()
        {
            var val = new uint[] { 2, 3, 4, 0 };
            Variant v1 = val;
            var v2 = new Variant(val);

            v1.Value
                .Should().Be(v2.Value);
        }
        
        [Fact]
        public void ExplicitConvertToUIntArray()
        {
            var v1 = new uint[] { 2, 3, 4, 0 };
            var val = new Variant(v1);
            var v2 = (uint[])val;

            v1
                .Should().BeEquivalentTo(v2);
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
        public void ImplicitCreateLong()
        {
            var val = (long)2;
            Variant v1 = val;
            var v2 = new Variant(val);

            v1
                .Should().Be(v2);
        }

        [Fact]
        public void ExplicitConvertToLong()
        {
            var v1 = (long)2;
            var val = new Variant(v1);
            var v2 = (long)val;

            v1
                .Should().Be(v2);
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
        public void ImplicitCreateLongArray()
        {
            var val = new long[] { 2, 0 };
            Variant v1 = val;
            var v2 = new Variant(val);

            v1.Value
                .Should().Be(v2.Value);
        }
        
        [Fact]
        public void ExplicitConvertToLongArray()
        {
            var v1 = new long[] { 2, 0 };
            var val = new Variant(v1);
            var v2 = (long[])val;

            v1
                .Should().BeEquivalentTo(v2);
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
        public void ImplicitCreateULong()
        {
            var val = (ulong)2;
            Variant v1 = val;
            var v2 = new Variant(val);

            v1
                .Should().Be(v2);
        }

        [Fact]
        public void ExplicitConvertToULong()
        {
            var v1 = (ulong)2;
            var val = new Variant(v1);
            var v2 = (ulong)val;

            v1
                .Should().Be(v2);
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
        public void ImplicitCreateULongArray()
        {
            var val = new ulong[] { 2, 0 };
            Variant v1 = val;
            var v2 = new Variant(val);

            v1.Value
                .Should().Be(v2.Value);
        }
        
        [Fact]
        public void ExplicitConvertToULongArray()
        {
            var v1 = new ulong[] { 2, 0 };
            var val = new Variant(v1);
            var v2 = (ulong[])val;

            v1
                .Should().BeEquivalentTo(v2);
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
        public void ImplicitCreateFloat()
        {
            var val = (float)2;
            Variant v1 = val;
            var v2 = new Variant(val);

            v1
                .Should().Be(v2);
        }

        [Fact]
        public void ExplicitConvertToFloat()
        {
            var v1 = (float)2;
            var val = new Variant(v1);
            var v2 = (float)val;

            v1
                .Should().Be(v2);
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
        public void ImplicitCreateFloatArray()
        {
            var val = new float[] { 2 };
            Variant v1 = val;
            var v2 = new Variant(val);

            v1.Value
                .Should().Be(v2.Value);
        }
        
        [Fact]
        public void ExplicitConvertToFloatArray()
        {
            var v1 = new float[] { 2 };
            var val = new Variant(v1);
            var v2 = (float[])val;

            v1
                .Should().BeEquivalentTo(v2);
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
        public void ImplicitCreateDouble()
        {
            var val = (double)2;
            Variant v1 = val;
            var v2 = new Variant(val);

            v1
                .Should().Be(v2);
        }

        [Fact]
        public void ExplicitConvertToDouble()
        {
            var v1 = (double)2;
            var val = new Variant(v1);
            var v2 = (double)val;

            v1
                .Should().Be(v2);
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
        public void ImplicitCreateDoubleArray()
        {
            var val = new double[] { 2.0, 6.7 };
            Variant v1 = val;
            var v2 = new Variant(val);

            v1.Value
                .Should().Be(v2.Value);
        }

        [Fact]
        public void ExplicitConvertToDoubleArray()
        {
            var v1 = new double[] { 2.0, 6.7 };
            var val = new Variant(v1);
            var v2 = (double[])val;

            v1
                .Should().BeEquivalentTo(v2);
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
        public void ImplicitCreateString()
        {
            var val = "Test string";
            Variant v1 = val;
            var v2 = new Variant(val);

            v1
                .Should().Be(v2);
        }

        [Fact]
        public void ExplicitConvertToString()
        {
            var v1 = "Test string";
            var val = new Variant(v1);
            var v2 = (string)val;

            v1
                .Should().Be(v2);
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
        public void ImplicitCreateStringArray()
        {
            var val = new[] { "Test string", "Test" };
            Variant v1 = val;
            var v2 = new Variant(val);

            v1.Value
                .Should().Be(v2.Value);
        }

        [Fact]
        public void ExplicitConvertToStringArray()
        {
            var v1 = new[] { "Test string", "Test" };
            var val = new Variant(v1);
            var v2 = (string[])val;

            v1
                .Should().BeEquivalentTo(v2);
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
        public void ImplicitCreateDateTime()
        {
            var val = DateTime.Parse("2012-12-24");
            Variant v1 = val;
            var v2 = new Variant(val);

            v1
                .Should().Be(v2);
        }

        [Fact]
        public void ExplicitConvertToDateTime()
        {
            var v1 = DateTime.Parse("2012-12-24");
            var val = new Variant(v1);
            var v2 = (DateTime)val;

            v1
                .Should().Be(v2);
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
        public void ImplicitCreateDateTimeArray()
        {
            var val = new[] { DateTime.Parse("2012-12-24") };
            Variant v1 = val;
            var v2 = new Variant(val);

            v1.Value
                .Should().Be(v2.Value);
        }

        [Fact]
        public void ExplicitConvertToDateTimeArray()
        {
            var v1 = new[] { DateTime.Parse("2012-12-24") };
            var val = new Variant(v1);
            var v2 = (DateTime[])val;

            v1
                .Should().BeEquivalentTo(v2);
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
        public void ImplicitCreateGuid()
        {
            var val = Guid.NewGuid();
            Variant v1 = val;
            var v2 = new Variant(val);

            v1
                .Should().Be(v2);
        }

        [Fact]
        public void ExplicitConvertToGuid()
        {
            var v1 = Guid.NewGuid();
            var val = new Variant(v1);
            var v2 = (Guid)val;

            v1
                .Should().Be(v2);
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
        public void ImplicitCreateGuidArray()
        {
            var val = new[] { Guid.NewGuid(), Guid.NewGuid() };
            Variant v1 = val;
            var v2 = new Variant(val);

            v1.Value
                .Should().Be(v2.Value);
        }

        [Fact]
        public void ExplicitConvertToGuidArray()
        {
            var v1 = new[] { Guid.NewGuid(), Guid.NewGuid() };
            var val = new Variant(v1);
            var v2 = (Guid[])val;

            v1
                .Should().BeEquivalentTo(v2);
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
        public void ImplicitCreateByteString()
        {
            var val = new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f };
            Variant v1 = val;
            var v2 = new Variant(val);

            v1
                .Should().Be(v2);
        }

        [Fact]
        public void ExplicitConvertToByteString()
        {
            var v1 = new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f };
            var val = new Variant(v1);
            var v2 = (byte[])val;

            v1
                .Should().BeEquivalentTo(v2);
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
        public void ImplicitCreateByteStringArray()
        {
            var val = new byte[][] { new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f } };
            Variant v1 = val;
            var v2 = new Variant(val);

            v1.Value
                .Should().Be(v2.Value);
        }

        [Fact]
        public void ExplicitConvertToByteStringArray()
        {
            var v1 = new byte[][] { new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f } };
            var val = new Variant(v1);
            var v2 = (byte[][])val;

            v1
                .Should().BeEquivalentTo(v2);
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
        public void ImplicitCreateXElement()
        {
            var val = XElement.Parse(@"<Item AttributeA=""A"" AttributeB=""B"" />");
            Variant v1 = val;
            var v2 = new Variant(val);

            v1
                .Should().Be(v2);
        }

        [Fact]
        public void ExplicitConvertToXElement()
        {
            var v1 = XElement.Parse(@"<Item AttributeA=""A"" AttributeB=""B"" />");
            var val = new Variant(v1);
            var v2 = (XElement)val;

            v1
                .Should().Be(v2);
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
        public void ImplicitCreateXElementArray()
        {
            var val = new[] { XElement.Parse(@"<Item AttributeA=""A"" AttributeB=""B"" />") };
            Variant v1 = val;
            var v2 = new Variant(val);

            v1.Value
                .Should().BeEquivalentTo(v2.Value);
        }

        [Fact]
        public void ExplicitConvertToXElementArray()
        {
            var v1 = new[] { XElement.Parse(@"<Item AttributeA=""A"" AttributeB=""B"" />") };
            var val = new Variant(v1);
            var v2 = (XElement[])val;

            v1
                .Should().BeEquivalentTo(v2);
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
        public void ImplicitCreateNodeId()
        {
            var val = new NodeId(Guid.NewGuid(), 2);
            Variant v1 = val;
            var v2 = new Variant(val);

            v1
                .Should().Be(v2);
        }

        [Fact]
        public void ExplicitConvertToNodeId()
        {
            var v1 = new NodeId(Guid.NewGuid(), 2);
            var val = new Variant(v1);
            var v2 = (NodeId)val;

            v1
                .Should().Be(v2);
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
        public void ImplicitCreateNodeIdArray()
        {
            var val = new[] { new NodeId(Guid.NewGuid(), 2) };
            Variant v1 = val;
            var v2 = new Variant(val);

            v1.Value
                .Should().Be(v2.Value);
        }

        [Fact]
        public void ExplicitConvertToNodeIdArray()
        {
            var v1 = new[] { new NodeId(Guid.NewGuid(), 2) };
            var val = new Variant(v1);
            var v2 = (NodeId[])val;

            v1
                .Should().BeEquivalentTo(v2);
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
        public void ImplicitCreateExpandedNodeId()
        {
            var val = new ExpandedNodeId(5);
            Variant v1 = val;
            var v2 = new Variant(val);

            v1
                .Should().Be(v2);
        }
        
        [Fact]
        public void ExplicitConvertToExpandedNodeId()
        {
            var v1 = new ExpandedNodeId("Identifier");
            var val = new Variant(v1);
            var v2 = (ExpandedNodeId)val;

            v1
                .Should().Be(v2);
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
        public void ImplicitCreateExpandedNodeIdArray()
        {
            var val = new[] { new ExpandedNodeId(5), new ExpandedNodeId(7) };
            Variant v1 = val;
            var v2 = new Variant(val);

            v1.Value
                .Should().Be(v2.Value);
        }

        [Fact]
        public void ExplicitConvertToExpandedNodeIdArray()
        {
            var v1 = new[] { new ExpandedNodeId(5), new ExpandedNodeId(7) };
            var val = new Variant(v1);
            var v2 = (ExpandedNodeId[])val;

            v1
                .Should().BeEquivalentTo(v2);
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
        public void ImplicitCreateStatusCode()
        {
            var val = new StatusCode(2);
            Variant v1 = val;
            var v2 = new Variant(val);

            v1
                .Should().Be(v2);
        }
        
        [Fact]
        public void ExplicitConvertToStatusCode()
        {
            var v1 = new StatusCode(2);
            var val = new Variant(v1);
            var v2 = (StatusCode)val;

            v1
                .Should().Be(v2);
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
        public void ImplicitCreateStatusCodeArray()
        {
            var val = new[] { new StatusCode(2), new StatusCode(3) };
            Variant v1 = val;
            var v2 = new Variant(val);

            v1.Value
                .Should().Be(v2.Value);
        }

        [Fact]
        public void ExplicitConvertToStatusCodeArray()
        {
            var v1 = new[] { new StatusCode(2), new StatusCode(3) };
            var val = new Variant(v1);
            var v2 = (StatusCode[])val;

            v1
                .Should().BeEquivalentTo(v2);
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
        public void ImplicitCreateQualifiedName()
        {
            var val = new QualifiedName("name");
            Variant v1 = val;
            var v2 = new Variant(val);

            v1
                .Should().Be(v2);
        }

        [Fact]
        public void ExplicitConvertToQualifiedName()
        {
            var v1 = new QualifiedName("name");
            var val = new Variant(v1);
            var v2 = (QualifiedName)val;

            v1
                .Should().Be(v2);
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
        public void ImplicitCreateQualifiedNameArray()
        {
            var val = new[] { new QualifiedName("name") };
            Variant v1 = val;
            var v2 = new Variant(val);

            v1.Value
                .Should().Be(v2.Value);
        }

        [Fact]
        public void ExplicitConvertToQualifiedNameArray()
        {
            var v1 = new[] { new QualifiedName("name") };
            var val = new Variant(v1);
            var v2 = (QualifiedName[])val;

            v1
                .Should().BeEquivalentTo(v2);
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
        public void ImplicitCreateLocalizedText()
        {
            var val = new LocalizedText("Text");
            Variant v1 = val;
            var v2 = new Variant(val);

            v1
                .Should().Be(v2);
        }

        [Fact]
        public void ExplicitConvertToLocalizedText()
        {
            var v1 = new LocalizedText("Text");
            var val = new Variant(v1);
            var v2 = (LocalizedText)val;

            v1
                .Should().Be(v2);
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

        [Fact]
        public void ImplicitCreateLocalizedTextArray()
        {
            var val = new[] { new LocalizedText("Text"), new LocalizedText("Test") };
            Variant v1 = val;
            var v2 = new Variant(val);

            v1.Value
                .Should().Be(v2.Value);
        }

        [Fact]
        public void ExplicitConvertToLocalizedTextArray()
        {
            var v1 = new[] { new LocalizedText("Text"), new LocalizedText("Test") };
            var val = new Variant(v1);
            var v2 = (LocalizedText[])val;

            v1
                .Should().BeEquivalentTo(v2);
        }

        [Fact]
        public void CreateExtensionObject()
        {
            var val = new ExtensionObject(null);
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.ExtensionObject);
            v.ArrayDimensions
                .Should().BeNull();
        }

        [Fact]
        public void ImplicitCreateExtensionObject()
        {
            var val = new ExtensionObject(null);
            Variant v1 = val;
            var v2 = new Variant(val);

            v1
                .Should().Be(v2);
        }
        
        [Fact]
        public void ExplicitConvertToExtensionObject()
        {
            var v1 = new ExtensionObject(null);
            var val = new Variant(v1);
            var v2 = (ExtensionObject)val;

            v1
                .Should().Be(v2);
        }

        [Fact]
        public void CreateExtensionObjectArray()
        {
            var val = new[] { new ExtensionObject(null) };
            var v = new Variant(val);

            v.Value
                .Should().Be(val);
            v.Type
                .Should().Be(VariantType.ExtensionObject);
            v.ArrayDimensions
                .Should().ContainSingle()
                .Which
                .Should().Be(val.Length);
        }
        
        [Fact]
        public void ImplicitCreateExtensionObjectArray()
        {
            var val = new[] { new ExtensionObject(null) };
            Variant v1 = val;
            var v2 = new Variant(val);

            v1.Value
                .Should().Be(v2.Value);
        }

        [Fact]
        public void ExplicitConvertToExtensionObjectArray()
        {
            var v1 = new[] { new ExtensionObject(null) };
            var val = new Variant(v1);
            var v2 = (ExtensionObject[])val;

            v1
                .Should().BeEquivalentTo(v2);
        }

        [Fact]
        public void CreateEncodeable()
        {
            var val = new ReadRequest();
            var v = new Variant(val);

            v.Value
                .Should().BeEquivalentTo(new ExtensionObject(val));
            v.Type
                .Should().Be(VariantType.ExtensionObject);
            v.ArrayDimensions
                .Should().BeNull();
        }

        [Fact]
        public void CreateEncodeableArray()
        {
            var val = new[] { new ReadRequest(), new ReadRequest() };
            var v = new Variant(val);

            v.Value
                .Should().BeEquivalentTo(val.Select(o => new ExtensionObject(o)));
            v.Type
                .Should().Be(VariantType.ExtensionObject);
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
