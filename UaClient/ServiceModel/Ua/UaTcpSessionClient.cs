// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Workstation.ServiceModel.Ua.Channels;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// A client for browsing, reading, writing and subscribing to nodes of an OPC UA server.
    /// </summary>
    public class UaTcpSessionClient : IRequestChannel, IDisposable
    {
        private const double DefaultPublishingInterval = 1000f;
        private const uint DefaultKeepaliveCount = 10;
        private const uint PublishTimeoutHint = 120 * 1000; // 2 minutes

        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
        private readonly EventAggregator eventAggregator = new EventAggregator();
        private BufferBlock<ServiceTask> pendingRequests;
        private CancellationTokenSource clientCts = new CancellationTokenSource();
        private PubSubEvent<PublishResponse> publishEvent;
        private PubSubEvent<CommunicationState> stateChangedEvent;
        private bool disposed = false;
        private Task stateMachineTask;
        private string discoveryUrl;
        private UaTcpSessionChannel innerChannel;
        private IDisposable linkToken;
        private uint id;
        private CommunicationState state;

        /// <summary>
        /// Initializes a new instance of the <see cref="UaTcpSessionClient"/> class.
        /// </summary>
        /// <param name="localDescription">The <see cref="ApplicationDescription"/> of the local application.</param>
        /// <param name="localCertificate">The <see cref="X509Certificate2"/> of the local application.</param>
        /// <param name="userIdentity">The user identity or null if anonymous. Supports <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> and <see cref="X509Identity"/>.</param>
        /// <param name="remoteEndpoint">The <see cref="EndpointDescription"/> of the remote application. Obtained from a prior call to UaTcpDiscoveryClient.GetEndpoints.</param>
        /// <param name="sessionTimeout">The requested number of milliseconds that a session may be unused before being closed by the server.</param>
        /// <param name="timeoutHint">The default number of milliseconds that may elapse before an operation is cancelled by the service.</param>
        /// <param name="diagnosticsHint">The default diagnostics flags to be requested by the service.</param>
        /// <param name="localReceiveBufferSize">The size of the receive buffer.</param>
        /// <param name="localSendBufferSize">The size of the send buffer.</param>
        /// <param name="localMaxMessageSize">The maximum total size of a message.</param>
        /// <param name="localMaxChunkCount">The maximum number of message chunks.</param>
        public UaTcpSessionClient(
            ApplicationDescription localDescription,
            X509Certificate2 localCertificate,
            IUserIdentity userIdentity,
            EndpointDescription remoteEndpoint,
            double sessionTimeout = UaTcpSessionChannel.DefaultSessionTimeout,
            uint timeoutHint = UaTcpSecureChannel.DefaultTimeoutHint,
            uint diagnosticsHint = UaTcpSecureChannel.DefaultDiagnosticsHint,
            uint localReceiveBufferSize = UaTcpTransportChannel.DefaultBufferSize,
            uint localSendBufferSize = UaTcpTransportChannel.DefaultBufferSize,
            uint localMaxMessageSize = UaTcpTransportChannel.DefaultMaxMessageSize,
            uint localMaxChunkCount = UaTcpTransportChannel.DefaultMaxChunkCount)
        {
            if (localDescription == null)
            {
                throw new ArgumentNullException(nameof(localDescription));
            }

            this.LocalDescription = localDescription;
            this.LocalCertificate = localCertificate;
            this.UserIdentity = userIdentity;
            if (remoteEndpoint == null)
            {
                throw new ArgumentNullException(nameof(remoteEndpoint));
            }

            this.RemoteEndpoint = remoteEndpoint;
            this.SessionTimeout = sessionTimeout;
            this.TimeoutHint = timeoutHint;
            this.DiagnosticsHint = diagnosticsHint;
            this.LocalReceiveBufferSize = localReceiveBufferSize;
            this.LocalSendBufferSize = localSendBufferSize;
            this.LocalMaxMessageSize = localMaxMessageSize;
            this.LocalMaxChunkCount = localMaxChunkCount;
            this.pendingRequests = new BufferBlock<ServiceTask>(new DataflowBlockOptions { CancellationToken = this.clientCts.Token });
            this.publishEvent = this.eventAggregator.GetEvent<PubSubEvent<PublishResponse>>();
            this.stateChangedEvent = this.eventAggregator.GetEvent<PubSubEvent<CommunicationState>>();
            this.stateMachineTask = this.StateMachine(this.clientCts.Token);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UaTcpSessionClient"/> class.
        /// </summary>
        /// <param name="localDescription">The <see cref="ApplicationDescription"/> of the local application.</param>
        /// <param name="localCertificate">The <see cref="X509Certificate2"/> of the local application.</param>
        /// <param name="userIdentity">The user identity or null if anonymous. Supports <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> and <see cref="X509Identity"/>.</param>
        /// <param name="discoveryUrl">The url of the remote application</param>
        /// <param name="sessionTimeout">The requested number of milliseconds that a session may be unused before being closed by the server.</param>
        /// <param name="timeoutHint">The default number of milliseconds that may elapse before an operation is cancelled by the service.</param>
        /// <param name="diagnosticsHint">The default diagnostics flags to be requested by the service.</param>
        /// <param name="localReceiveBufferSize">The size of the receive buffer.</param>
        /// <param name="localSendBufferSize">The size of the send buffer.</param>
        /// <param name="localMaxMessageSize">The maximum total size of a message.</param>
        /// <param name="localMaxChunkCount">The maximum number of message chunks.</param>
        public UaTcpSessionClient(
            ApplicationDescription localDescription,
            X509Certificate2 localCertificate,
            IUserIdentity userIdentity,
            string discoveryUrl,
            double sessionTimeout = UaTcpSessionChannel.DefaultSessionTimeout,
            uint timeoutHint = UaTcpSecureChannel.DefaultTimeoutHint,
            uint diagnosticsHint = UaTcpSecureChannel.DefaultDiagnosticsHint,
            uint localReceiveBufferSize = UaTcpTransportChannel.DefaultBufferSize,
            uint localSendBufferSize = UaTcpTransportChannel.DefaultBufferSize,
            uint localMaxMessageSize = UaTcpTransportChannel.DefaultMaxMessageSize,
            uint localMaxChunkCount = UaTcpTransportChannel.DefaultMaxChunkCount)
        {
            if (localDescription == null)
            {
                throw new ArgumentNullException(nameof(localDescription));
            }

            this.LocalDescription = localDescription;
            this.LocalCertificate = localCertificate;
            this.UserIdentity = userIdentity;
            if (string.IsNullOrEmpty(discoveryUrl))
            {
                throw new ArgumentNullException(nameof(discoveryUrl));
            }

            this.discoveryUrl = discoveryUrl;
            this.SessionTimeout = sessionTimeout;
            this.TimeoutHint = timeoutHint;
            this.DiagnosticsHint = diagnosticsHint;
            this.LocalReceiveBufferSize = localReceiveBufferSize;
            this.LocalSendBufferSize = localSendBufferSize;
            this.LocalMaxMessageSize = localMaxMessageSize;
            this.LocalMaxChunkCount = localMaxChunkCount;
            this.pendingRequests = new BufferBlock<ServiceTask>(new DataflowBlockOptions { CancellationToken = this.clientCts.Token });
            this.publishEvent = this.eventAggregator.GetEvent<PubSubEvent<PublishResponse>>();
            this.stateChangedEvent = this.eventAggregator.GetEvent<PubSubEvent<CommunicationState>>();
            this.stateMachineTask = this.StateMachine(this.clientCts.Token);
        }

        /// <summary>
        /// Gets the <see cref="ApplicationDescription"/> of the local application.
        /// </summary>
        public ApplicationDescription LocalDescription { get; }

        /// <summary>
        /// Gets the <see cref="X509Certificate2"/> of the local application.
        /// </summary>
        public X509Certificate2 LocalCertificate { get; }

        /// <summary>
        /// Gets the identity of the user. Supports <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> and <see cref="X509Identity"/>.
        /// </summary>
        public IUserIdentity UserIdentity { get; }

        /// <summary>
        /// Gets the <see cref="EndpointDescription"/> of the remote application.
        /// </summary>
        public EndpointDescription RemoteEndpoint { get; private set; }

        /// <summary>
        /// Gets the requested number of milliseconds that a session may be unused before being closed by the server.
        /// </summary>
        public double SessionTimeout { get; }

        /// <summary>
        /// Gets the default number of milliseconds that may elapse before an operation is cancelled by the service.
        /// </summary>
        public uint TimeoutHint { get; }

        /// <summary>
        /// Gets the default diagnostics flags to be requested by the service.
        /// </summary>
        public uint DiagnosticsHint { get; }

        /// <summary>
        /// Gets the size of the receive buffer.
        /// </summary>
        public uint LocalReceiveBufferSize { get; }

        /// <summary>
        /// Gets the size of the send buffer.
        /// </summary>
        public uint LocalSendBufferSize { get; }

        /// <summary>
        /// Gets the maximum total size of a message.
        /// </summary>
        public uint LocalMaxMessageSize { get; }

        /// <summary>
        /// Gets the maximum number of message chunks.
        /// </summary>
        public uint LocalMaxChunkCount { get; }

        /// <summary>
        /// Gets the NamespaceUris.
        /// </summary>
        public ReadOnlyCollection<string> NamespaceUris
        {
            get
            {
                return new ReadOnlyCollection<string>(this.innerChannel != null ? this.innerChannel.NamespaceUris : new List<string>());
            }
        }

        /// <summary>
        /// Gets the ServerUris.
        /// </summary>
        public ReadOnlyCollection<string> ServerUris
        {
            get
            {
                return new ReadOnlyCollection<string>(this.innerChannel != null ? this.innerChannel.ServerUris : new List<string>());
            }
        }

        /// <summary>
        /// Gets the state of communication channel.
        /// </summary>
        public CommunicationState State
        {
            get { return this.state; }

            private set
            {
                if (this.state != value)
                {
                    this.state = value;
                    this.stateChangedEvent.Publish(value);
                }
            }
        }

        /// <summary>
        /// Subscribes to data change and event notifications from the server.
        /// </summary>
        /// <param name="subscription">The subscription.</param>
        /// <returns>A token that unsubscribes when disposed.</returns>
        public IDisposable Subscribe(ISubscription subscription)
        {
            var token1 = this.publishEvent.Subscribe(subscription.OnPublishResponse, ThreadOption.PublisherThread, false);
            var token2 = this.stateChangedEvent.Subscribe(subscription.OnStateChanged, ThreadOption.PublisherThread, false);
            subscription.OnStateChanged(this.State);
            return new SubscriptionToken(t =>
                {
                    token2.Dispose();
                    token1.Dispose();
                });
        }

        /// <summary>
        /// Sends a service request.
        /// </summary>
        /// <param name="request">An <see cref="IServiceRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns an <see cref="IServiceResponse"/>.</returns>
        public async Task<IServiceResponse> RequestAsync(IServiceRequest request)
        {
            this.UpdateTimestamp(request);
            var task = new ServiceTask(request);
            using (var timeoutCts = new CancellationTokenSource((int)request.RequestHeader.TimeoutHint))
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, this.clientCts.Token))
            using (var registration = linkedCts.Token.Register(this.CancelTask, task, false))
            {
                if (this.pendingRequests.Post(task))
                {
                    return await task.Task.ConfigureAwait(false);
                }
                throw new ServiceResultException(StatusCodes.BadSecureChannelClosed);
            }
        }

        /// <summary>
        /// Suspends the communication channel to the remote endpoint.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that suspends the communication channel.</returns>
        public Task SuspendAsync()
        {
            this.clientCts?.Cancel();
            return this.stateMachineTask;
        }

        /// <summary>
        /// Resumes the communication channel to the remote endpoint.
        /// </summary>
        public void Resume()
        {
            if (this.clientCts.IsCancellationRequested)
            {
                this.clientCts = new CancellationTokenSource();
                this.pendingRequests = new BufferBlock<ServiceTask>(new DataflowBlockOptions { CancellationToken = this.clientCts.Token });
                this.stateMachineTask = this.StateMachine(this.clientCts.Token);
            }
        }

        /// <summary>
        /// Closes the communication channel to the remote endpoint.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        /// Closes the communication channel to the remote endpoint.
        /// </summary>
        /// <param name="disposing">If true, then dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing & !this.disposed)
            {
                this.disposed = true;
                this.clientCts?.Cancel();
                this.stateMachineTask.Wait(5000);
            }
        }

        /// <summary>
        /// The state machine manages the state of the communications channel.
        /// </summary>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A task.</returns>
        private async Task StateMachine(CancellationToken token = default(CancellationToken))
        {
            int reconnectDelay = 1000;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Opening.
                    this.State = CommunicationState.Opening;
                    await this.OpenAsync(token).ConfigureAwait(false);
                    reconnectDelay = 1000;

                    // Opened.
                    this.State = CommunicationState.Opened;
                    using (var localCts = CancellationTokenSource.CreateLinkedTokenSource(new[] { token }))
                    {
                        var tasks = new[]
                        {
                            this.PublishAsync(localCts.Token),
                            this.PublishAsync(localCts.Token),
                            this.PublishAsync(localCts.Token),
                            this.WhenChannelClosingAsync(localCts.Token),
                        };
                        await Task.WhenAny(tasks).ConfigureAwait(false);
                        localCts.Cancel();
                        await Task.WhenAll(tasks).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception)
                {
                    await Task.Delay(reconnectDelay, token).ConfigureAwait(false);
                    reconnectDelay = Math.Min(reconnectDelay * 2, 20000);
                }
            }

            // Closing
            this.State = CommunicationState.Closing;
            await this.CloseAsync();

            // Closed
            this.State = CommunicationState.Closed;
        }

        /// <summary>
        /// Opens a session with the remote endpoint.
        /// </summary>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A task.</returns>
        private async Task OpenAsync(CancellationToken token = default(CancellationToken))
        {
            await this.semaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (this.RemoteEndpoint == null)
                {
                    // If specific endpoint is not provided, use discovery to select endpoint with highest
                    // security level.
                    try
                    {
                        Trace.TraceInformation($"UaTcpSessionClient discovering endpoints of '{this.discoveryUrl}'.");
                        var getEndpointsRequest = new GetEndpointsRequest
                        {
                            EndpointUrl = this.discoveryUrl,
                            ProfileUris = new[] { TransportProfileUris.UaTcpTransport }
                        };
                        var getEndpointsResponse = await UaTcpDiscoveryClient.GetEndpointsAsync(getEndpointsRequest).ConfigureAwait(false);
                        if (getEndpointsResponse.Endpoints == null || getEndpointsResponse.Endpoints.Length == 0)
                        {
                            throw new InvalidOperationException($"'{this.discoveryUrl}' returned no endpoints.");
                        }

                        this.RemoteEndpoint = getEndpointsResponse.Endpoints.OrderBy(e => e.SecurityLevel).Last();
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceWarning($"UaTcpSessionClient error discovering endpoints of '{this.discoveryUrl}'. {ex.Message}");
                        throw;
                    }
                }

                // throw here to exit state machine.
                token.ThrowIfCancellationRequested();
                try
                {
                    this.linkToken?.Dispose();
                    Trace.TraceInformation($"UaTcpSessionClient opening channel with endpoint '{this.RemoteEndpoint.EndpointUrl}'.");
                    this.innerChannel = new UaTcpSessionChannel(
                        this.LocalDescription,
                        this.LocalCertificate,
                        this.UserIdentity,
                        this.RemoteEndpoint,
                        this.SessionTimeout,
                        this.TimeoutHint,
                        this.DiagnosticsHint,
                        this.LocalReceiveBufferSize,
                        this.LocalSendBufferSize,
                        this.LocalMaxMessageSize,
                        this.LocalMaxChunkCount);
                    await this.innerChannel.OpenAsync(token).ConfigureAwait(false);
                    this.linkToken = this.pendingRequests.LinkTo(this.innerChannel);

                    // create an internal subscription.
                    this.publishEvent.Subscribe(this.OnPublishResponse, ThreadOption.PublisherThread, false);
                    var subscriptionRequest = new CreateSubscriptionRequest
                    {
                        RequestedPublishingInterval = DefaultPublishingInterval,
                        RequestedMaxKeepAliveCount = DefaultKeepaliveCount,
                        RequestedLifetimeCount = (uint)(this.SessionTimeout / DefaultPublishingInterval),
                        PublishingEnabled = true,
                        Priority = 0
                    };
                    var subscriptionResponse = await this.CreateSubscriptionAsync(subscriptionRequest).ConfigureAwait(false);
                    this.id = subscriptionResponse.SubscriptionId;
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning($"UaTcpSessionClient error opening channel with endpoint '{this.RemoteEndpoint.EndpointUrl}'. {ex.Message}");
                    throw;
                }
            }
            finally
            {
                this.semaphore.Release();
            }
        }

        /// <summary>
        /// Closes the session with the remote endpoint.
        /// </summary>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A task.</returns>
        private async Task CloseAsync(CancellationToken token = default(CancellationToken))
        {
            await this.semaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                try
                {
                    await this.innerChannel.CloseAsync(token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning($"UaTcpSessionClient error closing channel. {ex.Message}");
                }
            }
            finally
            {
                this.semaphore.Release();
            }
        }

        /// <summary>
        /// Sends publish requests to the server.
        /// </summary>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A task.</returns>
        internal async Task PublishAsync(CancellationToken token = default(CancellationToken))
        {
            var publishRequest = new PublishRequest
            {
                RequestHeader = new RequestHeader { TimeoutHint = PublishTimeoutHint, ReturnDiagnostics = this.DiagnosticsHint },
                SubscriptionAcknowledgements = new SubscriptionAcknowledgement[0]
            };
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var publishResponse = await this.PublishAsync(publishRequest).ConfigureAwait(false);

                    // Views and view models may be abandoned at any time. This code detects when a subscription
                    // is garbage collected, and deletes the corresponding subscription from the server.
                    publishResponse.MoreNotifications = true; // set flag indicates message unhandled.

                    this.publishEvent.Publish(publishResponse);

                    // If event was not handled,
                    if (publishResponse.MoreNotifications)
                    {
                        // subscription was garbage collected. So delete from server.
                        var request = new DeleteSubscriptionsRequest
                        {
                            SubscriptionIds = new uint[] { publishResponse.SubscriptionId }
                        };
                        await this.DeleteSubscriptionsAsync(request).ConfigureAwait(false);
                    }

                    publishRequest = new PublishRequest
                    {
                        RequestHeader = new RequestHeader { TimeoutHint = PublishTimeoutHint, ReturnDiagnostics = this.DiagnosticsHint },
                        SubscriptionAcknowledgements = new[] { new SubscriptionAcknowledgement { SequenceNumber = publishResponse.NotificationMessage.SequenceNumber, SubscriptionId = publishResponse.SubscriptionId } }
                    };
                }
                catch (ServiceResultException ex)
                {
                    if (!token.IsCancellationRequested)
                    {
                        Trace.TraceWarning($"UaTcpSessionClient error publishing subscription. {ex.Message}");

                        // short delay, then retry.
                        await Task.Delay((int)DefaultPublishingInterval).ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Waits until the communication channel is closing, closed or faulted.
        /// </summary>
        /// <param name="token">A cancellation token. </param>
        /// <returns>A task.</returns>
        private Task WhenChannelClosingAsync(CancellationToken token = default(CancellationToken))
        {
            return this.innerChannel.Completion.WithCancellation(token);
        }

        /// <summary>
        /// Receive PublishResponse message.
        /// </summary>
        /// <param name="response">The publish response.</param>
        private void OnPublishResponse(PublishResponse response)
        {
            if (response.SubscriptionId != this.id)
            {
                return;
            }

            try
            {
                // internal subscription does nothing now.
            }
            finally
            {
                response.MoreNotifications = false; // reset flag indicates message handled.
            }
        }

        /// <summary>
        /// Validates the request's header and updates the timestamp.
        /// </summary>
        /// <param name="request">The service request</param>
        private void UpdateTimestamp(IServiceRequest request)
        {
            if (request.RequestHeader == null)
            {
                request.RequestHeader = new RequestHeader { TimeoutHint = this.TimeoutHint, ReturnDiagnostics = this.DiagnosticsHint };
            }

            request.RequestHeader.Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Cancels the ServiceTask
        /// </summary>
        /// <param name="o">the ServiceTask.</param>
        private void CancelTask(object o)
        {
            var task = (ServiceTask)o;
            if (task.TrySetException(new ServiceResultException(StatusCodes.BadRequestTimeout)))
            {
                var request = (IServiceRequest)task.Task.AsyncState;
                Trace.TraceInformation($"UaTcpSessionClient canceled {request.GetType().Name} Handle: {request.RequestHeader.RequestHandle}");
            }
        }
    }
}