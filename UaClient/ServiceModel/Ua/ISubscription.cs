// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;

namespace Workstation.ServiceModel.Ua
{
    public interface ISubscription : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets or sets the session with the server.
        /// </summary>
        UaTcpSessionClient Session { get; set; }

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
        /// Gets a value indicating whether publishing is enabled.
        /// </summary>
        bool PublishingEnabled { get; }

        /// <summary>
        /// Gets the collection of items to monitor.
        /// </summary>
        MonitoredItemCollection MonitoredItems { get; }
    }
}
