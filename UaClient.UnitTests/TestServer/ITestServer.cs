using System;
using System.Collections.Generic;
using System.Text;
using Workstation.ServiceModel.Ua;

namespace Workstation.UaClient.TestServer
{
    interface ITestServer
    {
        string EndpointUrl { get; }
        TestEndpoint[] TestEndpoints { get; }
        IUserIdentity[] UserIdentities { get; }
    }
}
