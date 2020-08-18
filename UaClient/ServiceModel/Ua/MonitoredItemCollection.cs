// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// A collection of <see cref="MonitoredItemBase"/>.
    /// </summary>
    public class MonitoredItemBaseCollection : ObservableCollection<MonitoredItemBase>
    {
        private readonly Dictionary<string, MonitoredItemBase> _nameMap = new Dictionary<string, MonitoredItemBase>();
        private readonly Dictionary<uint, MonitoredItemBase> _clientIdMap = new Dictionary<uint, MonitoredItemBase>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitoredItemBaseCollection"/> class.
        /// </summary>
        public MonitoredItemBaseCollection()
        {
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

                if (_nameMap.TryGetValue(name, out var item))
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
                if (_clientIdMap.TryGetValue(clientId, out var item))
                {
                    return item;
                }

                throw new KeyNotFoundException("An element with the specified id does not exist in the collection.");
            }
        }

        public static implicit operator MonitoredItemBaseCollection(MonitoredItemBase[] values)
        {
            if (values != null)
            {
                var col = new MonitoredItemBaseCollection();
                foreach (var value in values)
                {
                    col.Add(value);
                }

                return col;
            }

            return new MonitoredItemBaseCollection();
        }

        public static explicit operator MonitoredItemBase[]?(MonitoredItemBaseCollection values)
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
        /// <returns>true if the <see cref="MonitoredItemBaseCollection" /> contains an element with the specified name; otherwise, false.</returns>
        /// <param name="name">The name of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value" /> parameter. This parameter is passed uninitialized.</param>
        public bool TryGetValueByName(string name, [NotNullWhen(returnValue: true)] out MonitoredItemBase? value)
        {
            return _nameMap.TryGetValue(name, out value);
        }

        /// <summary>Gets the value associated with the specified clientId.</summary>
        /// <returns>true if the <see cref="MonitoredItemBaseCollection" /> contains an element with the specified clientId; otherwise, false.</returns>
        /// <param name="clientId">The clientId of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the <paramref name="value" /> parameter. This parameter is passed uninitialized.</param>
        public bool TryGetValueByClientId(uint clientId, [NotNullWhen(returnValue: true)] out MonitoredItemBase? value)
        {
            return _clientIdMap.TryGetValue(clientId, out value);
        }

        protected override void InsertItem(int index, MonitoredItemBase item)
        {
            _nameMap.Add(item.Name, item);
            _clientIdMap.Add(item.ClientId, item);
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            _nameMap.Remove(base[index].Name);
            _clientIdMap.Remove(base[index].ClientId);
            base.RemoveItem(index);
        }

        protected override void SetItem(int index, MonitoredItemBase item)
        {
            _nameMap.Remove(base[index].Name);
            _clientIdMap.Remove(base[index].ClientId);
            _nameMap.Add(item.Name, item);
            _clientIdMap.Add(item.ClientId, item);
            base.SetItem(index, item);
        }

        protected override void ClearItems()
        {
            _nameMap.Clear();
            _clientIdMap.Clear();
            base.ClearItems();
        }

    }
}
