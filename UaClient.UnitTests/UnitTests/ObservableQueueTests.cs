using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Workstation.Collections;
using Xunit;

namespace Workstation.UaClient.UnitTests
{
    public class ObservableQueueTests
    {
        [Fact]
        public void ObservePropertyChanged()
        {
            var queue = new ObservableQueue<int>();
            
            var props = new List<string>();
            var itemQuery = props.Where(p => p == "Item[]");
            var countQuery = props.Where(p => p == "Count");

            queue.PropertyChanged += (o, e) => props.Add(e.PropertyName);

            queue.Enqueue(1);
            itemQuery
                .Should().HaveCount(1);
            countQuery
                .Should().HaveCount(1);

            queue.Enqueue(2);
            itemQuery
                .Should().HaveCount(2);
            countQuery
                .Should().HaveCount(2);

            queue.Enqueue(3);
            itemQuery
                .Should().HaveCount(3);
            countQuery
                .Should().HaveCount(3);

            queue.Dequeue();
            itemQuery
                .Should().HaveCount(4);
            countQuery
                .Should().HaveCount(4);

            queue.Clear();
            itemQuery
                .Should().HaveCount(5);
            countQuery
                .Should().HaveCount(5);
        }
        
        [Fact]
        public void ObservePropertyChangedEmptyClear()
        {
            var queue = new ObservableQueue<int>();
            int count = 0;

            queue.PropertyChanged += (o, e) => count++;

            queue.Clear();

            count
                .Should().Be(0);
        }

        [Fact]
        public void ObserveCollectionChanged()
        {
            var queue = new ObservableQueue<string>();
            var args = new List<NotifyCollectionChangedEventArgs>();

            queue.CollectionChanged += (o, e) => args.Add(e);

            queue.Enqueue("A");
            args
                .Should().ContainSingle()
                .Which
                .Should().BeEquivalentTo(
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, "A", 0)
                );

            args.Clear();
            queue.Enqueue("B");
            args
                .Should().ContainSingle()
                .Which
                .Should().BeEquivalentTo(
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, "B", 1)
                );

            args.Clear();
            queue.Enqueue("C");
            args
                .Should().ContainSingle()
                .Which
                .Should().BeEquivalentTo(
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, "C", 2)
                );

            args.Clear();
            queue.Dequeue();
            args
                .Should().ContainSingle()
                .Which
                .Should().BeEquivalentTo(
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, "A", 0)
                );

            args.Clear();
            queue.Clear();
            args
                .Should().ContainSingle()
                .Which
                .Should().BeEquivalentTo(
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)
                );
        }
        
        [Fact]
        public void ObserveCollectionChangedEmptyClear()
        {
            var queue = new ObservableQueue<int>();
            int count = 0;

            queue.CollectionChanged += (o, e) => count++;

            queue.Clear();

            count
                .Should().Be(0);
        }
        
        [Fact]
        public void ObservePropertyChangedFixedSize()
        {
            var queue = new ObservableQueue<int>(2, isFixedSize: true);
            
            var props = new List<string>();
            var itemQuery = props.Where(p => p == "Item[]");
            var countQuery = props.Where(p => p == "Count");

            queue.PropertyChanged += (o, e) => props.Add(e.PropertyName);

            queue.Enqueue(1);
            itemQuery
                .Should().HaveCount(1);
            countQuery
                .Should().HaveCount(1);

            queue.Enqueue(2);
            itemQuery
                .Should().HaveCount(2);
            countQuery
                .Should().HaveCount(2);

            queue.Enqueue(3);
            itemQuery
                .Should().HaveCount(4);
            countQuery
                .Should().HaveCount(4);

            queue.Dequeue();
            itemQuery
                .Should().HaveCount(5);
            countQuery
                .Should().HaveCount(5);

            queue.Clear();
            itemQuery
                .Should().HaveCount(6);
            countQuery
                .Should().HaveCount(6);
        }
        
        [Fact]
        public void ObserveCollectionChangedFixedSize()
        {
            var queue = new ObservableQueue<string>(2, isFixedSize: true);
            var args = new List<NotifyCollectionChangedEventArgs>();

            queue.CollectionChanged += (o, e) => args.Add(e);

            queue.Enqueue("A");
            args
                .Should().ContainSingle()
                .Which
                .Should().BeEquivalentTo(
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, "A", 0)
                );

            args.Clear();
            queue.Enqueue("B");
            args
                .Should().ContainSingle()
                .Which
                .Should().BeEquivalentTo(
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, "B", 1)
                 );

            args.Clear();
            queue.Enqueue("C");
            args
                .Should().BeEquivalentTo(new[]
                {
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, "A", 0),
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, "C", 1)
                });

            args.Clear();
            queue.Dequeue();
            args
                .Should().ContainSingle()
                .Which
                .Should().BeEquivalentTo(
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, "B", 0)
                );

            args.Clear();
            queue.Clear();
            args
                .Should().ContainSingle()
                .Which
                .Should().BeEquivalentTo(
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)
                );
        }
    }
}
