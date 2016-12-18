// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// A collection of items to be monitored by the OPC UA server.
    /// </summary>
    public class Subscription : IDisposable
    {
        private const uint PublishTimeoutHint = 120 * 1000; // 2 minutes
        private const uint DiagnosticsHint = (uint)DiagnosticFlags.None;

        private static ConditionalWeakTable<object, Subscription> attachedSubscriptions = new ConditionalWeakTable<object, Subscription>();

        private readonly ILogger logger;
        private volatile bool isPublishing = false;
        private WeakReference subscriptionRef;
        private UaTcpSessionClient session;
        private bool publishingEnabled = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="Subscription"/> class.
        /// </summary>
        /// <param name="session">The session client.</param>
        /// <param name="target">The target model.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public Subscription(UaTcpSessionClient session, object target, ILoggerFactory loggerFactory = null)
        {
            this.session = session;
            subscriptionRef = new WeakReference(target);
            logger = loggerFactory?.CreateLogger<Subscription>();

            // get values from [Subscription] attribute.
            var typeInfo = target.GetType().GetTypeInfo();
            var sa = typeInfo.GetCustomAttribute<SubscriptionAttribute>();
            if (sa != null)
            {
                PublishingInterval = sa.PublishingInterval;
                KeepAliveCount = sa.KeepAliveCount;
                LifetimeCount = sa.LifetimeCount;
                PublishingEnabled = sa.PublishingEnabled;
                MonitoredItems = new MonitoredItemCollection(target);
            }

            // register for property change.
            var inpc = target as INotifyPropertyChanged;
            if (inpc != null)
            {
                inpc.PropertyChanged += OnPropertyChanged;
            }

            // store this in the shared attached subscriptions list
            attachedSubscriptions.Remove(target);
            attachedSubscriptions.Add(target, this);
        }

        /// <summary>
        /// Gets the publishing interval.
        /// </summary>
        public double PublishingInterval { get; } = 1000f;

        /// <summary>
        /// Gets the number of PublishingIntervals before the server should return an empty Publish response.
        /// </summary>
        public uint KeepAliveCount { get; } = 10u;

        /// <summary>
        /// Gets the number of PublishingIntervals before the server should delete the subscription.
        /// </summary>
        public uint LifetimeCount { get; } = 0u;

        /// <summary>
        /// Gets or sets a value indicating whether publishing is enabled.
        /// </summary>
        public bool PublishingEnabled
        {
            get { return publishingEnabled; }

            set
            {
                if (publishingEnabled != value)
                {
                    publishingEnabled = value;
                    if (session.State == CommunicationState.Opened && SubscriptionId != 0u)
                    {
                        var request = new SetPublishingModeRequest
                        {
                            SubscriptionIds = new[] { SubscriptionId },
                            PublishingEnabled = value
                        };
                        session.SetPublishingModeAsync(request)
                            .ContinueWith(
                                t => Logger?.LogError("Error setting publishing mode for subscription."),
                                TaskContinuationOptions.OnlyOnFaulted);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the collection of items to monitor.
        /// </summary>
        public MonitoredItemCollection MonitoredItems { get; } = new MonitoredItemCollection();

        /// <summary>
        /// Gets the UaTcpSessionClient.
        /// </summary>
        public UaTcpSessionClient Session => session;

        /// <summary>
        /// Gets the target.
        /// </summary>
        public object Target => subscriptionRef.Target;

        /// <summary>
        /// Gets the SubscriptionId assigned by the server.
        /// </summary>
        public uint SubscriptionId { get; internal set; }

        /// <summary>
        /// Gets the current logger
        /// </summary>
        protected virtual ILogger Logger => logger;

        /// <summary>
        /// Gets the <see cref="Subscription"/> attached to this model.
        /// </summary>
        /// <param name="model">the model.</param>
        /// <returns>Returns the attached <see cref="Subscription"/> or null.</returns>
        public static Subscription FromModel(object model)
        {
            Subscription subscription;
            if (attachedSubscriptions.TryGetValue(model, out subscription))
            {
                return subscription;
            }
            return null;
        }

        /// <summary>
        /// Disposes the subscription.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                var target = Target;
                if (target != null)
                {
                    attachedSubscriptions.Remove(target);
                    var inpc = Target as INotifyPropertyChanged;
                    if (inpc != null)
                    {
                        inpc.PropertyChanged -= OnPropertyChanged;
                    }
                }
            }
        }

        /// <summary>
        /// Handles PublishResponse message.
        /// </summary>
        /// <param name="response">The publish response.</param>
        /// <returns>False if target reference is not alive.</returns>
        internal bool OnPublishResponse(PublishResponse response)
        {
            var target = Target;
            if (target == null)
            {
                return false;
            }

            isPublishing = true;
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
                            if (MonitoredItems.TryGetValueByClientId(min.ClientHandle, out item))
                            {
                                try
                                {
                                    item.Publish(target, min.Value);
                                }
                                catch (Exception ex)
                                {
                                    Logger?.LogError($"Error publishing value for NodeId {item.NodeId}. {ex.Message}");
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
                            if (MonitoredItems.TryGetValueByClientId(efl.ClientHandle, out item))
                            {
                                try
                                {
                                    item.Publish(target, efl.EventFields);
                                }
                                catch (Exception ex)
                                {
                                    Logger?.LogError($"Error publishing event for NodeId {item.NodeId}. {ex.Message}");
                                }
                            }
                        }
                    }
                }
                return true;
            }
            finally
            {
                isPublishing = false;
            }
        }

        /// <summary>
        /// Handles PropertyChanged event. If the property is associated with a MonitoredItem, writes the property value to the node of the server.
        /// </summary>
        /// <param name="sender">the sender.</param>
        /// <param name="e">the event.</param>
        internal async void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (isPublishing || string.IsNullOrEmpty(e.PropertyName))
            {
                return;
            }

            MonitoredItemBase item;
            if (MonitoredItems.TryGetValueByName(e.PropertyName, out item))
            {
                DataValue value;
                if (item.TryGetValue(sender, out value))
                {
                    StatusCode statusCode;
                    var writeRequest = new WriteRequest
                    {
                        NodesToWrite = new[] { new WriteValue { NodeId = item.NodeId, AttributeId = item.AttributeId, IndexRange = item.IndexRange, Value = value } }
                    };
                    try
                    {
                        var writeResponse = await session.WriteAsync(writeRequest).ConfigureAwait(false);
                        statusCode = writeResponse.Results[0];
                    }
                    catch (ServiceResultException ex)
                    {
                        statusCode = (uint)ex.HResult;
                    }
                    catch (Exception)
                    {
                        statusCode = StatusCodes.BadServerNotConnected;
                    }
                    item.OnWriteResult(sender, statusCode);
                    if (StatusCode.IsBad(statusCode))
                    {
                        Logger?.LogError($"Error writing value for {item.NodeId}. {StatusCodes.GetDefaultMessage(statusCode)}");
                    }
                }
            }
        }
    }
}