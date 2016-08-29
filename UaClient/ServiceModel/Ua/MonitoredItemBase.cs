// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Workstation.Collections;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// Subscribes to data changes or events of an attribute of a node.
    /// </summary>
    public abstract class MonitoredItemBase
    {
        private static long lastClientId;
        private StatusCode statusCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitoredItemBase"/> class.
        /// </summary>
        /// <param name="target">the target model to store the published value.</param>
        /// <param name="property">the property of the model to store the published value.</param>
        /// <param name="nodeId">the NodeId to monitor.</param>
        /// <param name="attributeId">the attribute to monitor.</param>
        /// <param name="indexRange">the range of array indexes to monitor.</param>
        /// <param name="monitoringMode">the monitoring mode.</param>
        /// <param name="samplingInterval">the sampling interval.</param>
        /// <param name="filter">the properties that trigger a notification.</param>
        /// <param name="queueSize">the length of the queue used by the server to buffer values.</param>
        /// <param name="discardOldest">a value indicating whether to discard the oldest entries in the queue when it is full.</param>
        public MonitoredItemBase(ISubscription target, PropertyInfo property, NodeId nodeId, uint attributeId = AttributeIds.Value, string indexRange = null, MonitoringMode monitoringMode = MonitoringMode.Reporting, int samplingInterval = -1, MonitoringFilter filter = null, uint queueSize = 0, bool discardOldest = true)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            if (nodeId == null)
            {
                throw new ArgumentNullException(nameof(nodeId));
            }

            this.Target = target;
            this.Property = property;
            this.NodeId = nodeId;
            this.AttributeId = attributeId;
            this.IndexRange = indexRange;
            this.MonitoringMode = monitoringMode;
            this.SamplingInterval = samplingInterval;
            this.Filter = filter;
            this.QueueSize = queueSize;
            this.DiscardOldest = discardOldest;
            this.ClientId = (uint)Interlocked.Increment(ref lastClientId);
        }

        /// <summary>
        /// Gets the target model to store the published value.
        /// </summary>
        public ISubscription Target { get; }

        /// <summary>
        /// Gets the property of the model to store the published value.
        /// </summary>
        public PropertyInfo Property { get; }

        /// <summary>
        /// Gets the NodeId to monitor.
        /// </summary>
        public NodeId NodeId { get; }

        /// <summary>
        /// Gets the attribute to monitor.
        /// </summary>
        public uint AttributeId { get; }

        /// <summary>
        /// Gets the range of array indexes to monitor.
        /// </summary>
        public string IndexRange { get; }

        /// <summary>
        /// Gets the monitoring mode.
        /// </summary>
        public MonitoringMode MonitoringMode { get; }

        /// <summary>
        /// Gets the sampling interval.
        /// </summary>
        public int SamplingInterval { get; }

        /// <summary>
        /// Gets the filter used by the server to select values to return.
        /// </summary>
        public MonitoringFilter Filter { get; }

        /// <summary>
        /// Gets the length of the queue used by the server to buffer values.
        /// </summary>
        public uint QueueSize { get; }

        /// <summary>
        /// Gets a value indicating whether to discard the oldest entries in the queue when it is full.
        /// </summary>
        public bool DiscardOldest { get; }

        /// <summary>
        /// Gets the identifier assigned by the client.
        /// </summary>
        public uint ClientId { get; }

        /// <summary>
        /// Gets the identifier assigned by the server.
        /// </summary>
        public uint ServerId { get; private set; }

        /// <summary>
        /// Gets the latest status code assigned by the server.
        /// </summary>
        public StatusCode StatusCode
        {
            get { return this.statusCode; }

            private set
            {
                if (this.statusCode == value)
                {
                    return;
                }
                this.statusCode = value;
                if (!StatusCode.IsGood(value))
                {
                    this.Target.SetErrors(this.Property.Name, new string[] { StatusCodes.GetDefaultMessage(value) });
                    return;
                }
                this.Target.SetErrors(this.Property.Name, null);
            }
        }

        public virtual void Publish(DataValue dataValue)
        {
            this.StatusCode = dataValue.StatusCode;
        }

        public virtual void Publish(Variant[] eventFields)
        {
        }

        public virtual void OnPropertyChanged()
        {
        }

        public virtual void OnCreateResult(MonitoredItemCreateResult result)
        {
            this.ServerId = result.MonitoredItemId;
            this.StatusCode = result.StatusCode;
            if (StatusCode.IsBad(result.StatusCode))
            {
                Trace.TraceError($"Error creating MonitoredItem for {this.NodeId}. {StatusCodes.GetDefaultMessage(result.StatusCode)}");
            }
        }

        public virtual void OnWriteResult(StatusCode result)
        {
            this.StatusCode = result;
            if (StatusCode.IsBad(result))
            {
                Trace.TraceError($"Error writing value for {this.NodeId}. {StatusCodes.GetDefaultMessage(result)}");
            }
        }
    }

    /// <summary>
    /// Subscribes to data changes of an attribute of a node.
    /// Sets the published value in a property of type DataValue.
    /// </summary>
    public class DataValueMonitoredItem : MonitoredItemBase
    {
        public DataValueMonitoredItem(ISubscription target, PropertyInfo property, NodeId nodeId, uint attributeId = 13, string indexRange = null, MonitoringMode monitoringMode = MonitoringMode.Reporting, int samplingInterval = -1, MonitoringFilter filter = null, uint queueSize = 0, bool discardOldest = true)
            : base(target, property, nodeId, attributeId, indexRange, monitoringMode, samplingInterval, filter, queueSize, discardOldest)
        {
        }

        public override void Publish(DataValue dataValue)
        {
            this.Property.SetValue(this.Target, dataValue);
            base.Publish(dataValue);
        }

        public override async void OnPropertyChanged()
        {
            var pi = this.Property;
            if (pi.CanRead)
            {
                try
                {
                    var dataValue = (DataValue)pi.GetValue(this.Target);
                    var writeRequest = new WriteRequest
                    {
                        NodesToWrite = new[] { new WriteValue { NodeId = this.NodeId, AttributeId = this.AttributeId, IndexRange = this.IndexRange, Value = dataValue } }
                    };
                    var writeResponse = await this.Target.Session.WriteAsync(writeRequest).ConfigureAwait(false);
                    this.OnWriteResult(writeResponse.Results[0]);
                }
                catch (ServiceResultException ex)
                {
                    this.OnWriteResult((uint)ex.HResult);
                }
            }
        }
    }

    /// <summary>
    /// Subscribes to data changes of an attribute of a node.
    /// Unwraps the published value and sets it in a property.
    /// </summary>
    public class ValueMonitoredItem : MonitoredItemBase
    {
        public ValueMonitoredItem(ISubscription target, PropertyInfo property, NodeId nodeId, uint attributeId = 13, string indexRange = null, MonitoringMode monitoringMode = MonitoringMode.Reporting, int samplingInterval = -1, MonitoringFilter filter = null, uint queueSize = 0, bool discardOldest = true)
            : base(target, property, nodeId, attributeId, indexRange, monitoringMode, samplingInterval, filter, queueSize, discardOldest)
        {
        }

        public override void Publish(DataValue dataValue)
        {
            this.Property.SetValue(this.Target, dataValue.GetValue());
            base.Publish(dataValue);
        }

        public override async void OnPropertyChanged()
        {
            var pi = this.Property;
            if (pi.CanRead)
            {
                try
                {
                    var value = pi.GetValue(this.Target);
                    var writeRequest = new WriteRequest
                    {
                        NodesToWrite = new[] { new WriteValue { NodeId = this.NodeId, AttributeId = this.AttributeId, IndexRange = this.IndexRange, Value = new DataValue(value) } }
                    };
                    var writeResponse = await this.Target.Session.WriteAsync(writeRequest).ConfigureAwait(false);
                    this.OnWriteResult(writeResponse.Results[0]);
                }
                catch (ServiceResultException ex)
                {
                    this.OnWriteResult((uint)ex.HResult);
                }
            }
        }
    }

    /// <summary>
    /// Subscribes to data changes of an attribute of a node.
    /// Enqueues the published value to an <see cref="ObservableQueue{DataValue}"/>.
    /// </summary>
    public class DataValueQueueMonitoredItem : MonitoredItemBase
    {
        public DataValueQueueMonitoredItem(ISubscription target, PropertyInfo property, NodeId nodeId, uint attributeId = 13, string indexRange = null, MonitoringMode monitoringMode = MonitoringMode.Reporting, int samplingInterval = -1, MonitoringFilter filter = null, uint queueSize = 0, bool discardOldest = true)
            : base(target, property, nodeId, attributeId, indexRange, monitoringMode, samplingInterval, filter, queueSize, discardOldest)
        {
        }

        public override void Publish(DataValue dataValue)
        {
            var queue = (ObservableQueue<DataValue>)this.Property.GetValue(this.Target);
            queue.Enqueue(dataValue);
            base.Publish(dataValue);
        }
    }

    /// <summary>
    /// Subscribes to events of an attribute of a node.
    /// Sets the published event in a property of type BaseEvent or subtype.
    /// </summary>
    public class EventMonitoredItem : MonitoredItemBase
    {
        public EventMonitoredItem(ISubscription target, PropertyInfo property, NodeId nodeId, uint attributeId = 12, string indexRange = null, MonitoringMode monitoringMode = MonitoringMode.Reporting, int samplingInterval = -1, MonitoringFilter filter = null, uint queueSize = 0, bool discardOldest = true)
            : base(target, property, nodeId, attributeId, indexRange, monitoringMode, samplingInterval, filter, queueSize, discardOldest)
        {
        }

        public override void Publish(Variant[] eventFields)
        {
            var currentEvent = EventHelper.Deserialize(this.Property.PropertyType, eventFields);
            this.Property.SetValue(this.Target, currentEvent);
            base.Publish(eventFields);
        }
    }

    /// <summary>
    /// Subscribes to events of an attribute of a node.
    /// Enqueues the published event to an <see cref="ObservableQueue{T}"/>.
    /// </summary>
    public class EventQueueMonitoredItem<T> : MonitoredItemBase
            where T : BaseEvent, new()
    {
        public EventQueueMonitoredItem(ISubscription target, PropertyInfo property, NodeId nodeId, uint attributeId = 12, string indexRange = null, MonitoringMode monitoringMode = MonitoringMode.Reporting, int samplingInterval = -1, MonitoringFilter filter = null, uint queueSize = 0, bool discardOldest = true)
            : base(target, property, nodeId, attributeId, indexRange, monitoringMode, samplingInterval, filter, queueSize, discardOldest)
        {
        }

        public override void Publish(Variant[] eventFields)
        {
            var currentEvent = EventHelper.Deserialize<T>(eventFields);
            var queue = (ObservableQueue<T>)this.Property.GetValue(this.Target);
            queue.Enqueue(currentEvent);
            base.Publish(eventFields);
        }
    }
}