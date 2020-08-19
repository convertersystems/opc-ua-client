// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Workstation.ServiceModel.Ua
{
    public static class MonitoredItemServiceSet
    {
        /// <summary>
        /// Creates and adds one or more MonitoredItems to a Subscription.
        /// </summary>
        /// <param name="channel">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="CreateMonitoredItemsRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="CreateMonitoredItemsResponse"/>.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part4/5.12.2/">OPC UA specification Part 4: Services, 5.12.2</seealso>
        public static async Task<CreateMonitoredItemsResponse> CreateMonitoredItemsAsync(this IRequestChannel channel, CreateMonitoredItemsRequest request, CancellationToken token = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return (CreateMonitoredItemsResponse)await channel.RequestAsync(request, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Modifies MonitoredItems of a Subscription.
        /// </summary>
        /// <param name="channel">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="ModifyMonitoredItemsRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="ModifyMonitoredItemsResponse"/>.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part4/5.12.3/">OPC UA specification Part 4: Services, 5.12.3</seealso>
        public static async Task<ModifyMonitoredItemsResponse> ModifyMonitoredItemsAsync(this IRequestChannel channel, ModifyMonitoredItemsRequest request, CancellationToken token = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return (ModifyMonitoredItemsResponse)await channel.RequestAsync(request, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Sets the monitoring mode for one or more MonitoredItems of a Subscription.
        /// </summary>
        /// <param name="channel">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="SetMonitoringModeRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="SetMonitoringModeResponse"/>.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part4/5.12.4/">OPC UA specification Part 4: Services, 5.12.4</seealso>
        public static async Task<SetMonitoringModeResponse> SetMonitoringModeAsync(this IRequestChannel channel, SetMonitoringModeRequest request, CancellationToken token = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return (SetMonitoringModeResponse)await channel.RequestAsync(request, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates and deletes triggering links for a triggering item.
        /// </summary>
        /// <param name="channel">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="SetTriggeringRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="SetTriggeringResponse"/>.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part4/5.12.5/">OPC UA specification Part 4: Services, 5.12.5</seealso>
        public static async Task<SetTriggeringResponse> SetTriggeringAsync(this IRequestChannel channel, SetTriggeringRequest request, CancellationToken token = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return (SetTriggeringResponse)await channel.RequestAsync(request, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Removes one or more MonitoredItems of a Subscription.
        /// </summary>
        /// <param name="channel">A instance of <see cref="IRequestChannel"/>.</param>
        /// <param name="request">A <see cref="DeleteMonitoredItemsRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns a <see cref="DeleteMonitoredItemsResponse"/>.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part4/5.12.6/">OPC UA specification Part 4: Services, 5.12.6</seealso>
        public static async Task<DeleteMonitoredItemsResponse> DeleteMonitoredItemsAsync(this IRequestChannel channel, DeleteMonitoredItemsRequest request, CancellationToken token = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return (DeleteMonitoredItemsResponse)await channel.RequestAsync(request, token).ConfigureAwait(false);
        }

    }
}
