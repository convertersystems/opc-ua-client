// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Workstation.ServiceModel.Ua.Channels
{
    /// <summary>
    /// The <see cref="ITransportConnection"/> interface implementation
    /// for the OPC UA Connection Protocol.
    /// </summary>
    /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/7.1.1/">OPC UA specification Part 6: Mappings, 7.1.1</seealso>
    public class UaClientConnection : ITransportConnection
    {
        private const int MinBufferSize = 8 * 1024;

        /// <summary>
        /// The stream to read from and write to.
        /// </summary>
        public Stream Stream { get; }

        /// <summary>
        /// The Uri of the connection.
        /// </summary>
        public Uri Uri { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UaClientConnection"/> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="uri">The uri.</param>
        public UaClientConnection(Stream stream, Uri uri)
        {
            Stream = stream;
            Uri = uri;
        }

        /// <inheritdoc />
#pragma warning disable CS1998
        public async ValueTask DisposeAsync()
#pragma warning restore CS1998
        {
#if NETCOREAPP3_0_OR_GREATER
            await Stream.DisposeAsync();
#else
            Stream.Dispose();
#endif
        }

        /// <summary>
        /// Opens the connection. This includes the hello message handshake.
        /// </summary>
        /// <remakes>
        /// The underlying network client is already opened before this class is
        /// constructed. The
        /// connection is closed with <see cref="IAsyncDisposable.DisposeAsync"/>
        /// method.
        /// </remakes>
        /// <param name="protocolVersion">The protocol version.</param>
        /// <param name="localOptions">The requested transport connection options.</param>
        /// <param name="token">A cancellation token used to propagate notification that this operation should be canceled.</param>
        /// <returns>The transport connection options to be used.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/7.1.2/">OPC UA specification Part 6: Mappings, 7.1.2</seealso>
        public async Task<TransportConnectionOptions> OpenAsync(uint protocolVersion, TransportConnectionOptions localOptions, CancellationToken token)
        {
            var sendBuffer = new byte[MinBufferSize];
            var receiveBuffer = new byte[MinBufferSize];

            // send 'hello'.
            int count;
            var encoder = new BinaryEncoder(new MemoryStream(sendBuffer, 0, MinBufferSize, true, false));
            try
            {
                encoder.WriteUInt32(null, UaTcpMessageTypes.HELF);
                encoder.WriteUInt32(null, 0u);
                encoder.WriteUInt32(null, protocolVersion);
                encoder.WriteUInt32(null, localOptions.ReceiveBufferSize);
                encoder.WriteUInt32(null, localOptions.SendBufferSize);
                encoder.WriteUInt32(null, localOptions.MaxMessageSize);
                encoder.WriteUInt32(null, localOptions.MaxChunkCount);
                encoder.WriteString(null, Uri.ToString());
                count = encoder.Position;
                encoder.Position = 4;
                encoder.WriteUInt32(null, (uint)count);
                encoder.Position = count;

                await SendAsync(sendBuffer, 0, count, token).ConfigureAwait(false);
            }
            finally
            {
                encoder.Dispose();
            }

            // receive response
            count = await ReceiveAsync(receiveBuffer, 0, MinBufferSize, token).ConfigureAwait(false);
            if (count == 0)
            {
                throw new ObjectDisposedException("socket");
            }

            // decode 'ack' or 'err'.
            var decoder = new BinaryDecoder(new MemoryStream(receiveBuffer, 0, count, false, false));
            try
            {
                var type = decoder.ReadUInt32(null);
                var len = decoder.ReadUInt32(null);
                if (type == UaTcpMessageTypes.ACKF)
                {
                    var remoteProtocolVersion = decoder.ReadUInt32(null);
                    if (remoteProtocolVersion < protocolVersion)
                    {
                        throw new ServiceResultException(StatusCodes.BadProtocolVersionUnsupported);
                    }

                    var remoteOptions = new TransportConnectionOptions
                    {
                        SendBufferSize = decoder.ReadUInt32(null),
                        ReceiveBufferSize = decoder.ReadUInt32(null),
                        MaxMessageSize = decoder.ReadUInt32(null),
                        MaxChunkCount = decoder.ReadUInt32(null)
                    };

                    return remoteOptions;
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

                throw new InvalidOperationException($"{nameof(UaClientConnection)}.{nameof(OpenAsync)} received unexpected message type.");
            }
            finally
            {
                decoder.Dispose();
            }
        }

        /// <inheritdoc />
        public async Task SendAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            await Stream.WriteAsync(buffer, offset, count, token);
        }

        /// <inheritdoc />
        public async Task<int> ReceiveAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            int initialOffset = offset;
            int maxCount = count;
            int num;
            count = 8;
            while (count > 0)
            {
                try
                {
                    num = await Stream.ReadAsync(buffer, offset, count, token).ConfigureAwait(false);
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
                    num = await Stream.ReadAsync(buffer, offset, count, token).ConfigureAwait(false);
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
    }
}
