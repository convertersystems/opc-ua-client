// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// A collection of subscriptions.
    /// </summary>
    public class SubscriptionCollection : ICollection<ISubscription>
    {
        private UaTcpSessionService service;
        private List<SubscriptionRef> subscriptionRefs = new List<SubscriptionRef>();

        public SubscriptionCollection(UaTcpSessionService service)
        {
            this.service = service;
        }

        public int Count
        {
            get
            {
                return this.subscriptionRefs.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public void Add(ISubscription item)
        {
            var subscriptionRef = new SubscriptionRef(item);
            this.subscriptionRefs.Add(subscriptionRef);
            if (this.service != null && this.service.State == CommunicationState.Opened)
            {
                this.service.CreateSubscriptionOnServerAsync(item)
                    .ContinueWith(
                    t =>
                    {
                        foreach (var ex in t.Exception.InnerExceptions)
                        {
                            UaTcpSessionService.Log.Warn($"Error subscribing {this.GetType().Name}. {ex.Message}");
                        }
                    }, TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        public void Clear()
        {
            this.subscriptionRefs.Clear();
        }

        public bool Contains(ISubscription item)
        {
            var subscriptionRef = new SubscriptionRef(item);
            return this.subscriptionRefs.Contains(subscriptionRef);
        }

        public void CopyTo(ISubscription[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<ISubscription> GetEnumerator()
        {
            return new SubscriptionRefEnumerator(this.subscriptionRefs);
        }

        public bool Remove(ISubscription item)
        {
            var subscriptionRef = new SubscriptionRef(item);
            return this.subscriptionRefs.Remove(subscriptionRef);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private struct SubscriptionRefEnumerator : IEnumerator<ISubscription>
        {
            private List<SubscriptionRef> list;
            private ISubscription current;
            private int readIndex;
            private int writeIndex;

            internal SubscriptionRefEnumerator(List<SubscriptionRef> list)
            {
                this.list = list;
                this.readIndex = 0;
                this.writeIndex = 0;
                this.current = default(ISubscription);
            }

            public ISubscription Current
            {
                get
                {
                    return this.current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return this.Current;
                }
            }

            public void Dispose()
            {
                if (this.readIndex != this.writeIndex)
                {
                    this.list.RemoveRange(this.writeIndex, this.readIndex - this.writeIndex);
                    this.readIndex = this.writeIndex = this.list.Count;
                }

                this.current = default(ISubscription);
            }

            public bool MoveNext()
            {
                while (this.readIndex < this.list.Count)
                {
                    SubscriptionRef weakReference = this.list[this.readIndex];
                    this.current = (ISubscription)weakReference.Target;
                    if (this.current != null)
                    {
                        if (this.writeIndex != this.readIndex)
                        {
                            this.list[this.writeIndex] = weakReference;
                        }

                        this.readIndex++;
                        this.writeIndex++;
                        return true;
                    }

                    this.readIndex++;
                }

                this.Dispose();
                return false;
            }

            public void Reset()
            {
                this.readIndex = 0;
                this.writeIndex = 0;
                this.current = default(ISubscription);
            }
        }

        /// <summary>
        /// Holds a weak reference to a subscription.
        /// </summary>
        private sealed class SubscriptionRef
            : WeakReference
        {
            private int hashCode;

            public SubscriptionRef(ISubscription subscription)
                : base(subscription)
            {
                this.hashCode = subscription.GetHashCode();
            }

            public bool TryGetTarget(out ISubscription subscription)
            {
                subscription = this.Target as ISubscription;
                return subscription != null;
            }

            public override bool Equals(object o)
            {
                var sr = o as SubscriptionRef;
                return sr != null && sr.GetHashCode() == this.hashCode && (sr == this || (this.IsAlive && sr.Target == this.Target));
            }

            public override int GetHashCode()
            {
                return this.hashCode;
            }
        }
    }
}
