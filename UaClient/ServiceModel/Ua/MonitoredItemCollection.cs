// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using Workstation.Collections;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// A collection of <see cref="MonitoredItemBase"/>.
    /// </summary>
    public class MonitoredItemCollection : ObservableCollection<MonitoredItemBase>
    {
        private Dictionary<string, MonitoredItemBase> nameMap = new Dictionary<string, MonitoredItemBase>();
        private Dictionary<uint, MonitoredItemBase> clientIdMap = new Dictionary<uint, MonitoredItemBase>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitoredItemCollection"/> class.
        /// </summary>
        public MonitoredItemCollection()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitoredItemCollection"/> class.
        /// Attributes found in the given subscription are added to the collection.
        /// </summary>
        /// <param name="subscription">the instance that will be inspected for [MonitoredItem] attributes.</param>
        public MonitoredItemCollection(ISubscription subscription)
        {
            var typeInfo = subscription.GetType().GetTypeInfo();
            foreach (var propertyInfo in typeInfo.DeclaredProperties)
            {
                var itemAttribute = propertyInfo.GetCustomAttribute<MonitoredItemAttribute>();
                if (itemAttribute == null || string.IsNullOrEmpty(itemAttribute.NodeId))
                {
                    continue;
                }

                MonitoringFilter filter = null;
                if (itemAttribute.AttributeId == AttributeIds.Value && (itemAttribute.DataChangeTrigger != DataChangeTrigger.StatusValue || itemAttribute.DeadbandType != DeadbandType.None))
                {
                    filter = new DataChangeFilter() { Trigger = itemAttribute.DataChangeTrigger, DeadbandType = (uint)itemAttribute.DeadbandType, DeadbandValue = itemAttribute.DeadbandValue };
                }

                var propType = propertyInfo.PropertyType;
                if (propType == typeof(DataValue))
                {
                    this.Add(new DataValueMonitoredItem(
                        target: subscription,
                        property: propertyInfo,
                        nodeId: NodeId.Parse(itemAttribute.NodeId),
                        indexRange: itemAttribute.IndexRange,
                        attributeId: itemAttribute.AttributeId,
                        samplingInterval: itemAttribute.SamplingInterval,
                        filter: filter,
                        queueSize: itemAttribute.QueueSize,
                        discardOldest: itemAttribute.DiscardOldest));
                    continue;
                }

                if (propType == typeof(ObservableQueue<DataValue>))
                {
                    this.Add(new DataValueQueueMonitoredItem(
                        target: subscription,
                        property: propertyInfo,
                        nodeId: NodeId.Parse(itemAttribute.NodeId),
                        indexRange: itemAttribute.IndexRange,
                        attributeId: itemAttribute.AttributeId,
                        samplingInterval: itemAttribute.SamplingInterval,
                        filter: filter,
                        queueSize: itemAttribute.QueueSize,
                        discardOldest: itemAttribute.DiscardOldest));
                    continue;
                }

                if (propType == typeof(BaseEvent) || propType.GetTypeInfo().IsSubclassOf(typeof(BaseEvent)))
                {
                    this.Add(new EventMonitoredItem(
                        target: subscription,
                        property: propertyInfo,
                        nodeId: NodeId.Parse(itemAttribute.NodeId),
                        indexRange: itemAttribute.IndexRange,
                        attributeId: itemAttribute.AttributeId,
                        samplingInterval: itemAttribute.SamplingInterval,
                        filter: new EventFilter() { SelectClauses = EventHelper.GetSelectClauses(propType) },
                        queueSize: itemAttribute.QueueSize,
                        discardOldest: itemAttribute.DiscardOldest));
                    continue;
                }

                if (propType.IsConstructedGenericType && propType.GetGenericTypeDefinition() == typeof(ObservableQueue<>))
                {
                    var elemType = propType.GenericTypeArguments[0];
                    if (elemType == typeof(BaseEvent) || elemType.GetTypeInfo().IsSubclassOf(typeof(BaseEvent)))
                    {
                        this.Add((MonitoredItemBase)Activator.CreateInstance(
                        typeof(EventQueueMonitoredItem<>).MakeGenericType(elemType),
                        subscription,
                        propertyInfo,
                        NodeId.Parse(itemAttribute.NodeId),
                        itemAttribute.AttributeId,
                        itemAttribute.IndexRange,
                        MonitoringMode.Reporting,
                        itemAttribute.SamplingInterval,
                        new EventFilter() { SelectClauses = EventHelper.GetSelectClauses(elemType) },
                        itemAttribute.QueueSize,
                        itemAttribute.DiscardOldest));
                        continue;
                    }
                }

                this.Add(new ValueMonitoredItem(
                    target: subscription,
                    property: propertyInfo,
                    nodeId: NodeId.Parse(itemAttribute.NodeId),
                    indexRange: itemAttribute.IndexRange,
                    attributeId: itemAttribute.AttributeId,
                    samplingInterval: itemAttribute.SamplingInterval,
                    filter: filter,
                    queueSize: itemAttribute.QueueSize,
                    discardOldest: itemAttribute.DiscardOldest));
            }
        }

        /// <summary>Gets the element with the specified name. </summary>
        /// <returns>The element with the specified name. If an element with the specified key is not found, an exception is thrown.</returns>
        /// <param name="name">The name of the element to get.</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="name" /> is null.</exception>
        /// <exception cref="T:System.Collections.Generic.KeyNotFoundException">An element with the specified name does not exist in the collection.</exception>
        public MonitoredItemBase this[string name]
        {
            get
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                MonitoredItemBase item;
                if (this.nameMap.TryGetValue(name, out item))
                {
                    return item;
                }

                throw new KeyNotFoundException("An element with the specified name does not exist in the collection.");
            }
        }

        /// <summary>Gets the element with the specified clientId. </summary>
        /// <returns>The element with the specified clientId. If an element with the specified key is not found, an exception is thrown.</returns>
        /// <param name="clientId">The clientId of the element to get.</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="clientId" /> is null.</exception>
        /// <exception cref="T:System.Collections.Generic.KeyNotFoundException">An element with the specified clientId does not exist in the collection.</exception>
        public MonitoredItemBase this[uint clientId]
        {
            get
            {
                MonitoredItemBase item;
                if (this.clientIdMap.TryGetValue(clientId, out item))
                {
                    return item;
                }

                throw new KeyNotFoundException("An element with the specified id does not exist in the collection.");
            }
        }

        public static implicit operator MonitoredItemCollection(MonitoredItemBase[] values)
        {
            if (values != null)
            {
                var col = new MonitoredItemCollection();
                foreach (var value in values)
                {
                    col.Add(value);
                }

                return col;
            }

            return new MonitoredItemCollection();
        }

        public static explicit operator MonitoredItemBase[] (MonitoredItemCollection values)
        {
            if (values != null)
            {
                var arr = new MonitoredItemBase[values.Count];
                values.CopyTo(arr, 0);
                return arr;
            }

            return null;
        }

        /// <summary>Gets the value associated with the specified name.</summary>
        /// <returns>true if the <see cref="MonitoredItemCollection" /> contains an element with the specified name; otherwise, false.</returns>
        /// <param name="name">The name of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value" /> parameter. This parameter is passed uninitialized.</param>
        public bool TryGetValueByName(string name, out MonitoredItemBase value)
        {
            return this.nameMap.TryGetValue(name, out value);
        }

        /// <summary>Gets the value associated with the specified clientId.</summary>
        /// <returns>true if the <see cref="MonitoredItemCollection" /> contains an element with the specified clientId; otherwise, false.</returns>
        /// <param name="clientId">The clientId of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value" /> parameter. This parameter is passed uninitialized.</param>
        public bool TryGetValueByClientId(uint clientId, out MonitoredItemBase value)
        {
            return this.clientIdMap.TryGetValue(clientId, out value);
        }

        protected override void InsertItem(int index, MonitoredItemBase item)
        {
            this.nameMap.Add(item.Property.Name, item);
            this.clientIdMap.Add(item.ClientId, item);
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            this.nameMap.Remove(base[index].Property.Name);
            this.clientIdMap.Remove(base[index].ClientId);
            base.RemoveItem(index);
        }

        protected override void SetItem(int index, MonitoredItemBase item)
        {
            this.nameMap.Remove(base[index].Property.Name);
            this.clientIdMap.Remove(base[index].ClientId);
            this.nameMap.Add(item.Property.Name, item);
            this.clientIdMap.Add(item.ClientId, item);
            base.SetItem(index, item);
        }

        protected override void ClearItems()
        {
            this.nameMap.Clear();
            this.clientIdMap.Clear();
            base.ClearItems();
        }

    }
}
