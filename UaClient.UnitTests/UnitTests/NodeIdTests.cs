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
        public void CreateFromStringNull()
        {
            var id = default(string);
            id.Invoking(i => new NodeId(i, 2))
                .Should().Throw<ArgumentNullException>();
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
            var id = new byte[] { 0x65, 0x66 };
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
        public void CreateFromOpaqueNull()
        {
            var id = default(byte[]);
            id.Invoking(i => new NodeId(i, 2))
                .Should().Throw<ArgumentNullException>();
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
                () => new NodeId(new Guid("23c76470-5796-4164-b6e2-c071847f9ea0"), 2),
                () => new NodeId(new Guid("23c76470-5796-4164-b6e2-c071847f9ea0"), 4),
                () => new NodeId("1"),
                () => new NodeId("2"),
                () => new NodeId("1", 2),
                () => new NodeId("2", 2),
                () => new NodeId("2", 4),
                () => new NodeId(1),
                () => new NodeId(2),
                () => new NodeId(1, 2),
                () => new NodeId(2, 2),
                () => new NodeId(2, 4),
                () => new NodeId(new byte [] {1, 2}),
                () => new NodeId(new byte [] {1, 2, 3}),
                () => new NodeId(new byte [] {1, 2}, 2),
                () => new NodeId(new byte [] {1, 2, 3}, 2),
                () => new NodeId(new byte [] {1, 2, 3}, 4)
            };

        public static IEnumerable<object[]> ValueEqualityData =>
            from a in NodeIds.Select((f, i) => (id: f(), index: i))
            from b in NodeIds.Select((f, i) => (id: f(), index: i))
            select new object[] { a.id, b.id, a.index == b.index };

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
            {
                // Should().Be() is using Equal(object)
                a
                    .Should().Be(b);
                a
                    .Should().NotBe(5);

                // Test Equal(NodeId)
                a.Equals(b)
                    .Should().BeTrue();

                // operator
                (a == b)
                    .Should().BeTrue();
                (a != b)
                    .Should().BeFalse();

                a.GetHashCode()
                    .Should().Be(b.GetHashCode());
            }
            else
            {
                // Should().Be() is using Equal(object)
                a
                    .Should().NotBe(b);
                a
                    .Should().NotBe(5);

                // Test Equal(NodeId)
                a.Equals(b)
                    .Should().BeFalse();

                // operator
                (a != b)
                    .Should().BeTrue();
                (a == b)
                    .Should().BeFalse();

                // This is technically not required but the current
                // implementation fulfills this. If this should ever
                // fail it could be bad luck or the the implementation
                // is really broken.
                a.GetHashCode()
                    .Should().NotBe(b.GetHashCode());
            }
        }

        public static IEnumerable<object[]> EqualityNullData =>
            NodeIds.Select(id => new[] { id() });

        [MemberData(nameof(EqualityNullData))]
        [Theory]
        public void EqualityNull(NodeId val)
        {
            (val == null)
                .Should().BeFalse();
            (val != null)
                .Should().BeTrue();
            (null == val)
                .Should().BeFalse();
            (null != val)
                .Should().BeTrue();

            // This is using Equals(object)
            val.Should()
                .NotBeNull();

            val.Equals((NodeId)null)
                .Should().BeFalse();
        }

        public static IEnumerable<object[]> ParseData
            => NodeIds.Select(f => f()).Where(n => n != null).Select(n => new object[] { n.ToString(), n });

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
                "123412",
                "ns=5i=2"
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

        public static IEnumerable<object[]> ToExpandedNodeIdData =>
            NodeIds.Select(id => new[] { id() });

        [MemberData(nameof(ToExpandedNodeIdData))]
        [Theory]
        public void ToExpandedNodeId(NodeId nodeId)
        {
            var nsUris = new[]
            {
                "default",
                "Namespace1",
                "Namespace2",
                "Namespace3"
            };

            // The test data contains only namespace indeces
            // that are zero, 2 or greater 3
            switch (nodeId.NamespaceIndex)
            {
                case 0:
                    var x = NodeId.ToExpandedNodeId(nodeId, nsUris);
                    x.NamespaceUri.Should().BeNull();
                    x.NodeId.Identifier.Should().Be(nodeId.Identifier);
                    break;
                case 2:
                    var y = NodeId.ToExpandedNodeId(nodeId, nsUris);
                    y.NamespaceUri.Should().Be("Namespace2");
                    y.NodeId.Identifier.Should().Be(nodeId.Identifier);
                    break;
                default:
                    nodeId.Invoking(n => NodeId.ToExpandedNodeId(n, nsUris))
                        .Should().Throw<ServiceResultException>();
                    break;
            }
        }

        [Fact]
        public void ToExpandedNodeIdNull()
        {
            var nodeId = default(NodeId);
            var nsUris = new string[] { };

            nodeId.Invoking(n => NodeId.ToExpandedNodeId(n, nsUris))
                .Should().Throw<ArgumentNullException>();
        }
    }
}
