using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using Workstation.ServiceModel.Ua;
using Xunit;

namespace Workstation.UaClient.UnitTests
{
    public class UserNameIdentityTests
    {
        [InlineData("", "")]
        [InlineData("UserName", "Password")]
        [InlineData(null, "Password")]
        [InlineData("UserName", null)]
        [Theory]
        public void Create(string userName, string password)
        {
            var user = new UserNameIdentity(userName, password);

            user.UserName
                .Should().Be(userName);
            user.Password
                .Should().Be(password);
        }
    }
}
