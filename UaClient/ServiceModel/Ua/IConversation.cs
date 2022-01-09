// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// The conversation interface is used to support different
    /// security protocols in the secure channel implementation.
    /// </summary>
    /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/4/">OPC UA specification Part 6: Mappings, 4</seealso>
    public interface IConversation
    {
        /// <summary>
        /// The channel ID. 
        /// </summary>
        uint ChannelId { get; set; }

        /// <summary>
        /// The token ID.
        /// </summary>
        uint TokenId { get; set; }

        /// <summary>
        /// The remote nounce.
        /// </summary>
        byte[]? RemoteNonce { get; set; }

        /// <summary>
        /// This will replace the current local nounce, with the next local
        /// nounce.
        /// </summary>
        /// <returns>The next nounce.</returns>
        byte[]? GetNextNonce();

        /// <summary>
        /// Encrypts the content given by the <paramref name="bodyStream"/> stream
        /// into message chunks that are propagated further by the
        /// <paramref name="consume"/> delegate.
        /// </summary>
        /// <param name="bodyStream">The content to be encrypted.</param>
        /// <param name="messageType">The message type, <see cref="UaTcpMessageTypes"/>.</param>
        /// <param name="requestHandle">The request handle.</param>
        /// <param name="consume">The delegate to consume the encrypted chunks.</param>
        /// <param name="token">A cancellation token used to propagate notification that this operation should be canceled.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task EncryptMessageAsync(Stream bodyStream, uint messageType, uint requestHandle, Func<byte[], int, int, CancellationToken, Task> consume, CancellationToken token);
        
        /// <summary>
        /// Decrypts the content received by the <paramref name="receive"/>
        /// delegate into the <paramref name="bodyStream"/> stream.
        /// </summary>
        /// <param name="bodyStream">The stream to recieve the decrypted content.</param>
        /// <param name="receive">The delgate to receive the chunks.</param>
        /// <param name="token">A cancellation token used to propagate notification that this operation should be canceled.</param>
        /// <returns>The (last) message type and request handle.</returns>
        Task<(uint messageType, uint requestHandle)> DecryptMessageAsync(Stream bodyStream, Func<byte[], int, int, CancellationToken, Task<int>> receive, CancellationToken token);
    }
}
