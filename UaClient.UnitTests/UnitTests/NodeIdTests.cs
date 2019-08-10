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

        public static IEnumerable<Func<NodeId>> NodeIds { get; } = new Func<NodeId>[]
            {
                () => new NodeId(new Guid("d0c133bc-470d-40ae-a871-981376c0c762")),
                () => new NodeId(new Guid("23c76470-5796-4164-b6e2-c071847f9ea0")),
                () => new NodeId("1"),
                () => new NodeId("2"),
                () => new NodeId("1", 4),
                () => new NodeId("2", 4),
                () => new NodeId(1),
                () => new NodeId(2),
                () => new NodeId(1, 4),
                () => new NodeId(2, 4),
                () => new NodeId(new byte [] {1, 2}),
                () => new NodeId(new byte [] {1, 2, 3}),
                () => new NodeId(new byte [] {1, 2}, 4),
                () => new NodeId(new byte [] {1, 2, 3}, 4),
                () => null,
                () => NodeId.Null
            };

        public static IEnumerable<object[]> ValueEqualityData =>
            from a in NodeIds.Select((f, i) => (id: f(), index: i))
            from b in NodeIds.Select((f, i) => (id: f(), index: i))
            select new object[] { a.id, b.id, a.index == b.index};

        public static IEnumerable<object[]> ReferenceEqualityData
        {
            get
            {
                var list = NodeIds.Select((f, i) => (id: f(), index: i)).ToList();
                return from a in list
                    from b in list
                    select new object[] { a.id, b.id, a.index == b.index };
            }
        }

        [MemberData(nameof(ValueEqualityData))]
        [MemberData(nameof(ReferenceEqualityData))]
        [Theory]
        public void Equality(NodeId a, NodeId b, bool shouldBeEqual)
        {
            if (shouldBeEqual)
                a.Should().Be(b);
            else
                a.Should().NotBe(b);
        }

        public static IEnumerable<object[]> ParseData
            => NodeIds.Select(f => f()).Where(n => n != null).Select(n => new object[] { n.ToString(), n});

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
