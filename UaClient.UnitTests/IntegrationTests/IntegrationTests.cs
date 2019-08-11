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
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;
using Workstation.Collections;
using Workstation.ServiceModel.Ua;
using Workstation.ServiceModel.Ua.Channels;
using Xunit;

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

        public IntegrationTests()
        {
            this.loggerFactory = new LoggerFactory();
            this.loggerFactory.AddDebug(LogLevel.Trace);
            this.logger = this.loggerFactory?.CreateLogger<IntegrationTests>();

            this.localDescription = new ApplicationDescription
            {
                ApplicationName = "Workstation.UaClient.UnitTests",
                ApplicationUri = $"urn:{Dns.GetHostName()}:Workstation.UaClient.UnitTests",
                ApplicationType = ApplicationType.Client
            };

            var pkiPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Workstation.UaClient.UnitTests",
                    "pki");
            this.certificateStore = new DirectoryStore(pkiPath);

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
            if (userCert != null && userKey != null) {
                x509Identity = new X509Identity(userCert, userKey);
            }
        }

        /// <summary>
        /// Tests endpoint with no security and with no Certificate.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact(Skip = "Only run this with a running opc test server")]
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
        [Fact(Skip = "Only run this with a running opc test server")]
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
            foreach (var selectedEndpoint in getEndpointsResponse.Endpoints
                .OrderBy(e => e.SecurityLevel))
            {
                foreach (var selectedTokenPolicy in selectedEndpoint.UserIdentityTokens)
                {
                    IUserIdentity selectedUserIdentity;
                    switch (selectedTokenPolicy.TokenType)
                    {
                        case UserTokenType.Certificate:
                            selectedUserIdentity = this.x509Identity;
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
        /// Tests browsing the Objects folder.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact(Skip = "Only run this with a running opc test server")]
        public async Task BrowseObjects()
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

            Console.WriteLine("+ Objects, 0:Objects, Object");
            foreach (var rd in rds)
            {
                Console.WriteLine("   + {0}, {1}, {2}", rd.DisplayName, rd.BrowseName, rd.NodeClass);
            }

            Console.WriteLine($"Closing session '{channel.SessionId}'.");
            await channel.CloseAsync();
        }

        /// <summary>
        /// Tests read server status.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact(Skip = "Only run this with a running opc test server")]
        public async Task Read()
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

            var readRequest = new ReadRequest { NodesToRead = new[] { new ReadValueId { NodeId = NodeId.Parse(VariableIds.Server_ServerStatus), AttributeId = AttributeIds.Value } } };
            var readResult = await channel.ReadAsync(readRequest);
            var serverStatus = readResult.Results[0].GetValueOrDefault<ServerStatusDataType>();

            Console.WriteLine("Server status:");
            Console.WriteLine("  ProductName: {0}", serverStatus.BuildInfo.ProductName);
            Console.WriteLine("  SoftwareVersion: {0}", serverStatus.BuildInfo.SoftwareVersion);
            Console.WriteLine("  ManufacturerName: {0}", serverStatus.BuildInfo.ManufacturerName);
            Console.WriteLine("  State: {0}", serverStatus.State);
            Console.WriteLine("  CurrentTime: {0}", serverStatus.CurrentTime);

            Console.WriteLine($"Closing session '{channel.SessionId}'.");
            await channel.CloseAsync();
        }

        /// <summary>
        /// Tests polling the current time.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact(Skip = "Only run this with a running opc test server")]
        public async Task Polling()
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


            var readRequest = new ReadRequest { NodesToRead = new[] { new ReadValueId { NodeId = NodeId.Parse(VariableIds.Server_ServerStatus_CurrentTime), AttributeId = AttributeIds.Value } } };
            for (int i = 0; i < 10; i++)
            {
                var readResult = await channel.ReadAsync(readRequest);
                Console.WriteLine("Read {0}", readResult.Results[0].GetValueOrDefault<DateTime>());
                await Task.Delay(1000);
            }
            Console.WriteLine($"Closing session '{channel.SessionId}'.");
            await channel.CloseAsync();
        }

        /// <summary>
        /// Tests creating a subscription and monitoring current time.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact(Skip = "Only run this with a running opc test server")]
        public async Task TestSubscription()
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

            var req = new CreateSubscriptionRequest
            {
                RequestedPublishingInterval = 1000.0,
                RequestedMaxKeepAliveCount = 30,
                RequestedLifetimeCount = 30 * 3,
                PublishingEnabled = true,
            };
            var res = await channel.CreateSubscriptionAsync(req);
            var id = res.SubscriptionId;
            Console.WriteLine($"Created subscription '{id}'.");

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

            Console.WriteLine("Subscribe to PublishResponse stream.");
            var numOfResponses = 0;
            var token = channel.Where(pr => pr.SubscriptionId == id).Subscribe(
                pr =>
                {
                    numOfResponses++;

                    // loop thru all the data change notifications
                    var dcns = pr.NotificationMessage.NotificationData.OfType<DataChangeNotification>();
                    foreach (var dcn in dcns)
                    {
                        foreach (var min in dcn.MonitoredItems)
                        {
                            Console.WriteLine($"sub: {pr.SubscriptionId}; handle: {min.ClientHandle}; value: {min.Value}");
                        }
                    }
                },
                ex =>
                {
                    Console.WriteLine("Exception in publish response handler: {0}", ex.GetBaseException().Message);
                });

            await Task.Delay(5000);

            Console.WriteLine($"Closing session '{channel.SessionId}'.");
            await channel.CloseAsync();

            numOfResponses
                .Should().BeGreaterThan(0);
        }

        /// <summary>
        /// Tests calling a method of the UACPPServer.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact(Skip = "Only run this with a running opc test server")]
        public async Task VectorAdd()
        {
            var channel = new UaTcpSessionChannel(
                this.localDescription,
                this.certificateStore,
                new AnonymousIdentity(),
                "opc.tcp://localhost:48010",
                SecurityPolicyUris.None,
                loggerFactory: this.loggerFactory,
                additionalTypes: new[] { typeof(Vector) });

            await channel.OpenAsync();

            Console.WriteLine("4 - Call VectorAdd method with structure arguments.");
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

            Console.WriteLine($"  {v1}");
            Console.WriteLine($"+ {v2}");
            Console.WriteLine(@"  ------------------");
            Console.WriteLine($"  {result}");

            Console.WriteLine($"Closing session '{channel.SessionId}'.");
            await channel.CloseAsync();

            result.Z
                .Should().Be(6.0);
        }

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
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact(Skip = "Only run this with a running opc test server")]
        public async Task ReadHistorical()
        {
            var channel = new UaTcpSessionChannel(
                this.localDescription,
                this.certificateStore,
                new AnonymousIdentity(),
                "opc.tcp://localhost:48010",
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
            StatusCode.IsGood(result.StatusCode)
                .Should().BeTrue();
            Console.WriteLine($"HistoryRead response status code: {result.StatusCode}, HistoryData count: {((HistoryData)result.HistoryData).DataValues.Length}.");

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
                Console.WriteLine($"HistoryRead response status code: {result2.StatusCode}, HistoryEvent count: {((HistoryEvent)result2.HistoryData).Events.Length}.");

                // Use EventHelper to create AlarmConditions from the HistoryEventFieldList
                var alarms = ((HistoryEvent)result2.HistoryData).Events.Select(e => EventHelper.Deserialize<AlarmCondition>(e.EventFields));
            }
            Console.WriteLine($"Closing session '{channel.SessionId}'.");
            await channel.CloseAsync();
        }

        /// <summary>
        /// Tests connecting to endpoint and creating a subscription based view model.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact(Skip = "Only run this with a running opc test server")]
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
                .SetLoggerFactory(this.loggerFactory)
                .ConfigureOptions(o => o.SessionTimeout = 30000)
                .Build();

            app.Run();

            var vm = new MyViewModel();
            // simulate a view <-> view-model
            var d = new PropertyChangedEventHandler((s, e) => { });
            vm.PropertyChanged += d;

            Console.WriteLine($"Created subscription.");

            await Task.Delay(5000);

            vm.PropertyChanged -= d;
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

        [Fact(Skip = "Only run this with a running opc test server")]
        public async Task StackTest()
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
            Console.WriteLine($"{sw.ElapsedMilliseconds} ms");

            Console.WriteLine($"Closing session '{channel.SessionId}'.");
            await channel.CloseAsync();
        }

        /// <summary>
        /// Tests result of transfer subscription from channel1 to channel2.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact(Skip = "Only run this with a running opc test server")]
        public async Task TransferSubscription()
        {
            var channel1 = new UaTcpSessionChannel(
                this.localDescription,
                this.certificateStore,
                new UserNameIdentity("root", "secret"),
                EndpointUrl,
                loggerFactory: this.loggerFactory);

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

            StatusCode.IsGood(transferResult.Results[0].StatusCode)
                .Should().BeTrue();

            await Task.Delay(3000);

            Console.WriteLine($"Closing session '{channel1.SessionId}'.");
            await channel1.CloseAsync();

            Console.WriteLine($"Closing session '{channel2.SessionId}'.");
            await channel2.CloseAsync();
        }

    }
}