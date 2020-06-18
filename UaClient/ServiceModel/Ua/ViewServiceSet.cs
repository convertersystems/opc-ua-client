// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

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
