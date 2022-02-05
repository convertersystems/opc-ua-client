// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Workstation.ServiceModel.Ua;
using Workstation.ServiceModel.Ua.Channels;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// The UaApplication options.
    /// </summary>
    public class UaApplicationOptions : ClientSessionChannelOptions
    {
    }

    /// <summary>
    /// The <see cref="ClientSessionChannel"/> options.
    /// </summary>
    public class ClientSessionChannelOptions : ClientSecureChannelOptions
    {
        /// <summary>
        /// Gets the requested number of milliseconds that a session may be unused before being closed by the server.
        /// </summary>
        public double SessionTimeout { get; set; } = ClientSessionChannel.DefaultSessionTimeout;
    }

    /// <summary>
    /// The <see cref="ClientSecureChannel"/> options.
    /// </summary>
    public class ClientSecureChannelOptions : ClientTransportChannelOptions
    {
        /// <summary>
        /// Gets or sets the default number of milliseconds that may elapse before an operation is cancelled by the service.
        /// </summary>
        public uint TimeoutHint { get; set; } = ClientSecureChannel.DefaultTimeoutHint;

        /// <summary>
        /// Gets or sets the default diagnostics flags to be requested by the service.
        /// </summary>
        public uint DiagnosticsHint { get; set; } = ClientSecureChannel.DefaultDiagnosticsHint;
    }

    /// <summary>
    /// The <see cref="ClientTransportChannel"/> options.
    /// </summary>
    public class ClientTransportChannelOptions
    {
        /// <summary>
        /// Gets or sets the size of the receive buffer.
        /// </summary>
        public uint LocalReceiveBufferSize { get; set; } = ClientTransportChannel.DefaultBufferSize;

        /// <summary>
        /// Gets or sets the size of the send buffer.
        /// </summary>
        public uint LocalSendBufferSize { get; set; } = ClientTransportChannel.DefaultBufferSize;

        /// <summary>
        /// Gets or sets the maximum total size of a message.
        /// </summary>
        public uint LocalMaxMessageSize { get; set; } = ClientTransportChannel.DefaultMaxMessageSize;

        /// <summary>
        /// Gets or sets the maximum number of message chunks.
        /// </summary>
        public uint LocalMaxChunkCount { get; set; } = ClientTransportChannel.DefaultMaxChunkCount;
    }
}
