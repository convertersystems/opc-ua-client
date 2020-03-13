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
    public class UaApplicationOptions : UaTcpSessionChannelOptions
    {
    }

    /// <summary>
    /// The UaTcpSessionChannel options.
    /// </summary>
    public class UaTcpSessionChannelOptions : UaTcpSecureChannelOptions
    {
        /// <summary>
        /// Gets the requested number of milliseconds that a session may be unused before being closed by the server.
        /// </summary>
        public double SessionTimeout { get; set; } = UaTcpSessionChannel.DefaultSessionTimeout;
    }

    /// <summary>
    /// The UaTcpSessionChannel options.
    /// </summary>
   
    public class UaTcpSessionChannelReconnectParameter
    {
        public byte[] RemoteNonce { get; set; } = null;
        public NodeId AuthenticationToken { get; set; } = null;
        public NodeId SessionId { get; set; } = null;
    }


    /// <summary>
    /// The UaTcpSecureChannel options.
    /// </summary>
    public class UaTcpSecureChannelOptions : UaTcpTransportChannelOptions
    {
        /// <summary>
        /// Gets or sets the default number of milliseconds that may elapse before an operation is cancelled by the service.
        /// </summary>
        public uint TimeoutHint { get; set; } = UaTcpSecureChannel.DefaultTimeoutHint;

        /// <summary>
        /// Gets or sets the default diagnostics flags to be requested by the service.
        /// </summary>
        public uint DiagnosticsHint { get; set; } = UaTcpSecureChannel.DefaultDiagnosticsHint;
    }

    /// <summary>
    /// The UaTcpTransportChannel options.
    /// </summary>
    public class UaTcpTransportChannelOptions
    {
        /// <summary>
        /// Gets or sets the size of the receive buffer.
        /// </summary>
        public uint LocalReceiveBufferSize { get; set; } = UaTcpTransportChannel.DefaultBufferSize;

        /// <summary>
        /// Gets or sets the size of the send buffer.
        /// </summary>
        public uint LocalSendBufferSize { get; set; } = UaTcpTransportChannel.DefaultBufferSize;

        /// <summary>
        /// Gets or sets the maximum total size of a message.
        /// </summary>
        public uint LocalMaxMessageSize { get; set; } = UaTcpTransportChannel.DefaultMaxMessageSize;

        /// <summary>
        /// Gets or sets the maximum number of message chunks.
        /// </summary>
        public uint LocalMaxChunkCount { get; set; } = UaTcpTransportChannel.DefaultMaxChunkCount;
    }
}
