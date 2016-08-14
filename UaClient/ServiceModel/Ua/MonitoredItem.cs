// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// Subscribes to data changes or events of an attribute of a node.
    /// </summary>
    public class MonitoredItem
    {
        private static long lastClientId;

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitoredItem"/> class.
        /// </summary>
        /// <param name="property">the property of the model to store the published value.</param>
        /// <param name="nodeId">the NodeId to monitor.</param>
        /// <param name="attributeId">the attribute to monitor.</param>
        /// <param name="indexRange">the range of array indexes to monitor.</param>
        /// <param name="monitoringMode">the monitoring mode.</param>
        /// <param name="samplingInterval">the sampling interval.</param>
        /// <param name="filter">the properties that trigger a data change.</param>
        /// <param name="queueSize">the length of the queue used by the server to buffer values.</param>
        /// <param name="discardOldest">a value indicating whether to discard the oldest entries in the queue when it is full.</param>
        public MonitoredItem(PropertyInfo property, NodeId nodeId, uint attributeId = AttributeIds.Value, string indexRange = null, MonitoringMode monitoringMode = MonitoringMode.Reporting, int samplingInterval = -1, MonitoringFilter filter = null, uint queueSize = 0, bool discardOldest = true)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            if (nodeId == null)
            {
                throw new ArgumentNullException(nameof(nodeId));
            }

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
        /// Gets or sets the identifier assigned by the server.
        /// </summary>
        public uint ServerId { get; set; }

        internal virtual void Publish(object target, DataValue dataValue)
        {
            if (this.Property.PropertyType == typeof(DataValue))
            {
                this.Property.SetValue(target, dataValue);
                return;
            }

            this.Property.SetValue(target, dataValue.GetValue());
        }

        internal virtual void Publish(object target, Variant[] eventFields)
        {
            var currentEvent = EventHelper.Deserialize(this.Property.PropertyType, eventFields);
            this.Property.SetValue(target, currentEvent);
        }
    }
}