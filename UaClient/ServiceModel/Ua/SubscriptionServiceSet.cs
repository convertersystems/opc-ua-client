// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Workstation.ServiceModel.Ua.Channels;

namespace Workstation.ServiceModel.Ua
{
    public static class SubscriptionServiceSet
    {
        /// <summary>
        /// Creates a Subscription.
        /// </summary>
        /// <param name="channel">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="CreateSubscriptionRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="CreateSubscriptionResponse"/>.</returns>
        public static async Task<CreateSubscriptionResponse> CreateSubscriptionAsync(this IRequestChannel channel, CreateSubscriptionRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            return (CreateSubscriptionResponse)await channel.RequestAsync(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Modifies a Subscription.
        /// </summary>
        /// <param name="channel">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="ModifySubscriptionRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="ModifySubscriptionResponse"/>.</returns>
        public static async Task<ModifySubscriptionResponse> ModifySubscriptionAsync(this IRequestChannel channel, ModifySubscriptionRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            return (ModifySubscriptionResponse)await channel.RequestAsync(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Enables sending of Notifications on one or more Subscriptions.
        /// </summary>
        /// <param name="channel">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="SetPublishingModeRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="SetPublishingModeResponse"/>.</returns>
        public static async Task<SetPublishingModeResponse> SetPublishingModeAsync(this IRequestChannel channel, SetPublishingModeRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            return (SetPublishingModeResponse)await channel.RequestAsync(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Requests the Server to return a NotificationMessage or a keep-alive Message.
        /// </summary>
        /// <param name="channel">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="PublishRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="PublishResponse"/>.</returns>
        public static async Task<PublishResponse> PublishAsync(this IRequestChannel channel, PublishRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            return (PublishResponse)await channel.RequestAsync(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Requests the Server to republish a NotificationMessage from its retransmission queue.
        /// </summary>
        /// <param name="channel">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="RepublishRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="RepublishResponse"/>.</returns>
        public static async Task<RepublishResponse> RepublishAsync(this IRequestChannel channel, RepublishRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            return (RepublishResponse)await channel.RequestAsync(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Transfers a Subscription and its MonitoredItems from one Session to another.
        /// </summary>
        /// <param name="channel">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="TransferSubscriptionsRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="TransferSubscriptionsResponse"/>.</returns>
        public static async Task<TransferSubscriptionsResponse> TransferSubscriptionsAsync(this IRequestChannel channel, TransferSubscriptionsRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            return (TransferSubscriptionsResponse)await channel.RequestAsync(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes one or more Subscriptions.
        /// </summary>
        /// <param name="channel">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="DeleteSubscriptionsRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="DeleteSubscriptionsResponse"/>.</returns>
        public static async Task<DeleteSubscriptionsResponse> DeleteSubscriptionsAsync(this IRequestChannel channel, DeleteSubscriptionsRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            return (DeleteSubscriptionsResponse)await channel.RequestAsync(request).ConfigureAwait(false);
        }
    }
}
