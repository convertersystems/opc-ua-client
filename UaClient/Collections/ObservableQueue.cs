// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Workstation.Collections
{
    /// <summary>
    /// Represents a first-in, first-out collection that implements INotifyCollectionChanged.
    /// </summary>
    /// <typeparam name="T">Type of element.</typeparam>
    public class ObservableQueue<T> : Queue<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private readonly int _capacity;
        private readonly bool _isFixedSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableQueue{T}"/> class.
        /// </summary>
        public ObservableQueue()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableQueue{T}"/> class.
        /// </summary>
        /// <param name="capacity">The number of elements that the queue can initially store.</param>
        /// <param name="isFixedSize">If true, older elements are discarded.</param>
        public ObservableQueue(int capacity, bool isFixedSize = false)
            : base(capacity)
        {
            _capacity = capacity;
            _isFixedSize = isFixedSize;
        }

        /// <summary>
        /// Occurs when an item is added, removed, changed, moved, or the entire list is refreshed.
        /// </summary>
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Removes all objects from the queue.
        /// </summary>
        public new void Clear()
        {
            if (Count == 0)
            {
                return;
            }

            base.Clear();
            OnPropertyChanged("Count");
            OnPropertyChanged("Item[]");
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Removes and returns the object at the beginning of the queue.
        /// </summary>
        /// <returns>The object that is removed from the beginning of the queue.</returns>
        public new T Dequeue()
        {
            var item = base.Dequeue();
            OnPropertyChanged("Count");
            OnPropertyChanged("Item[]");
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, 0));
            return item;
        }

        /// <summary>
        /// Adds an object to the end of the queue.
        /// </summary>
        /// <param name="item">The object to add to the queue.</param>
        public new void Enqueue(T item)
        {
            if (_isFixedSize && _capacity > 0)
            {
                while (Count >= _capacity)
                {
                    Dequeue();
                }
            }

            base.Enqueue(item);
            OnPropertyChanged("Count");
            OnPropertyChanged("Item[]");
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, Count - 1));
        }

        /// <summary>
        /// Raises the CollectionChanged event with the provided arguments.
        /// </summary>
        /// <param name="e">Arguments of the event being raised.</param>
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the PropertyChanged event with the provided arguments.
        /// </summary>
        /// <param name="e">Arguments of the event being raised.</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        private void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }
    }
}