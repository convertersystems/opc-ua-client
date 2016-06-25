// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// A collection of <see cref="MonitoredItem"/>.
    /// </summary>
    public class MonitoredItemCollection : KeyedCollection<uint, MonitoredItem>
    {
        private Dictionary<string, MonitoredItem> nameMap = new Dictionary<string, MonitoredItem>();

        /// <summary>Gets the element with the specified name. </summary>
        /// <returns>The element with the specified name. If an element with the specified key is not found, an exception is thrown.</returns>
        /// <param name="name">The name of the element to get.</param>
        /// <exception cref="T:System.ArgumentNullException">
        ///   <paramref name="name" /> is null.</exception>
        /// <exception cref="T:System.Collections.Generic.KeyNotFoundException">An element with the specified name does not exist in the collection.</exception>
        public MonitoredItem this[string name]
        {
            get
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                MonitoredItem item;
                if (this.nameMap.TryGetValue(name, out item))
                {
                    return item;
                }

                throw new KeyNotFoundException("An element with the specified name does not exist in the collection.");
            }
        }

        public static implicit operator MonitoredItemCollection(MonitoredItem[] values)
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

        public static explicit operator MonitoredItem[] (MonitoredItemCollection values)
        {
            if (values != null)
            {
                var arr = new MonitoredItem[values.Count];
                values.CopyTo(arr, 0);
                return arr;
            }

            return null;
        }

        /// <summary>Gets the value associated with the specified name.</summary>
        /// <returns>true if the <see cref="MonitoredItemCollection" /> contains an element with the specified name; otherwise, false.</returns>
        /// <param name="name">The name of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value" /> parameter. This parameter is passed uninitialized.</param>
        public bool TryGetValueByName(string name, out MonitoredItem value)
        {
            return this.nameMap.TryGetValue(name, out value);
        }

        protected override uint GetKeyForItem(MonitoredItem item)
        {
            return item.ClientId;
        }

        protected override void InsertItem(int index, MonitoredItem item)
        {
            this.nameMap.Add(item.Property.Name, item);
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            this.nameMap.Remove(base[index].Property.Name);
            base.RemoveItem(index);
        }

        protected override void SetItem(int index, MonitoredItem item)
        {
            this.nameMap.Remove(base[index].Property.Name);
            this.nameMap.Add(item.Property.Name, item);
            base.SetItem(index, item);
        }

        protected override void ClearItems()
        {
            this.nameMap.Clear();
            base.ClearItems();
        }

    }
}
