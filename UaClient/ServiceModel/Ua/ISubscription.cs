// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Workstation.ServiceModel.Ua
{
    public interface ISubscription
    {
        /// <summary>
        /// Gets or sets the publishing interval.
        /// </summary>
        double PublishingInterval { get; set; }

        /// <summary>
        /// Gets or sets the number of PublishingIntervals before the server should return an empty Publish response.
        /// </summary>
        uint KeepAliveCount { get; set; }

        /// <summary>
        /// Gets or sets the number of PublishingIntervals before the server should delete the subscription.
        /// </summary>
        uint LifetimeCount { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of notifications per publish request.
        /// </summary>
        uint MaxNotificationsPerPublish { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether whether publishing is enabled.
        /// </summary>
        bool PublishingEnabled { get; set; }

        /// <summary>
        /// Gets or sets the priority assigned to subscription.
        /// </summary>
        byte Priority { get; set; }

        /// <summary>
        /// Gets or sets the collection of items to monitor.
        /// </summary>
        MonitoredItemCollection MonitoredItems { get; set; }

        /// <summary>
        /// Gets or sets the session with the server.
        /// </summary>
        ISessionClient Session { get; set; }

        /// <summary>
        /// Gets or sets the identifier assigned by the server.
        /// </summary>
        uint Id { get; set; }

        /// <summary>
        /// Receive PublishResponse message.
        /// </summary>
        /// <param name="response">The publish response.</param>
        /// <returns>True, if event was handled, else false.</returns>
        bool OnPublishResponse(PublishResponse response);

    }
}
