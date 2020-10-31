// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Workstation.ServiceModel.Ua
{
    public static class QueryServiceSet
    {
        /// <summary>
        /// Issues a Query request to a View.
        /// </summary>
        /// <param name="channel">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="QueryFirstRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="QueryFirstResponse"/>.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part4/5.9.3/">OPC UA specification Part 4: Services, 5.9.3</seealso>
        public static async Task<QueryFirstResponse> QueryFirstAsync(this IRequestChannel channel, QueryFirstRequest request, CancellationToken token = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return (QueryFirstResponse)await channel.RequestAsync(request, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Requests the next set of Query responses, when the information is too large to be sent in a single response.
        /// </summary>
        /// <param name="channel">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="QueryNextRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="QueryNextResponse"/>.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part4/5.9.4/">OPC UA specification Part 4: Services, 5.9.4</seealso>
        public static async Task<QueryNextResponse> QueryNextAsync(this IRequestChannel channel, QueryNextRequest request, CancellationToken token = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return (QueryNextResponse)await channel.RequestAsync(request, token).ConfigureAwait(false);
        }
    }
}
