// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
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

        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger logger;
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
        private readonly SynchronizationContext syncContext = SynchronizationContext.Current ?? new SynchronizationContext();
        private BufferBlock<ServiceOperation> pendingRequests;
        private CancellationTokenSource clientCts = new CancellationTokenSource();
        private ObservableCollection<Subscription> subscriptions = new ObservableCollection<Subscription>();
        private bool disposed = false;
        private Task stateMachineTask;
        private string discoveryUrl;
        private UaTcpSessionChannel innerChannel;
        private IDisposable linkToken;
        private uint subscriptionId;
        private CommunicationState state;

        /// <summary>
        /// Initializes a new instance of the <see cref="UaTcpSessionClient"/> class.
        /// </summary>
        /// <param name="localDescription">The <see cref="ApplicationDescription"/> of the local application.</param>
        /// <param name="certificateStore">The local certificate store.</param>
        /// <param name="userIdentityProvider">An asynchronous function that provides the user identity. Provide an <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> or <see cref="X509Identity"/>.</param>
        /// <param name="remoteEndpoint">The <see cref="EndpointDescription"/> of the remote application. Obtained from a prior call to UaTcpDiscoveryClient.GetEndpoints.</param>
        /// <param name="loggerFactory">A logger factory.</param>
        /// <param name="sessionTimeout">The requested number of milliseconds that a session may be unused before being closed by the server.</param>
        /// <param name="timeoutHint">The default number of milliseconds that may elapse before an operation is cancelled by the service.</param>
        /// <param name="diagnosticsHint">The default diagnostics flags to be requested by the service.</param>
        /// <param name="localReceiveBufferSize">The size of the receive buffer.</param>
        /// <param name="localSendBufferSize">The size of the send buffer.</param>
        /// <param name="localMaxMessageSize">The maximum total size of a message.</param>
        /// <param name="localMaxChunkCount">The maximum number of message chunks.</param>
        public UaTcpSessionClient(
            ApplicationDescription localDescription,
            ICertificateStore certificateStore,
            Func<EndpointDescription, Task<IUserIdentity>> userIdentityProvider,
            EndpointDescription remoteEndpoint,
            ILoggerFactory loggerFactory = null,
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
            this.CertificateStore = certificateStore;
            this.UserIdentityProvider = userIdentityProvider ?? (endpoint => Task.FromResult<IUserIdentity>(new AnonymousIdentity()));
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
            this.loggerFactory = loggerFactory;
            this.logger = loggerFactory?.CreateLogger<UaTcpSessionClient>();
            this.pendingRequests = new BufferBlock<ServiceOperation>(new DataflowBlockOptions { CancellationToken = this.clientCts.Token });
            this.stateMachineTask = Task.Run(() => this.StateMachineAsync(this.clientCts.Token));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UaTcpSessionClient"/> class.
        /// </summary>
        /// <param name="localDescription">The <see cref="ApplicationDescription"/> of the local application.</param>
        /// <param name="certificateStore">The local certificate store.</param>
        /// <param name="userIdentityProvider">An asynchronous function that provides the user identity. Provide an <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> or <see cref="X509Identity"/>.</param>
        /// <param name="endpointUrl">The url of the endpoint of the remote application</param>
        /// <param name="loggerFactory">A logger factory.</param>
        /// <param name="sessionTimeout">The requested number of milliseconds that a session may be unused before being closed by the server.</param>
        /// <param name="timeoutHint">The default number of milliseconds that may elapse before an operation is cancelled by the service.</param>
        /// <param name="diagnosticsHint">The default diagnostics flags to be requested by the service.</param>
        /// <param name="localReceiveBufferSize">The size of the receive buffer.</param>
        /// <param name="localSendBufferSize">The size of the send buffer.</param>
        /// <param name="localMaxMessageSize">The maximum total size of a message.</param>
        /// <param name="localMaxChunkCount">The maximum number of message chunks.</param>
        public UaTcpSessionClient(
            ApplicationDescription localDescription,
            ICertificateStore certificateStore,
            Func<EndpointDescription, Task<IUserIdentity>> userIdentityProvider,
            string endpointUrl,
            ILoggerFactory loggerFactory = null,
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
            this.CertificateStore = certificateStore;
            this.UserIdentityProvider = userIdentityProvider ?? (ep => Task.FromResult<IUserIdentity>(new AnonymousIdentity()));
            if (string.IsNullOrEmpty(endpointUrl))
            {
                throw new ArgumentNullException(nameof(endpointUrl));
            }

            this.discoveryUrl = endpointUrl;
            this.SessionTimeout = sessionTimeout;
            this.TimeoutHint = timeoutHint;
            this.DiagnosticsHint = diagnosticsHint;
            this.LocalReceiveBufferSize = localReceiveBufferSize;
            this.LocalSendBufferSize = localSendBufferSize;
            this.LocalMaxMessageSize = localMaxMessageSize;
            this.LocalMaxChunkCount = localMaxChunkCount;
            this.loggerFactory = loggerFactory;
            this.logger = loggerFactory?.CreateLogger<UaTcpSessionClient>();
            this.pendingRequests = new BufferBlock<ServiceOperation>(new DataflowBlockOptions { CancellationToken = this.clientCts.Token });
            this.stateMachineTask = Task.Run(() => this.StateMachineAsync(this.clientCts.Token));
        }

        /// <summary>
        /// Gets the <see cref="ApplicationDescription"/> of the local application.
        /// </summary>
        public ApplicationDescription LocalDescription { get; }

        /// <summary>
        /// Gets the local certificate store.
        /// </summary>
        public ICertificateStore CertificateStore { get; }

        /// <summary>
        /// Gets an asynchronous function that provides the identity of the user. Supports <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> and <see cref="X509Identity"/>.
        /// </summary>
        public Func<EndpointDescription, Task<IUserIdentity>> UserIdentityProvider { get; }

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
        public ReadOnlyCollection<string> NamespaceUris => new ReadOnlyCollection<string>(this.innerChannel?.NamespaceUris);

        /// <summary>
        /// Gets the ServerUris.
        /// </summary>
        public ReadOnlyCollection<string> ServerUris => new ReadOnlyCollection<string>(this.innerChannel?.ServerUris);

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
                }
            }
        }

        /// <summary>
        /// Gets the current logger
        /// </summary>
        protected virtual ILogger Logger => this.logger;

        /// <summary>
        /// Gets the <see cref="UaTcpSessionClient"/> attached to this model.
        /// </summary>
        /// <param name="model">the model.</param>
        /// <returns>Returns the attached <see cref="UaTcpSessionClient"/> or null.</returns>
        public static UaTcpSessionClient FromModel(object model)
        {
            var subscription = Subscription.FromModel(model);
            if (subscription != null)
            {
                return subscription.Session;
            }
            return null;
        }

        /// <summary>
        /// Subscribes for data change and event notifications from the server.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>Returns a disposable token.</returns>
        public IDisposable Subscribe(object model)
        {
            var subscription = new Subscription(this, model, this.loggerFactory);
            this.subscriptions.Add(subscription);
            return new Disposer(subscription, this.subscriptions);
        }

        /// <summary>
        /// Sends a service request.
        /// </summary>
        /// <param name="request">An <see cref="IServiceRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns an <see cref="IServiceResponse"/>.</returns>
        public async Task<IServiceResponse> RequestAsync(IServiceRequest request)
        {
            this.UpdateTimestamp(request);
            var operation = new ServiceOperation(request);
            using (var timeoutCts = new CancellationTokenSource((int)request.RequestHeader.TimeoutHint))
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, this.clientCts.Token))
            using (var registration = linkedCts.Token.Register(this.CancelRequest, operation, false))
            {
                if (this.pendingRequests.Post(operation))
                {
                    return await operation.Task.ConfigureAwait(false);
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
                this.pendingRequests = new BufferBlock<ServiceOperation>(new DataflowBlockOptions { CancellationToken = this.clientCts.Token });
                this.stateMachineTask = Task.Run(() => this.StateMachineAsync(this.clientCts.Token));
            }
        }

        /// <summary>
        /// Closes the communication channel to the remote endpoint.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
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
                try
                {
                    this.stateMachineTask.Wait(5000);
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// The state machine manages the state of the communications channel.
        /// </summary>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A task.</returns>
        private async Task StateMachineAsync(CancellationToken token = default(CancellationToken))
        {
            int reconnectDelay = 1000;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    this.Logger?.LogTrace($"Connecting in {reconnectDelay} ms.");
                    await Task.Delay(reconnectDelay, token).ConfigureAwait(false);

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
                            this.AutoCreateSubscriptionsAsync(localCts.Token),
                            this.PublishAsync(localCts.Token),
                            this.PublishAsync(localCts.Token),
                            this.PublishAsync(localCts.Token),
                            this.WhenChannelClosingAsync(localCts.Token),
                        };
                        var task = await Task.WhenAny(tasks).ConfigureAwait(false);
                        localCts.Cancel();
                        await Task.WhenAll(tasks).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception)
                {
                    reconnectDelay = Math.Min(reconnectDelay * 2, 20000);
                }

                // Closing
                this.State = CommunicationState.Closing;
                await this.CloseAsync().ConfigureAwait(false);

                // Closed
                this.State = CommunicationState.Closed;
            }
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
                        this.Logger?.LogInformation($"Discovering endpoints of '{this.discoveryUrl}'.");
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
                        this.Logger?.LogTrace($"Success discovering endpoints of '{this.discoveryUrl}'.");
                    }
                    catch (Exception ex)
                    {
                        this.Logger?.LogError($"Error discovering endpoints of '{this.discoveryUrl}'. {ex.Message}");
                        throw;
                    }
                }

                // throw here to exit state machine.
                token.ThrowIfCancellationRequested();

                // evaluate the user identity provider (may show a dialog).
                var userIdentity = await this.UserIdentityProvider(this.RemoteEndpoint);

                try
                {
                    this.linkToken?.Dispose();
                    if (this.innerChannel != null && this.innerChannel.State != CommunicationState.Closed)
                    {
                        await this.innerChannel.AbortAsync();
                    }

                    this.innerChannel = new UaTcpSessionChannel(
                        this.LocalDescription,
                        this.CertificateStore,
                        userIdentity,
                        this.RemoteEndpoint,
                        this.loggerFactory,
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
                    var subscriptionRequest = new CreateSubscriptionRequest
                    {
                        RequestedPublishingInterval = DefaultPublishingInterval,
                        RequestedMaxKeepAliveCount = DefaultKeepaliveCount,
                        RequestedLifetimeCount = (uint)(this.SessionTimeout / DefaultPublishingInterval),
                        PublishingEnabled = true,
                        Priority = 0
                    };
                    var subscriptionResponse = await this.CreateSubscriptionAsync(subscriptionRequest).ConfigureAwait(false);
                    this.subscriptionId = subscriptionResponse.SubscriptionId;
                }
                catch (Exception ex)
                {
                    this.Logger?.LogError($"Error opening channel with endpoint '{this.RemoteEndpoint.EndpointUrl}'. {ex.Message}");
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
                this.Logger?.LogInformation($"Closing channel with endpoint '{this.RemoteEndpoint?.EndpointUrl ?? this.discoveryUrl}'.");
                try
                {
                    switch (this.innerChannel.State)
                    {
                        case CommunicationState.Created:
                        case CommunicationState.Opening:
                        case CommunicationState.Faulted:
                            await this.innerChannel.AbortAsync(token).ConfigureAwait(false);
                            break;

                        case CommunicationState.Opened:
                            await this.innerChannel.CloseAsync(token).ConfigureAwait(false);
                            break;

                        case CommunicationState.Closing:
                        case CommunicationState.Closed:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    this.Logger?.LogError($"Error closing channel. {ex.Message}");
                }
            }
            finally
            {
                this.semaphore.Release();
            }
        }

        /// <summary>
        /// Creates the subscriptions on the server.
        /// </summary>
        /// <param name="token">A cancellation token. </param>
        /// <returns>A task.</returns>
        private async Task AutoCreateSubscriptionsAsync(CancellationToken token = default(CancellationToken))
        {
            var tcs = new TaskCompletionSource<bool>();
            NotifyCollectionChangedEventHandler handler = async (o, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (var subscription in e.NewItems.OfType<Subscription>())
                    {
                        var target = subscription.Target;
                        if (target == null)
                        {
                            continue;
                        }

                        try
                        {
                            // create the subscription.
                            var subscriptionRequest = new CreateSubscriptionRequest
                            {
                                RequestedPublishingInterval = subscription.PublishingInterval,
                                RequestedMaxKeepAliveCount = subscription.KeepAliveCount,
                                RequestedLifetimeCount = Math.Max(subscription.LifetimeCount, 3 * subscription.KeepAliveCount),
                                PublishingEnabled = subscription.PublishingEnabled
                            };
                            var subscriptionResponse = await this.CreateSubscriptionAsync(subscriptionRequest).ConfigureAwait(false);
                            var id = subscription.SubscriptionId = subscriptionResponse.SubscriptionId;

                            // add the items.
                            if (subscription.MonitoredItems.Count > 0)
                            {
                                var items = subscription.MonitoredItems.ToList();
                                var requests = items.Select(m => new MonitoredItemCreateRequest { ItemToMonitor = new ReadValueId { NodeId = m.NodeId, AttributeId = m.AttributeId, IndexRange = m.IndexRange }, MonitoringMode = m.MonitoringMode, RequestedParameters = new MonitoringParameters { ClientHandle = m.ClientId, DiscardOldest = m.DiscardOldest, QueueSize = m.QueueSize, SamplingInterval = m.SamplingInterval, Filter = m.Filter } }).ToArray();
                                var itemsRequest = new CreateMonitoredItemsRequest
                                {
                                    SubscriptionId = id,
                                    ItemsToCreate = requests,
                                };
                                var itemsResponse = await this.CreateMonitoredItemsAsync(itemsRequest).ConfigureAwait(false);
                                for (int i = 0; i < itemsResponse.Results.Length; i++)
                                {
                                    var item = items[i];
                                    var result = itemsResponse.Results[i];
                                    item.OnCreateResult(target, result);
                                    if (StatusCode.IsBad(result.StatusCode))
                                    {
                                        this.Logger?.LogError($"Error creating MonitoredItem for {item.NodeId}. {StatusCodes.GetDefaultMessage(result.StatusCode)}");
                                    }
                                }
                            }
                        }
                        catch (ServiceResultException ex)
                        {
                            this.Logger?.LogError($"Error creating subscription. {ex.Message}");
                            this.innerChannel.Fault(ex);
                        }
                    }
                }
                else if (e.Action == NotifyCollectionChangedAction.Remove)
                {
                    try
                    {
                        // delete the subscriptions.
                        var request = new DeleteSubscriptionsRequest
                        {
                            SubscriptionIds = e.OldItems.OfType<Subscription>().Select(s => s.SubscriptionId).ToArray()
                        };
                        await this.DeleteSubscriptionsAsync(request).ConfigureAwait(false);
                    }
                    catch (ServiceResultException ex)
                    {
                        this.Logger?.LogError($"Error deleting subscriptions. {ex.Message}");
                    }
                }
            };
            using (token.Register(state => ((TaskCompletionSource<bool>)state).TrySetResult(true), tcs, false))
            {
                try
                {
                    handler.Invoke(this.subscriptions, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, this.subscriptions.ToList()));
                    this.subscriptions.CollectionChanged += handler;
                    await tcs.Task;
                }
                finally
                {
                    this.subscriptions.CollectionChanged -= handler;
                    foreach (var subscription in this.subscriptions)
                    {
                        subscription.SubscriptionId = 0;
                    }
                }
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

                    this.syncContext.Post(
                        o =>
                        {
                            var pr = (PublishResponse)o;
                            var sub = this.subscriptions.FirstOrDefault(s => s.SubscriptionId == pr.SubscriptionId);
                            if (sub != null)
                            {
                                if (!sub.OnPublishResponse(pr))
                                {
                                    // target was garbage collected. So delete subscription from server.
                                    this.subscriptions.Remove(sub);
                                }
                            }
                        }, publishResponse);

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
                        this.Logger?.LogError($"Error publishing subscription. {ex.Message}");

                        // short delay, then retry.
                        await Task.Delay((int)DefaultPublishingInterval).ConfigureAwait(false);

                        // retry with fresh request.
                        publishRequest = new PublishRequest
                        {
                            RequestHeader = new RequestHeader { TimeoutHint = PublishTimeoutHint, ReturnDiagnostics = this.DiagnosticsHint },
                            SubscriptionAcknowledgements = new SubscriptionAcknowledgement[0]
                        };
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
            if (response.SubscriptionId != this.subscriptionId)
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
        /// Cancels the Request
        /// </summary>
        /// <param name="o">the ServiceTask.</param>
        private void CancelRequest(object o)
        {
            var operation = (ServiceOperation)o;
            if (operation.TrySetException(new ServiceResultException(StatusCodes.BadRequestTimeout)))
            {
                this.Logger?.LogTrace($"Canceled {operation.Request.GetType().Name} Handle: {operation.Request.RequestHeader.RequestHandle}");
            }
        }

        private struct Disposer : IDisposable
        {
            private readonly Subscription subscription;
            private readonly ObservableCollection<Subscription> subscriptions;

            public Disposer(Subscription subscription, ObservableCollection<Subscription> subscriptions)
            {
                this.subscription = subscription;
                this.subscriptions = subscriptions;
            }

            public void Dispose()
            {
                this.subscriptions.Remove(this.subscription);
                this.subscription.Dispose();
            }
        }
    }
}