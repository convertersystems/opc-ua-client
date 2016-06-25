// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// A collection of items to be monitored by the OPC UA server.
    /// </summary>
    public class Subscription : ISubscription, INotifyPropertyChanged
    {
        private const double DefaultPublishingInterval = 1000f;
        private const uint DefaultKeepaliveCount = 10;
        private const uint PublishTimeoutHint = 120 * 1000; // 2 minutes
        private const uint DiagnosticsHint = (uint)DiagnosticFlags.None;
        protected static readonly MetroLog.ILogger Log = MetroLog.LogManagerFactory.DefaultLogManager.GetLogger<Subscription>();
        private bool isPublishing = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Subscription"/> class.
        /// </summary>
        /// <param name="session">A session.</param>
        public Subscription(UaTcpSessionService session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            this.Session = session;
            this.PublishingInterval = DefaultPublishingInterval;
            this.KeepAliveCount = DefaultKeepaliveCount;
            this.LifetimeCount = 0; // use session lifetime
            this.MaxNotificationsPerPublish = 0; // no limit
            this.PublishingEnabled = true;
            this.MonitoredItems = new MonitoredItemCollection();
            this.PropertyChanged += this.OnPropertyChanged;
            var typeInfo = this.GetType().GetTypeInfo();

            foreach (var propertyInfo in typeInfo.DeclaredProperties)
            {
                var itemAttribute = propertyInfo.GetCustomAttribute<MonitoredItemAttribute>();
                if (itemAttribute == null)
                {
                    continue;
                }

                var item = new MonitoredItem { Property = propertyInfo, NodeId = !string.IsNullOrEmpty(itemAttribute.NodeId) ? NodeId.Parse(itemAttribute.NodeId) : null, IndexRange = itemAttribute.IndexRange, AttributeId = itemAttribute.AttributeId, SamplingInterval = itemAttribute.SamplingInterval, QueueSize = itemAttribute.QueueSize, DiscardOldest = itemAttribute.DiscardOldest };
                if (itemAttribute.AttributeId == AttributeIds.Value && (itemAttribute.DataChangeTrigger != DataChangeTrigger.StatusValue || itemAttribute.DeadbandType != DeadbandType.None))
                {
                    item.Filter = new DataChangeFilter() { Trigger = itemAttribute.DataChangeTrigger, DeadbandType = (uint)itemAttribute.DeadbandType, DeadbandValue = itemAttribute.DeadbandValue };
                }
                else if (itemAttribute.AttributeId == AttributeIds.EventNotifier)
                {
                    item.Filter = new EventFilter() { SelectClauses = EventHelper.GetSelectClauses(propertyInfo.PropertyType) };
                }

                this.MonitoredItems.Add(item);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the publishing interval.
        /// </summary>
        public double PublishingInterval { get; set; }

        /// <summary>
        /// Gets or sets the number of PublishingIntervals before the server should return an empty Publish response.
        /// </summary>
        public uint KeepAliveCount { get; set; }

        /// <summary>
        /// Gets or sets the number of PublishingIntervals before the server should delete the subscription.
        /// </summary>
        public uint LifetimeCount { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of notifications per publish request.
        /// </summary>
        public uint MaxNotificationsPerPublish { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether whether publishing is enabled.
        /// </summary>
        public bool PublishingEnabled { get; set; }

        /// <summary>
        /// Gets or sets the priority assigned to subscription.
        /// </summary>
        public byte Priority { get; set; }

        /// <summary>
        /// Gets or sets the collection of items to monitor.
        /// </summary>
        public MonitoredItemCollection MonitoredItems { get; set; }

        /// <summary>
        /// Gets or sets the identifier assigned by the server.
        /// </summary>
        public uint Id { get; set; }

        /// <summary>
        /// Gets the UaTcpSessionService.
        /// </summary>
        public UaTcpSessionService Session { get; private set; }

        /// <summary>
        /// Receive PublishResponse message.
        /// </summary>
        /// <param name="response">The publish response.</param>
        /// <returns>True, if event was handled, else false.</returns>
        public bool OnPublishResponse(PublishResponse response)
        {
            if (response.SubscriptionId != this.Id)
            {
                return false;
            }

            try
            {
                this.isPublishing = true;

                // loop thru all the notifications
                var nd = response.NotificationMessage.NotificationData;
                foreach (var n in nd)
                {
                    // if data change.
                    var dcn = n as DataChangeNotification;
                    if (dcn != null)
                    {
                        MonitoredItem item;
                        foreach (var min in dcn.MonitoredItems)
                        {
                            try
                            {
                                item = this.MonitoredItems[min.ClientHandle];
                                item.Publish(this, min.Value);
                            }
                            catch (Exception ex)
                            {
                                Log.Warn($"Error publishing value for ClientId {min.ClientHandle}. {ex.Message}");
                            }
                        }

                        continue;
                    }

                    // if event.
                    var enl = n as EventNotificationList;
                    if (enl != null)
                    {
                        MonitoredItem item;
                        foreach (var efl in enl.Events)
                        {
                            try
                            {
                                item = this.MonitoredItems[efl.ClientHandle];
                                item.Publish(this, efl.EventFields);
                            }
                            catch (Exception ex)
                            {
                                Log.Warn($"Error publishing event for ClientId {efl.ClientHandle}. {ex.Message}");
                            }
                        }
                    }
                }

                return true;
            }
            finally
            {
                this.isPublishing = false;
            }
        }

        protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            this.NotifyPropertyChanged(propertyName);
            return true;
        }

        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Handles PropertyChanged event. If the property is associated with a MonitoredItem, then writes the property value to the node of the server.
        /// </summary>
        /// <param name="sender">the sender.</param>
        /// <param name="e">the event.</param>
        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this.isPublishing || string.IsNullOrEmpty(e.PropertyName))
            {
                return;
            }

            MonitoredItem item;
            if (this.MonitoredItems.TryGetValueByName(e.PropertyName, out item))
            {
                var pi = item.Property;
                if (pi != null && pi.CanRead)
                {
                    var value = pi.GetValue(sender);
                    this.Session.WriteAsync(new WriteRequest { NodesToWrite = new[] { new WriteValue { NodeId = item.NodeId, AttributeId = item.AttributeId, IndexRange = item.IndexRange, Value = value as DataValue ?? new DataValue(value) } } })
                        .ContinueWith(
                            t =>
                            {
                                foreach (var ex in t.Exception.InnerExceptions)
                                {
                                    Log.Warn($"Error writing value for NodeId {item.NodeId}. {ex.Message}");
                                }
                            }, TaskContinuationOptions.OnlyOnFaulted);
                }
            }
        }
    }
}