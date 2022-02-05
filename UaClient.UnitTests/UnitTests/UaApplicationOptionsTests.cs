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
        public void ClientTransportChannelOptionsDefaults()
        {
            var lowestBufferSize = 8192u;

            var options = new ClientTransportChannelOptions();

            options.LocalReceiveBufferSize
                .Should().BeGreaterOrEqualTo(lowestBufferSize);
            options.LocalSendBufferSize
                .Should().BeGreaterOrEqualTo(lowestBufferSize);
        }

        [Fact]
        public void ClientSecureChannelOptionsDefaults()
        {
            var shortestTimespan = TimeSpan.FromMilliseconds(100);

            var options = new ClientSecureChannelOptions();

            TimeSpan.FromMilliseconds(options.TimeoutHint)
                .Should().BeGreaterOrEqualTo(shortestTimespan);

            options.DiagnosticsHint
                .Should().Be(0);
        }

        [Fact]
        public void ClientSessionChannelOptionsDefaults()
        {
            var shortestTimespan = TimeSpan.FromMilliseconds(100);

            var options = new ClientSessionChannelOptions();

            TimeSpan.FromMilliseconds(options.SessionTimeout)
                .Should().BeGreaterOrEqualTo(shortestTimespan);
        }
    }
}
