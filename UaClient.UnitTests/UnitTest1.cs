// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Workstation.Collections;
using Workstation.ServiceModel.Ua;
using Workstation.ServiceModel.Ua.Channels;

namespace Workstation.UaClient.UnitTests
{
    [TestClass]
    public class UnitTest1
    {
        // private const string EndpointUrl = "opc.tcp://localhost:16664"; // open62541
        // private const string EndpointUrl = "opc.tcp://bculz-PC:53530/OPCUA/SimulationServer"; // the endpoint of the Prosys UA Simulation Server
        // private const string EndpointUrl = "opc.tcp://localhost:51210/UA/SampleServer"; // the endpoint of the OPCF SampleServer
        // private const string EndpointUrl = "opc.tcp://localhost:48010"; // the endpoint of the UaCPPServer.
        private const string EndpointUrl = "opc.tcp://localhost:26543"; // the endpoint of the Workstation.RobotServer.
        // private const string EndpointUrl = "opc.tcp://192.168.0.11:4840"; // the endpoint of the Siemens 1500 PLC.

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
            var getEndpointsResponse = await UaTcpDiscoveryService.GetEndpointsAsync(getEndpointsRequest, this.loggerFactory);

            // for each endpoint and user identity type, try creating a session and reading a few nodes.
            foreach (var selectedEndpoint in getEndpointsResponse.Endpoints.Where(e => e.SecurityPolicyUri == SecurityPolicyUris.None))
            {
                foreach (var selectedTokenPolicy in selectedEndpoint.UserIdentityTokens)
                {
                    IUserIdentity selectedUserIdentity;
                    switch (selectedTokenPolicy.TokenType)
                    {
                        case UserTokenType.UserName:
                            selectedUserIdentity = new UserNameIdentity("root", "secret");
                            break;

                        case UserTokenType.Anonymous:
                            selectedUserIdentity = new AnonymousIdentity();
                            break;

                        default:
                            continue;
                    }

                    var channel = new UaTcpSessionChannel(
                        this.localDescription,
                        null,
                        selectedUserIdentity,
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

                        case UserTokenType.Anonymous:
                            selectedUserIdentity = new AnonymousIdentity();
                            break;

                        default:
                            continue;
                    }

                    var channel = new UaTcpSessionChannel(
                        this.localDescription,
                        this.certificateStore,
                        selectedUserIdentity,
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

                default:
                    selectedUserIdentity = new AnonymousIdentity();
                    break;
            }

            var channel = new UaTcpSessionChannel(
                this.localDescription,
                this.certificateStore,
                selectedUserIdentity,
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

        [TestMethod]
        public async Task ReadHistorical()
        {
            var channel = new UaTcpSessionChannel(
                this.localDescription,
                this.certificateStore,
                new AnonymousIdentity(),
                EndpointUrl,
                loggerFactory: this.loggerFactory);

            await channel.OpenAsync();
            Console.WriteLine($"Opened session with endpoint '{channel.RemoteEndpoint.EndpointUrl}'.");
            Console.WriteLine($"SecurityPolicy: '{channel.RemoteEndpoint.SecurityPolicyUri}'.");
            Console.WriteLine($"SecurityMode: '{channel.RemoteEndpoint.SecurityMode}'.");
            Console.WriteLine($"Activated session '{channel.SessionId}'.");

            var historyReadRequest = new HistoryReadRequest
            {
                HistoryReadDetails = new ReadRawModifiedDetails
                {
                    StartTime = DateTime.UtcNow - TimeSpan.FromMinutes(10),
                    EndTime = DateTime.UtcNow,
                    ReturnBounds = true,
                    IsReadModified = false
                },
                NodesToRead = new[]
                {
                    new HistoryReadValueId
                    {
                        NodeId = NodeId.Parse("ns=2;s=Demo.History.DoubleWithHistory")
                    }
                },
            };
            var historyReadResponse = await channel.HistoryReadAsync(historyReadRequest);
            var result = historyReadResponse.Results[0];
            Assert.IsTrue(StatusCode.IsGood(result.StatusCode));
            Console.WriteLine($"HistoryRead response status code: {result.StatusCode}, HistoryData count: {((HistoryData)result.HistoryData).DataValues.Length}.");

            var historyReadRequest2 = new HistoryReadRequest
            {
                HistoryReadDetails = new ReadEventDetails
                {
                    StartTime = DateTime.UtcNow - TimeSpan.FromMinutes(10),
                    EndTime = DateTime.UtcNow,
                    Filter = new EventFilter // Use EventHelper to select all the fields of AlarmCondition.
                    {
                        SelectClauses = EventHelper.GetSelectClauses<AlarmCondition>()
                    }
                },
                NodesToRead = new[]
                {
                    new HistoryReadValueId
                    {
                        NodeId = NodeId.Parse("ns=2;s=Demo.History.DoubleWithHistory")
                    }
                },
            };
            var historyReadResponse2 = await channel.HistoryReadAsync(historyReadRequest2);
            var result2 = historyReadResponse2.Results[0];
            Assert.IsTrue(StatusCode.IsGood(result2.StatusCode));
            Console.WriteLine($"HistoryRead response status code: {result2.StatusCode}, HistoryEvent count: {((HistoryEvent)result2.HistoryData).Events.Length}.");

            // Use EventHelper to create AlarmConditions from the HistoryEventFieldList
            var alarms = ((HistoryEvent)result2.HistoryData).Events.Select(e => EventHelper.Deserialize<AlarmCondition>(e.EventFields));

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
            // Read 'appSettings.json' for endpoint configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appSettings.json", true)
                .Build();

            var app = new UaApplicationBuilder()
                .SetApplicationUri($"urn:{Dns.GetHostName()}:Workstation.UaClient.UnitTests")
                .SetDirectoryStore(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Workstation.UaClient.UnitTests",
                    "pki"))
                .SetIdentity(new UserNameIdentity("root", "secret"))
                .AddMappedEndpoints(config)
                .SetLoggerFactory(this.loggerFactory)
                .ConfigureOptions(o => o.SessionTimeout = 30000)
                .Build();

            app.Run();

            UaTcpSecureChannel.RegisterEncodables(Assembly.GetExecutingAssembly());

            var sub = new MySubscription();
            var d = new PropertyChangedEventHandler((s, e) => { });
            sub.PropertyChanged += d;

            Console.WriteLine($"Created subscription.");

            await Task.Delay(5000);

            sub.PropertyChanged -= d;
            await Task.Delay(5000);

            app.Dispose();

            Assert.IsTrue(sub.CurrentTime != DateTime.MinValue, "CurrentTime");
            Assert.IsTrue(sub.CurrentTimeAsDataValue != null, "CurrentTimeAsDataValue");
            Assert.IsTrue(sub.CurrentTimeQueue.Count > 0, "CurrentTimeQueue");
        }

        [Subscription(endpointUrl: "opc.tcp://localhost:48010", publishingInterval: 500, keepAliveCount: 20)]
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

        [TestMethod]
        public async Task StackTest()
        {
            var channel = new UaTcpSessionChannel(
                this.localDescription,
                this.certificateStore,
                new AnonymousIdentity(),
                EndpointUrl);

            await channel.OpenAsync();
            Console.WriteLine($"Opened session with endpoint '{channel.RemoteEndpoint.EndpointUrl}'.");
            Console.WriteLine($"SecurityPolicy: '{channel.RemoteEndpoint.SecurityPolicyUri}'.");
            Console.WriteLine($"SecurityMode: '{channel.RemoteEndpoint.SecurityMode}'.");
            Console.WriteLine($"Activated session '{channel.SessionId}'.");

            var readRequest = new ReadRequest
            {
                NodesToRead = new[]
                {
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Scalar.Boolean") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Scalar.SByte") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Scalar.Int16") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Scalar.Int32") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Scalar.Int64") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Scalar.Byte") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Scalar.UInt16") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Scalar.UInt32") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Scalar.UInt64") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Scalar.Float") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Scalar.Double") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Scalar.String") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Scalar.DateTime") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Scalar.Guid") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Scalar.ByteString") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Scalar.XmlElement") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Scalar.LocalizedText") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Scalar.QualifiedName") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Arrays.Boolean") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Arrays.SByte") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Arrays.Int16") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Arrays.Int32") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Arrays.Int64") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Arrays.Byte") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Arrays.UInt16") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Arrays.UInt32") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Arrays.UInt64") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Arrays.Float") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Arrays.Double") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Arrays.String") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Arrays.DateTime") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Arrays.Guid") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Arrays.ByteString") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Arrays.XmlElement") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Arrays.LocalizedText") },
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Arrays.QualifiedName") },
                },
            };

            readRequest = new ReadRequest
            {
                NodesToRead = new[]
               {
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("i=11494") },
                },
            };
            var sw = new Stopwatch();
            sw.Restart();
            for (int i = 0; i < 1; i++)
            {
                var readResponse = await channel.ReadAsync(readRequest);
                foreach (var result in readResponse.Results)
                {
                    Assert.IsTrue(StatusCode.IsGood(result.StatusCode));
                    var obj = result.GetValue();
                }
            }

            sw.Stop();
            Console.WriteLine($"{sw.ElapsedMilliseconds} ms");

            Console.WriteLine($"Closing session '{channel.SessionId}'.");
            await channel.CloseAsync();
        }

        /// <summary>
        /// Tests result of transfer subscription from channel1 to channel2.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task TransferSubscription()
        {
            var channel1 = new UaTcpSessionChannel(
                this.localDescription,
                this.certificateStore,
                new UserNameIdentity("root", "secret"),
                EndpointUrl);

            await channel1.OpenAsync();
            Console.WriteLine($"Opened session with endpoint '{channel1.RemoteEndpoint.EndpointUrl}'.");
            Console.WriteLine($"SecurityPolicy: '{channel1.RemoteEndpoint.SecurityPolicyUri}'.");
            Console.WriteLine($"SecurityMode: '{channel1.RemoteEndpoint.SecurityMode}'.");
            Console.WriteLine($"Activated session '{channel1.SessionId}'.");

            // create the keep alive subscription.
            var subscriptionRequest = new CreateSubscriptionRequest
            {
                RequestedPublishingInterval = 1000f,
                RequestedMaxKeepAliveCount = 30,
                RequestedLifetimeCount = 30 * 3,
                PublishingEnabled = true,
            };
            var subscriptionResponse = await channel1.CreateSubscriptionAsync(subscriptionRequest).ConfigureAwait(false);
            var id = subscriptionResponse.SubscriptionId;

            var token = channel1.Where(pr => pr.SubscriptionId == id).Subscribe(pr =>
            {
                // loop thru all the data change notifications
                var dcns = pr.NotificationMessage.NotificationData.OfType<DataChangeNotification>();
                foreach (var dcn in dcns)
                {
                    foreach (var min in dcn.MonitoredItems)
                    {
                        Console.WriteLine($"channel: 1; sub: {pr.SubscriptionId}; handle: {min.ClientHandle}; value: {min.Value}");
                    }
                }
            });

            var itemsRequest = new CreateMonitoredItemsRequest
            {
                SubscriptionId = id,
                ItemsToCreate = new MonitoredItemCreateRequest[]
                {
                    new MonitoredItemCreateRequest { ItemToMonitor = new ReadValueId { NodeId = NodeId.Parse("i=2258"), AttributeId = AttributeIds.Value }, MonitoringMode = MonitoringMode.Reporting, RequestedParameters = new MonitoringParameters { ClientHandle = 12345, SamplingInterval = -1, QueueSize = 0, DiscardOldest = true } }
                },
            };
            var itemsResponse = await channel1.CreateMonitoredItemsAsync(itemsRequest);

            await Task.Delay(3000);

            var channel2 = new UaTcpSessionChannel(
                this.localDescription,
                this.certificateStore,
                new UserNameIdentity("root", "secret"),
                EndpointUrl);

            await channel2.OpenAsync();
            var token2 = channel2.Where(pr => pr.SubscriptionId == id).Subscribe(pr =>
            {
                // loop thru all the data change notifications
                var dcns = pr.NotificationMessage.NotificationData.OfType<DataChangeNotification>();
                foreach (var dcn in dcns)
                {
                    foreach (var min in dcn.MonitoredItems)
                    {
                        Console.WriteLine($"channel: 2; sub: {pr.SubscriptionId}; handle: {min.ClientHandle}; value: {min.Value}");
                    }
                }
            });

            var transferRequest = new TransferSubscriptionsRequest
            {
                SubscriptionIds = new[] { id },
                SendInitialValues = true
            };
            var transferResult = await channel2.TransferSubscriptionsAsync(transferRequest);

            Assert.IsTrue(StatusCode.IsGood(transferResult.Results[0].StatusCode));

            await Task.Delay(3000);

            Console.WriteLine($"Closing session '{channel1.SessionId}'.");
            await channel1.CloseAsync();

            Console.WriteLine($"Closing session '{channel2.SessionId}'.");
            await channel2.CloseAsync();
        }

        [TestMethod]
        public async Task BrowseRoot()
        {
            var channel = new UaTcpSessionChannel(
                this.localDescription,
                this.certificateStore,
                new AnonymousIdentity(),
                EndpointUrl,
                SecurityPolicyUris.None,
                loggerFactory: this.loggerFactory);

            await channel.OpenAsync();

            var rds = new List<ReferenceDescription>();
            var browseRequest = new BrowseRequest { NodesToBrowse = new[] { new BrowseDescription { NodeId = ExpandedNodeId.ToNodeId(ExpandedNodeId.Parse(ObjectIds.RootFolder), channel.NamespaceUris), ReferenceTypeId = NodeId.Parse(ReferenceTypeIds.HierarchicalReferences), ResultMask = (uint)BrowseResultMask.TargetInfo, NodeClassMask = (uint)NodeClass.Unspecified, BrowseDirection = BrowseDirection.Forward, IncludeSubtypes = true } }, RequestedMaxReferencesPerNode = 1000 };
            var browseResponse = await channel.BrowseAsync(browseRequest).ConfigureAwait(false);
            rds.AddRange(browseResponse.Results.Where(result => result.References != null).SelectMany(result => result.References));
            var continuationPoints = browseResponse.Results.Select(br => br.ContinuationPoint).Where(cp => cp != null).ToArray();
            while (continuationPoints.Length > 0)
            {
                var browseNextRequest = new BrowseNextRequest { ContinuationPoints = continuationPoints, ReleaseContinuationPoints = false };
                var browseNextResponse = await channel.BrowseNextAsync(browseNextRequest);
                rds.AddRange(browseNextResponse.Results.Where(result => result.References != null).SelectMany(result => result.References));
                continuationPoints = browseNextResponse.Results.Select(br => br.ContinuationPoint).Where(cp => cp != null).ToArray();
            }

            Assert.IsTrue(rds.Count == 3);

            Console.WriteLine($"Closing session '{channel.SessionId}'.");
            await channel.CloseAsync();
        }

    }
}