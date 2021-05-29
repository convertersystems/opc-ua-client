using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Workstation.ServiceModel.Ua;
using Xunit;

namespace Workstation.UaClient.UnitTests
{
    public class DataValueExtensionTests
    {
        [Fact]
        public void GetValueAsString()
        {
            var val = new DataValue("Hi");

            val.GetValueOrDefault<string>()
                .Should().Be("Hi");
            val.GetValueOrDefault(-1)
               .Should().Be(-1);
        }

        [Fact]
        public void GetValueAsCustomVector()
        {
            var obj = new CustomTypeLibrary.CustomVector { X = 1.0, Y = 2.0, Z = 3.0 };
            var val = new DataValue(obj);

            val.GetValueOrDefault<CustomTypeLibrary.CustomVector>()
                .Should().BeEquivalentTo(obj);
            val.GetValueOrDefault<object>()
                .Should().BeEquivalentTo(obj);
            val.GetValueOrDefault(-1)
               .Should().Be(-1);
        }

        [Fact]
        public void GetValueAsArrayInt32()
        {
            var array = new int[] { 1, 2, 3, 4, 5 };
            var val = new DataValue(array);

            val.GetValueOrDefault<int[]>()
                .Should().BeEquivalentTo(array);
            val.GetValueOrDefault(-1)
               .Should().Be(-1);
        }

        [Fact]
        public void GetValueAsArrayCustomVector()
        {
            var array = new CustomTypeLibrary.CustomVector[]
            {
                new CustomTypeLibrary.CustomVector { X = 1.0, Y = 2.0, Z = 3.0 }
            };
            var val = new DataValue(array);

            val.GetValueOrDefault<CustomTypeLibrary.CustomVector[]>()
                .Should().BeEquivalentTo(array);
            val.GetValueOrDefault<object[]>()
                .Should().BeEquivalentTo(array);
            val.GetValueOrDefault(-1)
               .Should().Be(-1);
        }
    }
}
