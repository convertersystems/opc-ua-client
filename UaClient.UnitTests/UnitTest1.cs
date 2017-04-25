// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Workstation.Collections;
using Workstation.ServiceModel.Ua;
using Workstation.ServiceModel.Ua.Channels;
using Microsoft.Extensions.Configuration;

namespace Workstation.UaClient.UnitTests
{
    [TestClass]
    public class UnitTest1
    {
        //private const string EndpointUrl = "opc.tcp://bculz-PC:53530/OPCUA/SimulationServer"; // the endpoint of the Prosys UA Simulation Server
        // private const string EndpointUrl = "opc.tcp://localhost:51210/UA/SampleServer"; // the endpoint of the OPCF SampleServer
        // private const string EndpointUrl = "opc.tcp://localhost:48010"; // the endpoint of the UaCPPServer.
        private const string EndpointUrl = "opc.tcp://localhost:26543"; // the endpoint of the Workstation.NodeServer.

        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger<UnitTest1> logger;
        private readonly ApplicationDescription localDescription;
        private readonly ICertificateStore certificateStore;

        public UnitTest1()
        {
            this.loggerFactory = new LoggerFactory();
            this.loggerFactory.AddDebug(LogLevel.Trace);
            this.logger = this.loggerFactory?.CreateLogger<UnitTest1>();

            this.localDescription = new ApplicationDescription
            {
                ApplicationName = "Workstation.UaClient.UnitTests",
                ApplicationUri = $"urn:{Dns.GetHostName()}:Workstation.UaClient.UnitTests",
                ApplicationType = ApplicationType.Client
            };

            this.certificateStore = new DirectoryStore(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Workstation.UaClient.UnitTests",
                    "pki"));
        }

        /// <summary>
        /// Tests endpoint with no security and with no Certificate.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task ConnnectToEndpointsWithNoSecurityAndWithNoCertificate()
        {
            // discover available endpoints of server.
            var getEndpointsRequest = new GetEndpointsRequest
            {
                EndpointUrl = EndpointUrl,
                ProfileUris = new[] { TransportProfileUris.UaTcpTransport }
            };
            Console.WriteLine($"Discovering endpoints of '{getEndpointsRequest.EndpointUrl}'.");
            var getEndpointsResponse = await UaTcpDiscoveryService.GetEndpointsAsync(getEndpointsRequest);

            // for each endpoint and user identity type, try creating a session and reading a few nodes.
            foreach (var selectedEndpoint in getEndpointsResponse.Endpoints.Where(e => e.SecurityMode == MessageSecurityMode.None))
            {
                foreach (var selectedTokenPolicy in selectedEndpoint.UserIdentityTokens)
                {
                    IUserIdentity selectedUserIdentity;
                    switch (selectedTokenPolicy.TokenType)
                    {
                        case UserTokenType.UserName:
                            selectedUserIdentity = new UserNameIdentity("root", "secret");
                            break;

                        //case UserTokenType.Certificate:
                        //    selectedUserIdentity = new X509Identity(localCertificate);
                        //    break;

                        case UserTokenType.Anonymous:
                            selectedUserIdentity = new AnonymousIdentity();
                            break;

                        default:
                            continue;
                    }

                    var channel = new UaTcpSessionChannel(
                        this.localDescription,
                        null,
                        async e => selectedUserIdentity,
                        selectedEndpoint,
                        loggerFactory: this.loggerFactory);

                    await channel.OpenAsync();
                    Console.WriteLine($"Opened session with endpoint '{channel.RemoteEndpoint.EndpointUrl}'.");
                    Console.WriteLine($"SecurityPolicy: '{channel.RemoteEndpoint.SecurityPolicyUri}'.");
                    Console.WriteLine($"SecurityMode: '{channel.RemoteEndpoint.SecurityMode}'.");
                    Console.WriteLine($"UserIdentityToken: '{channel.UserIdentity}'.");

                    Console.WriteLine($"Closing session '{channel.SessionId}'.");
                    await channel.CloseAsync();
                }
            }
        }

        /// <summary>
        /// Tests all combinations of endpoint security and user identity types supported by the server.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task ConnnectToAllEndpoints()
        {

            // discover available endpoints of server.
            var getEndpointsRequest = new GetEndpointsRequest
            {
                EndpointUrl = EndpointUrl,
                ProfileUris = new[] { TransportProfileUris.UaTcpTransport }
            };
            Console.WriteLine($"Discovering endpoints of '{getEndpointsRequest.EndpointUrl}'.");
            var getEndpointsResponse = await UaTcpDiscoveryService.GetEndpointsAsync(getEndpointsRequest);

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

                        //case UserTokenType.Certificate:
                        //    selectedUserIdentity = new X509Identity(localCertificate);
                        //    break;

                        case UserTokenType.Anonymous:
                            selectedUserIdentity = new AnonymousIdentity();
                            break;

                        default:
                            continue;
                    }

                    var channel = new UaTcpSessionChannel(
                        this.localDescription,
                        this.certificateStore,
                        async e => selectedUserIdentity,
                        selectedEndpoint,
                        loggerFactory: this.loggerFactory,
                        options: new UaTcpSessionChannelOptions { TimeoutHint = 60000 });

                    await channel.OpenAsync();
                    Console.WriteLine($"Opened session with endpoint '{channel.RemoteEndpoint.EndpointUrl}'.");
                    Console.WriteLine($"SecurityPolicy: '{channel.RemoteEndpoint.SecurityPolicyUri}'.");
                    Console.WriteLine($"SecurityMode: '{channel.RemoteEndpoint.SecurityMode}'.");
                    Console.WriteLine($"UserIdentityToken: '{channel.UserIdentity}'.");

                    Console.WriteLine($"Closing session '{channel.SessionId}'.");
                    await channel.CloseAsync();
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
            // discover available endpoints of server.
            var getEndpointsRequest = new GetEndpointsRequest
            {
                EndpointUrl = EndpointUrl,
                ProfileUris = new[] { TransportProfileUris.UaTcpTransport }
            };
            Console.WriteLine($"Discovering endpoints of '{getEndpointsRequest.EndpointUrl}'.");
            var getEndpointsResponse = await UaTcpDiscoveryService.GetEndpointsAsync(getEndpointsRequest);
            var selectedEndpoint = getEndpointsResponse.Endpoints.OrderBy(e => e.SecurityLevel).Last();

            var selectedTokenType = selectedEndpoint.UserIdentityTokens[0].TokenType;
            IUserIdentity selectedUserIdentity;
            switch (selectedTokenType)
            {
                case UserTokenType.UserName:
                    selectedUserIdentity = new UserNameIdentity("root", "secret");
                    break;

                //case UserTokenType.Certificate:
                //    selectedUserIdentity = new X509Identity(localCertificate);
                //    break;

                default:
                    selectedUserIdentity = new AnonymousIdentity();
                    break;
            }

            var channel = new UaTcpSessionChannel(
                this.localDescription,
                this.certificateStore,
                async e => selectedUserIdentity,
                selectedEndpoint,
                loggerFactory: this.loggerFactory,
                options: new UaTcpSessionChannelOptions { SessionTimeout = 10000 });

            await channel.OpenAsync();
            Console.WriteLine($"Opened session with endpoint '{channel.RemoteEndpoint.EndpointUrl}'.");
            Console.WriteLine($"SecurityPolicy: '{channel.RemoteEndpoint.SecurityPolicyUri}'.");
            Console.WriteLine($"SecurityMode: '{channel.RemoteEndpoint.SecurityMode}'.");
            Console.WriteLine($"Activated session '{channel.SessionId}'.");

            // server should close session due to inactivity
            await Task.Delay(20000);

            // should throw exception
            var readRequest = new ReadRequest { NodesToRead = new[] { new ReadValueId { NodeId = NodeId.Parse(VariableIds.Server_ServerStatus_CurrentTime), AttributeId = AttributeIds.Value } } };
            await channel.ReadAsync(readRequest);

            Console.WriteLine($"Closing session '{channel.SessionId}'.");
            await channel.CloseAsync();
        }

        /// <summary>
        /// Tests connecting to endpoint and creating subscriptions.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task TestSubscription()
        {
            var config = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appSettings.json", true)
               .Build();

            var app = new UaApplicationBuilder()
                .UseApplicationUri($"urn:{Dns.GetHostName()}:Workstation.UaClient.UnitTests")
                .UseDirectoryStore(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Workstation.UaClient.UnitTests",
                    "pki"))
                .UseIdentity(new UserNameIdentity("root", "secret"))
                .UseLoggerFactory(this.loggerFactory)
                .Map(config)
                .Build();

            app.Run();

            var sub = new MySubscription();
            sub.PropertyChanged += (s, e) => { };

            Console.WriteLine($"Created subscription.");

            await Task.Delay(5000);
            app.Dispose();

            Assert.IsTrue(sub.CurrentTime != DateTime.MinValue, "CurrentTime");
            Assert.IsTrue(sub.CurrentTimeAsDataValue != null, "CurrentTimeAsDataValue");
            Assert.IsTrue(sub.CurrentTimeQueue.Count > 0, "CurrentTimeQueue");
        }

        [Subscription(endpointUrl: "opc.tcp://localhost:26543", publishingInterval: 500, keepAliveCount: 20)]
        private class MySubscription : SubscriptionBase
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