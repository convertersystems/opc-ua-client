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
    /// A service for discovery of remote OPC UA servers and their endpoints.
    /// </summary>
    public class DiscoveryService : ICommunicationObject, IDisposable
    {
        private readonly ClientSecureChannel innerChannel;
        private readonly SemaphoreSlim semaphore;
        private readonly ILogger? logger;
        private bool disposed = false;

        private DiscoveryService(EndpointDescription remoteEndpoint, ILoggerFactory? loggerFactory = null, ClientSecureChannelOptions? options = null, StackProfile? stackProfile = null)
        {
            this.innerChannel = new ClientSecureChannel(new ApplicationDescription { ApplicationName = nameof(DiscoveryService) }, null, remoteEndpoint, loggerFactory, options, stackProfile);
            this.semaphore = new SemaphoreSlim(1);
            this.logger = loggerFactory?.CreateLogger<DiscoveryService>();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                semaphore?.Dispose();
                innerChannel.Dispose();
                disposed = true;
            }
        }

        /// <summary>
        /// Gets the <see cref="EndpointDescription"/> of the remote application.
        /// </summary>
        public EndpointDescription RemoteEndpoint => this.innerChannel.RemoteEndpoint;

        /// <summary>
        /// Gets a value that indicates the current state of the communication object.
        /// </summary>
        public CommunicationState State => this.innerChannel.State;

        /// <summary>
        /// This Service returns the Servers known to a Server or Discovery Server.
        /// </summary>
        /// <param name="request">a request.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part4/5.4.2/">OPC UA specification Part 4: Services, 5.4.2</seealso>
        public static async Task<FindServersResponse> FindServersAsync(FindServersRequest request, ILoggerFactory? loggerFactory = null, UaApplicationOptions? options = null, StackProfile? stackProfile = null)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            using var client = new DiscoveryService(
                new EndpointDescription
                {
                    EndpointUrl = request.EndpointUrl,
                    SecurityMode = MessageSecurityMode.None,
                    SecurityPolicyUri = SecurityPolicyUris.None
                },
                loggerFactory,
                options,
                stackProfile);
            try
            {
                await client.OpenAsync().ConfigureAwait(false);
                var response = await client.innerChannel.RequestAsync(request).ConfigureAwait(false);
                await client.CloseAsync().ConfigureAwait(false);
                return (FindServersResponse)response;
            }
            catch (Exception)
            {
                await client.AbortAsync().ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// This Service returns the Endpoints supported by a Server and all of the configuration information required to establish a SecureChannel and a Session.
        /// </summary>
        /// <param name="request">a request.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="options">The secure channel options.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part4/5.4.4/">OPC UA specification Part 4: Services, 5.4.4</seealso>
        public static async Task<GetEndpointsResponse> GetEndpointsAsync(GetEndpointsRequest request, ILoggerFactory? loggerFactory = null, UaApplicationOptions? options = null, StackProfile? stackProfile = null)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            using var client = new DiscoveryService(
                new EndpointDescription
                {
                    EndpointUrl = request.EndpointUrl,
                    SecurityMode = MessageSecurityMode.None,
                    SecurityPolicyUri = SecurityPolicyUris.None
                },
                loggerFactory,
                options,
                stackProfile);
            try
            {
                await client.OpenAsync().ConfigureAwait(false);
                var response = await client.innerChannel.RequestAsync(request).ConfigureAwait(false);
                await client.CloseAsync().ConfigureAwait(false);
                return (GetEndpointsResponse)response;
            }
            catch (Exception)
            {
                await client.AbortAsync().ConfigureAwait(false);
                throw;
            }
            finally
            {
                await client.CloseAsync().ConfigureAwait(false);
                client.Dispose();
            }
        }

        /// <summary>
        /// Causes a communication object to transition immediately from its current state into the closing state.
        /// </summary>
        /// <param name="token">The <see cref="T:System.Threading.CancellationToken" /> that notifies when the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task AbortAsync(CancellationToken token = default)
        {
            await this.semaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                await ((ICommunicationObject)this.innerChannel).AbortAsync(token).ConfigureAwait(false);
            }
            finally
            {
                this.semaphore.Release();
            }
        }

        /// <summary>
        /// Causes a communication object to transition from its current state into the closed state.
        /// </summary>
        /// <param name="token">The <see cref="T:System.Threading.CancellationToken" /> that notifies when the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task CloseAsync(CancellationToken token = default)
        {
            await this.semaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                await this.innerChannel.CloseAsync(token).ConfigureAwait(false);
            }
            finally
            {
                this.semaphore.Release();
            }
        }

        /// <summary>
        /// Causes a communication object to transition from the created state into the opened state.
        /// </summary>
        /// <param name="token">The <see cref="T:System.Threading.CancellationToken" /> that notifies when the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OpenAsync(CancellationToken token = default)
        {
            await this.semaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                await this.innerChannel.OpenAsync(token).ConfigureAwait(false);
            }
            finally
            {
                this.semaphore.Release();
            }
        }
    }
}