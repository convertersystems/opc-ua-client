// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Prism.Events;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// A collection of items to be monitored by the OPC UA server.
    /// </summary>
    public class SubscriptionAdapter : IDisposable
    {
        private const uint PublishTimeoutHint = 120 * 1000; // 2 minutes
        private const uint DiagnosticsHint = (uint)DiagnosticFlags.None;
        private volatile bool isPublishing = false;
        private IDisposable token1;
        private IDisposable token2;
        private UaTcpSessionClient session;
        private ISubscription subscription;
        private uint subscriptionId;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionAdapter"/> class.
        /// </summary>
        /// <param name="session">The session client.</param>
        /// <param name="subscription">The ISubscription target.</param>
        public SubscriptionAdapter(UaTcpSessionClient session, ISubscription subscription)
        {
            this.session = session;
            this.subscription = subscription;
            this.subscription.Session = session;
            this.subscription.PropertyChanged += this.OnPropertyChanged;
            var publishEvent = this.session.GetEvent<PubSubEvent<PublishResponse>>();
            this.token1 = publishEvent.Subscribe(this.OnPublishResponse, publishEvent.SynchronizationContext != null ? ThreadOption.UIThread : ThreadOption.BackgroundThread, false, this.CanExecutePublishResponse);
            var stateChangedEvent = this.session.GetEvent<PubSubEvent<CommunicationState>>();
            this.token2 = stateChangedEvent.Subscribe(this.OnStateChanged, ThreadOption.BackgroundThread, false);
            this.OnStateChanged(this.session.State);
        }

        /// <summary>
        /// Disposes the subscription.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.subscription.PropertyChanged -= this.OnPropertyChanged;
                this.token1?.Dispose();
                this.token2?.Dispose();
            }
        }

        /// <summary>
        /// Receive StateChanged message.
        /// </summary>
        /// <param name="state">The service's CommunicationState.</param>
        public async void OnStateChanged(CommunicationState state)
        {
            if (state == CommunicationState.Opened)
            {
                try
                {
                    // create the subscription.
                    var subscriptionRequest = new CreateSubscriptionRequest
                    {
                        RequestedPublishingInterval = this.subscription.PublishingInterval,
                        RequestedMaxKeepAliveCount = this.subscription.KeepAliveCount,
                        RequestedLifetimeCount = Math.Max(this.subscription.LifetimeCount, 3 * this.subscription.KeepAliveCount),
                        PublishingEnabled = this.subscription.PublishingEnabled
                    };
                    var subscriptionResponse = await this.session.CreateSubscriptionAsync(subscriptionRequest).ConfigureAwait(false);
                    var id = this.subscriptionId = subscriptionResponse.SubscriptionId;

                    // add the items.
                    if (this.subscription.MonitoredItems.Count > 0)
                    {
                        var items = this.subscription.MonitoredItems.ToList();
                        var requests = items.Select(m => new MonitoredItemCreateRequest { ItemToMonitor = new ReadValueId { NodeId = m.NodeId, AttributeId = m.AttributeId, IndexRange = m.IndexRange }, MonitoringMode = m.MonitoringMode, RequestedParameters = new MonitoringParameters { ClientHandle = m.ClientId, DiscardOldest = m.DiscardOldest, QueueSize = m.QueueSize, SamplingInterval = m.SamplingInterval, Filter = m.Filter } }).ToArray();
                        var itemsRequest = new CreateMonitoredItemsRequest
                        {
                            SubscriptionId = id,
                            ItemsToCreate = requests,
                        };
                        var itemsResponse = await this.session.CreateMonitoredItemsAsync(itemsRequest).ConfigureAwait(false);
                        for (int i = 0; i < itemsResponse.Results.Length; i++)
                        {
                            var item = items[i];
                            var result = itemsResponse.Results[i];
                            item.ServerId = result.MonitoredItemId;
                            if (StatusCode.IsBad(result.StatusCode))
                            {
                                Trace.TraceError($"Subscription error response from MonitoredItemCreateRequest for {item.NodeId}. {StatusCodes.GetDefaultMessage(result.StatusCode)}");
                            }
                        }
                    }
                }
                catch (ServiceResultException ex)
                {
                    Trace.TraceError($"Subscription error creating subscription '{this.GetType().Name}'. {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Can execute PublishResponse message.
        /// </summary>
        /// <param name="response">The publish response.</param>
        /// <returns>True if this subscription can handle the publish response.</returns>
        public bool CanExecutePublishResponse(PublishResponse response)
        {
            if (response.SubscriptionId == this.subscriptionId)
            {
                response.MoreNotifications = false; // reset flag indicates message handled.
                return true;
            }
            return false;
        }

        /// <summary>
        /// Receive PublishResponse message.
        /// </summary>
        /// <param name="response">The publish response.</param>
        public void OnPublishResponse(PublishResponse response)
        {
            this.isPublishing = true;
            try
            {
                // loop thru all the notifications
                var nd = response.NotificationMessage.NotificationData;
                foreach (var n in nd)
                {
                    // if data change.
                    var dcn = n as DataChangeNotification;
                    if (dcn != null)
                    {
                        MonitoredItemBase item;
                        foreach (var min in dcn.MonitoredItems)
                        {
                            if (this.subscription.MonitoredItems.TryGetValueByClientId(min.ClientHandle, out item))
                            {
                                try
                                {
                                    item.Publish(this.subscription, min.Value);
                                }
                                catch (Exception ex)
                                {
                                    Trace.TraceError($"Subscription error publishing value for NodeId {item.NodeId}. {ex.Message}");
                                }
                            }
                        }

                        continue;
                    }

                    // if event.
                    var enl = n as EventNotificationList;
                    if (enl != null)
                    {
                        MonitoredItemBase item;
                        foreach (var efl in enl.Events)
                        {
                            if (this.subscription.MonitoredItems.TryGetValueByClientId(efl.ClientHandle, out item))
                            {
                                try
                                {
                                    item.Publish(this.subscription, efl.EventFields);
                                }
                                catch (Exception ex)
                                {
                                    Trace.TraceError($"Subscription error publishing event for NodeId {item.NodeId}. {ex.Message}");
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                this.isPublishing = false;
            }
        }

        /// <summary>
        /// Handles PropertyChanged event. If the property is associated with a MonitoredItem, writes the property value to the node of the server.
        /// </summary>
        /// <param name="sender">the sender.</param>
        /// <param name="e">the event.</param>
        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this.isPublishing || string.IsNullOrEmpty(e.PropertyName))
            {
                return;
            }

            if (e.PropertyName == nameof(ISubscription.PublishingEnabled))
            {
                var setPublishingModeRequest = new SetPublishingModeRequest
                {
                    PublishingEnabled = this.subscription.PublishingEnabled,
                    SubscriptionIds = new[] { this.subscriptionId }
                };
                this.session.SetPublishingModeAsync(setPublishingModeRequest)
                        .ContinueWith(
                            t =>
                            {
                                foreach (var ex in t.Exception.InnerExceptions)
                                {
                                    Trace.TraceWarning($"Subscription error setting publishing mode for subscription. {ex.Message}");
                                }
                            }, TaskContinuationOptions.OnlyOnFaulted);
                return;
            }

            MonitoredItemBase item;
            if (this.subscription.MonitoredItems.TryGetValueByName(e.PropertyName, out item))
            {
                var pi = item.Property;
                if (pi != null && pi.CanRead)
                {
                    var value = pi.GetValue(sender);
                    this.session.WriteAsync(new WriteRequest { NodesToWrite = new[] { new WriteValue { NodeId = item.NodeId, AttributeId = item.AttributeId, IndexRange = item.IndexRange, Value = value as DataValue ?? new DataValue(value) } } })
                        .ContinueWith(
                            t =>
                            {
                                foreach (var ex in t.Exception.InnerExceptions)
                                {
                                    Trace.TraceWarning($"Subscription error writing value for NodeId {item.NodeId}. {ex.Message}");
                                }
                            }, TaskContinuationOptions.OnlyOnFaulted);
                }
            }
        }
    }
}