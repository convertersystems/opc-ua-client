using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Workstation.ServiceModel.Ua;

namespace Workstation.UaClient.UnitTests
{

    public class NodeIdTests
    {
        [Fact]
        public void CreateFromUint()
        {
            var id = (uint)42;
            ushort ns = 2;
            var node = new NodeId(id, ns);

            node.Identifier
                .Should().Be(id);
            node.NamespaceIndex
                .Should().Be(ns);
            node.IdType
                .Should().Be(IdType.Numeric);
        }

        [Fact]
        public void CreateFromString()
        {
            var id = "fromstring";
            ushort ns = 2;
            var node = new NodeId(id, ns);

            node.Identifier
                .Should().Be(id);
            node.NamespaceIndex
                .Should().Be(ns);
            node.IdType
                .Should().Be(IdType.String);
        }

        [Fact]
        public void CreateFromGuid()
        {
            var id = Guid.NewGuid();
            ushort ns = 2;
            var node = new NodeId(id, ns);

            node.Identifier
                .Should().Be(id);
            node.NamespaceIndex
                .Should().Be(ns);
            node.IdType
                .Should().Be(IdType.Guid);
        }

        [Fact]
        public void CreateFromOpaque()
        {
            var id = new byte [] { 0x65, 0x66 };
            ushort ns = 2;
            var node = new NodeId(id, ns);

            node.Identifier
                .Should().Be(id);
            node.NamespaceIndex
                .Should().Be(ns);
            node.IdType
                .Should().Be(IdType.Opaque);
        }

        [Fact]
        public void IsNull()
        {
            NodeId.IsNull(null)
                .Should().BeTrue();
        }

        [Fact]
        public void IsNodeIdNull()
        {
            NodeId.IsNull(NodeId.Null)
                .Should().BeTrue();
        }

        [Fact]
        public void IsNotNull()
        {
            NodeId.IsNull(new NodeId(42))
                .Should().BeFalse();
        }

        public static IEnumerable<NodeId> NodeIds { get; } = new[]
            {
                new NodeId(Guid.NewGuid()),
                new NodeId(Guid.NewGuid()),
                new NodeId("1"),
                new NodeId("2"),
                new NodeId("1", 4),
                new NodeId("2", 4),
                new NodeId(1),
                new NodeId(2),
                new NodeId(1, 4),
                new NodeId(2, 4),
                new NodeId(new byte [] {1, 2}),
                new NodeId(new byte [] {1, 2, 3}),
                new NodeId(new byte [] {1, 2}, 4),
                new NodeId(new byte [] {1, 2, 3}, 4),
                null,
                NodeId.Null
            }
            .ToList();

        public static IEnumerable<object[]> EqualityData =>
            from a in NodeIds.Select((n, i) => (id: n, index: i))
            from b in NodeIds.Select((n, i) => (id: n, index: i))
            select new object[] { a.id, b.id, a.index == b.index};

        [MemberData(nameof(EqualityData))]
        [Theory]
        public void Equality(NodeId a, NodeId b, bool shouldBeEqual)
        {
            if (shouldBeEqual)
                a.Should().Be(b);
            else
                a.Should().NotBe(b);
        }

        public static IEnumerable<object[]> ParseData
            => NodeIds.Where(n => n != null).Select(n => new object[] { n.ToString(), n});

        [MemberData(nameof(ParseData))]
        [Theory]
        public void Parse(string s, NodeId id)
        {
            NodeId.Parse(s)
                .Should().Be(id);
        }

        public static IEnumerable<object[]> BadParseData
            => new object[]
            {
                null,
                "g=",
                "s",
                ";s=A",
                "n=",
                "g=234234",
                "n=ABC",
                "123412"
            }
            .Select(o => new[] { o });

        [MemberData(nameof(BadParseData))]
        [Theory]
        public void NotParsable(string s)
        {
            s.Invoking(t => NodeId.Parse(t))
                .Should().Throw<ServiceResultException>()
                .Which.HResult
                .Should().Be(unchecked((int)StatusCodes.BadNodeIdInvalid));
        }
    }
}
