// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Workstation.Collections;
using Workstation.ServiceModel.Ua;
using Workstation.ServiceModel.Ua.Channels;

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
            using (var eventlistener = new ConsoleEventListener())
            {
                eventlistener.EnableEvents(Workstation.ServiceModel.Ua.EventSource.Log, EventLevel.Verbose);

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

                        var channel = new UaTcpSessionChannel(
                            this.localDescription,
                            localCertificate,
                            selectedUserIdentity,
                            selectedEndpoint);

                        Console.WriteLine($"Creating session with endpoint '{channel.RemoteEndpoint.EndpointUrl}'.");
                        Console.WriteLine($"SecurityPolicy: '{channel.RemoteEndpoint.SecurityPolicyUri}'.");
                        Console.WriteLine($"SecurityMode: '{channel.RemoteEndpoint.SecurityMode}'.");
                        Console.WriteLine($"UserIdentityToken: '{channel.UserIdentity}'.");
                        try
                        {
                            await channel.OpenAsync();
                            Console.WriteLine($"Closing session '{channel.SessionId}'.");
                            await channel.CloseAsync();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error opening session '{channel.SessionId}'. {ex.Message}");
                        }
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
            using (var eventlistener = new ConsoleEventListener())
            {
                eventlistener.EnableEvents(Workstation.ServiceModel.Ua.EventSource.Log, EventLevel.Verbose);

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

                var channel = new UaTcpSessionChannel(
                    this.localDescription,
                    localCertificate,
                    selectedUserIdentity,
                    selectedEndpoint,
                    sessionTimeout: 10000);

                Console.WriteLine($"Creating session with endpoint '{channel.RemoteEndpoint.EndpointUrl}'.");
                Console.WriteLine($"SecurityPolicy: '{channel.RemoteEndpoint.SecurityPolicyUri}'.");
                Console.WriteLine($"SecurityMode: '{channel.RemoteEndpoint.SecurityMode}'.");
                await channel.OpenAsync();
                Console.WriteLine($"Activated session '{channel.SessionId}'.");

                // server should close session due to inactivity
                await Task.Delay(20000);

                // should throw exception
                var readRequest = new ReadRequest { NodesToRead = new[] { new ReadValueId { NodeId = NodeId.Parse(VariableIds.Server_ServerStatus_CurrentTime), AttributeId = AttributeIds.Value } } };
                await channel.ReadAsync(readRequest);

                Console.WriteLine($"Closing session '{channel.SessionId}'.");
                await channel.CloseAsync();
            }
        }

        /// <summary>
        /// Tests reconnecting to previous endpoint and transferring subscriptions.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task TransferSubscriptions()
        {
            using (var eventlistener = new ConsoleEventListener())
            {
                eventlistener.EnableEvents(Workstation.ServiceModel.Ua.EventSource.Log, EventLevel.Verbose);

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

                var channel = new UaTcpSessionChannel(
                    this.localDescription,
                    localCertificate,
                    selectedUserIdentity,
                    selectedEndpoint);

                Console.WriteLine($"Creating session with endpoint '{channel.RemoteEndpoint.EndpointUrl}'.");
                Console.WriteLine($"SecurityPolicy: '{channel.RemoteEndpoint.SecurityPolicyUri}'.");
                Console.WriteLine($"SecurityMode: '{channel.RemoteEndpoint.SecurityMode}'.");
                await channel.OpenAsync();
                Console.WriteLine($"Activated session '{channel.SessionId}'.");
                var req = new CreateSubscriptionRequest
                {
                    RequestedPublishingInterval = 1000,
                    RequestedMaxKeepAliveCount = 20,
                    PublishingEnabled = true
                };
                var res = await channel.CreateSubscriptionAsync(req);
                Console.WriteLine($"Created subscription '{res.SubscriptionId}'.");

                Console.WriteLine($"Aborting session '{channel.SessionId}'.");
                await channel.AbortAsync();

                var channel2 = new UaTcpSessionChannel(
                    this.localDescription,
                    localCertificate,
                    selectedUserIdentity,
                    selectedEndpoint);

                await channel2.OpenAsync();
                Console.WriteLine($"Activated session '{channel2.SessionId}'.");

                var req2 = new TransferSubscriptionsRequest
                {
                    SubscriptionIds = new[] { res.SubscriptionId }
                };
                var res2 = await channel2.TransferSubscriptionsAsync(req2);
                Console.WriteLine($"Transferred subscription result '{res2.Results[0].StatusCode}'.");
                Console.WriteLine($"Closing session '{channel2.SessionId}'.");
                await channel2.CloseAsync();

                Assert.IsTrue(StatusCode.IsGood(res2.Results[0].StatusCode));
            }
        }

        /// <summary>
        /// Tests connecting to endpoint and creating subscriptions.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task TestSubscription()
        {
            // Setup a logger for the EventSource
            using (var eventlistener = new ConsoleEventListener())
            {
                eventlistener.EnableEvents(Workstation.ServiceModel.Ua.EventSource.Log, EventLevel.Verbose);

                // discover available endpoints of server.
                var getEndpointsRequest = new GetEndpointsRequest
                {
                    EndpointUrl = this.endpointUrl,
                    ProfileUris = new[] { TransportProfileUris.UaTcpTransport }
                };
                Console.WriteLine($"Discovering endpoints of '{getEndpointsRequest.EndpointUrl}'.");
                var getEndpointsResponse = await UaTcpDiscoveryClient.GetEndpointsAsync(getEndpointsRequest);
                var selectedEndpoint = getEndpointsResponse.Endpoints.OrderBy(e => e.SecurityLevel).Last();

                var session = new UaTcpSessionClient(
                    this.localDescription,
                    ad => Task.FromResult<X509Certificate2>(ad.GetCertificate()),
                    ed => Task.FromResult<IUserIdentity>(new UserNameIdentity("root", "secret")),
                    selectedEndpoint);

                Console.WriteLine($"Creating session with endpoint '{session.RemoteEndpoint.EndpointUrl}'.");
                Console.WriteLine($"SecurityPolicy: '{session.RemoteEndpoint.SecurityPolicyUri}'.");
                Console.WriteLine($"SecurityMode: '{session.RemoteEndpoint.SecurityMode}'.");

                var sub = new MySubscription();
                session.Subscribe(sub);

                Console.WriteLine($"Created subscription.");

                await Task.Delay(5000);
                session.Dispose();

                Assert.IsTrue(sub.CurrentTime != DateTime.MinValue, "CurrentTime");
                Assert.IsTrue(sub.CurrentTimeAsDataValue != null, "CurrentTimeAsDataValue");
                Assert.IsTrue(sub.CurrentTimeQueue.Count > 0, "CurrentTimeQueue");
            }
        }

        [Subscription(publishingInterval: 500, keepAliveCount: 20)]
        private class MySubscription
        {
            /// <summary>
            /// Gets the value of CurrentTime.
            /// </summary>
            [MonitoredItem(nodeId: "i=2258")]
            public DateTime CurrentTime
            {
                get { return this.currentTime; }
                private set { this.currentTime = value; }
            }

            private DateTime currentTime;

            /// <summary>
            /// Gets the value of CurrentTimeAsDataValue.
            /// </summary>
            [MonitoredItem(nodeId: "i=2258")]
            public DataValue CurrentTimeAsDataValue
            {
                get { return this.currentTimeAsDataValue; }
                private set { this.currentTimeAsDataValue = value; }
            }

            private DataValue currentTimeAsDataValue;

            /// <summary>
            /// Gets the value of CurrentTimeQueue.
            /// </summary>
            [MonitoredItem(nodeId: "i=2258")]
            public ObservableQueue<DataValue> CurrentTimeQueue { get; } = new ObservableQueue<DataValue>(capacity: 16, isFixedSize: true);
        }
    }
}