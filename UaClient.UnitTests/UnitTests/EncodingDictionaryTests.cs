using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Workstation.ServiceModel.Ua;
using Xunit;

namespace Workstation.UaClient.UnitTests
{
    public class EncodingDictionaryTests
    {
        public class TestType1 : Structure { };
        public class TestType2 : Structure { };
        public class TestType3 : Structure { };
        public class TestType4 : Structure { };

        [Fact]
        public void CreateNull()
        {
            var dic = EncodingDictionary.BinaryEncodingDictionary;
            var table = Enumerable.Empty<(ExpandedNodeId, Type)>();
            var uris = new List<string>();

            Action act1 = () => new EncodingDictionary(null);
            act1
                .Should().Throw<ArgumentNullException>();

            Action act2 = () => new EncodingDictionary(null, table, uris);
            act2
                .Should().Throw<ArgumentNullException>();
            
            Action act3 = () => new EncodingDictionary(dic, null, uris);
            act3
                .Should().Throw<ArgumentNullException>();
            
            Action act4 = () => new EncodingDictionary(dic, table, null);
            act4
                .Should().Throw<ArgumentNullException>();
            
            Action act5 = () => new EncodingDictionary(dic, table, uris);
            act5
                .Should().NotThrow();
        }

        [Fact]
        public void TryGetEncodingId()
        {
            var dic = new EncodingDictionary(new CustomEncodingTable
            {
                (ExpandedNodeId.Parse("i=5"), typeof(TestType1)),
                (ExpandedNodeId.Parse("i=6"), typeof(TestType2)),
                (ExpandedNodeId.Parse("nsu=Test;i=7"), typeof(TestType3))
            });

            dic.TryGetEncodingId(typeof(TestType1), out var encodingId)
                .Should().BeTrue();
            encodingId
                .Should().Be(NodeId.Parse("i=5"));

            dic.TryGetEncodingId(typeof(TestType2), out encodingId)
                .Should().BeTrue();
            encodingId
                .Should().Be(NodeId.Parse("i=6"));
            
            dic.TryGetEncodingId(typeof(TestType3), out encodingId)
                .Should().BeFalse();
            encodingId
                .Should().BeNull();
            
            dic.TryGetEncodingId(typeof(TestType4), out encodingId)
                .Should().BeFalse();
            encodingId
                .Should().BeNull();
        }
        
        [Fact]
        public void TryGetType()
        {
            var dic = new EncodingDictionary(new CustomEncodingTable
            {
                (ExpandedNodeId.Parse("i=5"), typeof(TestType1)),
                (ExpandedNodeId.Parse("i=6"), typeof(TestType2))
            });

            dic.TryGetType(new NodeId(5), out var type)
                .Should().BeTrue();
            type
                .Should().Be(typeof(TestType1));

            dic.TryGetType(new NodeId(6), out type)
                .Should().BeTrue();
            type
                .Should().Be(typeof(TestType2));

            dic.TryGetType(new NodeId(7), out type)
                .Should().BeFalse();
            type
                .Should().BeNull();
        }

        [Fact]
        public void CreateRecurringType()
        {
            Action act = () => new EncodingDictionary(new CustomEncodingTable
            {
                (ExpandedNodeId.Parse("i=5"), typeof(TestType1)),
                (ExpandedNodeId.Parse("i=6"), typeof(TestType1))
            });

            act
                .Should().Throw<ArgumentException>();
        }
        
        [Fact]
        public void CreateRecurringEncodingId()
        {
            Action act = () => new EncodingDictionary(new CustomEncodingTable
            {
                (ExpandedNodeId.Parse("i=5"), typeof(TestType1)),
                (ExpandedNodeId.Parse("i=5"), typeof(TestType2))
            });

            act
                .Should().Throw<ArgumentException>();
        }
        
        [Fact]
        public void TryGetEncodingId2()
        {
            var dic = new EncodingDictionary(
                EncodingDictionary.BinaryEncodingDictionary,
                new CustomEncodingTable
                {
                    (ExpandedNodeId.Parse("nsu=Test;i=5"), typeof(TestType1)),
                    (ExpandedNodeId.Parse("nsu=Test;i=6"), typeof(TestType2)),
                },
                new[] { "Standard", "Test1", "Test" });

            dic.TryGetEncodingId(typeof(TestType1), out var encodingId)
                .Should().BeTrue();
            encodingId
                .Should().Be(NodeId.Parse("ns=2;i=5"));

            dic.TryGetEncodingId(typeof(TestType2), out encodingId)
                .Should().BeTrue();
            encodingId
                .Should().Be(NodeId.Parse("ns=2;i=6"));
            
            dic.TryGetEncodingId(typeof(TestType3), out encodingId)
                .Should().BeFalse();
            encodingId
                .Should().BeNull();
        }
        
        [Fact]
        public void TryGetType2()
        {
            var dic = new EncodingDictionary(
                EncodingDictionary.BinaryEncodingDictionary,
                new CustomEncodingTable
                {
                    (ExpandedNodeId.Parse("nsu=Test;i=5"), typeof(TestType1)),
                    (ExpandedNodeId.Parse("nsu=Test;s=B"), typeof(TestType2)),
                },
                new[] { "Standard", "Test1", "Test" });

            dic.TryGetType(NodeId.Parse("ns=2;i=5"), out var type)
                .Should().BeTrue();
            type
                .Should().Be(typeof(TestType1));

            dic.TryGetType(NodeId.Parse("ns=2;s=B"), out type)
                .Should().BeTrue();
            type
                .Should().Be(typeof(TestType2));

            dic.TryGetType(NodeId.Parse("ns=2;i=6"), out type)
                .Should().BeFalse();
            type
                .Should().BeNull();
        }

        [Fact]
        public void BinaryEncodingDictionary()
        {
            var dic = EncodingDictionary.BinaryEncodingDictionary;

            dic.TryGetType(NodeId.Parse(ObjectIds.ThreeDVector_Encoding_DefaultBinary), out var type)
                .Should().BeTrue();
            type
                .Should().Be(typeof(ThreeDVector));

            dic.TryGetEncodingId(typeof(ThreeDVector), out var encodingId)
                .Should().BeTrue();
            encodingId
                .Should().Be(NodeId.Parse(ObjectIds.ThreeDVector_Encoding_DefaultBinary));
        }
    }
}
