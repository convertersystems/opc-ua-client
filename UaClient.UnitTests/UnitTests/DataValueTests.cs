using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workstation.ServiceModel.Ua;
using Xunit;

namespace Workstation.UaClient.UnitTests
{
    public class DataValueTests
    {
        [Fact]
        public void CreateFromNull()
        {
            var val = new DataValue(default(object));

            val.ServerPicoseconds
                .Should().Be(0);
            val.ServerTimestamp
                .Should().Be(default);
            val.SourcePicoseconds
                .Should().Be(0);
            val.SourceTimestamp
                .Should().Be(default);
            val.StatusCode
                .Should().Be((StatusCode)StatusCodes.Good);
            val.Value
                .Should().Be(null);
            val.Variant.Value
                .Should().Be(null);
        }

        public static IEnumerable<object[]> CreateFromObjectData { get; } = new object[][]
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

        [MemberData(nameof(CreateFromObjectData))]
        [Theory]
        public void CreateFromObject(object obj)
        {
            var val = new DataValue(obj);

            val.ServerPicoseconds
                .Should().Be(0);
            val.ServerTimestamp
                .Should().Be(default);
            val.SourcePicoseconds
                .Should().Be(0);
            val.SourceTimestamp
                .Should().Be(default);
            val.StatusCode
                .Should().Be((StatusCode)StatusCodes.Good);
            val.Value
                .Should().Be(obj);
            val.Variant.Value
                .Should().Be(obj);
        }

        [MemberData(nameof(CreateFromObjectData))]
        [Theory]
        public void CreateFromVariant(object obj)
        {
            var variant = new Variant(obj);
            var val = new DataValue(variant);

            val.ServerPicoseconds
                .Should().Be(0);
            val.ServerTimestamp
                .Should().Be(default);
            val.SourcePicoseconds
                .Should().Be(0);
            val.SourceTimestamp
                .Should().Be(default);
            val.StatusCode
                .Should().Be((StatusCode)StatusCodes.Good);
            val.Value
                .Should().Be(obj);
            val.Variant.Value
                .Should().Be(obj);
            val.Variant
                .Should().Be(variant);
        }

        [Fact]
        public void CreateWithSourceTimestamp()
        {
            var ts = DateTime.Now;
            var val = new DataValue(1, sourceTimestamp: ts, sourcePicoseconds: 13);

            val.SourcePicoseconds
                .Should().Be(13);
            val.SourceTimestamp
                .Should().Be(ts);
        }

        [Fact]
        public void CreateWithServerTimestamp()
        {
            var ts = DateTime.Now;
            var val = new DataValue(1, serverTimestamp: ts, serverPicoseconds: 13);

            val.ServerPicoseconds
                .Should().Be(13);
            val.ServerTimestamp
                .Should().Be(ts);
        }
    }
}
