// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// Specifies the Subscription that will be created for this viewmodel.
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
        /// <param name="publishingEnabled">whether publishing is enabled.</param>
        public SubscriptionAttribute(double publishingInterval = 1000f, uint keepAliveCount = 10, uint lifetimeCount = 0, bool publishingEnabled = true)
        {
            PublishingInterval = publishingInterval;
            KeepAliveCount = keepAliveCount;
            LifetimeCount = lifetimeCount;
            PublishingEnabled = publishingEnabled;
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
        /// Gets a value indicating whether publishing is enabled.
        /// </summary>
        public bool PublishingEnabled { get; }
    }
}