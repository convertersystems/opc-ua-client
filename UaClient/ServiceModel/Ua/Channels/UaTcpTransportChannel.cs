// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
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
        public const int DefaultBufferSize = 64 * 1024;
        public const int DefaultMaxMessageSize = 16 * 1024 * 1024;
        public const int DefaultMaxChunkCount = 4 * 1024;

        private const int MinBufferSize = 8 * 1024;
        private byte[] sendBuffer;
        private byte[] receiveBuffer;
        private Stream outstream;
        private Stream instream;
        private System.Net.Sockets.TcpClient tcpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="UaTcpTransportChannel"/> class.
        /// </summary>
        /// <param name="remoteEndpoint">the remoteEndpoint.</param>
        public UaTcpTransportChannel(EndpointDescription remoteEndpoint)
        {
            if (remoteEndpoint == null)
            {
                throw new ArgumentNullException(nameof(remoteEndpoint));
            }

            this.RemoteEndpoint = remoteEndpoint;
            this.LocalReceiveBufferSize = DefaultBufferSize;
            this.LocalSendBufferSize = DefaultBufferSize;
            this.LocalMaxMessageSize = DefaultMaxMessageSize;
            this.LocalMaxChunkCount = DefaultMaxChunkCount;
        }

        public EndpointDescription RemoteEndpoint { get; }

        public uint LocalReceiveBufferSize { get; set; }

        public uint LocalSendBufferSize { get; set; }

        public uint LocalMaxMessageSize { get; set; }

        public uint LocalMaxChunkCount { get; set; }

        public uint RemoteReceiveBufferSize { get; protected set; }

        public uint RemoteSendBufferSize { get; protected set; }

        public uint RemoteMaxMessageSize { get; protected set; }

        public uint RemoteMaxChunkCount { get; protected set; }

        protected virtual async Task SendAsync(byte[] buffer, int offset, int count, CancellationToken token = default(CancellationToken))
        {
            this.ThrowIfClosedOrNotOpening();
            await this.outstream.WriteAsync(buffer, offset, count, token).ConfigureAwait(false);
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
                num = await this.instream.ReadAsync(buffer, offset, count, token).ConfigureAwait(false);
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
                num = await this.instream.ReadAsync(buffer, offset, count, token).ConfigureAwait(false);
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

            this.tcpClient = new System.Net.Sockets.TcpClient { NoDelay = true };
            var uri = new UriBuilder(this.RemoteEndpoint.EndpointUrl);
            await this.tcpClient.ConnectAsync(uri.Host, uri.Port).ConfigureAwait(false);
            this.instream = this.outstream = this.tcpClient.GetStream();

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
                    throw new ServiceResultException(statusCode, message);
                }

                throw new InvalidOperationException("UaTcpTransportChannel.OnOpenAsync received unexpected message type.");
            }
            finally
            {
                decoder.Dispose();
            }
        }

        protected override Task OnCloseAsync(CancellationToken token)
        {
            return Task.CompletedTask;
        }

        protected override Task OnAbortAsync(CancellationToken token)
        {
            return Task.CompletedTask;
        }

        protected override Task OnClosedAsync(CancellationToken token)
        {
            this.tcpClient?.Dispose();
            return base.OnClosedAsync(token);
        }

    }
}