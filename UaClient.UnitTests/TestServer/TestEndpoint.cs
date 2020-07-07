using System;
using System.Collections.Generic;
using System.Text;
using Workstation.ServiceModel.Ua;

namespace Workstation.UaClient.TestServer
{
    public class TestEndpoint
    {
        public string EndpointUrl { get; set; }
        public string SecurityPolicyUri { get; set; }
        public MessageSecurityMode SecurityMode { get; set; }
        public IUserIdentity UserIdentity { get; set; }

        public EndpointDescription EndpointDescription => new EndpointDescription
        {
            EndpointUrl = this.EndpointUrl,
            SecurityPolicyUri = this.SecurityPolicyUri,
            SecurityMode = this.SecurityMode,
        };
}
}
