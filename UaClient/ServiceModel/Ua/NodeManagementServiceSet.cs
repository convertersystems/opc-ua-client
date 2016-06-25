// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Workstation.ServiceModel.Ua
{
    public static class NodeManagementServiceSet
    {
        /// <summary>
        /// Adds one or more Nodes into the AddressSpace hierarchy.
        /// </summary>
        /// <param name="client">A instance of <see cref="ISessionClient"/>.</param>
        /// <param name="request">A <see cref="AddNodesRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="AddNodesResponse"/>.</returns>
        public static async Task<AddNodesResponse> AddNodesAsync(this ISessionClient client, AddNodesRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            return (AddNodesResponse)await client.RequestAsync(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds one or more References to one or more Nodes.
        /// </summary>
        /// <param name="client">A instance of <see cref="ISessionClient"/>.</param>
        /// <param name="request">A <see cref="AddReferencesRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="AddReferencesResponse"/>.</returns>
        public static async Task<AddReferencesResponse> AddReferencesAsync(this ISessionClient client, AddReferencesRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            return (AddReferencesResponse)await client.RequestAsync(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes one or more Nodes from the AddressSpace.
        /// </summary>
        /// <param name="client">A instance of <see cref="ISessionClient"/>.</param>
        /// <param name="request">A <see cref="DeleteNodesRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="DeleteNodesResponse"/>.</returns>
        public static async Task<DeleteNodesResponse> DeleteNodesAsync(this ISessionClient client, DeleteNodesRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            return (DeleteNodesResponse)await client.RequestAsync(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes one or more References of a Node.
        /// </summary>
        /// <param name="client">A instance of <see cref="ISessionClient"/>.</param>
        /// <param name="request">A <see cref="DeleteReferencesRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="DeleteReferencesResponse"/>.</returns>
        public static async Task<DeleteReferencesResponse> DeleteReferencesAsync(this ISessionClient client, DeleteReferencesRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            return (DeleteReferencesResponse)await client.RequestAsync(request).ConfigureAwait(false);
        }
    }
}
