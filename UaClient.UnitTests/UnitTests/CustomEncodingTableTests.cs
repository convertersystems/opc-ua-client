using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using Workstation.ServiceModel.Ua;
using Xunit;

namespace Workstation.UaClient.UnitTests
{
    public class CustomEncodingTableTests
    {
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
        public sealed class TestEncodingIdAttribute : Attribute, IEncodingIdAttribute
        {
            public TestEncodingIdAttribute(string s)
            {
                this.NodeId = ExpandedNodeId.Parse(s);
            }

            public ExpandedNodeId NodeId { get; }
        }

        [TestEncodingId("nsu=Workstation.UaClient.UnitTests;s=TestType1")]
        public class TestType1 : Structure
        {
        }
        
        [TestEncodingId("nsu=Workstation.UaClient.UnitTests;s=TestType2")]
        public class TestType2 : Structure
        {
        }
        
        [TestEncodingId("nsu=Workstation.UaClient.UnitTests;s=TestTypeWithoutIEncodable")]
        public class TestTypeWithoutIEncodable
        {
        }

        public class TestTypeWithoutAttribute : Structure
        {
        }


        [Fact]
        public void CreateWithCollectionInitializer1()
        {
            var nodeId1 = ExpandedNodeId.Parse("nsu=Workstation.UaClient.UnitTests;s=TestType1");
            var nodeId2 = ExpandedNodeId.Parse("nsu=Workstation.UaClient.UnitTests;s=TestType2");

            var table = new CustomEncodingTable
            {
                {
                    nodeId1,
                    typeof(TestType1)
                },
                {
                    nodeId2,
                    typeof(TestType2)
                }
            };

            table
                .Should().HaveCount(2)
                .And.Contain((nodeId1, typeof(TestType1)))
                .And.Contain((nodeId2, typeof(TestType2)));
        }
        
        [Fact]
        public void CreateWithCollectionInitializer2()
        {
            var nodeId1 = ExpandedNodeId.Parse("nsu=Workstation.UaClient.UnitTests;s=TestType1");
            var nodeId2 = ExpandedNodeId.Parse("nsu=Workstation.UaClient.UnitTests;s=TestType2");

            var table = new CustomEncodingTable
            {
                (
                    nodeId1,
                    typeof(TestType1)
                ),
                (
                    nodeId2,
                    typeof(TestType2)
                )
            };

            table
                .Should().HaveCount(2)
                .And.Contain((nodeId1, typeof(TestType1)))
                .And.Contain((nodeId2, typeof(TestType2)));
        }
        
        [Fact]
        public void CreateFromEncodingTable()
        {
            var nodeId1 = ExpandedNodeId.Parse("nsu=Workstation.UaClient.UnitTests;s=TestType1");
            var nodeId2 = ExpandedNodeId.Parse("nsu=Workstation.UaClient.UnitTests;s=TestType2");

            var table = new CustomEncodingTable(new []
            {
                (
                    nodeId1,
                    typeof(TestType1)
                ),
                (
                    nodeId2,
                    typeof(TestType2)
                )
            });

            table
                .Should().HaveCount(2)
                .And.Contain((nodeId1, typeof(TestType1)))
                .And.Contain((nodeId2, typeof(TestType2)));
        }
        
        [Fact]
        public void CreateFromNullEncodingTable()
        {
            Action act = () => new CustomEncodingTable(null);

            act
                .Should().Throw<ArgumentNullException>();
        }
        
        [Fact]
        public void CreateFromTypes()
        {
            var nodeId1 = ExpandedNodeId.Parse("nsu=Workstation.UaClient.UnitTests;s=TestType1");
            var nodeId2 = ExpandedNodeId.Parse("nsu=Workstation.UaClient.UnitTests;s=TestType2");

            var table = new CustomEncodingTable<TestEncodingIdAttribute>(new []
            {
                    typeof(TestType1),
                    typeof(TestType2)
            });

            table
                .Should().HaveCount(2)
                .And.Contain((nodeId1, typeof(TestType1)))
                .And.Contain((nodeId2, typeof(TestType2)));
        }
        
        [Fact]
        public void CreateWithTypeCollectionInitializer()
        {
            var nodeId1 = ExpandedNodeId.Parse("nsu=Workstation.UaClient.UnitTests;s=TestType1");
            var nodeId2 = ExpandedNodeId.Parse("nsu=Workstation.UaClient.UnitTests;s=TestType2");

            var table = new CustomEncodingTable<TestEncodingIdAttribute>
            {
                    typeof(TestType1),
                    typeof(TestType2)
            };

            table
                .Should().HaveCount(2)
                .And.Contain((nodeId1, typeof(TestType1)))
                .And.Contain((nodeId2, typeof(TestType2)));
        }
        
        [Fact]
        public void CreateFromAssembly()
        {
            var nodeId1 = ExpandedNodeId.Parse("nsu=Workstation.UaClient.UnitTests;s=TestType1");
            var nodeId2 = ExpandedNodeId.Parse("nsu=Workstation.UaClient.UnitTests;s=TestType2");

            var table = new CustomEncodingTable<TestEncodingIdAttribute>(this.GetType().Assembly);

            table
                .Should().HaveCount(2)
                .And.Contain((nodeId1, typeof(TestType1)))
                .And.Contain((nodeId2, typeof(TestType2)));
        }
        
        [Fact]
        public void CreateFromNonEncodable()
        {
            Action act = () => new CustomEncodingTable<TestEncodingIdAttribute>(new[]
            {
                typeof(TestTypeWithoutIEncodable)
            });

            act
                .Should().Throw<ArgumentException>();
        }
        
        [Fact]
        public void AddNonEncodable()
        {
            var table = new CustomEncodingTable<TestEncodingIdAttribute>();

            table.Invoking(t => t.Add(typeof(TestTypeWithoutIEncodable)))
                .Should().Throw<ArgumentException>();
        }
        
        [Fact]
        public void CreateFromAttributeless()
        {
            Action act = () => new CustomEncodingTable<TestEncodingIdAttribute>(new[]
            {
                typeof(TestTypeWithoutAttribute)
            });

            act
                .Should().Throw<ArgumentException>();
        }
        
        [Fact]
        public void AddAttributeless()
        {
            var table = new CustomEncodingTable<TestEncodingIdAttribute>();

            table.Invoking(t => t.Add(typeof(TestTypeWithoutAttribute)))
                .Should().Throw<ArgumentException>();
        }
        
        [Fact]
        public void CreateFromNull()
        {
            Action act = () => new CustomEncodingTable<TestEncodingIdAttribute>(new[]
            {
                default(Type)
            });

            act
                .Should().Throw<ArgumentNullException>();
        }
        
        [Fact]
        public void AddNull()
        {
            var table = new CustomEncodingTable<TestEncodingIdAttribute>();

            table.Invoking(t => t.Add(null))
                .Should().Throw<ArgumentNullException>();
        }
        
        [Fact]
        public void CreateFromNullAssembly()
        {
            Action act = () => new CustomEncodingTable<TestEncodingIdAttribute>(default(System.Reflection.Assembly));

            act
                .Should().Throw<ArgumentNullException>();
        }
        
        [Fact]
        public void CreateFromNullEnumerable()
        {
            Action act = () => new CustomEncodingTable<TestEncodingIdAttribute>(default(IEnumerable<Type>));

            act
                .Should().Throw<ArgumentNullException>();
        }
    }
}
