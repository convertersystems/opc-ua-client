using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Workstation.ServiceModel.Ua;
using Xunit;

namespace Workstation.UaClient.UnitTests
{
    public class ServiceExtensionsTests
    {
        private static Task Never() => Task.Delay(-1);

        private static async Task<T> Never<T>(T value)
        {
            await Never();
            return value;
        }

        [Fact]
        public void ToVariantArray()
        {
            var input = new object[] { 1, 2, 3, 70 };
            var expected = new Variant[] { 1, 2, 3, 70 };

            input.ToVariantArray()
                .Should().BeEquivalentTo(expected);
        }
        
        [Fact]
        public void ToVariantArrayEmpty()
        {
            var input = new object[] { };
            var expected = new Variant[] { };

            input.ToVariantArray()
                .Should().BeEquivalentTo(expected);
        }
        
        [Fact]
        public void ToVariantArrayNull()
        {
            var input = default(object[]);

            Action act = () => input.ToVariantArray();
            act.Should().Throw<ArgumentNullException>();
        }
        
        [Fact]
        public void ToObjectArray()
        {
            var input = new Variant[] { 1, 2, 3, 70 };
            var expected = new object[] { 1, 2, 3, 70 };

            input.ToObjectArray()
                .Should().BeEquivalentTo(expected);
        }
        
        [Fact]
        public void ToObjectArrayEmpty()
        {
            var input = new Variant[] { };
            var expected = new object[] { };

            input.ToObjectArray()
                .Should().BeEquivalentTo(expected);
        }
        
        [Fact]
        public void ToObjectArrayNull()
        {
            var input = default(Variant[]);

            Action act = () => input.ToObjectArray();
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void WithCancellationCompleted()
        {
            var task = Task.CompletedTask;

            task.WithCancellation(default).IsCompleted
                .Should().BeTrue();
        }
        
        [Fact]
        public async Task WithCancellationFastTask()
        {
            using (var tcs = new CancellationTokenSource())
            {
                var fast = Task.Delay(1);

                await fast.Invoking(t => t.WithCancellation(tcs.Token))
                    .Should().NotThrowAsync();
            }
        }

        [Fact]
        public async Task WithCancellation()
        {
            using (var tcs = new CancellationTokenSource())
            {
                var never = Never();
                var task = never.WithCancellation(tcs.Token);
                tcs.Cancel();

                await task.Invoking(t => t)
                    .Should().ThrowAsync<OperationCanceledException>();
            }
        }
        
        [Fact]
        public void ValueWithCancellationCompleted()
        {
            var task = Task.FromResult(10);

            task.WithCancellation(default).IsCompleted
                .Should().BeTrue();
        }

        [Fact]
        public async Task ValueWithCancellation()
        {
            using (var tcs = new CancellationTokenSource())
            {
                var never = Never(10);
                var task = never.WithCancellation(tcs.Token);
                tcs.Cancel();

                await task.Invoking(t => t)
                    .Should().ThrowAsync<OperationCanceledException>();
            }
        }
        
        [Fact]
        public void WithTimeoutAfterCompleted()
        {
            var task = Task.CompletedTask;

            task.TimeoutAfter(-1).IsCompleted
                .Should().BeTrue();
        }
        
        [Fact]
        public void WithTimeoutAfterCompletedCanceledImmediately()
        {
            var task = Task.CompletedTask;
            var token = new CancellationToken(true);

            task.TimeoutAfter(-1, token).IsCompleted
                .Should().BeTrue();
        }
        
        [Fact]
        public async Task WithTimeoutAfterFastTask()
        {
            var fast = Task.Delay(10);

            await fast.Invoking(t => t.TimeoutAfter(-1))
                .Should().NotThrowAsync();
        }
        
        [Fact]
        public async Task WithTimeoutAfterFastTaskCanceledImmediately()
        {
            var task = Task.Delay(10);
            var token = new CancellationToken(true);

            await task.Invoking(t => t.TimeoutAfter(-1, token))
                .Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task WithTimeoutAfter()
        {
            var never = Never();

            await never.Invoking(t => t.TimeoutAfter(0))
                .Should().ThrowAsync<TimeoutException>();
        }
        
        [Fact]
        public async Task WithTimeoutAfterCanceled()
        {
            var never = Never();
            var token = new CancellationToken(true);

            await never.Invoking(t => t.TimeoutAfter(0, token))
                .Should().ThrowAsync<TaskCanceledException>();
        }
        
        [Fact]
        public void ValueWithTimeoutAfterCompleted()
        {
            var task = Task.FromResult(10);

            task.TimeoutAfter(-1).IsCompleted
                .Should().BeTrue();
        }

        [Fact]
        public async Task ValueWithTimeoutAfter()
        {
            var never = Never(10);

            await never.Invoking(t => t.TimeoutAfter(0))
                .Should().ThrowAsync<TimeoutException>();
        }
    }
}
