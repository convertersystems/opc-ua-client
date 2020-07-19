using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using Workstation.ServiceModel.Ua;
using Xunit;

namespace Workstation.UaClient.UnitTests
{
    public class MergedEncodingTableTests
    {
        public class TestType1 : Structure { }
        public class TestType2 : Structure { }

        [Fact]
        public void AddOne()
        {
            var id = ExpandedNodeId.Parse("nsu=Test;i=35");

            var merged = new MergedEncodingTable
            {
                new[] { (id, typeof(TestType1)) }
            };

            merged
                .Should().HaveCount(1)
                .And.Contain((id, typeof(TestType1)));
        }
        
        [Fact]
        public void AddTwo()
        {
            var id1 = ExpandedNodeId.Parse("nsu=Test;i=35");
            var id2 = ExpandedNodeId.Parse("nsu=Test;i=36");

            var merged = new MergedEncodingTable
            {
                new[] { (id1, typeof(TestType1)) },
                new[] { (id2, typeof(TestType2)) }
            };

            merged
                .Should().HaveCount(2)
                .And.Contain((id1, typeof(TestType1)))
                .And.Contain((id2, typeof(TestType2)));
        }
        
        [Fact]
        public void AddNull()
        {
            var merged = new MergedEncodingTable();

            merged.Invoking(m => m.Add(null))
                .Should().Throw<ArgumentNullException>();
        }
    }
}
