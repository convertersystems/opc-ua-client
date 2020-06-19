// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Workstation.ServiceModel.Ua
{
    public static class NodeManagementServiceSet
    {
        /// <summary>
        /// Adds one or more Nodes into the AddressSpace hierarchy.
        /// </summary>
        /// <param name="channel">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="AddNodesRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="AddNodesResponse"/>.</returns>
        public static async Task<AddNodesResponse> AddNodesAsync(this IRequestChannel channel, AddNodesRequest request, CancellationToken token = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return (AddNodesResponse)await channel.RequestAsync(request, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds one or more References to one or more Nodes.
        /// </summary>
        /// <param name="channel">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="AddReferencesRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="AddReferencesResponse"/>.</returns>
        public static async Task<AddReferencesResponse> AddReferencesAsync(this IRequestChannel channel, AddReferencesRequest request, CancellationToken token = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return (AddReferencesResponse)await channel.RequestAsync(request, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes one or more Nodes from the AddressSpace.
        /// </summary>
        /// <param name="channel">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="DeleteNodesRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="DeleteNodesResponse"/>.</returns>
        public static async Task<DeleteNodesResponse> DeleteNodesAsync(this IRequestChannel channel, DeleteNodesRequest request, CancellationToken token = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return (DeleteNodesResponse)await channel.RequestAsync(request, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes one or more References of a Node.
        /// </summary>
        /// <param name="channel">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="DeleteReferencesRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="DeleteReferencesResponse"/>.</returns>
        public static async Task<DeleteReferencesResponse> DeleteReferencesAsync(this IRequestChannel channel, DeleteReferencesRequest request, CancellationToken token = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return (DeleteReferencesResponse)await channel.RequestAsync(request, token).ConfigureAwait(false);
        }
    }
}
