// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

#nullable enable

namespace Workstation.ServiceModel.Ua.Channels
{
    /// <summary>
    /// A channel for communicating with OPC UA servers using the UA TCP transport profile.
    /// </summary>
    public class UaTcpTransportChannel : CommunicationObject
    {
        public const uint ProtocolVersion = 0u;
        public const uint DefaultBufferSize = 64 * 1024;
        public const uint DefaultMaxMessageSize = 16 * 1024 * 1024;
        public const uint DefaultMaxChunkCount = 4 * 1024;
        private const int MinBufferSize = 8 * 1024;
        private const int ConnectTimeout = 5000;
        private static readonly Task CompletedTask = Task.FromResult(true);

        private readonly ILogger? logger;
        private byte[]? sendBuffer;
        private byte[]? receiveBuffer;
        private Stream? stream;
        private TcpClient? tcpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="UaTcpTransportChannel"/> class.
        /// </summary>
        /// <param name="remoteEndpoint">The remote endpoint.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="options">The transport channel options.</param>
        public UaTcpTransportChannel(
            EndpointDescription remoteEndpoint,
            ILoggerFactory? loggerFactory = null,
            UaTcpTransportChannelOptions? options = null)
            : base(loggerFactory)
        {
            this.RemoteEndpoint = remoteEndpoint ?? throw new ArgumentNullException(nameof(remoteEndpoint));
            this.logger = loggerFactory?.CreateLogger<UaTcpTransportChannel>();
            this.LocalReceiveBufferSize = options?.LocalReceiveBufferSize ?? DefaultBufferSize;
            this.LocalSendBufferSize = options?.LocalSendBufferSize ?? DefaultBufferSize;
            this.LocalMaxMessageSize = options?.LocalMaxMessageSize ?? DefaultMaxMessageSize;
            this.LocalMaxChunkCount = options?.LocalMaxChunkCount ?? DefaultMaxChunkCount;
        }

        /// <summary>
        /// Gets the remote endpoint.
        /// </summary>
        public EndpointDescription RemoteEndpoint { get; }

        /// <summary>
        /// Gets the size of the receive buffer.
        /// </summary>
        public uint LocalReceiveBufferSize { get; }

        /// <summary>
        /// Gets the size of the send buffer.
        /// </summary>
        public uint LocalSendBufferSize { get; }

        /// <summary>
        /// Gets the maximum total size of a message.
        /// </summary>
        public uint LocalMaxMessageSize { get; }

        /// <summary>
        /// Gets the maximum number of message chunks.
        /// </summary>
        public uint LocalMaxChunkCount { get; }

        /// <summary>
        /// Gets the size of the remote receive buffer.
        /// </summary>
        public uint RemoteReceiveBufferSize { get; private set; }

        /// <summary>
        /// Gets the size of the remote send buffer.
        /// </summary>
        public uint RemoteSendBufferSize { get; private set; }

        /// <summary>
        /// Gets the maximum size of a message that may be sent.
        /// </summary>
        public uint RemoteMaxMessageSize { get; private set; }

        /// <summary>
        /// Gets the maximum number of message chunks that may be sent.
        /// </summary>
        public uint RemoteMaxChunkCount { get; private set; }

        /// <summary>
        /// Gets the inner TCP socket.
        /// </summary>
        protected virtual Socket? Socket => this.tcpClient?.Client;

        /// <summary>
        /// Asynchronously sends a sequence of bytes to the remote endpoint.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A task.</returns>
        protected virtual async Task SendAsync(byte[] buffer, int offset, int count, CancellationToken token = default(CancellationToken))
        {
            this.ThrowIfClosedOrNotOpening();
            var stream = this.stream ?? throw new InvalidOperationException("The stream field is null!");
            await stream.WriteAsync(buffer, offset, count, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously receives a sequence of bytes from the remote endpoint.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A task.</returns>
        protected virtual async Task<int> ReceiveAsync(byte[] buffer, int offset, int count, CancellationToken token = default(CancellationToken))
        {
            this.ThrowIfClosedOrNotOpening();
            var stream = this.stream ?? throw new InvalidOperationException("The stream field is null!");
            int initialOffset = offset;
            int maxCount = count;
            int num = 0;
            count = 8;
            while (count > 0)
            {
                try
                {
                    num = await stream.ReadAsync(buffer, offset, count, token).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    return 0;
                }

                if (num == 0)
                {
                    return 0;
                }

                offset += num;
                count -= num;
            }

            var len = BitConverter.ToUInt32(buffer, 4);
            if (len > maxCount)
            {
                throw new ServiceResultException(StatusCodes.BadResponseTooLarge);
            }

            count = (int)len - 8;
            while (count > 0)
            {
                try
                {
                    num = await stream.ReadAsync(buffer, offset, count, token).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    return 0;
                }

                if (num == 0)
                {
                    return 0;
                }

                offset += num;
                count -= num;
            }

            return offset - initialOffset;
        }

        /// <inheritdoc/>
        protected override async Task OnOpenAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            this.sendBuffer = new byte[MinBufferSize];
            this.receiveBuffer = new byte[MinBufferSize];

            this.tcpClient = new TcpClient { NoDelay = true };
            var uri = new UriBuilder(this.RemoteEndpoint.EndpointUrl!);
            await this.tcpClient.ConnectAsync(uri.Host, uri.Port).WithTimeoutAfter(ConnectTimeout).ConfigureAwait(false);
            this.stream = this.tcpClient.GetStream();

            // send 'hello'.
            int count;
            var encoder = new BinaryEncoder(new MemoryStream(this.sendBuffer, 0, MinBufferSize, true, false));
            try
            {
                encoder.WriteUInt32(null, UaTcpMessageTypes.HELF);
                encoder.WriteUInt32(null, 0u);
                encoder.WriteUInt32(null, ProtocolVersion);
                encoder.WriteUInt32(null, this.LocalReceiveBufferSize);
                encoder.WriteUInt32(null, this.LocalSendBufferSize);
                encoder.WriteUInt32(null, this.LocalMaxMessageSize);
                encoder.WriteUInt32(null, this.LocalMaxChunkCount);
                encoder.WriteString(null, uri.ToString());
                count = encoder.Position;
                encoder.Position = 4;
                encoder.WriteUInt32(null, (uint)count);
                encoder.Position = count;

                await this.SendAsync(this.sendBuffer, 0, count, token).ConfigureAwait(false);
            }
            finally
            {
                encoder.Dispose();
            }

            // receive response
            count = await this.ReceiveAsync(this.receiveBuffer, 0, MinBufferSize, token).ConfigureAwait(false);
            if (count == 0)
            {
                throw new ObjectDisposedException("socket");
            }

            // decode 'ack' or 'err'.
            var decoder = new BinaryDecoder(new MemoryStream(this.receiveBuffer, 0, count, false, false));
            try
            {
                var type = decoder.ReadUInt32(null);
                var len = decoder.ReadUInt32(null);
                if (type == UaTcpMessageTypes.ACKF)
                {
                    var remoteProtocolVersion = decoder.ReadUInt32(null);
                    if (remoteProtocolVersion < ProtocolVersion)
                    {
                        throw new ServiceResultException(StatusCodes.BadProtocolVersionUnsupported);
                    }

                    this.RemoteSendBufferSize = decoder.ReadUInt32(null);
                    this.RemoteReceiveBufferSize = decoder.ReadUInt32(null);
                    this.RemoteMaxMessageSize = decoder.ReadUInt32(null);
                    this.RemoteMaxChunkCount = decoder.ReadUInt32(null);
                    return;
                }
                else if (type == UaTcpMessageTypes.ERRF)
                {
                    var statusCode = decoder.ReadUInt32(null);
                    var message = decoder.ReadString(null);
                    if (message != null)
                    {
                        throw new ServiceResultException(statusCode, message);
                    }

                    throw new ServiceResultException(statusCode);
                }

                throw new InvalidOperationException("UaTcpTransportChannel.OnOpenAsync received unexpected message type.");
            }
            finally
            {
                decoder.Dispose();
            }
        }

        /// <inheritdoc/>
        protected override Task OnCloseAsync(CancellationToken token)
        {
#if NET45
            this.tcpClient?.Close();
#else
            this.tcpClient?.Dispose();
#endif
            return CompletedTask;
        }

        /// <inheritdoc/>
        protected override Task OnAbortAsync(CancellationToken token)
        {
#if NET45
            this.tcpClient?.Close();
#else
            this.tcpClient?.Dispose();
#endif
            return CompletedTask;
        }
    }
}