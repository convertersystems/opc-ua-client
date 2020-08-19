// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Workstation.ServiceModel.Ua
{
    public static class SessionServiceSet
    {
        /// <summary>
        /// Creates a Session.
        /// </summary>
        /// <param name="channel">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="CreateSessionRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="CreateSessionResponse"/>.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part4/5.6.2/">OPC UA specification Part 4: Services, 5.6.2</seealso>
        internal static async Task<CreateSessionResponse> CreateSessionAsync(this IRequestChannel channel, CreateSessionRequest request, CancellationToken token = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return (CreateSessionResponse)await channel.RequestAsync(request, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Activates a session.
        /// </summary>
        /// <param name="channel">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="ActivateSessionRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="ActivateSessionResponse"/>.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part4/5.6.3/">OPC UA specification Part 4: Services, 5.6.3</seealso>
        internal static async Task<ActivateSessionResponse> ActivateSessionAsync(this IRequestChannel channel, ActivateSessionRequest request, CancellationToken token = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return (ActivateSessionResponse)await channel.RequestAsync(request, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Closes a session.
        /// </summary>
        /// <param name="channel">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="CloseSessionRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="CloseSessionResponse"/>.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part4/5.6.4/">OPC UA specification Part 4: Services, 5.6.4</seealso>
        internal static async Task<CloseSessionResponse> CloseSessionAsync(this IRequestChannel channel, CloseSessionRequest request, CancellationToken token = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return (CloseSessionResponse)await channel.RequestAsync(request, token).ConfigureAwait(false);
        }
    }
}
