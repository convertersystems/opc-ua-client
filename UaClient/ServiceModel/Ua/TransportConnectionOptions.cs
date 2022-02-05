// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// The transport connection options.
    /// </summary>
    public class TransportConnectionOptions
    {
        public const uint DefaultBufferSize = 64 * 1024;
        public const uint DefaultMaxMessageSize = 16 * 1024 * 1024;
        public const uint DefaultMaxChunkCount = 4 * 1024;

        /// <summary>
        /// Gets or sets the size of the receive buffer.
        /// </summary>
        public uint ReceiveBufferSize { get; set; } = DefaultBufferSize;

        /// <summary>
        /// Gets or sets the size of the send buffer.
        /// </summary>
        public uint SendBufferSize { get; set; } = DefaultBufferSize;

        /// <summary>
        /// Gets or sets the maximum total size of a message.
        /// </summary>
        public uint MaxMessageSize { get; set; } = DefaultMaxMessageSize;

        /// <summary>
        /// Gets or sets the maximum number of message chunks.
        /// </summary>
        public uint MaxChunkCount { get; set; } = DefaultMaxChunkCount;
    }
}
