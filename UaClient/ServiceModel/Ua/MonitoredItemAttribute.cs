// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// Specifies the MonitoredItem that will created for this property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class MonitoredItemAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MonitoredItemAttribute"/> class.
        /// </summary>
        /// <param name="nodeId">the NodeId to monitor.</param>
        /// <param name="attributeId">the attribute to monitor.</param>
        /// <param name="indexRange">the range of array indexes to monitor.</param>
        /// <param name="samplingInterval">the sampling interval.</param>
        /// <param name="queueSize">the length of the queue used by the server to buffer values.</param>
        /// <param name="discardOldest">a value indicating whether to discard the oldest entries in the queue when it is full.</param>
        /// <param name="dataChangeTrigger">the properties that trigger a data change.</param>
        /// <param name="deadbandType">the type of deadband calculation.</param>
        /// <param name="deadbandValue">the deadband value.</param>
        public MonitoredItemAttribute(string nodeId = null, uint attributeId = AttributeIds.Value, string indexRange = null, int samplingInterval = -1, uint queueSize = 0, bool discardOldest = true, DataChangeTrigger dataChangeTrigger = DataChangeTrigger.StatusValue, DeadbandType deadbandType = DeadbandType.None, double deadbandValue = 0.0)
        {
            this.NodeId = nodeId;
            this.AttributeId = attributeId;
            this.IndexRange = indexRange;
            this.SamplingInterval = samplingInterval;
            this.QueueSize = queueSize;
            this.DiscardOldest = discardOldest;
            this.DataChangeTrigger = dataChangeTrigger;
            this.DeadbandType = deadbandType;
            this.DeadbandValue = deadbandValue;
        }

        /// <summary>
        /// Gets the NodeId to monitor.
        /// </summary>
        public string NodeId { get; }

        /// <summary>
        /// Gets the attribute to monitor.
        /// </summary>
        public uint AttributeId { get; }

        /// <summary>
        /// Gets the range of array indexes to monitor.
        /// </summary>
        public string IndexRange { get; }

        /// <summary>
        /// Gets the sampling interval.
        /// </summary>
        public int SamplingInterval { get; }

        /// <summary>
        /// Gets the length of the queue used by the server to buffer values.
        /// </summary>
        public uint QueueSize { get; }

        /// <summary>
        /// Gets a value indicating whether to discard the oldest entries in the queue when it is full.
        /// </summary>
        public bool DiscardOldest { get; }

        /// <summary>
        /// Gets the properties that trigger a data change.
        /// </summary>
        public DataChangeTrigger DataChangeTrigger { get; }

        /// <summary>
        /// Gets the type of deadband calculation.
        /// </summary>
        public DeadbandType DeadbandType { get; }

        /// <summary>
        /// Gets the deadband value.
        /// </summary>
        public double DeadbandValue { get; }
    }
}