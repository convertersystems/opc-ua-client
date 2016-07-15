// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

namespace Workstation.ServiceModel.Ua
{
    public interface ISubscription
    {
        /// <summary>
        /// Gets the publishing interval.
        /// </summary>
        double PublishingInterval { get; }

        /// <summary>
        /// Gets the number of PublishingIntervals before the server should return an empty Publish response.
        /// </summary>
        uint KeepAliveCount { get; }

        /// <summary>
        /// Gets the number of PublishingIntervals before the server should delete the subscription.
        /// </summary>
        uint LifetimeCount { get; }

        /// <summary>
        /// Gets the maximum number of notifications per publish request.
        /// </summary>
        uint MaxNotificationsPerPublish { get; }

        /// <summary>
        /// Gets the priority assigned to subscription.
        /// </summary>
        byte Priority { get; }

        /// <summary>
        /// Gets the collection of items to monitor.
        /// </summary>
        ReadOnlyCollection<MonitoredItem> MonitoredItems { get; }

        /// <summary>
        /// Gets the session with the server.
        /// </summary>
        UaTcpSessionClient Session { get; }

        /// <summary>
        /// Gets the identifier assigned by the server.
        /// </summary>
        uint Id { get; }

        /// <summary>
        /// Receive StateChanged message.
        /// </summary>
        /// <param name="state">The session's CommunicationState.</param>
        void OnStateChanged(CommunicationState state);

        /// <summary>
        /// Receive PublishResponse message.
        /// </summary>
        /// <param name="response">The publish response.</param>
        void OnPublishResponse(PublishResponse response);
    }
}
