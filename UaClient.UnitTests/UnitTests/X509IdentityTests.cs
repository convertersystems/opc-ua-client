using FluentAssertions;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.Text;
using Workstation.ServiceModel.Ua;
using Xunit;

namespace Workstation.UaClient.UnitTests
{
    public class X509IdentityTests
    {
        [Fact]
        public void CreateNull()
        {
            var id = new X509Identity(null, null);

            id.Certificate
                .Should().BeNull();
            id.PrivateKey
                .Should().BeNull();
        }
    }
}
