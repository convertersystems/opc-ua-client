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

        public static IEnumerable<ExpandedNodeId> ExpandedNodeIds { get; } = new[]
            {
                new ExpandedNodeId(Guid.NewGuid()),
                new ExpandedNodeId(Guid.NewGuid()),
                new ExpandedNodeId("1"),
                new ExpandedNodeId("2"),
                new ExpandedNodeId("2", null, 4),
                new ExpandedNodeId("1", "namespace"),
                new ExpandedNodeId("2", "namespace"),
                new ExpandedNodeId("2", "namespace", 4),
                new ExpandedNodeId(1),
                new ExpandedNodeId(2),
                new ExpandedNodeId(1, "namespace"),
                new ExpandedNodeId(2, "namespace"),
                new ExpandedNodeId(new byte [] {1, 2}),
                new ExpandedNodeId(new byte [] {1, 2, 3}),
                new ExpandedNodeId(new byte [] {1, 2}, "namespace", 2),
                new ExpandedNodeId(new byte [] {1, 2}, "namespace"),
                null,
                ExpandedNodeId.Null
            }
            .ToList();

        public static IEnumerable<object[]> EqualityData =>
            from a in ExpandedNodeIds.Select((n, i) => (id: n, index: i))
            from b in ExpandedNodeIds.Select((n, i) => (id: n, index: i))
            select new object[] { a.id, b.id, a.index == b.index};

        [MemberData(nameof(EqualityData))]
        [Theory]
        public void Equality(ExpandedNodeId a, ExpandedNodeId b, bool shouldBeEqual)
        {
            if (shouldBeEqual)
                a.Should().Be(b);
            else
                a.Should().NotBe(b);
        }


        public static IEnumerable<object[]> ParseData
            => ExpandedNodeIds.Where(n => n != null).Select(n => new object[] { n.ToString(), n});

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

        [Fact]
        public void ToNodeTest()
        {
            var enodeId = ExpandedNodeId.Parse("nsu=http://some.more;i=42");
            var nodeId = ExpandedNodeId.ToNodeId(enodeId, NamespaceUris);

            nodeId.Identifier
                .Should().Be(enodeId.NodeId.Identifier);
            nodeId.IdType
                .Should().Be(IdType.Numeric);
            nodeId.NamespaceIndex
                .Should().Be(2);
        }
    }
}
