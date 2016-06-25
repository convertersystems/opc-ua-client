// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// Specifies the Subscription that will be created for this class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SubscriptionAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionAttribute"/> class.
        /// </summary>
        /// <param name="publishingInterval">the publishing interval.</param>
        /// <param name="keepAliveCount">the number of PublishingIntervals before the server should return an empty Publish response.</param>
        /// <param name="lifetimeCount">the number of PublishingIntervals before the server should delete the subscription.</param>
        /// <param name="maxNotificationsPerPublish">the maximum number of notifications per publish response.</param>
        /// <param name="priority">the priority assigned to subscription.</param>
        public SubscriptionAttribute(double publishingInterval = 1000f, uint keepAliveCount = 10u, uint lifetimeCount = 0u, uint maxNotificationsPerPublish = 0u, byte priority = 0)
        {
            this.PublishingInterval = publishingInterval;
            this.KeepAliveCount = keepAliveCount;
            this.LifetimeCount = lifetimeCount;
            this.MaxNotificationsPerPublish = maxNotificationsPerPublish;
            this.Priority = priority;
        }

        /// <summary>
        /// Gets the publishing interval.
        /// </summary>
        public double PublishingInterval { get; }

        /// <summary>
        /// Gets the number of PublishingIntervals before the server should return an empty Publish response.
        /// </summary>
        public uint KeepAliveCount { get; }

        /// <summary>
        /// Gets the number of PublishingIntervals before the server should delete the subscription.
        /// </summary>
        public uint LifetimeCount { get; }

        /// <summary>
        /// Gets the maximum number of notifications per publish response.
        /// </summary>
        public uint MaxNotificationsPerPublish { get; }

        /// <summary>
        /// Gets the priority assigned to subscription.
        /// </summary>
        public byte Priority { get; }
    }
}
