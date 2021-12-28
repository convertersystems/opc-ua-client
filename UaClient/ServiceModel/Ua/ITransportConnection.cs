// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// The transport connection interface is used to support different
    /// transport protocols in the transport channel implementation.
    /// </summary>
    /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/4/">OPC UA specification Part 6: Mappings, 4</seealso>
    public interface ITransportConnection : IAsyncDisposable
    {
        /// <summary>
        /// Opens the connection. This includes the hello message handshake.
        /// </summary>
        /// <remakes>
        /// The real network may already be opened at a previous point. The
        /// connection is closed with <see cref="IAsyncDisposable.DisposeAsync"/>
        /// method.
        /// </remakes>
        /// <param name="protocolVersion">The protocol version.</param>
        /// <param name="localOptions">The requested transport connection options.</param>
        /// <param name="token">A cancellation token used to propagate notification that this operation should be canceled.</param>
        /// <returns>The transport connection options to be used.</returns>
        Task<TransportConnectionOptions> OpenAsync(uint protocolVersion, TransportConnectionOptions localOptions, CancellationToken token);

        /// <summary>
        /// Sends content from the buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The starting offset.</param>
        /// <param name="count">The count of bytes to be send.</param>
        /// <param name="token">A cancellation token used to propagate notification that this operation should be canceled.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SendAsync(byte[] buffer, int offset, int count, CancellationToken token);
        
        /// <summary>
        /// Receive content into the buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The starting offset.</param>
        /// <param name="count">The count of bytes to be received.</param>
        /// <param name="token">A cancellation token used to propagate notification that this operation should be canceled.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task<int> ReceiveAsync(byte[] buffer, int offset, int count, CancellationToken token);
    }
}
