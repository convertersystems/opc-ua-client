// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Workstation.ServiceModel.Ua
{
    public static class AttributeServiceSet
    {
        /// <summary>
        /// Reads a list of Node attributes.
        /// </summary>
        /// <param name="client">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="ReadRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="ReadResponse"/>.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part4/5.10.2/">OPC UA specification Part 4: Services, 5.10.2</seealso>
        public static async Task<ReadResponse> ReadAsync(this IRequestChannel client, ReadRequest request, CancellationToken token = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return (ReadResponse)await client.RequestAsync(request, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Writes a list of Node attributes.
        /// </summary>
        /// <param name="client">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="WriteRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="WriteResponse"/>.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part4/5.10.4/">OPC UA specification Part 4: Services, 5.10.4</seealso>
        public static async Task<WriteResponse> WriteAsync(this IRequestChannel client, WriteRequest request, CancellationToken token = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return (WriteResponse)await client.RequestAsync(request, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Reads historical values or Events of one or more Nodes.
        /// </summary>
        /// <param name="client">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="HistoryReadRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="HistoryReadResponse"/>.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part4/5.10.3/">OPC UA specification Part 4: Services, 5.10.3</seealso>
        public static async Task<HistoryReadResponse> HistoryReadAsync(this IRequestChannel client, HistoryReadRequest request, CancellationToken token = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return (HistoryReadResponse)await client.RequestAsync(request, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Updates historical values or Events of one or more Nodes.
        /// </summary>
        /// <param name="client">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="HistoryUpdateRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="HistoryUpdateResponse"/>.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part4/5.10.5/">OPC UA specification Part 4: Services, 5.10.5</seealso>
        public static async Task<HistoryUpdateResponse> HistoryUpdateAsync(this IRequestChannel client, HistoryUpdateRequest request, CancellationToken token = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return (HistoryUpdateResponse)await client.RequestAsync(request, token).ConfigureAwait(false);
        }
    }
}
