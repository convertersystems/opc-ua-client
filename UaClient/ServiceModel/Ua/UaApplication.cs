﻿// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
        private static readonly object globalLock = new object();
        private static volatile UaApplication? appInstance;

        private readonly ILogger? logger;
        private readonly ConcurrentDictionary<string, Lazy<Task<UaTcpSessionChannel>>> channelMap;
        private readonly TaskCompletionSource<bool> completionTask = new TaskCompletionSource<bool>();
        private volatile TaskCompletionSource<bool> suspensionTask = new TaskCompletionSource<bool>();
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="UaApplication"/> class.
        /// </summary>
        /// <param name="localDescription">The <see cref="ApplicationDescription"/> of the local application.</param>
        /// <param name="certificateStore">The local certificate store.</param>
        /// <param name="identityProvider">An asynchronous function that provides the user identity. Provide an <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> or <see cref="X509Identity"/>.</param>
        /// <param name="mappedEndpoints">The mapped endpoints.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="options">The application options.</param>
        /// <param name="additionalTypes">Any additional types to be registered with encoder.</param>
        public UaApplication(
            ApplicationDescription localDescription,
            ICertificateStore? certificateStore,
            Func<EndpointDescription, Task<IUserIdentity>>? identityProvider,
            IEnumerable<MappedEndpoint> mappedEndpoints,
            ILoggerFactory? loggerFactory = null,
            UaApplicationOptions? options = null,
            IEnumerable<(ExpandedNodeId,Type)>? encodingTable = null)
        {
            if (localDescription == null)
            {
                throw new ArgumentNullException(nameof(localDescription));
            }

            this.LocalDescription = localDescription;
            this.CertificateStore = certificateStore;
            this.UserIdentityProvider = identityProvider;
            this.MappedEndpoints = mappedEndpoints;
            this.LoggerFactory = loggerFactory;
            this.Options = options ?? new UaApplicationOptions();
            this.EncodingTable = encodingTable;

            this.logger = loggerFactory?.CreateLogger<UaApplication>();
            this.channelMap = new ConcurrentDictionary<string, Lazy<Task<UaTcpSessionChannel>>>();

            lock (globalLock)
            {
                if (appInstance != null)
                {
                    throw new InvalidOperationException("You can only create a single instance of this type.");
                }

                appInstance = this;
            }
        }

        /// <summary>
        /// Gets the current <see cref="UaApplication"/>.
        /// </summary>
        public static UaApplication? Current => appInstance;

        /// <summary>
        /// Gets the <see cref="ApplicationDescription"/> of the local application.
        /// </summary>
        public ApplicationDescription LocalDescription { get; }

        /// <summary>
        /// Gets the local certificate store.
        /// </summary>
        public ICertificateStore? CertificateStore { get; }

        /// <summary>
        /// Gets an asynchronous function that provides the identity of the user. Supports <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> and <see cref="X509Identity"/>.
        /// </summary>
        public Func<EndpointDescription, Task<IUserIdentity>>? UserIdentityProvider { get; }

        /// <summary>
        /// Gets the mapped endpoints.
        /// </summary>
        public IEnumerable<MappedEndpoint> MappedEndpoints { get; }

        /// <summary>
        /// Gets the logger factory.
        /// </summary>
        public ILoggerFactory? LoggerFactory { get; }

        /// <summary>
        /// Gets the application options.
        /// </summary>
        public UaApplicationOptions Options { get; }

        /// <summary>
        /// Gets any additional types to be registered with encoder.
        /// </summary>
        public IEnumerable<(ExpandedNodeId,Type)>? EncodingTable { get; }

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

                foreach (var value in this.channelMap.Values)
                {
                    var task = value.Value;
                    if (task.Status == TaskStatus.RanToCompletion)
                    {
                        try
                        {
                            task.Result.CloseAsync().Wait(2000);
                        }
                        catch { }
                    }
                }
            }
        }

        /// <summary>
        /// Suspends the communication channels to the remote endpoints.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that suspends the communication channel.</returns>
        public async Task SuspendAsync()
        {
            this.logger?.LogTrace($"UaApplication suspended.");
            if (this.suspensionTask.Task.IsCompleted)
            {
                this.suspensionTask = new TaskCompletionSource<bool>();
            }

            foreach (var value in this.channelMap.Values)
            {
                var task = value.Value;
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    try
                    {
                        await task.Result.CloseAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                    }
                }
            }
        }

        /// <summary>
        /// Creates the communication channels to the remote endpoints.
        /// </summary>
        public void Run()
        {
            this.logger?.LogTrace($"UaApplication running.");
            this.suspensionTask.TrySetResult(true);
        }

        /// <summary>
        /// Checks if application state is suspended.
        /// </summary>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A task.</returns>
        private Task CheckSuspension(CancellationToken token = default)
        {
            return this.suspensionTask.Task.WithCancellation(token);
        }

        /// <summary>
        /// Gets or creates an <see cref="UaTcpSessionChannel"/>.
        /// </summary>
        /// <param name="endpointUrl">The endpoint url of the OPC UA server</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A <see cref="UaTcpSessionChannel"/>.</returns>
        public async Task<UaTcpSessionChannel> GetChannelAsync(string endpointUrl, CancellationToken token = default)
        {
            this.logger?.LogTrace($"Begin getting UaTcpSessionChannel for {endpointUrl}");
            if (string.IsNullOrEmpty(endpointUrl))
            {
                throw new ArgumentNullException(nameof(endpointUrl));
            }

            await this.CheckSuspension(token).ConfigureAwait(false);

            var ch = await this.channelMap
                .GetOrAdd(endpointUrl, k => new Lazy<Task<UaTcpSessionChannel>>(() => Task.Run(() => this.CreateChannelAsync(k, token))))
                .Value
                .ConfigureAwait(false);

            return ch;
        }

        private async Task<UaTcpSessionChannel> CreateChannelAsync(string endpointUrl, CancellationToken token = default)
        {
            try
            {
                this.logger?.LogTrace($"Begin creating UaTcpSessionChannel for {endpointUrl}");
                await this.CheckSuspension(token).ConfigureAwait(false);

                EndpointDescription endpoint;
                var mappedEndpoint = this.MappedEndpoints?.LastOrDefault(m => m.RequestedUrl == endpointUrl);
                if (mappedEndpoint?.Endpoint != null)
                {
                    endpoint = mappedEndpoint.Endpoint;
                }
                else
                {
                    endpoint = new EndpointDescription { EndpointUrl = endpointUrl };
                }

                var channel = new UaTcpSessionChannel(
                    this.LocalDescription,
                    this.CertificateStore,
                    this.UserIdentityProvider,
                    endpoint,
                    this.LoggerFactory,
                    this.Options,
                    this.EncodingTable);

                channel.Faulted += (s, e) =>
                {
                    this.logger?.LogTrace($"Error creating UaTcpSessionChannel for {endpointUrl}. OnFaulted");
                    var ch = (UaTcpSessionChannel)s!;
                    try
                    {
                        ch.AbortAsync().Wait();
                    }
                    catch
                    {
                    }
                };

                channel.Closing += (s, e) =>
                {
                    this.logger?.LogTrace($"Removing UaTcpSessionChannel for {endpointUrl} from channelMap.");
                    this.channelMap.TryRemove(endpointUrl, out _);
                };

                await channel.OpenAsync(token).ConfigureAwait(false);
                this.logger?.LogTrace($"Success creating UaTcpSessionChannel for {endpointUrl}.");
                return channel;

            }
            catch (Exception ex)
            {
                this.logger?.LogTrace($"Error creating UaTcpSessionChannel for {endpointUrl}. {ex.Message}");
                throw;
            }
        }
    }
}
