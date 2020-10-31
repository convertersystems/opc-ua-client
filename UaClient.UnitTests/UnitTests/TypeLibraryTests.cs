using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Workstation.ServiceModel.Ua;
using Xunit;

namespace Workstation.UaClient.UnitTests
{
    public class TypeLibraryTests
    {
        [Fact]
        public void FindBinaryEncodingIdByType()
        {
           TypeLibrary.TryGetBinaryEncodingIdFromType(typeof(ReadRequest), out ExpandedNodeId nodeid)
                .Should().BeTrue();
            nodeid
                .Should().Be(ExpandedNodeId.Parse(ObjectIds.ReadRequest_Encoding_DefaultBinary));
        }

        [Fact]
        public void FindTypeByBinaryEncodingId()
        {
            TypeLibrary.TryGetTypeFromBinaryEncodingId(ExpandedNodeId.Parse(ObjectIds.ReadRequest_Encoding_DefaultBinary), out Type type)
                .Should().BeTrue();
            type
                .Should().Be(typeof(ReadRequest));
        }

    }
}
