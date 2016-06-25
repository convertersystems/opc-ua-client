// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Workstation.ServiceModel.Ua;

namespace Workstation.UaClient.UnitTests
{
    [TestClass]
    public class UnitTest1
    {
        private ApplicationDescription localDescription = new ApplicationDescription
        {
            ApplicationName = typeof(UnitTest1).Namespace,
            ApplicationUri = $"urn:{System.Net.Dns.GetHostName()}:{typeof(UnitTest1).Namespace}",
            ApplicationType = ApplicationType.Client
        };

        // private string endpointUrl = "opc.tcp://localhost:51210/UA/SampleServer"; // the endpoint of the OPCF SampleServer
        // private string endpointUrl = "opc.tcp://localhost:48010"; // the endpoint of the UaCPPServer.
        // private string endpointUrl = "opc.tcp://localhost:51212"; // the endpoint of the Workstation.TestServer.
        private string endpointUrl = "opc.tcp://localhost:26543"; // the endpoint of the Workstation.NodeServer.

        /// <summary>
        /// Tests creation of self-signed certificate.
        /// </summary>
        [TestMethod]
        public void GetOrAddCertificate()
        {
            // get or add application certificate.
            var localCertificate = this.localDescription.GetCertificate();
            if (localCertificate == null)
            {
                throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed, "Application certificate is missing.");
            }
        }

        /// <summary>
        /// Tests all combinations of endpoint security and user identity types supported by the server.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task ConnnectToAllEndpoints()
        {
            // get or add application certificate.
            var localCertificate = this.localDescription.GetCertificate();
            if (localCertificate == null)
            {
                throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed, "Application certificate is missing.");
            }

            // discover available endpoints of server.
            var getEndpointsRequest = new GetEndpointsRequest
            {
                EndpointUrl = this.endpointUrl,
                ProfileUris = new[] { TransportProfileUris.UaTcpTransport }
            };
            Console.WriteLine($"Discovering endpoints of '{getEndpointsRequest.EndpointUrl}'.");
            var getEndpointsResponse = await UaTcpDiscoveryClient.GetEndpointsAsync(getEndpointsRequest);

            // for each endpoint and user identity type, try creating a session and reading a few nodes.
            foreach (var selectedEndpoint in getEndpointsResponse.Endpoints.OrderBy(e => e.SecurityLevel))
            {
                foreach (var selectedTokenPolicy in selectedEndpoint.UserIdentityTokens)
                {
                    IUserIdentity selectedUserIdentity;
                    switch (selectedTokenPolicy.TokenType)
                    {
                        case UserTokenType.UserName:
                            selectedUserIdentity = new UserNameIdentity("root", "secret");
                            break;

                        case UserTokenType.Certificate:
                            selectedUserIdentity = new X509Identity(localCertificate);
                            break;

                        default:
                            selectedUserIdentity = new AnonymousIdentity();
                            break;
                    }

                    var client = new UaTcpSessionClient(
                        this.localDescription,
                        localCertificate,
                        selectedUserIdentity,
                        selectedEndpoint);
                    Console.WriteLine($"Creating session with endpoint '{client.RemoteEndpoint.EndpointUrl}'.");
                    Console.WriteLine($"SecurityPolicy: '{client.RemoteEndpoint.SecurityPolicyUri}'.");
                    Console.WriteLine($"SecurityMode: '{client.RemoteEndpoint.SecurityMode}'.");
                    Console.WriteLine($"UserIdentityToken: '{client.UserIdentity}'.");
                    try
                    {
                        await client.OpenAsync();
                        Console.WriteLine($"Closing session '{client.SessionId}'.");
                        await client.CloseAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error opening session '{client.SessionId}'. {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Tests result of session timeout causes server to close socket.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        [ExpectedException(typeof(ServiceResultException), "The session id is not valid.")]
        public async Task SessionTimeoutCausesFault()
        {
            // get or add application certificate.
            var localCertificate = this.localDescription.GetCertificate();
            if (localCertificate == null)
            {
                throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed, "Application certificate is missing.");
            }

            // discover available endpoints of server.
            var getEndpointsRequest = new GetEndpointsRequest
            {
                EndpointUrl = this.endpointUrl,
                ProfileUris = new[] { TransportProfileUris.UaTcpTransport }
            };
            Console.WriteLine($"Discovering endpoints of '{getEndpointsRequest.EndpointUrl}'.");
            var getEndpointsResponse = await UaTcpDiscoveryClient.GetEndpointsAsync(getEndpointsRequest);
            var selectedEndpoint = getEndpointsResponse.Endpoints.OrderBy(e => e.SecurityLevel).Last();

            var selectedTokenType = selectedEndpoint.UserIdentityTokens[0].TokenType;
            IUserIdentity selectedUserIdentity;
            switch (selectedTokenType)
            {
                case UserTokenType.UserName:
                    selectedUserIdentity = new UserNameIdentity("root", "secret");
                    break;

                case UserTokenType.Certificate:
                    selectedUserIdentity = new X509Identity(localCertificate);
                    break;

                default:
                    selectedUserIdentity = new AnonymousIdentity();
                    break;
            }

            var client = new UaTcpSessionClient(
                this.localDescription,
                localCertificate,
                selectedUserIdentity,
                selectedEndpoint)
            {
                SessionTimeout = 10000
            };
            Console.WriteLine($"Creating session with endpoint '{client.RemoteEndpoint.EndpointUrl}'.");
            Console.WriteLine($"SecurityPolicy: '{client.RemoteEndpoint.SecurityPolicyUri}'.");
            Console.WriteLine($"SecurityMode: '{client.RemoteEndpoint.SecurityMode}'.");
            await client.OpenAsync();
            Console.WriteLine($"Activated session '{client.SessionId}'.");

            // server should close session due to inactivity
            await Task.Delay(20000);

            // should throw exception
            var readRequest = new ReadRequest { NodesToRead = new[] { new ReadValueId { NodeId = NodeId.Parse(VariableIds.Server_ServerStatus_CurrentTime), AttributeId = AttributeIds.Value } } };
            await client.ReadAsync(readRequest);

            Console.WriteLine($"Closing session '{client.SessionId}'.");
            await client.CloseAsync();
        }

        /// <summary>
        /// Tests reconnecting to previous endpoint and transferring subscriptions.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task TransferSubscriptions()
        {
            // get or add application certificate.
            var localCertificate = this.localDescription.GetCertificate();
            if (localCertificate == null)
            {
                throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed, "Application certificate is missing.");
            }

            // discover available endpoints of server.
            var getEndpointsRequest = new GetEndpointsRequest
            {
                EndpointUrl = this.endpointUrl,
                ProfileUris = new[] { TransportProfileUris.UaTcpTransport }
            };
            Console.WriteLine($"Discovering endpoints of '{getEndpointsRequest.EndpointUrl}'.");
            var getEndpointsResponse = await UaTcpDiscoveryClient.GetEndpointsAsync(getEndpointsRequest);
            var selectedEndpoint = getEndpointsResponse.Endpoints.OrderBy(e => e.SecurityLevel).Last();

            IUserIdentity selectedUserIdentity = new UserNameIdentity("root", "secret");

            var client = new UaTcpSessionClient(
                this.localDescription,
                localCertificate,
                selectedUserIdentity,
                selectedEndpoint);
            Console.WriteLine($"Creating session with endpoint '{client.RemoteEndpoint.EndpointUrl}'.");
            Console.WriteLine($"SecurityPolicy: '{client.RemoteEndpoint.SecurityPolicyUri}'.");
            Console.WriteLine($"SecurityMode: '{client.RemoteEndpoint.SecurityMode}'.");
            await client.OpenAsync();
            Console.WriteLine($"Activated session '{client.SessionId}'.");
            var req = new CreateSubscriptionRequest
            {
                RequestedPublishingInterval = 1000,
                RequestedMaxKeepAliveCount = 20,
                PublishingEnabled = true
            };
            var res = await client.CreateSubscriptionAsync(req);
            Console.WriteLine($"Created subscription '{res.SubscriptionId}'.");

            Console.WriteLine($"Aborting session '{client.SessionId}'.");
            await client.AbortAsync();

            var client2 = new UaTcpSessionClient(
                this.localDescription,
                localCertificate,
                selectedUserIdentity,
                selectedEndpoint);
            await client2.OpenAsync();
            Console.WriteLine($"Activated session '{client2.SessionId}'.");

            var req2 = new TransferSubscriptionsRequest
            {
                SubscriptionIds = new[] { res.SubscriptionId }
            };
            var res2 = await client2.TransferSubscriptionsAsync(req2);
            Console.WriteLine($"Transferred subscription result '{res2.Results[0].StatusCode}'.");

            Assert.IsTrue(StatusCode.IsGood(res2.Results[0].StatusCode));

            Console.WriteLine($"Closing session '{client2.SessionId}'.");
            await client2.CloseAsync();
        }

    }
}