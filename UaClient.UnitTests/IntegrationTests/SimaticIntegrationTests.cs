// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using System;
using System.IO;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Workstation.ServiceModel.Ua;
using Workstation.ServiceModel.Ua.Channels;
using Xunit;
using Xunit.Abstractions;

namespace Workstation.UaClient.IntegrationTests
{
    public class SimaticIntegrationTests
    {
        private const string EndpointUrl = "opc.tcp://192.168.0.100:4840"; // the endpoint of the S7-1500 simulator.
 
        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger<IntegrationTests> logger;
        private readonly ApplicationDescription localDescription;
        private readonly ICertificateStore certificateStore;
        private readonly X509Identity x509Identity;
        private readonly ITestOutputHelper output;

        public SimaticIntegrationTests(ITestOutputHelper output)
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

        }

        /// <summary>
        /// Tests read server status.
        /// Only run this test with a running opc test server.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task Read()
        {
            var channel = new ClientSessionChannel(
                localDescription,
                certificateStore,
                new AnonymousIdentity(),
                EndpointUrl,
                SecurityPolicyUris.None,
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

        [Fact]
        public async Task DTLTest()
        {
            var channel = new ClientSessionChannel(
                localDescription,
                certificateStore,
                new AnonymousIdentity(),
                EndpointUrl,
                SecurityPolicyUris.None,
                loggerFactory: loggerFactory);

            await channel.OpenAsync();

            var readRequest = new ReadRequest
            {
                NodesToRead = new[]
                {
                new ReadValueId { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=3;s=\"Data_block_1\".\"Now\"") },
                },
            };

            var readResponse = await channel.ReadAsync(readRequest);
            foreach (var result in readResponse.Results)
            {
                StatusCode.IsGood(result.StatusCode)
                    .Should().BeTrue();
            }

            // reading this node returns an DTL value.
            var v1 = readResponse.Results[0].GetValueOrDefault<SimaticTypeLibrary.DTL>();

            logger.LogInformation($"  {v1}");

            // create new DataValue for writing. 

            var now = DateAndTime.Now;
            var v2 = new SimaticTypeLibrary.DTL();
            v2.YEAR = (ushort)now.Year;
            v2.MONTH = (byte)now.Month;
            v2.DAY = (byte)now.Day;
            v2.HOUR = (byte)now.Hour;
            v2.MINUTE = (byte)now.Minute;
            v2.SECOND = (byte)now.Second;
            v2.NANOSECOND = (uint)now.Nanosecond;

            var newValue = new DataValue(v2);

            var writeRequest = new WriteRequest
            {
                NodesToWrite = new[]
                {
                new WriteValue { AttributeId = AttributeIds.Value, NodeId = NodeId.Parse("ns=3;s=\"Data_block_1\".\"Now\""), Value =  newValue},
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