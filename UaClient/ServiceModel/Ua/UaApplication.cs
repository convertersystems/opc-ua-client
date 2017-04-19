// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Workstation.ServiceModel.Ua.Channels;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// A <see cref="UaApplication"/>.
    /// </summary>
    public class UaApplication : IDisposable
    {
        private static object globalLock = new object();
        private static volatile UaApplication appInstance;

        private readonly ILogger logger;
        private readonly ConcurrentDictionary<string, UaTcpSessionChannelFactory> channelMap = new ConcurrentDictionary<string, UaTcpSessionChannelFactory>();
        private readonly TaskCompletionSource<bool> completionTask = new TaskCompletionSource<bool>();
        private volatile TaskCompletionSource<bool> suspensionTask = new TaskCompletionSource<bool>();
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="UaApplication"/> class.
        /// </summary>
        /// <param name="localDescription">The <see cref="ApplicationDescription"/> of the local application.</param>
        /// <param name="certificateStore">The local certificate store.</param>
        /// <param name="userIdentityProvider">An asynchronous function that provides the user identity. Provide an <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> or <see cref="X509Identity"/>.</param>
        /// <param name="endpoints">The configured endpoints.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="options">The application options.</param>
        public UaApplication(
            ApplicationDescription localDescription,
            ICertificateStore certificateStore,
            Func<EndpointDescription, Task<IUserIdentity>> userIdentityProvider,
            IDictionary<string, EndpointDescription> endpoints,
            ILoggerFactory loggerFactory = null,
            UaApplicationOptions options = null)
        {
            lock (globalLock)
            {
                if (appInstance != null)
                {
                    throw new InvalidOperationException("You can only create a single instance of this type.");
                }

                appInstance = this;
            }

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

            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            this.LocalDescription = localDescription;
            this.CertificateStore = certificateStore;
            this.UserIdentityProvider = userIdentityProvider;
            this.ConfiguredEndpoints = new ReadOnlyDictionary<string, EndpointDescription>(endpoints);
            this.LoggerFactory = loggerFactory;
            this.Options = options ?? new UaApplicationOptions();

            this.logger = loggerFactory?.CreateLogger<UaApplication>();
        }

        /// <summary>
        /// Gets the current <see cref="UaApplication"/>.
        /// </summary>
        public static UaApplication Current => appInstance;

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
        /// Gets the configured endpoints.
        /// </summary>
        public IReadOnlyDictionary<string, EndpointDescription> ConfiguredEndpoints { get; }

        /// <summary>
        /// Gets the logger factory.
        /// </summary>
        public ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// Gets the application options.
        /// </summary>
        public UaApplicationOptions Options { get; }

        /// <summary>
        /// Gets a System.Threading.Tasks.Task that represents the completion of the UaApplication.
        /// </summary>
        internal Task Completion => this.completionTask.Task;

        /// <summary>
        /// Closes the communication channels.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Closes the communication channels to the remote endpoint.
        /// </summary>
        /// <param name="disposing">If true, then dispose managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing & !this.disposed)
            {
                this.disposed = true;
                this.completionTask.TrySetResult(true);

                lock (globalLock)
                {
                    appInstance = null;
                }

                foreach (var factory in this.channelMap.Values)
                {
                    factory.Dispose();
                }

                this.channelMap.Clear();
            }
        }

        /// <summary>
        /// Suspends the communication channels to the remote endpoints.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that suspends the communication channel.</returns>
        public async Task SuspendAsync()
        {
            if (this.suspensionTask.Task.IsCompleted)
            {
                this.suspensionTask = new TaskCompletionSource<bool>();
            }

            foreach (var factory in this.channelMap.Values)
            {
                await factory.SuspendAsync().ConfigureAwait(false);
            }

            this.channelMap.Clear();
        }

        /// <summary>
        /// Creates the communication channels to the remote endpoints.
        /// </summary>
        public void Run()
        {
            foreach (var endpoint in this.ConfiguredEndpoints)
            {
                this.channelMap.TryAdd(endpoint.Key, new UaTcpSessionChannelFactory(this.LocalDescription, this.CertificateStore, this.UserIdentityProvider, endpoint.Value, this.LoggerFactory, this.Options));
            }

            this.suspensionTask.TrySetResult(true);
        }

        /// <summary>
        /// Checks if application state is suspended.
        /// </summary>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A task.</returns>
        private Task CheckSuspension(CancellationToken token = default(CancellationToken))
        {
            return this.suspensionTask.Task.WithCancellation(token);
        }

        /// <summary>
        /// Gets the named <see cref="UaTcpSessionChannel"/>.
        /// </summary>
        /// <param name="name">The name of the configured endpoint.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A <see cref="UaTcpSessionChannel"/>.</returns>
        public async Task<UaTcpSessionChannel> GetChannelAsync(string name, CancellationToken token = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            await this.CheckSuspension(token).ConfigureAwait(false);

            UaTcpSessionChannelFactory factory;
            if (!this.channelMap.TryGetValue(name, out factory))
            {
                throw new InvalidOperationException("The endpoint name was not found.");
            }

            return await factory.GetChannelAsync(token).ConfigureAwait(false);
        }
    }
}
