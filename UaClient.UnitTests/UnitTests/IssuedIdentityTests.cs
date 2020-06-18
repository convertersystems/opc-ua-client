using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using Workstation.ServiceModel.Ua;
using Xunit;

namespace Workstation.UaClient.UnitTests
{
    public class IssuedIdentityTests
    {
        [InlineData(null)]
        [InlineData(new byte[] { })]
        [InlineData(new byte[] { 0x45, 0xff})]
        [Theory]
        public void Create(byte[] tokenData)
        {
            var id = new IssuedIdentity(tokenData);

            id.TokenData
                .Should().BeSameAs(tokenData);
        }
    }
}
