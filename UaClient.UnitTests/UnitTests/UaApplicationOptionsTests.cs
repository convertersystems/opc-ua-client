using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using Workstation.ServiceModel.Ua;
using Xunit;

namespace Workstation.UaClient.UnitTests
{
    public class UaApplicationOptionsTests
    {
        [Fact]
        public void UaTcpTransportChannelOptionsDefaults()
        {
            var lowestBufferSize = 1024u;

            var options = new UaTcpTransportChannelOptions();

            options.LocalMaxChunkCount
                .Should().BeGreaterOrEqualTo(lowestBufferSize);
            options.LocalMaxMessageSize
                .Should().BeGreaterOrEqualTo(lowestBufferSize);
            options.LocalReceiveBufferSize
                .Should().BeGreaterOrEqualTo(lowestBufferSize);
            options.LocalSendBufferSize
                .Should().BeGreaterOrEqualTo(lowestBufferSize);
        }

        [Fact]
        public void UaTcpSecureChannelOptionsDefaults()
        {
            var shortestTimespan = TimeSpan.FromMilliseconds(100);

            var options = new UaTcpSecureChannelOptions();

            TimeSpan.FromMilliseconds(options.TimeoutHint)
                .Should().BeGreaterOrEqualTo(shortestTimespan);

            options.DiagnosticsHint
                .Should().Be(0);
        }

        [Fact]
        public void UaTcpSessionChannelOptionsDefaults()
        {
            var shortestTimespan = TimeSpan.FromMilliseconds(100);

            var options = new UaTcpSessionChannelOptions();

            TimeSpan.FromMilliseconds(options.SessionTimeout)
                .Should().BeGreaterOrEqualTo(shortestTimespan);
        }
    }
}
