// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Workstation.ServiceModel.Ua.Channels;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// A <see cref="UaTcpSessionChannel"/> factory.
    /// </summary>
    public class UaTcpSessionChannelFactory : IDisposable
    {
        private readonly ILogger logger;
        private Task stateMachineTask;
        private CancellationTokenSource stateMachineCts;
        private volatile TaskCompletionSource<UaTcpSessionChannel> channelSource = new TaskCompletionSource<UaTcpSessionChannel>();
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="UaTcpSessionChannelFactory"/> class.
        /// </summary>
        /// <param name="localDescription">The <see cref="ApplicationDescription"/> of the local application.</param>
        /// <param name="certificateStore">The local certificate store.</param>
        /// <param name="userIdentityProvider">An asynchronous function that provides the user identity. Provide an <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> or <see cref="X509Identity"/>.</param>
        /// <param name="remoteEndpoint">The <see cref="EndpointDescription"/> of the remote application. Obtained from a prior call to UaTcpDiscoveryClient.GetEndpoints.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="options">The channel options.</param>
        public UaTcpSessionChannelFactory(
            ApplicationDescription localDescription,
            ICertificateStore certificateStore,
            Func<EndpointDescription, Task<IUserIdentity>> userIdentityProvider,
            EndpointDescription remoteEndpoint,
            ILoggerFactory loggerFactory = null,
            UaTcpSessionChannelOptions options = null)
        {
            if (localDescription == null)
            {
                throw new ArgumentNullException(nameof(localDescription));
            }

            if (certificateStore == null)
            {
                throw new ArgumentNullException(nameof(certificateStore));
            }

            if (userIdentityProvider == null)
            {
                throw new ArgumentNullException(nameof(userIdentityProvider));
            }

            if (remoteEndpoint == null)
            {
                throw new ArgumentNullException(nameof(remoteEndpoint));
            }

            this.LocalDescription = localDescription;
            this.CertificateStore = certificateStore;
            this.UserIdentityProvider = userIdentityProvider;
            this.RemoteEndpoint = remoteEndpoint;
            this.LoggerFactory = loggerFactory;
            this.Options = options ?? new UaTcpSessionChannelOptions();

            this.logger = loggerFactory?.CreateLogger<UaTcpSessionChannelFactory>();
            this.stateMachineCts = new CancellationTokenSource();
            this.stateMachineTask = Task.Run(() => this.StateMachineAsync(this.stateMachineCts.Token));
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
        public EndpointDescription RemoteEndpoint { get; }

        /// <summary>
        /// Gets the logger factory.
        /// </summary>
        public ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// Gets the channel options.
        /// </summary>
        public UaTcpSessionChannelOptions Options { get; }

        /// <summary>
        /// Gets the session channel.
        /// </summary>
        /// <param name="token">A cancellation token. Use an already canceled token to return immediately with channel.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<UaTcpSessionChannel> GetChannelAsync(CancellationToken token = default(CancellationToken))
        {
            return this.channelSource.Task.WithCancellation(token);
        }

        /// <summary>
        /// Suspends the communication channel to the remote endpoint.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that suspends the communication channel.</returns>
        public Task SuspendAsync()
        {
            this.stateMachineCts?.Cancel();
            return this.stateMachineTask;
        }

        /// <summary>
        /// Resumes the communication channel to the remote endpoint.
        /// </summary>
        public void Resume()
        {
            if (this.stateMachineCts.IsCancellationRequested)
            {
                this.stateMachineCts = new CancellationTokenSource();
                this.stateMachineTask = Task.Run(() => this.StateMachineAsync(this.stateMachineCts.Token));
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
                this.stateMachineCts?.Cancel();
                try
                {
                    this.stateMachineTask.Wait(2000);
                }
                catch { }
            }
        }

        /// <summary>
        /// Signals the channel state is Closing.
        /// </summary>
        /// <param name="channel">The session channel. </param>
        /// <param name="token">A cancellation token. </param>
        /// <returns>A task.</returns>
        private async Task WhenChannelClosingAsync(UaTcpSessionChannel channel, CancellationToken token = default(CancellationToken))
        {
            var tcs = new TaskCompletionSource<bool>();
            EventHandler handler = (o, e) =>
            {
                tcs.TrySetResult(true);
            };
            using (token.Register(state => ((TaskCompletionSource<bool>)state).TrySetCanceled(), tcs, false))
            {
                try
                {
                    channel.Closing += handler;

                    if (channel.State == CommunicationState.Opened)
                    {
                        await tcs.Task;
                    }
                }
                finally
                {
                    channel.Closing -= handler;
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
            int reconnectDelay = 2000;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    // open session channel.
                    var channel = new UaTcpSessionChannel(
                        this.LocalDescription,
                        this.CertificateStore,
                        this.UserIdentityProvider,
                        this.RemoteEndpoint,
                        this.LoggerFactory,
                        this.Options);

                    await channel.OpenAsync(token).ConfigureAwait(false);
                    reconnectDelay = 2000;

                    // tell waiting subscriptions that channel is ready.
                    this.channelSource.TrySetResult(channel);

                    try
                    {
                        // wait here until channel closing.
                        await this.WhenChannelClosingAsync(channel, token);
                    }
                    catch
                    {
                    }

                    // reset channel source.
                    this.channelSource = new TaskCompletionSource<UaTcpSessionChannel>();

                    // short delay to allow subscriptions to be deleted.
                    await Task.Delay(1000).ConfigureAwait(false);

                    // close the channel.
                    switch (channel.State)
                    {
                        case CommunicationState.Created:
                        case CommunicationState.Opening:
                        case CommunicationState.Faulted:
                            await channel.AbortAsync().ConfigureAwait(false);
                            break;

                        case CommunicationState.Opened:
                            await channel.CloseAsync().ConfigureAwait(false);
                            break;

                        case CommunicationState.Closing:
                        case CommunicationState.Closed:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    this.logger?.LogError($"Error connecting to server. {ex.Message}");

                    if (!token.IsCancellationRequested)
                    {
                        this.logger?.LogTrace($"Connecting in {reconnectDelay} ms.");
                        try
                        {
                            await Task.Delay(reconnectDelay, token).ConfigureAwait(false);
                            reconnectDelay = Math.Min(reconnectDelay * 2, 20000);
                        }
                        catch { }
                    }
                }
            }
        }
    }
}
