// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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

        private readonly ILogger? _logger;
        private ITransportConnection? _connection;

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
            RemoteEndpoint = remoteEndpoint ?? throw new ArgumentNullException(nameof(remoteEndpoint));
            _logger = loggerFactory?.CreateLogger<UaTcpTransportChannel>();
            LocalReceiveBufferSize = options?.LocalReceiveBufferSize ?? DefaultBufferSize;
            LocalSendBufferSize = options?.LocalSendBufferSize ?? DefaultBufferSize;
            LocalMaxMessageSize = options?.LocalMaxMessageSize ?? DefaultMaxMessageSize;
            LocalMaxChunkCount = options?.LocalMaxChunkCount ?? DefaultMaxChunkCount;
        }

        /// <summary>
        /// Gets the stack profile.
        /// </summary>
        public StackProfile StackProfile { get; } = StackProfiles.TcpUascBinary;

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
        [Obsolete]
        protected virtual Socket? Socket => null;

        /// <summary>
        /// Asynchronously sends a sequence of bytes to the remote endpoint.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A task.</returns>
        protected virtual async Task SendAsync(byte[] buffer, int offset, int count, CancellationToken token = default)
        {
            ThrowIfClosedOrNotOpening();
            var connection = _connection ?? throw new InvalidOperationException("The connection field is null!");
            await connection.SendAsync(buffer, offset, count, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously receives a sequence of bytes from the remote endpoint.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A task.</returns>
        protected virtual async Task<int> ReceiveAsync(byte[] buffer, int offset, int count, CancellationToken token = default)
        {
            ThrowIfClosedOrNotOpening();
            var connection = _connection ?? throw new InvalidOperationException("The connection field is null!");

            return await connection.ReceiveAsync(buffer, offset, count, token).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        protected override async Task OnOpenAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            _connection = await StackProfile.TransportConnectionProvider.ConnectAsync(RemoteEndpoint.EndpointUrl!).ConfigureAwait(false);

            var localOptions = new TransportConnectionOptions
            {
                ReceiveBufferSize = LocalReceiveBufferSize,
                SendBufferSize = LocalSendBufferSize,
                MaxMessageSize = LocalMaxMessageSize,
                MaxChunkCount = LocalMaxChunkCount
            };

            var remoteOptions = await _connection.OpenAsync(ProtocolVersion, localOptions, token).ConfigureAwait(false);

            RemoteSendBufferSize = remoteOptions.SendBufferSize;
            RemoteReceiveBufferSize = remoteOptions.ReceiveBufferSize;
            RemoteMaxMessageSize = remoteOptions.MaxMessageSize;
            RemoteMaxChunkCount = remoteOptions.MaxChunkCount;
        }

        /// <inheritdoc/>
        protected override async Task OnCloseAsync(CancellationToken token)
        {
            var connection = _connection;

            if (connection != null)
            {
                await connection.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        protected override async Task OnAbortAsync(CancellationToken token)
        {
            var connection = _connection;

            if (connection != null)
            {
                await connection.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}