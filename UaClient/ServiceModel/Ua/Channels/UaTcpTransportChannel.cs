// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Workstation.ServiceModel.Ua.Channels
{
    /// <summary>
    /// A channel that opens a TCP socket.
    /// </summary>
    public class UaTcpTransportChannel : CommunicationObject
    {
        public const uint ProtocolVersion = 0u;
        public const uint DefaultBufferSize = 64 * 1024;
        public const uint DefaultMaxMessageSize = 16 * 1024 * 1024;
        public const uint DefaultMaxChunkCount = 4 * 1024;

        private const int MinBufferSize = 8 * 1024;
        private static readonly Task completedTask = Task.FromResult(true);
        private byte[] sendBuffer;
        private byte[] receiveBuffer;
        private Stream stream;
        private TcpClient tcpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="UaTcpTransportChannel"/> class.
        /// </summary>
        /// <param name="remoteEndpoint">The remote endpoint.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="localReceiveBufferSize">The size of the receive buffer.</param>
        /// <param name="localSendBufferSize">The size of the send buffer.</param>
        /// <param name="localMaxMessageSize">The maximum total size of a message.</param>
        /// <param name="localMaxChunkCount">The maximum number of message chunks.</param>
        public UaTcpTransportChannel(
            EndpointDescription remoteEndpoint,
            ILoggerFactory loggerFactory = null,
            uint localReceiveBufferSize = DefaultBufferSize,
            uint localSendBufferSize = DefaultBufferSize,
            uint localMaxMessageSize = DefaultMaxMessageSize,
            uint localMaxChunkCount = DefaultMaxChunkCount)
            : base(loggerFactory)
        {
            this.RemoteEndpoint = remoteEndpoint ?? throw new ArgumentNullException(nameof(remoteEndpoint));
            this.LocalReceiveBufferSize = localReceiveBufferSize;
            this.LocalSendBufferSize = localSendBufferSize;
            this.LocalMaxMessageSize = localMaxMessageSize;
            this.LocalMaxChunkCount = localMaxChunkCount;
        }

        public EndpointDescription RemoteEndpoint { get; }

        public uint LocalReceiveBufferSize { get; }

        public uint LocalSendBufferSize { get; }

        public uint LocalMaxMessageSize { get; }

        public uint LocalMaxChunkCount { get; }

        public uint RemoteReceiveBufferSize { get; protected set; }

        public uint RemoteSendBufferSize { get; protected set; }

        public uint RemoteMaxMessageSize { get; protected set; }

        public uint RemoteMaxChunkCount { get; protected set; }

        protected virtual async Task SendAsync(byte[] buffer, int offset, int count, CancellationToken token = default(CancellationToken))
        {
            this.ThrowIfClosedOrNotOpening();
            await this.stream.WriteAsync(buffer, offset, count, token).ConfigureAwait(false);
        }

        protected virtual async Task<int> ReceiveAsync(byte[] buffer, int offset, int count, CancellationToken token = default(CancellationToken))
        {
            this.ThrowIfClosedOrNotOpening();
            int initialOffset = offset;
            int maxCount = count;
            int num = 0;
            count = 8;
            while (count > 0)
            {
                try
                {
                    num = await this.stream.ReadAsync(buffer, offset, count, token).ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    if (token.IsCancellationRequested)
                    {
                        return 0;
                    }
                    throw;
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
                    num = await this.stream.ReadAsync(buffer, offset, count, token).ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    if (token.IsCancellationRequested)
                    {
                        return 0;
                    }
                    throw;
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

        protected override async Task OnOpenAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            this.sendBuffer = new byte[MinBufferSize];
            this.receiveBuffer = new byte[MinBufferSize];

            this.tcpClient = new TcpClient { NoDelay = true };
            var uri = new UriBuilder(this.RemoteEndpoint.EndpointUrl);
            await this.tcpClient.ConnectAsync(uri.Host, uri.Port).ConfigureAwait(false);
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

        protected override Task OnOpenedAsync(CancellationToken token)
        {
            return base.OnOpenedAsync(token);
        }

        protected override Task OnCloseAsync(CancellationToken token)
        {
            return completedTask;
        }

        protected override Task OnAbortAsync(CancellationToken token)
        {
            return completedTask;
        }

        protected async override Task OnClosedAsync(CancellationToken token)
        {
#if NETSTANDARD
            this.tcpClient?.Dispose();
#else
            tcpClient?.Close();
#endif
            await base.OnClosedAsync(token);
        }

    }
}