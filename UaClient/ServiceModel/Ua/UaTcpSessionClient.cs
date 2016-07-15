// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Prism.Events;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
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
        private static readonly MetroLog.ILogger Log = MetroLog.LogManagerFactory.DefaultLogManager.GetLogger<UaTcpSessionClient>();

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
        private readonly PubSubEvent<PublishResponse> publishEvent = new PubSubEvent<PublishResponse>();
        private readonly PubSubEvent<CommunicationState> stateChangedEvent = new PubSubEvent<CommunicationState>();
        private bool disposed = false;
        private Task stateMachineTask;
        private string discoveryUrl;
        private UaTcpSessionChannel innerChannel;
        private uint id;

        /// <summary>
        /// Initializes a new instance of the <see cref="UaTcpSessionClient"/> class.
        /// </summary>
        /// <param name="localDescription">The <see cref="ApplicationDescription"/> of the local application.</param>
        /// <param name="localCertificate">The <see cref="X509Certificate2"/> of the local application.</param>
        /// <param name="userIdentity">The user identity or null if anonymous. Supports <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> and <see cref="X509Identity"/>.</param>
        /// <param name="remoteEndpoint">The <see cref="EndpointDescription"/> of the remote application. Obtained from a prior call to UaTcpDiscoveryClient.GetEndpoints.</param>
        public UaTcpSessionClient(ApplicationDescription localDescription, X509Certificate2 localCertificate, IUserIdentity userIdentity, EndpointDescription remoteEndpoint)
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
            this.stateMachineTask = this.StateMachine(this.cancellationTokenSource.Token);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UaTcpSessionClient"/> class.
        /// </summary>
        /// <param name="localDescription">The <see cref="ApplicationDescription"/> of the local application.</param>
        /// <param name="localCertificate">The <see cref="X509Certificate2"/> of the local application.</param>
        /// <param name="userIdentity">The user identity or null if anonymous. Supports <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> and <see cref="X509Identity"/>.</param>
        /// <param name="discoveryUrl">The url of the remote application</param>
        public UaTcpSessionClient(ApplicationDescription localDescription, X509Certificate2 localCertificate, IUserIdentity userIdentity, string discoveryUrl)
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
                        Task.Run(() => this.CloseAsync()).GetAwaiter().GetResult();
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
                    await this.OpenAsync(cancellationToken);

                    // Opened.
                    reconnectDelay = 1000;
                    this.stateChangedEvent.Publish(CommunicationState.Opened);
                    using (var localCts = CancellationTokenSource.CreateLinkedTokenSource(new[] { cancellationToken }))
                    {
                        var tasks = new[]
                        {
                            this.PublishAsync(localCts.Token),
                            this.PublishAsync(localCts.Token),
                            this.PublishAsync(localCts.Token),
                            this.WhenChannelClosingAsync(localCts.Token),
                        };
                        await Task.WhenAny(tasks);
                        localCts.Cancel();
                        await Task.WhenAll(tasks);
                    }

                    // Closing
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
        /// Opens a session with the remote endpoint.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A task.</returns>
        private async Task OpenAsync(CancellationToken cancellationToken = default(CancellationToken))
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

                    // create the internal subscription.
                    this.publishEvent.Subscribe(this.OnPublishResponse, ThreadOption.PublisherThread, false);
                    var subscriptionRequest = new CreateSubscriptionRequest
                    {
                        RequestedPublishingInterval = DefaultPublishingInterval,
                        RequestedMaxKeepAliveCount = DefaultKeepaliveCount,
                        RequestedLifetimeCount = (uint)(this.SessionTimeout / DefaultPublishingInterval),
                        PublishingEnabled = true,
                        Priority = 0
                    };
                    var subscriptionResponse = await this.CreateSubscriptionAsync(subscriptionRequest);
                    this.id = subscriptionResponse.SubscriptionId;
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
        private async Task CloseAsync(CancellationToken cancellationToken = default(CancellationToken))
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
                    publishResponse.MoreNotifications = false; // reset flag indicates message unhandled.

                    this.publishEvent.Publish(publishResponse);

                    // If event was not handled,
                    if (!publishResponse.MoreNotifications)
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
            var onClosing = new TaskCompletionSource<bool>();
            var handler = new EventHandler((s, e) => onClosing.TrySetResult(true));

            using (cancellationToken.Register(state => ((TaskCompletionSource<bool>)state).TrySetCanceled(), onClosing, false))
            {
                this.innerChannel.Closing += handler;
                try
                {
                    if (this.State == CommunicationState.Closing || this.State == CommunicationState.Closed || this.State == CommunicationState.Faulted)
                    {
                        return;
                    }

                    await onClosing.Task;
                }
                finally
                {
                    this.innerChannel.Closing -= handler;
                }
            }
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
                response.MoreNotifications = true; // set flag indicates message handled.
            }
        }
    }
}