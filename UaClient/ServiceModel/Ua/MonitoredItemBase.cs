// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitoredItemBase"/> class.
        /// </summary>
        /// <param name="name">the key.</param>
        /// <param name="nodeId">the ExpandedNodeId to monitor.</param>
        /// <param name="attributeId">the attribute to monitor.</param>
        /// <param name="indexRange">the range of array indexes to monitor.</param>
        /// <param name="monitoringMode">the monitoring mode.</param>
        /// <param name="samplingInterval">the sampling interval.</param>
        /// <param name="filter">the properties that trigger a notification.</param>
        /// <param name="queueSize">the length of the queue used by the server to buffer values.</param>
        /// <param name="discardOldest">a value indicating whether to discard the oldest entries in the queue when it is full.</param>
        public MonitoredItemBase(string name, ExpandedNodeId nodeId, uint attributeId = AttributeIds.Value, string? indexRange = null, MonitoringMode monitoringMode = MonitoringMode.Reporting, int samplingInterval = -1, MonitoringFilter? filter = null, uint queueSize = 0, bool discardOldest = true)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (nodeId == null)
            {
                throw new ArgumentNullException(nameof(nodeId));
            }

            Name = name;
            NodeId = nodeId;
            AttributeId = attributeId;
            IndexRange = indexRange;
            MonitoringMode = monitoringMode;
            SamplingInterval = samplingInterval;
            Filter = filter;
            QueueSize = queueSize;
            DiscardOldest = discardOldest;
            ClientId = (uint)Interlocked.Increment(ref lastClientId);
        }

        /// <summary>
        /// Gets the key.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the ExpandedNodeId to monitor.
        /// </summary>
        public ExpandedNodeId NodeId { get; }

        /// <summary>
        /// Gets the attribute to monitor.
        /// </summary>
        public uint AttributeId { get; }

        /// <summary>
        /// Gets the range of array indexes to monitor.
        /// </summary>
        public string? IndexRange { get; }

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
        public MonitoringFilter? Filter { get; }

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
        public uint ServerId { get; protected set; }

        public abstract void Publish(DataValue dataValue);

        public abstract void Publish(Variant[] eventFields);

        public abstract bool TryGetValue(out DataValue? value);

        public abstract void OnCreateResult(MonitoredItemCreateResult result);

        public abstract void OnWriteResult(StatusCode statusCode);
    }

    /// <summary>
    /// Subscribes to data changes of an attribute of a node.
    /// Sets the published value in a property of type DataValue.
    /// </summary>
    public class DataValueMonitoredItem : MonitoredItemBase
    {
        private StatusCode statusCode;

        public DataValueMonitoredItem(object target, PropertyInfo property, ExpandedNodeId nodeId, uint attributeId = 13, string? indexRange = null, MonitoringMode monitoringMode = MonitoringMode.Reporting, int samplingInterval = -1, MonitoringFilter? filter = null, uint queueSize = 0, bool discardOldest = true)
            : base(property.Name, nodeId, attributeId, indexRange, monitoringMode, samplingInterval, filter, queueSize, discardOldest)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            Target = target;
            Property = property;
        }

        /// <summary>
        /// Gets the target object.
        /// </summary>
        public object Target { get; }

        /// <summary>
        /// Gets the property of the target to store the published value.
        /// </summary>
        public PropertyInfo Property { get; }

        public override void Publish(DataValue dataValue)
        {
            Property.SetValue(Target, dataValue);
            SetDataErrorInfo(dataValue.StatusCode);
        }

        public override void Publish(Variant[] eventFields)
        {
        }

        public override bool TryGetValue(out DataValue? value)
        {
            var pi = Property;
            if (pi.CanRead)
            {
                value = (DataValue?)pi.GetValue(Target);
                return true;
            }
            value = default(DataValue);
            return false;
        }

        public override void OnCreateResult(MonitoredItemCreateResult result)
        {
            ServerId = result.MonitoredItemId;
            SetDataErrorInfo(result.StatusCode);
        }

        public override void OnWriteResult(StatusCode statusCode)
        {
            SetDataErrorInfo(statusCode);
        }

        private void SetDataErrorInfo(StatusCode statusCode)
        {
            if (this.statusCode == statusCode)
            {
                return;
            }

            this.statusCode = statusCode;
            var targetAsDataErrorInfo = Target as ISetDataErrorInfo;
            if (targetAsDataErrorInfo != null)
            {
                if (!StatusCode.IsGood(statusCode))
                {
                    targetAsDataErrorInfo.SetErrors(Property.Name, new string[] { StatusCodes.GetDefaultMessage(statusCode) });
                }
                else
                {
                    targetAsDataErrorInfo.SetErrors(Property.Name, null);
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
        private StatusCode statusCode;

        public ValueMonitoredItem(object target, PropertyInfo property, ExpandedNodeId nodeId, uint attributeId = 13, string? indexRange = null, MonitoringMode monitoringMode = MonitoringMode.Reporting, int samplingInterval = -1, MonitoringFilter? filter = null, uint queueSize = 0, bool discardOldest = true)
            : base(property.Name, nodeId, attributeId, indexRange, monitoringMode, samplingInterval, filter, queueSize, discardOldest)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            Target = target;
            Property = property;
        }

        /// <summary>
        /// Gets the target object.
        /// </summary>
        public object Target { get; }

        /// <summary>
        /// Gets the property of the target to store the published value.
        /// </summary>
        public PropertyInfo Property { get; }

        public override void Publish(DataValue dataValue)
        {
            Property.SetValue(Target, dataValue.GetValue());
            SetDataErrorInfo(dataValue.StatusCode);
        }

        public override void Publish(Variant[] eventFields)
        {
        }

        public override bool TryGetValue(out DataValue? value)
        {
            var pi = Property;
            if (pi.CanRead)
            {
                value = new DataValue(Property.GetValue(Target));
                return true;
            }
            value = default(DataValue);
            return false;
        }

        public override void OnCreateResult(MonitoredItemCreateResult result)
        {
            ServerId = result.MonitoredItemId;
            SetDataErrorInfo(result.StatusCode);
        }

        public override void OnWriteResult(StatusCode statusCode)
        {
            SetDataErrorInfo(statusCode);
        }

        private void SetDataErrorInfo(StatusCode statusCode)
        {
            if (this.statusCode == statusCode)
            {
                return;
            }

            this.statusCode = statusCode;
            var targetAsDataErrorInfo = Target as ISetDataErrorInfo;
            if (targetAsDataErrorInfo != null)
            {
                if (!StatusCode.IsGood(statusCode))
                {
                    targetAsDataErrorInfo.SetErrors(Property.Name, new string[] { StatusCodes.GetDefaultMessage(statusCode) });
                }
                else
                {
                    targetAsDataErrorInfo.SetErrors(Property.Name, null);
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
        private StatusCode statusCode;

        public DataValueQueueMonitoredItem(object target, PropertyInfo property, ExpandedNodeId nodeId, uint attributeId = 13, string? indexRange = null, MonitoringMode monitoringMode = MonitoringMode.Reporting, int samplingInterval = -1, MonitoringFilter? filter = null, uint queueSize = 0, bool discardOldest = true)
            : base(property.Name, nodeId, attributeId, indexRange, monitoringMode, samplingInterval, filter, queueSize, discardOldest)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            Target = target;
            Property = property;
        }

        /// <summary>
        /// Gets the target object.
        /// </summary>
        public object Target { get; }

        /// <summary>
        /// Gets the property of the target to store the published value.
        /// </summary>
        public PropertyInfo Property { get; }

        public override void Publish(DataValue dataValue)
        {
            var queue = (ObservableQueue<DataValue>)Property.GetValue(Target)!;
            queue.Enqueue(dataValue);
        }

        public override void Publish(Variant[] eventFields)
        {
        }

        public override bool TryGetValue(out DataValue? value)
        {
            value = default(DataValue);
            return false;
        }

        public override void OnCreateResult(MonitoredItemCreateResult result)
        {
            ServerId = result.MonitoredItemId;
            SetDataErrorInfo(result.StatusCode);
        }

        public override void OnWriteResult(StatusCode statusCode)
        {
            SetDataErrorInfo(statusCode);
        }

        private void SetDataErrorInfo(StatusCode statusCode)
        {
            if (this.statusCode == statusCode)
            {
                return;
            }

            this.statusCode = statusCode;
            var targetAsDataErrorInfo = Target as ISetDataErrorInfo;
            if (targetAsDataErrorInfo != null)
            {
                if (!StatusCode.IsGood(statusCode))
                {
                    targetAsDataErrorInfo.SetErrors(Property.Name, new string[] { StatusCodes.GetDefaultMessage(statusCode) });
                }
                else
                {
                    targetAsDataErrorInfo.SetErrors(Property.Name, null);
                }
            }
        }
    }

    /// <summary>
    /// Subscribes to data changes of an attribute of a node.
    /// Unwraps the published value and sets it in a property.
    /// </summary>
    public class ValueMonitoredItem<T> : MonitoredItemBase
    {
        private StatusCode statusCode;

        public ValueMonitoredItem(object target, PropertyInfo property, ExpandedNodeId nodeId, uint attributeId = 13, string? indexRange = null, MonitoringMode monitoringMode = MonitoringMode.Reporting, int samplingInterval = -1, MonitoringFilter? filter = null, uint queueSize = 0, bool discardOldest = true)
            : base(property.Name, nodeId, attributeId, indexRange, monitoringMode, samplingInterval, filter, queueSize, discardOldest)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            Target = target;
            Property = property;
        }

        /// <summary>
        /// Gets the target object.
        /// </summary>
        public object Target { get; }

        /// <summary>
        /// Gets the property of the target to store the published value.
        /// </summary>
        public PropertyInfo Property { get; }

        public override void Publish(DataValue dataValue)
        {
            var value = dataValue.GetValueOrDefault<T>();
            Property.SetValue(Target, value);
            SetDataErrorInfo(dataValue.StatusCode);
        }

        public override void Publish(Variant[] eventFields)
        {
        }

        public override bool TryGetValue(out DataValue? value)
        {
            var pi = Property;
            if (pi.CanRead)
            {
                value = new DataValue(Property.GetValue(Target));
                return true;
            }
            value = default(DataValue);
            return false;
        }

        public override void OnCreateResult(MonitoredItemCreateResult result)
        {
            ServerId = result.MonitoredItemId;
            SetDataErrorInfo(result.StatusCode);
        }

        public override void OnWriteResult(StatusCode statusCode)
        {
            SetDataErrorInfo(statusCode);
        }

        private void SetDataErrorInfo(StatusCode statusCode)
        {
            if (this.statusCode == statusCode)
            {
                return;
            }

            this.statusCode = statusCode;
            var targetAsDataErrorInfo = Target as ISetDataErrorInfo;
            if (targetAsDataErrorInfo != null)
            {
                if (!StatusCode.IsGood(statusCode))
                {
                    targetAsDataErrorInfo.SetErrors(Property.Name, new string[] { StatusCodes.GetDefaultMessage(statusCode) });
                }
                else
                {
                    targetAsDataErrorInfo.SetErrors(Property.Name, null);
                }
            }
        }
    }

    /// <summary>
    /// Subscribes to events of an attribute of a node.
    /// Sets the published event in a property of type BaseEvent or subtype.
    /// </summary>
    public class EventMonitoredItem : MonitoredItemBase
    {
        private StatusCode statusCode;

        public EventMonitoredItem(object target, PropertyInfo property, ExpandedNodeId nodeId, uint attributeId = 12, string? indexRange = null, MonitoringMode monitoringMode = MonitoringMode.Reporting, int samplingInterval = -1, MonitoringFilter? filter = null, uint queueSize = 0, bool discardOldest = true)
            : base(property.Name, nodeId, attributeId, indexRange, monitoringMode, samplingInterval, filter, queueSize, discardOldest)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            Target = target;
            Property = property;
        }

        /// <summary>
        /// Gets the target object.
        /// </summary>
        public object Target { get; }

        /// <summary>
        /// Gets the property of the target to store the published value.
        /// </summary>
        public PropertyInfo Property { get; }

        public override void Publish(DataValue dataValue)
        {
        }

        public override void Publish(Variant[] eventFields)
        {
            var currentEvent = EventHelper.Deserialize(Property.PropertyType, eventFields);
            Property.SetValue(Target, currentEvent);
        }

        public override bool TryGetValue(out DataValue? value)
        {
            value = default(DataValue);
            return false;
        }

        public override void OnCreateResult(MonitoredItemCreateResult result)
        {
            ServerId = result.MonitoredItemId;
            SetDataErrorInfo(result.StatusCode);
        }

        public override void OnWriteResult(StatusCode statusCode)
        {
            SetDataErrorInfo(statusCode);
        }

        private void SetDataErrorInfo(StatusCode statusCode)
        {
            if (this.statusCode == statusCode)
            {
                return;
            }

            this.statusCode = statusCode;
            var targetAsDataErrorInfo = Target as ISetDataErrorInfo;
            if (targetAsDataErrorInfo != null)
            {
                if (!StatusCode.IsGood(statusCode))
                {
                    targetAsDataErrorInfo.SetErrors(Property.Name, new string[] { StatusCodes.GetDefaultMessage(statusCode) });
                }
                else
                {
                    targetAsDataErrorInfo.SetErrors(Property.Name, null);
                }
            }
        }
    }

    /// <summary>
    /// Subscribes to events of an attribute of a node.
    /// Enqueues the published event to an <see cref="ObservableQueue{T}"/>.
    /// </summary>
    public class EventQueueMonitoredItem<T> : MonitoredItemBase
            where T : BaseEvent, new()
    {
        private StatusCode statusCode;

        public EventQueueMonitoredItem(object target, PropertyInfo property, ExpandedNodeId nodeId, uint attributeId = 12, string? indexRange = null, MonitoringMode monitoringMode = MonitoringMode.Reporting, int samplingInterval = -1, MonitoringFilter? filter = null, uint queueSize = 0, bool discardOldest = true)
            : base(property.Name, nodeId, attributeId, indexRange, monitoringMode, samplingInterval, filter, queueSize, discardOldest)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            Target = target;
            Property = property;
        }

        /// <summary>
        /// Gets the target object.
        /// </summary>
        public object Target { get; }

        /// <summary>
        /// Gets the property of the target to store the published value.
        /// </summary>
        public PropertyInfo Property { get; }

        public override void Publish(DataValue dataValue)
        {
        }

        public override void Publish(Variant[] eventFields)
        {
            var currentEvent = EventHelper.Deserialize<T>(eventFields);
            var queue = (ObservableQueue<T>)Property.GetValue(Target)!;
            queue.Enqueue(currentEvent);
        }

        public override bool TryGetValue(out DataValue? value)
        {
            value = default(DataValue);
            return false;
        }

        public override void OnCreateResult(MonitoredItemCreateResult result)
        {
            ServerId = result.MonitoredItemId;
            SetDataErrorInfo(result.StatusCode);
        }

        public override void OnWriteResult(StatusCode statusCode)
        {
            SetDataErrorInfo(statusCode);
        }

        private void SetDataErrorInfo(StatusCode statusCode)
        {
            if (this.statusCode == statusCode)
            {
                return;
            }

            this.statusCode = statusCode;
            var targetAsDataErrorInfo = Target as ISetDataErrorInfo;
            if (targetAsDataErrorInfo != null)
            {
                if (!StatusCode.IsGood(statusCode))
                {
                    targetAsDataErrorInfo.SetErrors(Property.Name, new string[] { StatusCodes.GetDefaultMessage(statusCode) });
                }
                else
                {
                    targetAsDataErrorInfo.SetErrors(Property.Name, null);
                }
            }
        }
    }
}