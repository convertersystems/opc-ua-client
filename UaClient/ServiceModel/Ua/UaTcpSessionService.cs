// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Workstation.ServiceModel.Ua.Channels;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// A service for browsing, reading, writing and subscribing to nodes of an OPC UA server.
    /// </summary>
    public class UaTcpSessionService : ISessionClient, IDisposable
    {
        private const double DefaultPublishingInterval = 1000f;
        private const uint DefaultKeepaliveCount = 10;
        private const uint PublishTimeoutHint = 120 * 1000; // 2 minutes
        private static readonly MetroLog.ILogger Log = MetroLog.LogManagerFactory.DefaultLogManager.GetLogger<UaTcpSessionService>();

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
        private bool disposed = false;
        private Task stateMachineTask;
        private string discoveryUrl;
        private UaTcpSessionChannel innerChannel;

        /// <summary>
        /// Initializes a new instance of the <see cref="UaTcpSessionService"/> class.
        /// </summary>
        /// <param name="localDescription">The <see cref="ApplicationDescription"/> of the local application.</param>
        /// <param name="localCertificate">The <see cref="X509Certificate2"/> of the local application.</param>
        /// <param name="userIdentity">The user identity or null if anonymous. Supports <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> and <see cref="X509Identity"/>.</param>
        /// <param name="remoteEndpoint">The <see cref="EndpointDescription"/> of the remote application. Obtained from a prior call to UaTcpDiscoveryClient.GetEndpoints.</param>
        public UaTcpSessionService(ApplicationDescription localDescription, X509Certificate2 localCertificate, IUserIdentity userIdentity, EndpointDescription remoteEndpoint)
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
            this.Subscriptions = new SubscriptionCollection(this);
            this.stateMachineTask = this.StateMachine(this.cancellationTokenSource.Token);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UaTcpSessionService"/> class.
        /// </summary>
        /// <param name="localDescription">The <see cref="ApplicationDescription"/> of the local application.</param>
        /// <param name="localCertificate">The <see cref="X509Certificate2"/> of the local application.</param>
        /// <param name="userIdentity">The user identity or null if anonymous. Supports <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> and <see cref="X509Identity"/>.</param>
        /// <param name="discoveryUrl">The url of the remote application</param>
        public UaTcpSessionService(ApplicationDescription localDescription, X509Certificate2 localCertificate, IUserIdentity userIdentity, string discoveryUrl)
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
            this.Subscriptions = new SubscriptionCollection(this);
            this.stateMachineTask = this.StateMachine(this.cancellationTokenSource.Token);
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
        /// Gets or sets the requested number of milliseconds that a session may be unused before being closed by the server.
        /// </summary>
        public double SessionTimeout { get; set; } = UaTcpSessionChannel.DefaultSessionTimeout;

        /// <summary>
        /// Gets or sets the default number of milliseconds that may elapse before an operation is cancelled by the service.
        /// </summary>
        public uint TimeoutHint { get; set; } = UaTcpSecureChannel.DefaultTimeoutHint;

        /// <summary>
        /// Gets or sets the default diagnostics flags to be requested by the service.
        /// </summary>
        public uint DiagnosticsHint { get; set; } = UaTcpSecureChannel.DefaultDiagnosticsHint;

        /// <summary>
        /// Gets or sets the size of the receive buffer.
        /// </summary>
        public uint LocalReceiveBufferSize { get; set; } = UaTcpTransportChannel.DefaultBufferSize;

        /// <summary>
        /// Gets or sets the size of the send buffer.
        /// </summary>
        public uint LocalSendBufferSize { get; set; } = UaTcpTransportChannel.DefaultBufferSize;

        /// <summary>
        /// Gets or sets the maximum total size of a message.
        /// </summary>
        public uint LocalMaxMessageSize { get; set; } = UaTcpTransportChannel.DefaultMaxMessageSize;

        /// <summary>
        /// Gets or sets the maximum number of message chunks.
        /// </summary>
        public uint LocalMaxChunkCount { get; set; } = UaTcpTransportChannel.DefaultMaxChunkCount;

        /// <summary>
        /// Gets the state of communication channel.
        /// </summary>
        public CommunicationState State => this.innerChannel?.State ?? CommunicationState.Closed;

        /// <summary>
        /// Gets the collection of subscriptions.
        /// </summary>
        public SubscriptionCollection Subscriptions { get; }

        /// <summary>
        /// Sends a service request.
        /// </summary>
        /// <param name="request">An <see cref="IServiceRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns an <see cref="IServiceResponse"/>.</returns>
        public Task<IServiceResponse> RequestAsync(IServiceRequest request)
        {
            var channel = this.innerChannel;
            if (channel == null || channel.State != CommunicationState.Opened)
            {
                throw new ServiceResultException(StatusCodes.BadServerNotConnected);
            }

            return channel.RequestAsync(request);
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
                this.cancellationTokenSource.Cancel();
                if (!(this.State == CommunicationState.Closed || this.State == CommunicationState.Faulted))
                {
                    try
                    {
                        Task.Run(() => this.CloseSessionAsync()).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        Log.Warn($"Error disposing UaTcpSessionService. {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// The state machine manages the state of the communications channel.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A task.</returns>
        private async Task StateMachine(CancellationToken cancellationToken = default(CancellationToken))
        {
            int reconnectDelay = 1000;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Opening.
                    await this.OpenSessionAsync(cancellationToken);

                    // Opened.
                    reconnectDelay = 1000;
                    using (var localCts = CancellationTokenSource.CreateLinkedTokenSource(new[] { cancellationToken }))
                    {
                        var tasks = new[]
                        {
                            this.CreateNewSubscriptionsAsync(localCts.Token),
                            this.PublishAsync(localCts.Token),
                            this.PublishAsync(localCts.Token),
                            this.PublishAsync(localCts.Token),
                            this.WhenChannelClosingAsync(localCts.Token),
                        };
                        await Task.WhenAny(tasks);
                        localCts.Cancel();
                        await Task.WhenAll(tasks);
                    }
                }
                catch (OperationCanceledException)
                {
                    Log.Trace("StateMachine canceled, returning.");
                    return;
                }
                catch (Exception)
                {
                    Log.Trace("StateMachine exception, retrying.");
                    await Task.Delay(reconnectDelay, cancellationToken);
                    reconnectDelay = Math.Min(reconnectDelay * 2, 20000);
                }
            }
        }

        /// <summary>
        /// Opens the session with the remote endpoint.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A task.</returns>
        private async Task OpenSessionAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await this.semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (this.RemoteEndpoint == null)
                {
                    // If specific endpoint is not provided, use discovery to select endpoint with highest
                    // security level.
                    try
                    {
                        Log.Info($"Discovering endpoints of '{this.discoveryUrl}'.");
                        var getEndpointsRequest = new GetEndpointsRequest
                        {
                            EndpointUrl = this.discoveryUrl,
                            ProfileUris = new[] { TransportProfileUris.UaTcpTransport }
                        };
                        var getEndpointsResponse = await UaTcpDiscoveryClient.GetEndpointsAsync(getEndpointsRequest);
                        if (getEndpointsResponse.Endpoints == null || getEndpointsResponse.Endpoints.Length == 0)
                        {
                            throw new InvalidOperationException($"'{this.discoveryUrl}' returned no endpoints.");
                        }

                        this.RemoteEndpoint = getEndpointsResponse.Endpoints.OrderBy(e => e.SecurityLevel).Last();
                    }
                    catch (Exception ex)
                    {
                        Log.Warn($"Error discovering endpoints of '{this.discoveryUrl}'. {ex.Message}");
                        throw;
                    }
                }

                // throw here to exit state machine.
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    Log.Info($"Opening UaTcpSessionChannel with endpoint '{this.RemoteEndpoint.EndpointUrl}'.");
                    this.innerChannel = new UaTcpSessionChannel(this.LocalDescription, this.LocalCertificate, this.UserIdentity, this.RemoteEndpoint)
                    {
                        SessionTimeout = this.SessionTimeout,
                        TimeoutHint = this.TimeoutHint,
                        DiagnosticsHint = this.DiagnosticsHint,
                        LocalSendBufferSize = this.LocalSendBufferSize,
                        LocalReceiveBufferSize = this.LocalReceiveBufferSize,
                        LocalMaxMessageSize = this.LocalMaxMessageSize,
                        LocalMaxChunkCount = this.LocalMaxChunkCount,
                    };

                    await this.innerChannel.OpenAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    Log.Warn($"Error opening UaTcpSessionChannel with endpoint '{this.RemoteEndpoint.EndpointUrl}'. {ex.Message}");
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
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A task.</returns>
        private async Task CloseSessionAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await this.semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                try
                {
                    await this.innerChannel.CloseAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    Log.Warn($"Error closing UaTcpSessionChannel. {ex.Message}");
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
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A task.</returns>
        private async Task PublishAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var publishRequest = new PublishRequest
            {
                RequestHeader = new RequestHeader { TimeoutHint = PublishTimeoutHint, ReturnDiagnostics = this.DiagnosticsHint },
                SubscriptionAcknowledgements = new SubscriptionAcknowledgement[0]
            };
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var publishResponse = await this.PublishAsync(publishRequest);

                    // Views and view models may be abandoned at any time. This code detects when a subscription
                    // is garbage collected, and deletes the corresponding subscription from the server.
                    var handled = false;

                    foreach (var subscription in this.Subscriptions)
                    {
                        handled |= subscription.OnPublishResponse(publishResponse);
                    }

                    // If event was not handled,
                    if (!handled)
                    {
                        // subscription was garbage collected. So delete from server.
                        var request = new DeleteSubscriptionsRequest
                        {
                            SubscriptionIds = new uint[] { publishResponse.SubscriptionId }
                        };
                        await this.DeleteSubscriptionsAsync(request);
                    }

                    publishRequest = new PublishRequest
                    {
                        RequestHeader = new RequestHeader { TimeoutHint = PublishTimeoutHint, ReturnDiagnostics = this.DiagnosticsHint },
                        SubscriptionAcknowledgements = new[] { new SubscriptionAcknowledgement { SequenceNumber = publishResponse.NotificationMessage.SequenceNumber, SubscriptionId = publishResponse.SubscriptionId } }
                    };
                }
                catch (ServiceResultException ex)
                {
                    Log.Warn($"Error publishing subscription. {ex.Message}");

                    // short delay, then retry.
                    await Task.Delay((int)DefaultPublishingInterval);
                }
            }
        }

        /// <summary>
        /// Waits until the communication channel is closing, closed or faulted.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token. </param>
        /// <returns>A task.</returns>
        private async Task WhenChannelClosingAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var tcs = new TaskCompletionSource<bool>();
            var onClosing = new EventHandler((s, e) => tcs.TrySetResult(true));

            using (cancellationToken.Register(state => ((TaskCompletionSource<bool>)state).TrySetCanceled(), tcs, false))
            {
                this.innerChannel.Closing += onClosing;
                try
                {
                    if (this.State == CommunicationState.Closing || this.State == CommunicationState.Closed || this.State == CommunicationState.Faulted)
                    {
                        return;
                    }

                    await tcs.Task;
                }
                finally
                {
                    this.innerChannel.Closing -= onClosing;
                }
            }
        }

        /// <summary>
        /// Creates new subscriptions on the server.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token. </param>
        /// <returns>A task.</returns>
        private async Task CreateNewSubscriptionsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var subscriptionsChanged = new AsyncAutoResetEvent();
            var handler = new NotifyCollectionChangedEventHandler((s, e) => subscriptionsChanged.Set());
            this.Subscriptions.CollectionChanged += handler;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    foreach (var subscription in this.Subscriptions)
                    {
                        if (subscription.Id == 0)
                        {
                            try
                            {
                                // create the subscription.
                                var subscriptionRequest = new CreateSubscriptionRequest
                                {
                                    RequestedPublishingInterval = subscription.PublishingInterval,
                                    RequestedMaxKeepAliveCount = subscription.KeepAliveCount,
                                    RequestedLifetimeCount = subscription.LifetimeCount > 0 ? subscription.LifetimeCount : (uint)(this.SessionTimeout / subscription.PublishingInterval),
                                    PublishingEnabled = false, // initially
                                    Priority = subscription.Priority
                                };
                                var subscriptionResponse = await this.CreateSubscriptionAsync(subscriptionRequest);
                                var id = subscription.Id = subscriptionResponse.SubscriptionId;

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
                                    var itemsResponse = await this.CreateMonitoredItemsAsync(itemsRequest);
                                    for (int i = 0; i < itemsResponse.Results.Length; i++)
                                    {
                                        var item = items[i];
                                        var result = itemsResponse.Results[i];
                                        item.ServerId = result.MonitoredItemId;
                                        if (StatusCode.IsBad(result.StatusCode))
                                        {
                                            Log.Warn($"Error response from MonitoredItemCreateRequest for {item.NodeId}. {result.StatusCode}");
                                        }
                                    }
                                }

                                // start publishing.
                                if (subscription.PublishingEnabled)
                                {
                                    var modeRequest = new SetPublishingModeRequest
                                    {
                                        SubscriptionIds = new[] { id },
                                        PublishingEnabled = true,
                                    };
                                    var modeResponse = await this.SetPublishingModeAsync(modeRequest);
                                }
                            }
                            catch (ServiceResultException ex)
                            {
                                Log.Warn($"Error creating subscription '{subscription.GetType().Name}'. {ex.Message}");
                            }
                        }
                    }

                    await subscriptionsChanged.WaitAsync().WithCancellation(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                this.Subscriptions.CollectionChanged -= handler;
            }
        }
    }
}