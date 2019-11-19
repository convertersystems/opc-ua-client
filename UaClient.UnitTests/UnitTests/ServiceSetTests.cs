using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workstation.ServiceModel.Ua;
using Xunit;

namespace Workstation.UaClient.UnitTests
{
    // All the *ServiceSet extensions methods are thin wrappers around
    // IServiceRequest.RequestAsync method. They enforce the correct
    // request data type and cast the result to corresponding response
    // data type. So they are simple forwarder and only do some type
    // casting. In the following we test exactly this.
    public class ServiceSetTests
    {
        class TestRequestChannel : IRequestChannel
        {
            private readonly IServiceResponse response;

            public IServiceRequest Request { get; set; }

            public TestRequestChannel(IServiceResponse response)
            {
                this.response = response;
            }

            public Task<IServiceResponse> RequestAsync(IServiceRequest request)
            {
                this.Request = request;
                return Task.FromResult(response);
            }
        }

        /*
         * AttributeServiceSet
         */
        [Fact]
        public void ReadAsyncNull()
        {
            var response = new ReadResponse();
            var channel = new TestRequestChannel(response);

            channel.Invoking(c => c.ReadAsync(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task ReadAsync()
        {
            var response = new ReadResponse();
            var request = new ReadRequest();
            var channel = new TestRequestChannel(response);

            var ret = await channel.ReadAsync(request);

            ret
                .Should().BeSameAs(response);

            channel.Request
                .Should().BeSameAs(request);
        }

        [Fact]
        public void WriteAsyncNull()
        {
            var response = new WriteResponse();
            var channel = new TestRequestChannel(response);

            channel.Invoking(c => c.WriteAsync(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task WriteAsync()
        {
            var response = new WriteResponse();
            var request = new WriteRequest();
            var channel = new TestRequestChannel(response);

            var ret = await channel.WriteAsync(request);

            ret
                .Should().BeSameAs(response);

            channel.Request
                .Should().BeSameAs(request);
        }

        [Fact]
        public void HistoryReadAsyncNull()
        {
            var response = new HistoryReadResponse();
            var channel = new TestRequestChannel(response);

            channel.Invoking(c => c.HistoryReadAsync(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task HistoryReadAsync()
        {
            var response = new HistoryReadResponse();
            var request = new HistoryReadRequest();
            var channel = new TestRequestChannel(response);

            var ret = await channel.HistoryReadAsync(request);

            ret
                .Should().BeSameAs(response);

            channel.Request
                .Should().BeSameAs(request);
        }

        [Fact]
        public void HistoryUpdateAsyncNull()
        {
            var response = new HistoryUpdateResponse();
            var channel = new TestRequestChannel(response);

            channel.Invoking(c => c.HistoryUpdateAsync(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task HistoryUpdateAsync()
        {
            var response = new HistoryUpdateResponse();
            var request = new HistoryUpdateRequest();
            var channel = new TestRequestChannel(response);

            var ret = await channel.HistoryUpdateAsync(request);

            ret
                .Should().BeSameAs(response);

            channel.Request
                .Should().BeSameAs(request);
        }

        /*
         * MethodServiceSet
         */
        [Fact]
        public void CallAsyncNull()
        {
            var response = new CallResponse();
            var channel = new TestRequestChannel(response);

            channel.Invoking(c => c.CallAsync(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task CallAsync()
        {
            var response = new CallResponse();
            var request = new CallRequest();
            var channel = new TestRequestChannel(response);

            var ret = await channel.CallAsync(request);

            ret
                .Should().BeSameAs(response);

            channel.Request
                .Should().BeSameAs(request);
        }

        /*
         * MonitoredItemServiceSet
         */
        [Fact]
        public void CreateMonitoredItemsAsyncNull()
        {
            var response = new CreateMonitoredItemsResponse();
            var channel = new TestRequestChannel(response);

            channel.Invoking(c => c.CreateMonitoredItemsAsync(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task CreateMonitoredItemsAsync()
        {
            var response = new CreateMonitoredItemsResponse();
            var request = new CreateMonitoredItemsRequest();
            var channel = new TestRequestChannel(response);

            var ret = await channel.CreateMonitoredItemsAsync(request);

            ret
                .Should().BeSameAs(response);

            channel.Request
                .Should().BeSameAs(request);
        }

        [Fact]
        public void ModifyMonitoredItemsAsyncNull()
        {
            var response = new ModifyMonitoredItemsResponse();
            var channel = new TestRequestChannel(response);

            channel.Invoking(c => c.ModifyMonitoredItemsAsync(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task ModifyMonitoredItemsAsync()
        {
            var response = new ModifyMonitoredItemsResponse();
            var request = new ModifyMonitoredItemsRequest();
            var channel = new TestRequestChannel(response);

            var ret = await channel.ModifyMonitoredItemsAsync(request);

            ret
                .Should().BeSameAs(response);

            channel.Request
                .Should().BeSameAs(request);
        }

        [Fact]
        public void SetMonitoringModeAsyncNull()
        {
            var response = new SetMonitoringModeResponse();
            var channel = new TestRequestChannel(response);

            channel.Invoking(c => c.SetMonitoringModeAsync(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task SetMonitoringModeAsync()
        {
            var response = new SetMonitoringModeResponse();
            var request = new SetMonitoringModeRequest();
            var channel = new TestRequestChannel(response);

            var ret = await channel.SetMonitoringModeAsync(request);

            ret
                .Should().BeSameAs(response);

            channel.Request
                .Should().BeSameAs(request);
        }

        [Fact]
        public void SetTriggeringAsyncNull()
        {
            var response = new SetTriggeringResponse();
            var channel = new TestRequestChannel(response);

            channel.Invoking(c => c.SetTriggeringAsync(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task SetTriggeringAsync()
        {
            var response = new SetTriggeringResponse();
            var request = new SetTriggeringRequest();
            var channel = new TestRequestChannel(response);

            var ret = await channel.SetTriggeringAsync(request);

            ret
                .Should().BeSameAs(response);

            channel.Request
                .Should().BeSameAs(request);
        }

        [Fact]
        public void DeleteMonitoredItemsAsyncNull()
        {
            var response = new DeleteMonitoredItemsResponse();
            var channel = new TestRequestChannel(response);

            channel.Invoking(c => c.DeleteMonitoredItemsAsync(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task DeleteMonitoredItemsAsync()
        {
            var response = new DeleteMonitoredItemsResponse();
            var request = new DeleteMonitoredItemsRequest();
            var channel = new TestRequestChannel(response);

            var ret = await channel.DeleteMonitoredItemsAsync(request);

            ret
                .Should().BeSameAs(response);

            channel.Request
                .Should().BeSameAs(request);
        }

        /*
-        * NodeManagementServiceSet
-        */
        [Fact]
        public void AddNodesAsyncNull()
        {
            var response = new AddNodesResponse();
            var channel = new TestRequestChannel(response);

            channel.Invoking(c => c.AddNodesAsync(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task AddNodesAsync()
        {
            var response = new AddNodesResponse();
            var request = new AddNodesRequest();
            var channel = new TestRequestChannel(response);

            var ret = await channel.AddNodesAsync(request);

            ret
                .Should().BeSameAs(response);

            channel.Request
                .Should().BeSameAs(request);
        }

        [Fact]
        public void AddReferencesAsyncNull()
        {
            var response = new AddReferencesResponse();
            var channel = new TestRequestChannel(response);

            channel.Invoking(c => c.AddReferencesAsync(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task AddReferencesAsync()
        {
            var response = new AddReferencesResponse();
            var request = new AddReferencesRequest();
            var channel = new TestRequestChannel(response);

            var ret = await channel.AddReferencesAsync(request);

            ret
                .Should().BeSameAs(response);

            channel.Request
                .Should().BeSameAs(request);
        }

        [Fact]
        public void DeleteNodesAsyncNull()
        {
            var response = new DeleteNodesResponse();
            var channel = new TestRequestChannel(response);

            channel.Invoking(c => c.DeleteNodesAsync(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task DeleteNodesAsync()
        {
            var response = new DeleteNodesResponse();
            var request = new DeleteNodesRequest();
            var channel = new TestRequestChannel(response);

            var ret = await channel.DeleteNodesAsync(request);

            ret
                .Should().BeSameAs(response);

            channel.Request
                .Should().BeSameAs(request);
        }

        [Fact]
        public void DeleteReferencesAsyncNull()
        {
            var response = new DeleteReferencesResponse();
            var channel = new TestRequestChannel(response);

            channel.Invoking(c => c.DeleteReferencesAsync(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task DeleteReferencesAsync()
        {
            var response = new DeleteReferencesResponse();
            var request = new DeleteReferencesRequest();
            var channel = new TestRequestChannel(response);

            var ret = await channel.DeleteReferencesAsync(request);

            ret
                .Should().BeSameAs(response);

            channel.Request
                .Should().BeSameAs(request);
        }

        /*
         * QueryServiceSet
         */
        [Fact]
        public void QueryFirstAsyncNull()
        {
            var response = new QueryFirstResponse();
            var channel = new TestRequestChannel(response);

            channel.Invoking(c => c.QueryFirstAsync(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task QueryFirstAsync()
        {
            var response = new QueryFirstResponse();
            var request = new QueryFirstRequest();
            var channel = new TestRequestChannel(response);

            var ret = await channel.QueryFirstAsync(request);

            ret
                .Should().BeSameAs(response);

            channel.Request
                .Should().BeSameAs(request);
        }

        [Fact]
        public void QueryNextAsyncNull()
        {
            var response = new QueryNextResponse();
            var channel = new TestRequestChannel(response);

            channel.Invoking(c => c.QueryNextAsync(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task QueryNextAsync()
        {
            var response = new QueryNextResponse();
            var request = new QueryNextRequest();
            var channel = new TestRequestChannel(response);

            var ret = await channel.QueryNextAsync(request);

            ret
                .Should().BeSameAs(response);

            channel.Request
                .Should().BeSameAs(request);
        }

        /*
         * SubscriptionServiceSet
         */
        [Fact]
        public void CreateSubscriptionAsyncNull()
        {
            var response = new CreateSubscriptionResponse();
            var channel = new TestRequestChannel(response);

            channel.Invoking(c => c.CreateSubscriptionAsync(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task CreateSubscriptionAsync()
        {
            var response = new CreateSubscriptionResponse();
            var request = new CreateSubscriptionRequest();
            var channel = new TestRequestChannel(response);

            var ret = await channel.CreateSubscriptionAsync(request);

            ret
                .Should().BeSameAs(response);

            channel.Request
                .Should().BeSameAs(request);
        }

        [Fact]
        public void ModifySubscriptionAsyncNull()
        {
            var response = new ModifySubscriptionResponse();
            var channel = new TestRequestChannel(response);

            channel.Invoking(c => c.ModifySubscriptionAsync(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task ModifySubscriptionAsync()
        {
            var response = new ModifySubscriptionResponse();
            var request = new ModifySubscriptionRequest();
            var channel = new TestRequestChannel(response);

            var ret = await channel.ModifySubscriptionAsync(request);

            ret
                .Should().BeSameAs(response);

            channel.Request
                .Should().BeSameAs(request);
        }

        [Fact]
        public void SetPublishingModeAsyncNull()
        {
            var response = new SetPublishingModeResponse();
            var channel = new TestRequestChannel(response);

            channel.Invoking(c => c.SetPublishingModeAsync(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task SetPublishingModeAsync()
        {
            var response = new SetPublishingModeResponse();
            var request = new SetPublishingModeRequest();
            var channel = new TestRequestChannel(response);

            var ret = await channel.SetPublishingModeAsync(request);

            ret
                .Should().BeSameAs(response);

            channel.Request
                .Should().BeSameAs(request);
        }

        [Fact]
        public void TransferSubscriptionsAsyncNull()
        {
            var response = new TransferSubscriptionsResponse();
            var channel = new TestRequestChannel(response);

            channel.Invoking(c => c.TransferSubscriptionsAsync(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task TransferSubscriptionsAsync()
        {
            var response = new TransferSubscriptionsResponse();
            var request = new TransferSubscriptionsRequest();
            var channel = new TestRequestChannel(response);

            var ret = await channel.TransferSubscriptionsAsync(request);

            ret
                .Should().BeSameAs(response);

            channel.Request
                .Should().BeSameAs(request);
        }

        [Fact]
        public void DeleteSubscriptionsAsyncNull()
        {
            var response = new DeleteSubscriptionsResponse();
            var channel = new TestRequestChannel(response);

            channel.Invoking(c => c.DeleteSubscriptionsAsync(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task DeleteSubscriptionsAsync()
        {
            var response = new DeleteSubscriptionsResponse();
            var request = new DeleteSubscriptionsRequest();
            var channel = new TestRequestChannel(response);

            var ret = await channel.DeleteSubscriptionsAsync(request);

            ret
                .Should().BeSameAs(response);

            channel.Request
                .Should().BeSameAs(request);
        }

        /*
  -      * ViewServiceSet
         */
        [Fact]
        public void BrowseAsyncNull()
        {
            var response = new BrowseResponse();
            var channel = new TestRequestChannel(response);

            channel.Invoking(c => c.BrowseAsync(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task BrowseAsync()
        {
            var response = new BrowseResponse();
            var request = new BrowseRequest();
            var channel = new TestRequestChannel(response);

            var ret = await channel.BrowseAsync(request);

            ret
                .Should().BeSameAs(response);

            channel.Request
                .Should().BeSameAs(request);
        }

        [Fact]
        public void BrowseNextAsyncNull()
        {
            var response = new BrowseNextResponse();
            var channel = new TestRequestChannel(response);

            channel.Invoking(c => c.BrowseNextAsync(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task BrowseNextAsync()
        {
            var response = new BrowseNextResponse();
            var request = new BrowseNextRequest();
            var channel = new TestRequestChannel(response);

            var ret = await channel.BrowseNextAsync(request);

            ret
                .Should().BeSameAs(response);

            channel.Request
                .Should().BeSameAs(request);
        }

        [Fact]
        public void TranslateBrowsePathsToNodeIdsAsyncNull()
        {
            var response = new TranslateBrowsePathsToNodeIdsResponse();
            var channel = new TestRequestChannel(response);

            channel.Invoking(c => c.TranslateBrowsePathsToNodeIdsAsync(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task TranslateBrowsePathsToNodeIdsAsync()
        {
            var response = new TranslateBrowsePathsToNodeIdsResponse();
            var request = new TranslateBrowsePathsToNodeIdsRequest();
            var channel = new TestRequestChannel(response);

            var ret = await channel.TranslateBrowsePathsToNodeIdsAsync(request);

            ret
                .Should().BeSameAs(response);

            channel.Request
                .Should().BeSameAs(request);
        }

        [Fact]
        public void RegisterNodesAsyncNull()
        {
            var response = new RegisterNodesResponse();
            var channel = new TestRequestChannel(response);

            channel.Invoking(c => c.RegisterNodesAsync(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task RegisterNodesAsync()
        {
            var response = new RegisterNodesResponse();
            var request = new RegisterNodesRequest();
            var channel = new TestRequestChannel(response);

            var ret = await channel.RegisterNodesAsync(request);

            ret
                .Should().BeSameAs(response);

            channel.Request
                .Should().BeSameAs(request);
        }

        [Fact]
        public void UnregisterNodesAsyncNull()
        {
            var response = new UnregisterNodesResponse();
            var channel = new TestRequestChannel(response);

            channel.Invoking(c => c.UnregisterNodesAsync(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task UnregisterNodesAsync()
        {
            var response = new UnregisterNodesResponse();
            var request = new UnregisterNodesRequest();
            var channel = new TestRequestChannel(response);

            var ret = await channel.UnregisterNodesAsync(request);

            ret
                .Should().BeSameAs(response);

            channel.Request
                .Should().BeSameAs(request);
        }
    }
}
