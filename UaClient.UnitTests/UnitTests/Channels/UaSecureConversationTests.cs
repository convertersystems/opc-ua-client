using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Workstation.ServiceModel.Ua;
using Workstation.ServiceModel.Ua.Channels;
using Xunit;

namespace Workstation.UaClient.UnitTests.Channels
{
    public partial class UaSecureConversationTests
    {
        private const int TokenId = 1000;

        public static IEnumerable<byte[]> Messages => new[]
        {
            ShortMessageContent,
            LongMessageContent,
            LongByteMessageContent1,
            LongByteMessageContent2
        };

        public static IEnumerable<string> PolicityUris => new[]
        {
            SecurityPolicyUris.None,
            SecurityPolicyUris.Basic128Rsa15,
            SecurityPolicyUris.Basic256Sha256,
            SecurityPolicyUris.Basic256,
            SecurityPolicyUris.Aes128_Sha256_RsaOaep,
            SecurityPolicyUris.Aes256_Sha256_RsaPss,
        };

        public static IEnumerable<MessageSecurityMode> SecurityModes => new[]
        {
            MessageSecurityMode.None,
            MessageSecurityMode.Sign,
            MessageSecurityMode.SignAndEncrypt
        };

        public static ApplicationDescription ClientDescription { get; } = new ApplicationDescription
        {
            ApplicationName = "ClientName",
            ApplicationUri = $"urn:{Dns.GetHostName()}:Workstation.UaClient.UnitTests.Client",
            ApplicationType = ApplicationType.Client,
        };

        public static ApplicationDescription ServerDescription { get; } = new ApplicationDescription
        {
            ApplicationName = "ServerName",
            ApplicationUri = $"urn:{Dns.GetHostName()}:Workstation.UaClient.UnitTests.Server",
            ApplicationType = ApplicationType.Server,
        };

        public static ITestCertificateStore Store = new TestCertificateStore(ServerDescription, ClientDescription);
        public static ITestCertificateStore ThrowingStore = new ThrowingTestCertificateStore();

        public static IEnumerable<object[]> SendOpeningMessageData { get; } =
            from uri in PolicityUris
            from mode in SecurityModes
            from message in Messages
            where IsValidCombination(uri, mode)
            select new object[] { uri, mode, message };

        private static bool IsValidCombination(string uri, MessageSecurityMode mode)
        {
            if (uri == SecurityPolicyUris.None)
            {
                return mode == MessageSecurityMode.None;
            }
            else
            {
                return mode != MessageSecurityMode.None;
            }
        }

        [MemberData(nameof(SendOpeningMessageData))]
        [Theory]
        public async Task SendOpeningMessage(string securityPolicyUri, MessageSecurityMode mode, byte[] request)
        {
            const uint channelId = 3;
            uint handle = 10;

            // create client and server
            var client = await CreateClientConversationAsync(securityPolicyUri, mode);
            var server = CreateServerConversation(channelId, mode);

            var result = await SendAndReceiveAsync(client, server, UaTcpMessageTypes.OPNF, handle, request);

            result.requestHandle
                .Should().Be(handle);
            result.messageType
                .Should().Be(UaTcpMessageTypes.OPNF);
            result.content
                .Should().Equal(request);
        }

        public static IEnumerable<object[]> SendReceiveOpeningMessageData { get; } =
            from uri in PolicityUris
            from mode in SecurityModes
            from message in Messages
            where IsValidCombination(uri, mode)
            select new object[]
            {
                uri,
                mode,
                ShortMessageContent,
                message
            };

        [MemberData(nameof(SendReceiveOpeningMessageData))]
        [Theory]
        public async Task SendReceiveOpeningMessage(string securityPolicyUri, MessageSecurityMode mode, byte[] request, byte[] response)
        {
            const uint channelId = 3;
            uint handle = 10;

            // create client and server
            var client = await CreateClientConversationAsync(securityPolicyUri, mode);
            var server = CreateServerConversation(channelId, mode);

            await SendAndReceiveAsync(client, server, UaTcpMessageTypes.OPNF, handle, request);
            server.SecurityMode = mode;
            var result = await SendAndReceiveAsync(server, client, UaTcpMessageTypes.OPNF, handle, response);
            client.TokenId = TokenId;

            result.requestHandle
                .Should().Be(handle);
            result.messageType
                .Should().Be(UaTcpMessageTypes.OPNF);
            result.content
                .Should().Equal(response);

            client.ChannelId
                .Should().Be(channelId);
        }

        public static IEnumerable<object[]> SendMessageData { get; } =
            from uri in PolicityUris
            from mode in SecurityModes
            from message in Messages
            where IsValidCombination(uri, mode)
            select new object[]
            {
                uri,
                mode,
                ShortMessageContent,
                LongMessageContent,
                message
            };

        [MemberData(nameof(SendMessageData))]
        [Theory]
        public async Task SendMessage(string securityPolicyUri, MessageSecurityMode mode, byte[] openRequest, byte[] openResponse, byte[] request)
        {
            const uint channelId = 3;
            uint handle = 10;

            // create client and server
            var client = await CreateClientConversationAsync(securityPolicyUri, mode);
            var server = CreateServerConversation(channelId, mode);

            await SendAndReceiveAsync(client, server, UaTcpMessageTypes.OPNF, handle, openRequest);
            server.SecurityMode = mode;
            await SendAndReceiveAsync(server, client, UaTcpMessageTypes.OPNF, handle, openResponse);
            client.TokenId = TokenId;

            handle++;
            var result = await SendAndReceiveAsync(client, server, UaTcpMessageTypes.MSGA, handle, request);

            result.requestHandle
                .Should().Be(handle);
            result.messageType
                .Should().Be(UaTcpMessageTypes.MSGF);
            result.content
                .Should().Equal(request);

            client.ChannelId
                .Should().Be(channelId);
        }

        public static IEnumerable<object[]> SendReceiveMessageData { get; } =
            from uri in PolicityUris
            from mode in SecurityModes
            from message in Messages
            where IsValidCombination(uri, mode)
            select new object[]
            {
                uri,
                mode,
                ShortMessageContent,
                LongMessageContent,
                ShortMessageContent,
                message
            };

        [MemberData(nameof(SendReceiveMessageData))]
        [Theory]
        public async Task SendReceiveMessage(string securityPolicyUri, MessageSecurityMode mode, byte[] openRequest, byte[] openResponse, byte[] request, byte[] response)
        {
            const uint channelId = 3;
            uint handle = 10;

            // create client and server
            var client = await CreateClientConversationAsync(securityPolicyUri, mode);
            var server = CreateServerConversation(channelId, mode);

            await SendAndReceiveAsync(client, server, UaTcpMessageTypes.OPNF, handle, openRequest);
            server.SecurityMode = mode;
            await SendAndReceiveAsync(server, client, UaTcpMessageTypes.OPNF, handle, openResponse);
            client.TokenId = TokenId;

            handle++;
            await SendAndReceiveAsync(client, server, UaTcpMessageTypes.MSGA, handle, request);
            var result = await SendAndReceiveAsync(server, client, UaTcpMessageTypes.MSGA, handle, response);

            result.requestHandle
                .Should().Be(handle);
            result.messageType
                .Should().Be(UaTcpMessageTypes.MSGF);
            result.content
                .Should().Equal(response);

            client.ChannelId
                .Should().Be(channelId);
        }

        public static IEnumerable<object[]> SendClosingMessageData { get; } =
            from uri in PolicityUris
            from mode in SecurityModes
            from message in Messages
            where IsValidCombination(uri, mode)
            select new object[]
            {
                uri,
                mode,
                ShortMessageContent,
                LongMessageContent,
                LongByteMessageContent1,
                ShortMessageContent,
                message
            };

        [MemberData(nameof(SendClosingMessageData))]
        [Theory]
        public async Task SendClosingMessage(string securityPolicyUri, MessageSecurityMode mode, byte[] openRequest, byte[] openResponse, byte[] request, byte[] response, byte[] closingRequest)
        {
            const uint channelId = 3;
            uint handle = 10;

            // create client and server
            var client = await CreateClientConversationAsync(securityPolicyUri, mode);
            var server = CreateServerConversation(channelId, mode);

            await SendAndReceiveAsync(client, server, UaTcpMessageTypes.OPNF, handle, openRequest);
            server.SecurityMode = mode;
            await SendAndReceiveAsync(server, client, UaTcpMessageTypes.OPNF, handle, openResponse);
            client.TokenId = TokenId;

            handle++;
            await SendAndReceiveAsync(client, server, UaTcpMessageTypes.MSGA, handle, request);
            await SendAndReceiveAsync(server, client, UaTcpMessageTypes.MSGA, handle, response);

            handle++;
            var result = await SendAndReceiveAsync(client, server, UaTcpMessageTypes.CLOF, handle, closingRequest);

            result.requestHandle
                .Should().Be(handle);
            result.messageType
                .Should().Be(UaTcpMessageTypes.CLOF);
            result.content
                .Should().Equal(closingRequest);

            client.ChannelId
                .Should().Be(channelId);
        }

        public static IEnumerable<object[]> SendReceiveClosingMessageData { get; } =
            from uri in PolicityUris
            from mode in SecurityModes
            from message in Messages
            where IsValidCombination(uri, mode)
            select new object[]
            {
                uri,
                mode,
                ShortMessageContent,
                LongMessageContent,
                LongByteMessageContent1,
                LongByteMessageContent2,
                ShortMessageContent,
                message
            };

        [MemberData(nameof(SendReceiveClosingMessageData))]
        [Theory]
        public async Task SendReceiveClosingMessage(string securityPolicyUri, MessageSecurityMode mode, byte[] openRequest, byte[] openResponse, byte[] request, byte[] response, byte[] closingRequest, byte[] closingResponse)
        {
            const uint channelId = 3;
            uint handle = 10;

            // create client and server
            var client = await CreateClientConversationAsync(securityPolicyUri, mode);
            var server = CreateServerConversation(channelId, mode);

            await SendAndReceiveAsync(client, server, UaTcpMessageTypes.OPNF, handle, openRequest);
            server.SecurityMode = mode;
            await SendAndReceiveAsync(server, client, UaTcpMessageTypes.OPNF, handle, openResponse);
            client.TokenId = TokenId;

            handle++;
            await SendAndReceiveAsync(client, server, UaTcpMessageTypes.MSGA, handle, request);
            await SendAndReceiveAsync(server, client, UaTcpMessageTypes.MSGA, handle, response);

            handle++;
            await SendAndReceiveAsync(client, server, UaTcpMessageTypes.CLOF, handle, closingRequest);
            var result = await SendAndReceiveAsync(server, client, UaTcpMessageTypes.CLOF, handle, closingResponse);

            result.requestHandle
                .Should().Be(handle);
            result.messageType
                .Should().Be(UaTcpMessageTypes.CLOF);
            result.content
                .Should().Equal(closingResponse);

            client.ChannelId
                .Should().Be(channelId);
        }

        private static async Task<UaSecureConversation> CreateClientConversationAsync(string securityPolicyUri, MessageSecurityMode mode)
        {
            var store = mode == MessageSecurityMode.None
                ? ThrowingStore
                : Store;

            var client = new UaSecureConversation(ClientDescription, new TransportConnectionOptions(), store, null)
            {
                SecurityMode = mode,
            };

            await client.SetRemoteCertificateAsync(securityPolicyUri, store.ServerCertificate);
            return client;
        }

        private static UaSecureConversation CreateServerConversation(uint channelId, MessageSecurityMode mode)
        {
            var store = mode == MessageSecurityMode.None
                ? ThrowingStore
                : Store;

            return new UaSecureConversation(channelId, ServerDescription, new TransportConnectionOptions(), store, null)
            {
                TokenId = TokenId,
            };
        }

        private async Task<(uint messageType, uint requestHandle, byte[] content)> SendAndReceiveAsync(IConversation sender, IConversation receiver, uint messageType, uint handle, byte[] content)
        {
            using var input = new MemoryStream();
            using var transfer = new MemoryStream();
            using var output = new MemoryStream();

            // write message into the body stream
            input.Write(content);
            input.Position = 0;

            // encrypt the message into the transfer stream
            await sender.EncryptMessageAsync(input, messageType, handle, transfer.WriteAsync, default);

            transfer.Position = 0;

            // dencrypt the message from the transfer stream
            var ret = await receiver.DecryptMessageAsync(output, transfer.ReadAsync, default);

            return (ret.messageType, ret.requestHandle, output.ToArray());
        }
    }
}
