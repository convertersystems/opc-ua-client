using FluentAssertions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Workstation.ServiceModel.Ua;
using Workstation.ServiceModel.Ua.Channels;
using Xunit;

namespace Workstation.UaClient.UnitTests.Channels
{
    public class CommunicationObjectTests
    {
        private class TestCommunicationObject : CommunicationObject
        {
            // Since we do not use concurrency this could in theory also be a simple queue
            private readonly ConcurrentQueue<string> callRecord;
            private readonly Dictionary<string, Func<CommunicationObject, Task>> actionMap
                    = new Dictionary<string, Func<CommunicationObject, Task>>();

            public IEnumerable<string> Record => this.callRecord;

            public bool IsInterceptingTest { get; set; }

            public TestCommunicationObject()
            {
                this.callRecord = new ConcurrentQueue<string>();

                Closed += Event_Closed;
                Closing += Event_Closing;
                Faulted += Event_Faulted;
                Opened += Event_Opened;
                Opening += Event_Opening;
            }

            protected override async Task OnAbortAsync(CancellationToken token = default)
            {
                this.Enter();
                // We are right at the transition so the object shouldn't be closed
                this.State
                    .Should().NotBe(CommunicationState.Closed);
                await this.LeaveAsync();
            }

            protected override async Task OnClosingAsync(CancellationToken token = default)
            {
                this.Enter();
                await base.OnClosingAsync(token);
                // We are before the transition so the object should be closing
                this.State
                    .Should().Be(CommunicationState.Closing);
                await this.LeaveAsync();
            }
            
            private void Event_Closing(object sender, EventArgs e)
            {
                this.Enter();
            }

            protected override async Task OnCloseAsync(CancellationToken token = default)
            {
                this.Enter();
                // We are right at the transition so the object shouldn't be closed
                this.State
                    .Should().Be(CommunicationState.Closing);

                await this.LeaveAsync();
            }

            protected override async Task OnClosedAsync(CancellationToken token = default)
            {
                this.Enter();
                await base.OnClosedAsync(token);
                // We are after the transition so the object should be closed
                this.State
                    .Should().Be(CommunicationState.Closed);
                await this.LeaveAsync();
            }

            private void Event_Closed(object sender, EventArgs e)
            {
                this.Enter();
            }

            protected override async Task OnOpeningAsync(CancellationToken token = default)
            {
                this.Enter();
                await base.OnOpeningAsync(token);
                if (!this.IsInterceptingTest)
                {
                    // We are before the transition so the object shouldn't yet be opend
                    this.State
                        .Should().Be(CommunicationState.Opening);
                }
                await this.LeaveAsync();
            }
            
            private void Event_Opening(object sender, EventArgs e)
            {
                this.Enter();
            }

            protected override async Task OnOpenAsync(CancellationToken token = default)
            {
                this.Enter();
                if (!this.IsInterceptingTest)
                {
                    // We are right at the transition so the object shouldn't be opend
                    this.State
                        .Should().Be(CommunicationState.Opening);
                }
                await this.LeaveAsync();
            }

            protected override async Task OnOpenedAsync(CancellationToken token = default)
            {
                this.Enter();
                await base.OnOpenedAsync(token);
                if (!this.IsInterceptingTest)
                {
                    // We are after the transition so the object should be opend
                    this.State
                        .Should().Be(CommunicationState.Opened);
                }
                await this.LeaveAsync();
            }
            
            private void Event_Opened(object sender, EventArgs e)
            {
                this.Enter();
            }

            protected override async Task OnFaulted(CancellationToken token = default)
            {
                this.Enter();
                await base.OnFaulted(token);
                // We are after the transition so the object should be faulted
                this.State
                    .Should().Be(CommunicationState.Faulted);
                await this.LeaveAsync();
            }
            
            private void Event_Faulted(object sender, EventArgs e)
            {
                this.Enter();
            }

            private void Enter([CallerMemberName] string name = default)
            {
                this.callRecord.Enqueue(name);
            }

            private async Task LeaveAsync([CallerMemberName] string name = default)
            {
                if (this.actionMap.TryGetValue(name, out var action))
                {
                    await action(this);
                }
            }

            public void DoOn(string methodName, Func<CommunicationObject,Task> action)
            {
                this.actionMap[methodName] = action;
            }
        }

        [Fact]
        public void FreshObject()
        {
            var o = new TestCommunicationObject();

            o.State
                .Should().Be(CommunicationState.Created);
            o.Record
                .Should().BeEmpty();
        }

        [Fact]
        public async Task Open()
        {
            var o = new TestCommunicationObject();

            await o.OpenAsync();
            
            o.State
                .Should().Be(CommunicationState.Opened);
            o.Record
                .Should().Equal(new string[]
                {
                    "OnOpeningAsync",
                    "Event_Opening",
                    "OnOpenAsync",
                    "OnOpenedAsync",
                    "Event_Opened",
                });
        }
        
        [Fact]
        public async Task OpenAndAbortOnOpening()
        {
            var o = new TestCommunicationObject
            {
                IsInterceptingTest = true
            };
            o.DoOn("OnOpeningAsync", async co => await co.AbortAsync());

            await o.OpenAsync();
            
            o.State
                .Should().Be(CommunicationState.Closed);

            o.Record
                .Should().Equal(new string[]
                {
                    "OnOpeningAsync",
                    "Event_Opening",
                    "OnClosingAsync", 
                    "Event_Closing",
                    "OnAbortAsync",
                    "OnClosedAsync",
                    "Event_Closed",
                    "OnOpenAsync",
                    "OnOpenedAsync"
                });
        }
        
        [Fact]
        public async Task OpenAndCloseOnOpening()
        {
            var o = new TestCommunicationObject
            {
                IsInterceptingTest = true
            };
            o.DoOn("OnOpeningAsync", async co => await co.CloseAsync());

            await o.OpenAsync();
            
            o.State
                .Should().Be(CommunicationState.Closed);

            o.Record
                .Should().Equal(new string[]
                {
                    "OnOpeningAsync",
                    "Event_Opening",
                    "OnClosingAsync", 
                    "Event_Closing",
                    "OnAbortAsync",
                    "OnClosedAsync",
                    "Event_Closed",
                    "OnOpenAsync",
                    "OnOpenedAsync"
                });
        }
        
        [Fact]
        public async Task OpenAndAbortOnOpen()
        {
            var o = new TestCommunicationObject
            {
                IsInterceptingTest = true
            };
            o.DoOn("OnOpenAsync", async co => await co.AbortAsync());

            await o.OpenAsync();
            
            o.State
                .Should().Be(CommunicationState.Closed);

            o.Record
                .Should().Equal(new string[]
                {
                    "OnOpeningAsync",
                    "Event_Opening",
                    "OnOpenAsync",
                    "OnClosingAsync", 
                    "Event_Closing",
                    "OnAbortAsync",
                    "OnClosedAsync",
                    "Event_Closed",
                    "OnOpenedAsync"
                });
        }
        
        [Fact]
        public async Task OpenAndCloseOnOpen()
        {
            var o = new TestCommunicationObject
            {
                IsInterceptingTest = true
            };
            o.DoOn("OnOpenAsync", async co => await co.CloseAsync());

            await o.OpenAsync();
            
            o.State
                .Should().Be(CommunicationState.Closed);

            o.Record
                .Should().Equal(new string[]
                {
                    "OnOpeningAsync",
                    "Event_Opening",
                    "OnOpenAsync",
                    "OnClosingAsync", 
                    "Event_Closing",
                    "OnAbortAsync",
                    "OnClosedAsync",
                    "Event_Closed",
                    "OnOpenedAsync"
                });
        }

        [Fact]
        public async Task OpenAndClose()
        {
            var o = new TestCommunicationObject();

            await o.OpenAsync();
            await o.CloseAsync();
            
            o.State
                .Should().Be(CommunicationState.Closed);
            o.Record
                .Should().Equal(new string[]
                {
                    "OnOpeningAsync",
                    "Event_Opening",
                    "OnOpenAsync",
                    "OnOpenedAsync",
                    "Event_Opened",
                    "OnClosingAsync",
                    "Event_Closing",
                    "OnCloseAsync",
                    "OnClosedAsync",
                    "Event_Closed"
                });
        }

        [Fact]
        public async Task OpenAndAbort()
        {
            var o = new TestCommunicationObject();

            await o.OpenAsync();
            await o.AbortAsync();

            o.State
                .Should().Be(CommunicationState.Closed);
            o.Record
                .Should().Equal(new string[]
                {
                    "OnOpeningAsync",
                    "Event_Opening",
                    "OnOpenAsync",
                    "OnOpenedAsync",
                    "Event_Opened",
                    "OnClosingAsync",
                    "Event_Closing",
                    "OnAbortAsync",
                    "OnClosedAsync",
                    "Event_Closed"
                });
        }
        
        [Fact]
        public async Task Close()
        {
            var o = new TestCommunicationObject();

            await o.CloseAsync();

            o.State
                .Should().Be(CommunicationState.Closed);
            o.Record
                .Should().Equal(new string[]
                {
                    "OnClosingAsync",
                    "Event_Closing",
                    "OnAbortAsync",
                    "OnClosedAsync",
                    "Event_Closed"
                });
        }

        [Fact]
        public async Task Abort()
        {
            var o = new TestCommunicationObject();

            await o.CloseAsync();

            o.State
                .Should().Be(CommunicationState.Closed);
            o.Record
                .Should().Equal(new string[]
                {
                    "OnClosingAsync",
                    "Event_Closing",
                    "OnAbortAsync",
                    "OnClosedAsync",
                    "Event_Closed"
                });
        }
        
        [Fact]
        public async Task CloseAndClose()
        {
            var o = new TestCommunicationObject();

            await o.CloseAsync();
            await o.CloseAsync();

            o.State
                .Should().Be(CommunicationState.Closed);
            o.Record
                .Should().Equal(new string[]
                {
                    "OnClosingAsync",
                    "Event_Closing",
                    "OnAbortAsync",
                    "OnClosedAsync",
                    "Event_Closed"
                });
        }
        
        [Fact]
        public async Task CloseAndAbort()
        {
            var o = new TestCommunicationObject();

            await o.CloseAsync();
            await o.AbortAsync();

            o.State
                .Should().Be(CommunicationState.Closed);
            o.Record
                .Should().Equal(new string[]
                {
                    "OnClosingAsync",
                    "Event_Closing",
                    "OnAbortAsync",
                    "OnClosedAsync",
                    "Event_Closed"
                });
        }
        
        [Fact]
        public async Task CloseAndCloseOnClosing()
        {
            var o = new TestCommunicationObject();
            o.DoOn("OnClosingAsync", async co => await co.CloseAsync());

            await o.CloseAsync();

            o.State
                .Should().Be(CommunicationState.Closed);
            o.Record
                .Should().Equal(new string[]
                {
                    "OnClosingAsync",
                    "Event_Closing",
                    "OnAbortAsync",
                    "OnClosedAsync",
                    "Event_Closed"
                });
        }
        
        [Fact]
        public async Task CloseAndAbortOnClosing()
        {
            var o = new TestCommunicationObject();
            o.DoOn("OnClosingAsync", async co => await co.AbortAsync());

            await o.CloseAsync();

            o.State
                .Should().Be(CommunicationState.Closed);
            o.Record
                .Should().Equal(new string[]
                {
                    "OnClosingAsync",
                    "Event_Closing",
                    "OnAbortAsync",
                    "OnClosedAsync",
                    "Event_Closed"
                });
        }
        
        [Fact]
        public async Task OpenAndOpen()
        {
            var o = new TestCommunicationObject();

            await o.OpenAsync();

            // Calling open is only allowed on a fresh object
            await o.Invoking(x => x.OpenAsync())
                .Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task OpenAndOpenOnOpening()
        {
            var o = new TestCommunicationObject();
            o.DoOn("OnOpeningAsync", async co => await co.OpenAsync());

            // Calling open is only allowed on a fresh object
            await o.Invoking(x => x.OpenAsync())
                .Should().ThrowAsync<InvalidOperationException>();
        }
        
        [Fact]
        public async Task OpenAndThrowOnOpening()
        {
            var o = new TestCommunicationObject();
            o.DoOn("OnOpeningAsync", co => Task.FromException(new TestException()));

            await o.Invoking(x => x.OpenAsync())
                .Should().ThrowAsync<TestException>();
            
            o.State
                .Should().Be(CommunicationState.Faulted);
            
            o.Record
                .Should().Equal(new string[]
                {
                    "OnOpeningAsync",
                    "Event_Opening",
                    "OnFaulted",
                    "Event_Faulted",
                });
        }
    }
}
