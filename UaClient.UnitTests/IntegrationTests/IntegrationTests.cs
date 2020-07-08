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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;
using Workstation.Collections;
using Workstation.ServiceModel.Ua;
using Workstation.ServiceModel.Ua.Channels;
using Workstation.UaClient.TestServer;
using Xunit;

namespace Workstation.UaClient.IntegrationTests
{
    public class IntegrationTests
    {
        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger<IntegrationTests> logger;
        private static readonly ApplicationDescription localDescription;
        private static readonly ICertificateStore certificateStore;
        private static readonly ITestServer testServer;
        private static readonly string pkiPath;

        public string EndpointUrl => testServer.EndpointUrl;
        
        public static IEnumerable<object[]> AllTestEndpointData => testServer.TestEndpoints
            .Select(t => new object[] { t });

        public static IEnumerable<object[]> UserIdentities => testServer.UserIdentities
            .Select(t => new object[] { t });

        static IntegrationTests()
        {

            localDescription = new ApplicationDescription
            {
                ApplicationName = "Workstation.UaClient.UnitTests",
                ApplicationUri = $"urn:{Dns.GetHostName()}:Workstation.UaClient.UnitTests",
                ApplicationType = ApplicationType.Client
            };

            pkiPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Workstation.UaClient.UnitTests",
                    "pki");

            certificateStore = new DirectoryStore(pkiPath);
            testServer = new UaTestServer(pkiPath);
        }
        
        public IntegrationTests()
        {
            this.loggerFactory = LoggerFactory.Create(builder => builder.AddDebug());
            this.logger = this.loggerFactory?.CreateLogger<IntegrationTests>();
        }

        /// <summary>
        /// Tests endpoint with no security and with no Certificate.
        /// Only run this test with a running opc test server.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task DiscoverAllEndpoints()
        {
            // discover available endpoints of server.
            var request = new GetEndpointsRequest
            {
                EndpointUrl = EndpointUrl,
                ProfileUris = new[] { TransportProfileUris.UaTcpTransport }
            };
            logger.LogInformation($"Discovering endpoints of '{request.EndpointUrl}'.");
            var response = await UaTcpDiscoveryService.GetEndpointsAsync(request, this.loggerFactory);

            var expected = from t in testServer.TestEndpoints
                           select new
                            {
                                t.SecurityPolicyUri,
                                t.SecurityMode,
                                TokenType = t.UserIdentity switch
                                {
                                    AnonymousIdentity   _ => UserTokenType.Anonymous,
                                    UserNameIdentity    _ => UserTokenType.UserName,
                                    X509Identity        _ => UserTokenType.Certificate,
                                                        _ => UserTokenType.Certificate
                                }
                            };

            var result = from t in response.Endpoints
                         from u in t.UserIdentityTokens
                         select new
                         {
                             t.SecurityPolicyUri,
                             t.SecurityMode,
                             u.TokenType
                         };

            result
                .Should().BeEquivalentTo(expected);
        }

        /// <summary>
        /// Tests all combinations of endpoint security and user identity types supported by the server.
        /// Only run this test with a running opc test server.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [MemberData(nameof(AllTestEndpointData))]
        [Theory]
        public async Task ConnnectToAllEndpoints(TestEndpoint endpoint)
        {
            var channel = new UaTcpSessionChannel(
                localDescription,
                certificateStore,
                endpoint.UserIdentity,
                endpoint.EndpointDescription,
                loggerFactory: this.loggerFactory);

            await channel.OpenAsync();

            channel.SessionId
                .Should().NotBeNull();

            await channel.CloseAsync();
        }

        /// <summary>
        /// Tests browsing the Objects folder.
        /// Only run this test with a running opc test server.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [MemberData(nameof(AllTestEndpointData))]
        [Theory]
        public async Task BrowseObjects(TestEndpoint endpoint)
        {
            var channel = new UaTcpSessionChannel(
                localDescription,
                certificateStore,
                endpoint.UserIdentity,
                endpoint.EndpointDescription,
                loggerFactory: this.loggerFactory);

            await channel.OpenAsync();

            var rds = new List<ReferenceDescription>();
            var browseRequest = new BrowseRequest { NodesToBrowse = new[] { new BrowseDescription { NodeId = ExpandedNodeId.ToNodeId(ExpandedNodeId.Parse(ObjectIds.ObjectsFolder), channel.NamespaceUris), ReferenceTypeId = NodeId.Parse(ReferenceTypeIds.HierarchicalReferences), ResultMask = (uint)BrowseResultMask.TargetInfo, NodeClassMask = (uint)NodeClass.Unspecified, BrowseDirection = BrowseDirection.Forward, IncludeSubtypes = true } }, RequestedMaxReferencesPerNode = 1000 };
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

            rds
                .Should().NotBeEmpty();

            rds.Select(rd => rd.DisplayName.Text)
                .Should().Contain("Server");

            await channel.CloseAsync();
        }

        /// <summary>
        /// Tests read server status.
        /// Only run this test with a running opc test server.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [MemberData(nameof(AllTestEndpointData))]
        [Theory]
        public async Task Read(TestEndpoint endpoint)
        {
            var channel = new UaTcpSessionChannel(
                localDescription,
                certificateStore,
                endpoint.UserIdentity,
                endpoint.EndpointDescription,
                loggerFactory: this.loggerFactory);

            await channel.OpenAsync();

            var readRequest = new ReadRequest { NodesToRead = new[] { new ReadValueId { NodeId = NodeId.Parse(VariableIds.Server_ServerStatus), AttributeId = AttributeIds.Value } } };
            var readResult = await channel.ReadAsync(readRequest);
            var serverStatus = readResult.Results[0].GetValueOrDefault<ServerStatusDataType>();

            serverStatus.CurrentTime
                .Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

            await channel.CloseAsync();
        }

        /// <summary>
        /// Tests polling the current time.
        /// Only run this test with a running opc test server.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [MemberData(nameof(AllTestEndpointData))]
        [Theory]
        public async Task Polling(TestEndpoint endpoint)
        {
            var channel = new UaTcpSessionChannel(
                localDescription,
                certificateStore,
                endpoint.UserIdentity,
                endpoint.EndpointDescription,
                loggerFactory: this.loggerFactory);

            await channel.OpenAsync();

            var readRequest = new ReadRequest { NodesToRead = new[] { new ReadValueId { NodeId = NodeId.Parse(VariableIds.Server_ServerStatus_CurrentTime), AttributeId = AttributeIds.Value } } };
            for (int i = 0; i < 10; i++)
            {
                var readResult = await channel.ReadAsync(readRequest);
                var currentTime = readResult.Results[0].GetValueOrDefault<DateTime>();
                currentTime
                    .Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

                await Task.Delay(100);
            }

            await channel.CloseAsync();
        }

        /// <summary>
        /// Tests creating a subscription and monitoring current time.
        /// Only run this test with a running opc test server.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [MemberData(nameof(AllTestEndpointData))]
        [Theory]
        public async Task TestSubscription(TestEndpoint endpoint)
        {
            var channel = new UaTcpSessionChannel(
                localDescription,
                certificateStore,
                endpoint.UserIdentity,
                endpoint.EndpointDescription,
                loggerFactory: this.loggerFactory);

            await channel.OpenAsync();

            var req = new CreateSubscriptionRequest
            {
                RequestedPublishingInterval = 1000.0,
                RequestedMaxKeepAliveCount = 30,
                RequestedLifetimeCount = 30 * 3,
                PublishingEnabled = true,
            };
            var res = await channel.CreateSubscriptionAsync(req);
            var id = res.SubscriptionId;

            id
                .Should().NotBe(0);

            var req2 = new CreateMonitoredItemsRequest
            {
                SubscriptionId = id,
                TimestampsToReturn = TimestampsToReturn.Both,
                ItemsToCreate = new MonitoredItemCreateRequest[]
                {
                    new MonitoredItemCreateRequest
                    {
                        ItemToMonitor= new ReadValueId{ AttributeId= AttributeIds.Value, NodeId= NodeId.Parse(VariableIds.Server_ServerStatus_CurrentTime)},
                        MonitoringMode= MonitoringMode.Reporting,
                        RequestedParameters= new MonitoringParameters{ ClientHandle= 42, QueueSize= 2, DiscardOldest= true, SamplingInterval= 500.0},
                    },
                },
            };
            var res2 = await channel.CreateMonitoredItemsAsync(req2);

            var numOfResponses = 0;

            void onPublish(PublishResponse pr)
            {
                numOfResponses++;

                // we have only one item, namely the current time
                var dcns = pr.NotificationMessage.NotificationData.OfType<DataChangeNotification>();
                var item = dcns.Single().MonitoredItems.Single();

                item.Value.Value
                    .Should().BeOfType<DateTime>()
                    .Which
                    .Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            }

            var exception = default(Exception);
            void onPublishError(Exception ex)
            {
                exception = ex;
            }

            var token = channel
                .Where(pr => pr.SubscriptionId == id)
                .Subscribe(onPublish, onPublishError);

            await Task.Delay(TimeSpan.FromMilliseconds(5000));

            await channel.CloseAsync();

            if (exception != null)
                throw exception;

            numOfResponses
                .Should().BeGreaterThan(0);
        }

        /// <summary>
        /// Tests calling a method of the UACPPServer.
        /// Only run this test with a running opc test server.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
#if false
        [MemberData(nameof(AllTestEndpointData))]
        [Theory]
        public async Task VectorAdd(TestEndpoint endpoint)
        {
            var channel = new UaTcpSessionChannel(
                localDescription,
                certificateStore,
                endpoint.UserIdentity,
                endpoint.EndpointDescription,
                loggerFactory: this.loggerFactory,
                additionalTypes: new[] { typeof(Vector) });

            await channel.OpenAsync();

            var v1 = new Vector { X = 1.0, Y = 2.0, Z = 3.0 };
            var v2 = new Vector { X = 1.0, Y = 2.0, Z = 3.0 };
            var request = new CallRequest
            {
                MethodsToCall = new[] {
                    new CallMethodRequest
                    {
                        ObjectId = NodeId.Parse("ns=2;s=Demo.Method"),
                        MethodId = NodeId.Parse("ns=2;s=Demo.Method.VectorAdd"),
                        InputArguments = new [] { new ExtensionObject(v1), new ExtensionObject(v2) }.ToVariantArray()
                    }
                }
            };
            var response = await channel.CallAsync(request);
            var result = response.Results[0].OutputArguments[0].GetValueOrDefault<Vector>();

            await channel.CloseAsync();

            result.Z
                .Should().Be(6.0);
        }
#endif

        [DataTypeId("nsu=http://www.unifiedautomation.com/DemoServer/;i=3002")]
        [BinaryEncodingId("nsu=http://www.unifiedautomation.com/DemoServer/;i=5054")]
        public class Vector : Structure
        {
            public double X { get; set; }

            public double Y { get; set; }

            public double Z { get; set; }

            public override void Encode(IEncoder encoder)
            {
                encoder.WriteDouble("X", this.X);
                encoder.WriteDouble("Y", this.Y);
                encoder.WriteDouble("Z", this.Z);
            }

            public override void Decode(IDecoder decoder)
            {
                this.X = decoder.ReadDouble("X");
                this.Y = decoder.ReadDouble("Y");
                this.Z = decoder.ReadDouble("Z");
            }

            public override string ToString() => $"{{ X={this.X}; Y={this.Y}; Z={this.Z}; }}";
        }

        /// <summary>
        /// Tests reading the historical data of the UACPPServer.
        /// Only run this test with a running opc test server.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
#if false
        [Fact]
        public async Task ReadHistorical()
        {
            var channel = new UaTcpSessionChannel(
                localDescription,
                certificateStore,
                new AnonymousIdentity(),
                EndpointUrl,
                loggerFactory: this.loggerFactory);

            await channel.OpenAsync();
            logger.LogInformation($"Opened session with endpoint '{channel.RemoteEndpoint.EndpointUrl}'.");
            logger.LogInformation($"SecurityPolicy: '{channel.RemoteEndpoint.SecurityPolicyUri}'.");
            logger.LogInformation($"SecurityMode: '{channel.RemoteEndpoint.SecurityMode}'.");
            logger.LogInformation($"Activated session '{channel.SessionId}'.");

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
            StatusCode.IsGood(result.StatusCode)
                .Should().BeTrue();
            logger.LogInformation($"HistoryRead response status code: {result.StatusCode}, HistoryData count: {((HistoryData)result.HistoryData).DataValues.Length}.");

            if (false) // UaCPPserver does not appear to store event history.  
            {
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
                StatusCode.IsGood(result2.StatusCode)
                    .Should().BeTrue();
                logger.LogInformation($"HistoryRead response status code: {result2.StatusCode}, HistoryEvent count: {((HistoryEvent)result2.HistoryData).Events.Length}.");

                // Use EventHelper to create AlarmConditions from the HistoryEventFieldList
                var alarms = ((HistoryEvent)result2.HistoryData).Events.Select(e => EventHelper.Deserialize<AlarmCondition>(e.EventFields));
            }
            logger.LogInformation($"Closing session '{channel.SessionId}'.");
            await channel.CloseAsync();
        }
#endif
        /// <summary>
        /// Tests connecting to endpoint and creating a subscription based view model.
        /// Only run this test with a running opc test server.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [MemberData(nameof(UserIdentities))]
        [Theory]
        public async Task TestViewModel(IUserIdentity identity)
        {
            // Read 'appSettings.json' for endpoint configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appSettings.json", true)
                .Build();

            var app = new UaApplicationBuilder()
                .SetApplicationUri($"urn:{Dns.GetHostName()}:Workstation.UaClient.UnitTests")
                .SetDirectoryStore(pkiPath)
                .SetIdentity(identity)
                .AddMappedEndpoints(config)
                .SetLoggerFactory(this.loggerFactory)
                .ConfigureOptions(o => o.SessionTimeout = 30000)
                .Build();

            app.Run();

            var vm = new MyViewModel();

            void onPropertyChanged(object s, PropertyChangedEventArgs e) { }

            vm.PropertyChanged += onPropertyChanged; // simulate xaml binding

            logger.LogInformation($"Created subscription.");

            await Task.Delay(TimeSpan.FromMilliseconds(5000));

            vm.PropertyChanged -= onPropertyChanged; // simulate un-binding
            app.Dispose();

            vm.CurrentTime
                .Should().NotBe(DateTime.MinValue);
            vm.CurrentTimeAsDataValue
                .Should().NotBeNull();
            vm.CurrentTimeQueue
                .Should().NotBeEmpty();
        }

        [Subscription(endpointUrl: "opc.tcp://localhost:62541/UaTestServer", publishingInterval: 500, keepAliveCount: 20)]
        private class MyViewModel : SubscriptionBase
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

        [Fact]
        public async Task StackTest()
        {
            var channel = new UaTcpSessionChannel(
                localDescription,
                certificateStore,
                new AnonymousIdentity(),
                EndpointUrl,
                loggerFactory: this.loggerFactory);

            await channel.OpenAsync();
            logger.LogInformation($"Opened session with endpoint '{channel.RemoteEndpoint.EndpointUrl}'.");
            logger.LogInformation($"SecurityPolicy: '{channel.RemoteEndpoint.SecurityPolicyUri}'.");
            logger.LogInformation($"SecurityMode: '{channel.RemoteEndpoint.SecurityMode}'.");
            logger.LogInformation($"Activated session '{channel.SessionId}'.");

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
                    StatusCode.IsGood(result.StatusCode)
                        .Should().BeTrue();
                    var obj = result.GetValue();
                }
            }

            sw.Stop();
            logger.LogInformation($"{sw.ElapsedMilliseconds} ms");

            logger.LogInformation($"Closing session '{channel.SessionId}'.");
            await channel.CloseAsync();
        }

        /// <summary>
        /// Tests result of transfer subscription from channel1 to channel2.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
#if false
        [MemberData(nameof(UserIdentities))]
        [Theory]
        public async Task TransferSubscription(IUserIdentity identity)
        {
            var channel1 = new UaTcpSessionChannel(
                localDescription,
                certificateStore,
                identity,
                EndpointUrl,
                loggerFactory: this.loggerFactory);

            await channel1.OpenAsync();
            logger.LogInformation($"Opened session with endpoint '{channel1.RemoteEndpoint.EndpointUrl}'.");
            logger.LogInformation($"SecurityPolicy: '{channel1.RemoteEndpoint.SecurityPolicyUri}'.");
            logger.LogInformation($"SecurityMode: '{channel1.RemoteEndpoint.SecurityMode}'.");
            logger.LogInformation($"Activated session '{channel1.SessionId}'.");

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

            void onPublish(PublishResponse pr)
            {
                // loop thru all the data change notifications and log them.
                var dcns = pr.NotificationMessage.NotificationData.OfType<DataChangeNotification>();
                foreach (var dcn in dcns)
                {
                    foreach (var min in dcn.MonitoredItems)
                    {
                        logger.LogInformation($"sub: {pr.SubscriptionId}; handle: {min.ClientHandle}; value: {min.Value}");
                    }
                }
            }

            void onPublishError(Exception ex)
            {
                logger.LogInformation("Exception in publish response handler: {0}", ex.GetBaseException().Message);
            }

            var token = channel1
                .Where(pr => pr.SubscriptionId == id)
                .Subscribe(onPublish, onPublishError);

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
                localDescription,
                certificateStore,
                identity,
                EndpointUrl);

            await channel2.OpenAsync();

            var token2 = channel2
                .Where(pr => pr.SubscriptionId == id)
                .Subscribe(onPublish, onPublishError);

            var transferRequest = new TransferSubscriptionsRequest
            {
                SubscriptionIds = new[] { id },
                SendInitialValues = true
            };
            var transferResult = await channel2.TransferSubscriptionsAsync(transferRequest);

            StatusCode.IsGood(transferResult.Results[0].StatusCode)
                .Should().BeTrue();
            logger.LogInformation($"Transfered subscriptions to new client.");

            await Task.Delay(3000);

            logger.LogInformation($"Closing session '{channel1.SessionId}'.");
            await channel1.CloseAsync();

            logger.LogInformation($"Closing session '{channel2.SessionId}'.");
            await channel2.CloseAsync();
        }
#endif

    }
}