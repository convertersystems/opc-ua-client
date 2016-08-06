// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Workstation.ServiceModel.Ua.Channels;

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
        public static async Task<CreateSessionResponse> CreateSessionAsync(this IRequestChannel channel, CreateSessionRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            return (CreateSessionResponse)await channel.RequestAsync(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Activates a session.
        /// </summary>
        /// <param name="channel">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="ActivateSessionRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="ActivateSessionResponse"/>.</returns>
        public static async Task<ActivateSessionResponse> ActivateSessionAsync(this IRequestChannel channel, ActivateSessionRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            return (ActivateSessionResponse)await channel.RequestAsync(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Closes a session.
        /// </summary>
        /// <param name="channel">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="CloseSessionRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="CloseSessionResponse"/>.</returns>
        public static async Task<CloseSessionResponse> CloseSessionAsync(this IRequestChannel channel, CloseSessionRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            return (CloseSessionResponse)await channel.RequestAsync(request).ConfigureAwait(false);
        }
    }
}
