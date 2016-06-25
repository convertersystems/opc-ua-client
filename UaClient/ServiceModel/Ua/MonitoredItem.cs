// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
        private Action<object, DataValue> publishData;

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitoredItem"/> class.
        /// </summary>
        public MonitoredItem()
        {
            this.AttributeId = AttributeIds.Value;
            this.MonitoringMode = MonitoringMode.Reporting;
            this.SamplingInterval = -1;
            this.QueueSize = 0;
            this.DiscardOldest = true;
        }

        /// <summary>
        /// Gets or sets the property of the model.
        /// </summary>
        public PropertyInfo Property { get; set; }

        /// <summary>
        /// Gets or sets the NodeId to monitor.
        /// </summary>
        public NodeId NodeId { get; set; }

        /// <summary>
        /// Gets or sets the attribute to monitor.
        /// </summary>
        public uint AttributeId { get; set; }

        /// <summary>
        /// Gets or sets the range of array indexes to monitor.
        /// </summary>
        public string IndexRange { get; set; }

        /// <summary>
        /// Gets or sets the monitoring mode.
        /// </summary>
        public MonitoringMode MonitoringMode { get; set; }

        /// <summary>
        /// Gets or sets the sampling interval.
        /// </summary>
        public int SamplingInterval { get; set; }

        /// <summary>
        /// Gets or sets the filter used by the server to select values to return.
        /// </summary>
        public MonitoringFilter Filter { get; set; }

        /// <summary>
        /// Gets or sets the length of the queue used by the server to buffer values.
        /// </summary>
        public uint QueueSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to discard the oldest entries in the queue when it is full.
        /// </summary>
        public bool DiscardOldest { get; set; }

        /// <summary>
        /// Gets the identifier assigned by the client.
        /// </summary>
        public uint ClientId { get; } = (uint)Interlocked.Increment(ref lastClientId);

        /// <summary>
        /// Gets or sets the identifier assigned by the server.
        /// </summary>
        public uint ServerId { get; set; }

        internal virtual void Publish(object target, DataValue dataValue)
        {
            if (this.publishData == null)
            {
                if (this.Property.PropertyType == typeof(DataValue))
                {
                    this.publishData = new Action<object, DataValue>((t, d) => this.Property.SetValue(t, d));
                    return;
                }

                this.publishData = new Action<object, DataValue>((t, d) => this.Property.SetValue(t, d.Value));
            }

            this.publishData(target, dataValue);
        }

        internal virtual void Publish(object target, Variant[] eventFields)
        {
            var currentEvent = EventHelper.Deserialize(this.Property.PropertyType, eventFields);
            this.Property.SetValue(target, currentEvent);
        }
    }
}