// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Workstation.ServiceModel.Ua
{
    public static class MethodServiceSet
    {
        /// <summary>
        /// Calls (invokes) a list of Methods.
        /// </summary>
        /// <param name="channel">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="CallRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="CallResponse"/>.</returns>
        public static async Task<CallResponse> CallAsync(this IRequestChannel channel, CallRequest request, CancellationToken token = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return (CallResponse)await channel.RequestAsync(request, token).ConfigureAwait(false);
        }

    }
}
