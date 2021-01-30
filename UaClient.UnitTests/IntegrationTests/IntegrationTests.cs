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
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;
using Workstation.Collections;
using Workstation.ServiceModel.Ua;
using Workstation.ServiceModel.Ua.Channels;
using Xunit;
using Xunit.Abstractions;

namespace Workstation.UaClient.IntegrationTests
{
    public class IntegrationTests
    {
        // private const string EndpointUrl = "opc.tcp://localhost:16664"; // open62541
        // private const string EndpointUrl = "opc.tcp://bculz-PC:53530/OPCUA/SimulationServer"; // the endpoint of the Prosys UA Simulation Server
        // private const string EndpointUrl = "opc.tcp://localhost:51210/UA/SampleServer"; // the endpoint of the OPCF SampleServer
        private const string EndpointUrl = "opc.tcp://localhost:48010"; // the endpoint of the UaCPPServer.
        // private const string EndpointUrl = "opc.tcp://localhost:26543"; // the endpoint of the Workstation.RobotServer.
        // private const string EndpointUrl = "opc.tcp://192.168.0.11:4840"; // the endpoint of the Siemens 1500 PLC.

        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger<IntegrationTests> logger;
        private readonly ApplicationDescription localDescription;
        private readonly ICertificateStore certificateStore;
        private readonly X509Identity x509Identity;
        private readonly ITestOutputHelper output;

        public IntegrationTests(ITestOutputHelper output)
        {
            this.output = output;
            loggerFactory = LoggerFactory.Create(builder => builder.AddDebug());
            logger = loggerFactory?.CreateLogger<IntegrationTests>();

            localDescription = new ApplicationDescription
            {
                ApplicationName = "Workstation.UaClient.UnitTests",
                ApplicationUri = $"urn:{Dns.GetHostName()}:Workstation.UaClient.UnitTests",
                ApplicationType = ApplicationType.Client
            };

            var pkiPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Workstation.UaClient.UnitTests",
                    "pki");
            certificateStore = new DirectoryStore(pkiPath);

            // read x509Identity
            var userCert = default(X509Certificate);
            var userKey = default(RsaKeyParameters);

            var certParser = new X509CertificateParser();
            var userCertInfo = new FileInfo(Path.Combine(pkiPath, "user", "certs", "ctt_usrT.der"));
            if (userCertInfo.Exists)
            {
                using (var crtStream = userCertInfo.OpenRead())
                {
                    var c = certParser.ReadCertificate(crtStream);
                    if (c != null)
                    {
                        userCert = c;
                    }
                }
            }
            var userKeyInfo = new FileInfo(Path.Combine(pkiPath, "user", "private", "ctt_usrT.pem"));
            if (userKeyInfo.Exists)
            {
                using (var keyStream = new StreamReader(userKeyInfo.OpenRead()))
                {
                    var keyReader = new PemReader(keyStream);
                    var keyPair = keyReader.ReadObject() as AsymmetricCipherKeyPair;
                    if (keyPair != null)
                    {
                        userKey = keyPair.Private as RsaKeyParameters;
                    }
                }
            }
            if (userCert != null && userKey != null)
            {
                x509Identity = new X509Identity(userCert, userKey);
            }
        }

        /// <summary>
        /// Tests endpoint with no security and with no Certificate.
        /// Only run this test with a running opc test server.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ConnnectToEndpointsWithNoSecurityAndWithNoCertificate()
        {
            // discover available endpoints of server.
            var getEndpointsRequest = new GetEndpointsRequest
            {
                EndpointUrl = EndpointUrl,
                ProfileUris = new[] { TransportProfileUris.UaTcpTransport }
            };
            logger.LogInformation($"Discovering endpoints of '{getEndpointsRequest.EndpointUrl}'.");
            var getEndpointsResponse = await UaTcpDiscoveryService.GetEndpointsAsync(getEndpointsRequest, loggerFactory);

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
                        localDescription,
                        null,
                        selectedUserIdentity,
                        selectedEndpoint,
                        loggerFactory: loggerFactory);

                    await channel.OpenAsync();
                    logger.LogInformation($"Opened session with endpoint '{channel.RemoteEndpoint.EndpointUrl}'.");
                    logger.LogInformation($"SecurityPolicy: '{channel.RemoteEndpoint.SecurityPolicyUri}'.");
                    logger.LogInformation($"SecurityMode: '{channel.RemoteEndpoint.SecurityMode}'.");
                    logger.LogInformation($"UserIdentityToken: '{channel.UserIdentity}'.");

                    logger.LogInformation($"Closing session '{channel.SessionId}'.");
                    await channel.CloseAsync();
                }
            }
        }

        /// <summary>
        /// Tests all combinations of endpoint security and user identity types supported by the server.
        /// Only run this test with a running opc test server.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ConnnectToAllEndpoints()
        {
            // discover available endpoints of server.
            var getEndpointsRequest = new GetEndpointsRequest
            {
                EndpointUrl = EndpointUrl,
                ProfileUris = new[] { TransportProfileUris.UaTcpTransport }
            };
            logger.LogInformation($"Discovering endpoints of '{getEndpointsRequest.EndpointUrl}'.");
            var getEndpointsResponse = await UaTcpDiscoveryService.GetEndpointsAsync(getEndpointsRequest);

            // for each endpoint and user identity type, try creating a session and reading a few nodes.
            foreach (var selectedEndpoint in getEndpointsResponse.Endpoints
                .OrderBy(e => e.SecurityLevel))
            {
                foreach (var selectedTokenPolicy in selectedEndpoint.UserIdentityTokens)
                {
                    IUserIdentity selectedUserIdentity;
                    switch (selectedTokenPolicy.TokenType)
                    {
                        case UserTokenType.Certificate:
                            selectedUserIdentity = x509Identity;
                            break;

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
                        localDescription,
                        certificateStore,
                        selectedUserIdentity,
                        selectedEndpoint,
                        loggerFactory: loggerFactory);

                    await channel.OpenAsync();
                    logger.LogInformation($"Opened session with endpoint '{channel.RemoteEndpoint.EndpointUrl}'.");
                    logger.LogInformation($"SecurityPolicy: '{channel.RemoteEndpoint.SecurityPolicyUri}'.");
                    logger.LogInformation($"SecurityMode: '{channel.RemoteEndpoint.SecurityMode}'.");
                    logger.LogInformation($"UserIdentityToken: '{channel.UserIdentity}'.");

                    logger.LogInformation($"Closing session '{channel.SessionId}'.");
                    await channel.CloseAsync();
                }
            }
        }

        /// <summary>
        /// Tests browsing the Objects folder.
        /// Only run this test with a running opc test server.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task BrowseObjects()
        {
            var channel = new UaTcpSessionChannel(
                localDescription,
                certificateStore,
                new AnonymousIdentity(),
                EndpointUrl,
                SecurityPolicyUris.None,
                loggerFactory: loggerFactory);

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

            logger.LogInformation("+ Objects, 0:Objects, Object");
            foreach (var rd in rds)
            {
                logger.LogInformation("   + {0}, {1}, {2}", rd.DisplayName, rd.BrowseName, rd.NodeClass);
            }

            logger.LogInformation($"Closing session '{channel.SessionId}'.");
            await channel.CloseAsync();
        }

        /// <summary>
        /// Tests read server status.
        /// Only run this test with a running opc test server.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task Read()
        {
            var channel = new UaTcpSessionChannel(
                localDescription,
                certificateStore,
                new AnonymousIdentity(),
                EndpointUrl,
                loggerFactory: loggerFactory);

            await channel.OpenAsync();
            logger.LogInformation($"Opened session with endpoint '{channel.RemoteEndpoint.EndpointUrl}'.");
            logger.LogInformation($"SecurityPolicy: '{channel.RemoteEndpoint.SecurityPolicyUri}'.");
            logger.LogInformation($"SecurityMode: '{channel.RemoteEndpoint.SecurityMode}'.");
            logger.LogInformation($"Activated session '{channel.SessionId}'.");

            var readRequest = new ReadRequest { NodesToRead = new[] { new ReadValueId { NodeId = NodeId.Parse(VariableIds.Server_ServerStatus), AttributeId = AttributeIds.Value } } };
            var readResult = await channel.ReadAsync(readRequest);
            var serverStatus = readResult.Results[0].GetValueOrDefault<ServerStatusDataType>();

            logger.LogInformation("Server status:");
            logger.LogInformation("  ProductName: {0}", serverStatus.BuildInfo.ProductName);
            logger.LogInformation("  SoftwareVersion: {0}", serverStatus.BuildInfo.SoftwareVersion);
            logger.LogInformation("  ManufacturerName: {0}", serverStatus.BuildInfo.ManufacturerName);
            logger.LogInformation("  State: {0}", serverStatus.State);
            logger.LogInformation("  CurrentTime: {0}", serverStatus.CurrentTime);

            logger.LogInformation($"Closing session '{channel.SessionId}'.");
            await channel.CloseAsync();
        }

        /// <summary>
        /// Tests polling the current time.
        /// Only run this test with a running opc test server.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task Polling()
        {
            var channel = new UaTcpSessionChannel(
                localDescription,
                certificateStore,
                new AnonymousIdentity(),
                EndpointUrl,
                loggerFactory: loggerFactory);

            await channel.OpenAsync();
            logger.LogInformation($"Opened session with endpoint '{channel.RemoteEndpoint.EndpointUrl}'.");
            logger.LogInformation($"SecurityPolicy: '{channel.RemoteEndpoint.SecurityPolicyUri}'.");
            logger.LogInformation($"SecurityMode: '{channel.RemoteEndpoint.SecurityMode}'.");
            logger.LogInformation($"Activated session '{channel.SessionId}'.");

            var readRequest = new ReadRequest { NodesToRead = new[] { new ReadValueId { NodeId = NodeId.Parse(VariableIds.Server_ServerStatus_CurrentTime), AttributeId = AttributeIds.Value } } };
            for (int i = 0; i < 10; i++)
            {
                var readResult = await channel.ReadAsync(readRequest);
                logger.LogInformation("Read {0}", readResult.Results[0].GetValueOrDefault<DateTime>());
                await Task.Delay(1000);
            }

            logger.LogInformation($"Closing session '{channel.SessionId}'.");
            await channel.CloseAsync();
        }

        /// <summary>
        /// Tests creating a subscription and monitoring current time.
        /// Only run this test with a running opc test server.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task TestSubscription()
        {
            var channel = new UaTcpSessionChannel(
                localDescription,
                certificateStore,
                new AnonymousIdentity(),
                EndpointUrl,
                loggerFactory: loggerFactory);

            await channel.OpenAsync();
            logger.LogInformation($"Opened session with endpoint '{channel.RemoteEndpoint.EndpointUrl}'.");
            logger.LogInformation($"SecurityPolicy: '{channel.RemoteEndpoint.SecurityPolicyUri}'.");
            logger.LogInformation($"SecurityMode: '{channel.RemoteEndpoint.SecurityMode}'.");
            logger.LogInformation($"Activated session '{channel.SessionId}'.");

            var req = new CreateSubscriptionRequest
            {
                RequestedPublishingInterval = 1000.0,
                RequestedMaxKeepAliveCount = 30,
                RequestedLifetimeCount = 30 * 3,
                PublishingEnabled = true,
            };
            var res = await channel.CreateSubscriptionAsync(req);
            var id = res.SubscriptionId;
            logger.LogInformation($"Created subscription '{id}'.");

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

            logger.LogInformation("Subscribe to PublishResponse stream.");
            var numOfResponses = 0;

            void onPublish(PublishResponse pr)
            {
                numOfResponses++;

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

            var token = channel
                .Where(pr => pr.SubscriptionId == id)
                .Subscribe(onPublish, onPublishError);

            await Task.Delay(5000);

            logger.LogInformation($"Closing session '{channel.SessionId}'.");
            await channel.CloseAsync();

            numOfResponses
                .Should().BeGreaterThan(0);
        }

        /// <summary>
        /// Tests calling a method of the UACPPServer.
        /// Only run this test with a running opc test server.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CustomVectorAdd()
        {
            var channel = new UaTcpSessionChannel(
                localDescription,
                certificateStore,
                new AnonymousIdentity(),
                "opc.tcp://localhost:48010",
                SecurityPolicyUris.None,
                loggerFactory: loggerFactory);

            await channel.OpenAsync();

            logger.LogInformation("4 - Call VectorAdd method with structure arguments.");
            var v1 = new CustomTypeLibrary.CustomVector { X = 1.0, Y = 2.0, Z = 3.0 };
            var v2 = new CustomTypeLibrary.CustomVector { X = 1.0, Y = 2.0, Z = 3.0 };
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
            var result = response.Results[0].OutputArguments[0].GetValueOrDefault<CustomTypeLibrary.CustomVector>();

            logger.LogInformation($"  {v1}");
            logger.LogInformation($"+ {v2}");
            logger.LogInformation(@"  ------------------");
            logger.LogInformation($"  {result}");

            logger.LogInformation($"Closing session '{channel.SessionId}'.");
            await channel.CloseAsync();

            result.Z
                .Should().Be(6.0);
        }

        /// <summary>
        /// Tests reading the historical data of the UACPPServer.
        /// Only run this test with a running opc test server.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task ReadHistorical()
        {
            var channel = new UaTcpSessionChannel(
                localDescription,
                certificateStore,
                new AnonymousIdentity(),
                "opc.tcp://localhost:48010",
                loggerFactory: loggerFactory);

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

        /// <summary>
        /// Tests connecting to endpoint and creating a subscription based view model.
        /// Only run this test with a running opc test server.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task TestViewModel()
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
                .SetLoggerFactory(loggerFactory)
                .ConfigureOptions(o => o.SessionTimeout = 30000)
                .Build();

            app.Run();

            var vm = new MyViewModel();

            void onPropertyChanged(object s, PropertyChangedEventArgs e) { }

            vm.PropertyChanged += onPropertyChanged; // simulate xaml binding

            logger.LogInformation($"Created subscription.");

            await Task.Delay(5000);

            vm.PropertyChanged -= onPropertyChanged; // simulate un-binding
            app.Dispose();

            vm.CurrentTime
                .Should().NotBe(DateTime.MinValue);
            vm.CurrentTimeAsDataValue
                .Should().NotBeNull();
            vm.CurrentTimeQueue
                .Should().NotBeEmpty();
        }

        [Subscription(endpointUrl: "opc.tcp://localhost:48010", publishingInterval: 500, keepAliveCount: 20)]
        private class MyViewModel : SubscriptionBase
        {
            /// <summary>
            /// Gets the value of CurrentTime.
            /// </summary>
            [MonitoredItem(nodeId: "i=2258")]
            public DateTime CurrentTime
            {
                get { return currentTime; }
                private set { currentTime = value; }
            }

            private DateTime currentTime;

            /// <summary>
            /// Gets the value of CurrentTimeAsDataValue.
            /// </summary>
            [MonitoredItem(nodeId: "i=2258")]
            public DataValue CurrentTimeAsDataValue
            {
                get { return currentTimeAsDataValue; }
                private set { currentTimeAsDataValue = value; }
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
                loggerFactory: loggerFactory);

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
        [Fact]
        public async Task TransferSubscription()
        {
            var channel1 = new UaTcpSessionChannel(
                localDescription,
                certificateStore,
                new UserNameIdentity("root", "secret"),
                EndpointUrl,
                loggerFactory: loggerFactory);

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
                new UserNameIdentity("root", "secret"),
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

        [Fact]
        public async Task StructureTest()
        {
            var channel = new UaTcpSessionChannel(
                localDescription,
                certificateStore,
                new AnonymousIdentity(),
                EndpointUrl,
                loggerFactory: loggerFactory);

            await channel.OpenAsync();

            var readRequest = new ReadRequest
            {
                NodesToRead = new[]
                {
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Arrays.Structure") },
                },
            };

            var readResponse = await channel.ReadAsync(readRequest);
            foreach (var result in readResponse.Results)
            {
                StatusCode.IsGood(result.StatusCode)
                    .Should().BeTrue();
            }

            // reading this node returns an array of ExtensionObjects 
            var obj = readResponse.Results[0].Value;

            // create new DataValue for writing.  Most servers reject writing values with timestamps.
            var newValue = new DataValue(obj);

            var writeRequest = new WriteRequest
            {
                NodesToWrite = new[]
                {
                new WriteValue { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=2;s=Demo.Static.Arrays.Structure"), Value =  newValue},
                },
            };
            var writeResponse = await channel.WriteAsync(writeRequest);
            foreach (var result in writeResponse.Results)
            {
                StatusCode.IsGood(result)
                    .Should().BeTrue();
            }

            logger.LogInformation($"Closing session '{channel.SessionId}'.");
            await channel.CloseAsync();
        }

    }
}