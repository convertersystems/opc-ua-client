// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Workstation.ServiceModel.Ua
{
    public static class ViewServiceSet
    {
        /// <summary>
        /// Discovers the References of a specified Node.
        /// </summary>
        /// <param name="channel">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="BrowseRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="BrowseResponse"/>.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part4/5.8.2/">OPC UA specification Part 4: Services, 5.8.2</seealso>
        public static async Task<BrowseResponse> BrowseAsync(this IRequestChannel channel, BrowseRequest request, CancellationToken token = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return (BrowseResponse)await channel.RequestAsync(request, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Requests the next set of Browse responses, when the information is too large to be sent in a single response.
        /// </summary>
        /// <param name="channel">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="BrowseNextRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="BrowseNextResponse"/>.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part4/5.8.3/">OPC UA specification Part 4: Services, 5.8.3</seealso>
        public static async Task<BrowseNextResponse> BrowseNextAsync(this IRequestChannel channel, BrowseNextRequest request, CancellationToken token = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return (BrowseNextResponse)await channel.RequestAsync(request, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Translates one or more browse paths to NodeIds.
        /// </summary>
        /// <param name="channel">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="TranslateBrowsePathsToNodeIdsRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="TranslateBrowsePathsToNodeIdsResponse"/>.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part4/5.8.4/">OPC UA specification Part 4: Services, 5.8.4</seealso>
        public static async Task<TranslateBrowsePathsToNodeIdsResponse> TranslateBrowsePathsToNodeIdsAsync(this IRequestChannel channel, TranslateBrowsePathsToNodeIdsRequest request, CancellationToken token = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return (TranslateBrowsePathsToNodeIdsResponse)await channel.RequestAsync(request, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Registers the Nodes that will be accessed repeatedly (e.g. Write, Call).
        /// </summary>
        /// <param name="channel">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="RegisterNodesRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="RegisterNodesResponse"/>.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part4/5.8.5/">OPC UA specification Part 4: Services, 5.8.5</seealso>
        public static async Task<RegisterNodesResponse> RegisterNodesAsync(this IRequestChannel channel, RegisterNodesRequest request, CancellationToken token = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return (RegisterNodesResponse)await channel.RequestAsync(request, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Unregisters NodeIds that have been obtained via the RegisterNodes service.
        /// </summary>
        /// <param name="channel">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="UnregisterNodesRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="UnregisterNodesResponse"/>.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part4/5.8.6/">OPC UA specification Part 4: Services, 5.8.6</seealso>
        public static async Task<UnregisterNodesResponse> UnregisterNodesAsync(this IRequestChannel channel, UnregisterNodesRequest request, CancellationToken token = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return (UnregisterNodesResponse)await channel.RequestAsync(request, token).ConfigureAwait(false);
        }
    }
}
