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

    public class ExpandedNodeIdTests
    {
        [Fact]
        public void CreateFromUint()
        {
            var id = (uint)42;
            var node = new ExpandedNodeId(id);

            node.NodeId.Identifier
                .Should().Be(id);
            node.NodeId.NamespaceIndex
                .Should().Be(0);
            node.NodeId.IdType
                .Should().Be(IdType.Numeric);
        }

        [Fact]
        public void CreateFromUintWithNamespace()
        {
            var id = (uint)42;
            var node = new ExpandedNodeId(id, namespaceUri: "namespace", serverIndex: 2);

            node.NodeId.Identifier
                .Should().Be(id);
            node.NodeId.NamespaceIndex
                .Should().Be(0);
            node.NodeId.IdType
                .Should().Be(IdType.Numeric);
            node.NamespaceUri
                .Should().Be("namespace");
            node.ServerIndex
                .Should().Be(2);
        }

        [Fact]
        public void CreateFromString()
        {
            var id = "42";
            var node = new ExpandedNodeId(id);

            node.NodeId.Identifier
                .Should().Be(id);
            node.NodeId.NamespaceIndex
                .Should().Be(0);
            node.NodeId.IdType
                .Should().Be(IdType.String);
        }

        [Fact]
        public void CreateFromStringWithNamespace()
        {
            var id = "42";
            var node = new ExpandedNodeId(id, namespaceUri: "namespace", serverIndex: 2);

            node.NodeId.Identifier
                .Should().Be(id);
            node.NodeId.NamespaceIndex
                .Should().Be(0);
            node.NodeId.IdType
                .Should().Be(IdType.String);
            node.NamespaceUri
                .Should().Be("namespace");
            node.ServerIndex
                .Should().Be(2);
        }

        [Fact]
        public void CreateGuidString()
        {
            var id = Guid.NewGuid();
            var node = new ExpandedNodeId(id);

            node.NodeId.Identifier
                .Should().Be(id);
            node.NodeId.NamespaceIndex
                .Should().Be(0);
            node.NodeId.IdType
                .Should().Be(IdType.Guid);
        }

        [Fact]
        public void CreateFromGuidWithNamespace()
        {
            var id = Guid.NewGuid();
            var node = new ExpandedNodeId(id, namespaceUri: "namespace", serverIndex: 2);

            node.NodeId.Identifier
                .Should().Be(id);
            node.NodeId.NamespaceIndex
                .Should().Be(0);
            node.NodeId.IdType
                .Should().Be(IdType.Guid);
            node.NamespaceUri
                .Should().Be("namespace");
            node.ServerIndex
                .Should().Be(2);
        }

        [Fact]
        public void CreateFromOpaque()
        {
            var id = new byte [] { 0x65, 0x66 };
            var node = new ExpandedNodeId(id);

            node.NodeId.Identifier
                .Should().Be(id);
            node.NodeId.NamespaceIndex
                .Should().Be(0);
            node.NodeId.IdType
                .Should().Be(IdType.Opaque);
        }

        [Fact]
        public void CreateFromÓpaqueWithNamespace()
        {
            var id = new byte [] { 0x65, 0x66 };
            var node = new ExpandedNodeId(id, namespaceUri: "namespace", serverIndex: 2);

            node.NodeId.Identifier
                .Should().Be(id);
            node.NodeId.NamespaceIndex
                .Should().Be(0);
            node.NodeId.IdType
                .Should().Be(IdType.Opaque);
            node.NamespaceUri
                .Should().Be("namespace");
            node.ServerIndex
                .Should().Be(2);
        }

        [Fact]
        public void IsNull()
        {
            ExpandedNodeId.IsNull(null)
                .Should().BeTrue();
        }

        [Fact]
        public void IsNodeIdNull()
        {
            ExpandedNodeId.IsNull(ExpandedNodeId.Null)
                .Should().BeTrue();
        }

        [Fact]
        public void IsNotNull()
        {
            ExpandedNodeId.IsNull(new ExpandedNodeId(42))
                .Should().BeFalse();
        }

        public static IEnumerable<Func<ExpandedNodeId>> ExpandedNodeIds { get; } = new Func<ExpandedNodeId>[]
            {
                () => new ExpandedNodeId(Guid.Parse("28fc4ae1-9eb0-49fc-93a9-7a6ae34ba151")),
                () => new ExpandedNodeId(Guid.Parse("77628a5c-a82a-43a1-838f-cfdbd037d15f")),
                () => new ExpandedNodeId("1"),
                () => new ExpandedNodeId("2"),
                () => new ExpandedNodeId("2", null, 4),
                () => new ExpandedNodeId("1", "namespace"),
                () => new ExpandedNodeId("2", "namespace"),
                () => new ExpandedNodeId("2", "namespace", 4),
                () => new ExpandedNodeId(1),
                () => new ExpandedNodeId(2),
                () => new ExpandedNodeId(1, "namespace"),
                () => new ExpandedNodeId(2, "namespace"),
                () => new ExpandedNodeId(new byte [] {1, 2}),
                () => new ExpandedNodeId(new byte [] {1, 2, 3}),
                () => new ExpandedNodeId(new byte [] {1, 2}, "namespace", 2),
                () => new ExpandedNodeId(new byte [] {1, 2}, "namespace"),
                () => ExpandedNodeId.Null
            };

        public static IEnumerable<object[]> ValueEqualityData =>
            from a in ExpandedNodeIds.Select((f, i) => (id: f(), index: i))
            from b in ExpandedNodeIds.Select((f, i) => (id: f(), index: i))
            select new object[] { a.id, b.id, a.index == b.index };

        public static IEnumerable<object[]> ReferenceEqualityData
        {
            get
            {
                var list = ExpandedNodeIds.Select((f, i) => (id: f(), index: i)).ToList();
                return from a in list
                       from b in list
                       select new object[] { a.id, b.id, a.index == b.index };
            }
        }

        [MemberData(nameof(ValueEqualityData))]
        [MemberData(nameof(ReferenceEqualityData))]
        [Theory]
        public void Equality(ExpandedNodeId a, ExpandedNodeId b, bool shouldBeEqual)
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
            ExpandedNodeIds.Select(id => new[] { id() });

        [MemberData(nameof(EqualityNullData))]
        [Theory]
        public void EqualityNull(ExpandedNodeId val)
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

            val.Equals((ExpandedNodeId)null)
                .Should().BeFalse();
        }

        public static IEnumerable<object[]> ParseData
            => ExpandedNodeIds.Select(f => f()).Where(n => n != null).Select(n => new object[] { n.ToString(), n});

        [MemberData(nameof(ParseData))]
        [Theory]
        public void Parse(string s, ExpandedNodeId id)
        {
            ExpandedNodeId.Parse(s)
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
                "svr=234",
                "svr=234;nsu=Namespace",
                "nsu=Namespace",
                "nsu=namespace;svr=3;s=text"
            }
            .Select(o => new[] { o });

        [MemberData(nameof(BadParseData))]
        [Theory]
        public void NotParsable(string s)
        {
            s.Invoking(t => ExpandedNodeId.Parse(t))
                .Should().Throw<ServiceResultException>()
                .Which.HResult
                .Should().Be(unchecked((int)StatusCodes.BadNodeIdInvalid));
        }

        private readonly string[] NamespaceUris = new[]
        {
            "http://opcfoundation.org/UA/",
            "http://PLCopen.org/OpcUa/IEC61131-3/",
            "http://some.more",
            "http://www.siemens.com/simatic-s7-opcua"
        };

        public static IEnumerable<object[]> ToNodeIdData => new[]
        {
            new ExpandedNodeId(Guid.Parse("77628a5c-a82a-43a1-838f-cfdbd037d15f"), "http://some.more"),
            new ExpandedNodeId("1", "http://some.more"),
            new ExpandedNodeId(1, "http://some.more"),
            new ExpandedNodeId(new byte [] {1, 2}, "http://some.more"),
            new ExpandedNodeId(1),
            new ExpandedNodeId(1, ""),
            new ExpandedNodeId(1, "Foo")
        }
        .Select(o => new[] { o });

        [MemberData(nameof(ToNodeIdData))]
        [Theory]
        public void ToNodeId(ExpandedNodeId exnodeId)
        {
            // The test data contains only namespace indeces
            // that are zero, 2 or greater 3
            switch (exnodeId.NamespaceUri)
            {
                case null:
                case "":
                    var x = ExpandedNodeId.ToNodeId(exnodeId, NamespaceUris);
                    x.NamespaceIndex.Should().Be(0);
                    x.Identifier.Should().Be(exnodeId.NodeId.Identifier);
                    break;
                case "http://some.more":
                    var y = ExpandedNodeId.ToNodeId(exnodeId, NamespaceUris);
                    y.NamespaceIndex.Should().Be(2);
                    y.Identifier.Should().Be(exnodeId.NodeId.Identifier);
                    break;
                default:
                    exnodeId.Invoking(n => ExpandedNodeId.ToNodeId(n, NamespaceUris))
                        .Should().Throw<IndexOutOfRangeException>();
                    break;
            }
        }

        [Fact]
        public void ToNodeIdNull()
        {
            var nodeId = default(ExpandedNodeId);
            var nsUris = new string[] { };

            nodeId.Invoking(n => ExpandedNodeId.ToNodeId(n, nsUris))
                .Should().Throw<ArgumentNullException>();
        }
    }
}
