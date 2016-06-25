// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
        private int capacity;
        private bool isFixedSize;

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
            this.capacity = capacity;
            this.isFixedSize = isFixedSize;
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public new void Clear()
        {
            base.Clear();
            this.OnPropertyChanged("Count");
            this.OnPropertyChanged("Item[]");
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public new T Dequeue()
        {
            var item = base.Dequeue();
            this.OnPropertyChanged("Count");
            this.OnPropertyChanged("Item[]");
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, 0));
            return item;
        }

        public new void Enqueue(T item)
        {
            if (this.isFixedSize && this.capacity > 0)
            {
                while (this.Count >= this.capacity)
                {
                    this.Dequeue();
                }
            }

            base.Enqueue(item);
            this.OnPropertyChanged("Count");
            this.OnPropertyChanged("Item[]");
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, this.Count - 1));
        }

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (this.CollectionChanged != null)
            {
                this.CollectionChanged(this, e);
            }
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, e);
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            this.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }
    }
}