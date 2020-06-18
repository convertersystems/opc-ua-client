using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using Workstation.ServiceModel.Ua;
using Xunit;

namespace Workstation.UaClient.UnitTests
{
    public class MonitoredItemAttributeTests
    {
        [Fact]
        public void Create()
        {
            var att = new MonitoredItemAttribute("s=Hello");

            att.AttributeId
                .Should().Be(AttributeIds.Value);
            att.DataChangeTrigger
                .Should().Be(DataChangeTrigger.StatusValue);
            att.DeadbandType
                .Should().Be(DeadbandType.None);
            att.DeadbandValue
                .Should().Be(0.0);
            att.DiscardOldest
                .Should().BeTrue();
            att.IndexRange
                .Should().BeNull();
            att.NodeId
                .Should().Be("s=Hello");
            att.QueueSize
                .Should().Be(0);
            att.SamplingInterval
                .Should().Be(-1);
        }
    }
}
