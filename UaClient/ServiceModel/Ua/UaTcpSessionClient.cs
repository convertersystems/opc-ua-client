// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Workstation.ServiceModel.Ua.Channels;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// A client for browsing, reading, writing and subscribing to nodes of an OPC UA server.
    /// </summary>
    public class UaTcpSessionClient : ISessionClient, IDisposable
    {
        private UaTcpSessionChannel innerChannel;
        private SemaphoreSlim semaphore;

        /// <summary>
        /// Initializes a new instance of the <see cref="UaTcpSessionClient"/> class.
        /// </summary>
        /// <param name="localDescription">The <see cref="ApplicationDescription"/> of the local application.</param>
        /// <param name="localCertificate">The <see cref="X509Certificate2"/> of the local application.</param>
        /// <param name="userIdentity">The user identity or null if anonymous. Supports <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> and <see cref="X509Identity"/>.</param>
        /// <param name="remoteEndpoint">The <see cref="EndpointDescription"/> of the remote application. Obtained from a prior call to UaTcpDiscoveryClient.GetEndpoints.</param>
        public UaTcpSessionClient(ApplicationDescription localDescription, X509Certificate2 localCertificate, IUserIdentity userIdentity, EndpointDescription remoteEndpoint)
        {
            this.innerChannel = new UaTcpSessionChannel(localDescription, localCertificate, userIdentity, remoteEndpoint);
            this.semaphore = new SemaphoreSlim(1);
        }

        /// <summary>
        /// Occurs when the communication object completes its transition from the closing state into the closed state.
        /// </summary>
        public event EventHandler Closed
        {
            add { this.innerChannel.Closed += value; }
            remove { this.innerChannel.Closed -= value; }
        }

        /// <summary>
        /// Occurs when the communication object first enters the closing state.
        /// </summary>
        public event EventHandler Closing
        {
            add { this.innerChannel.Closing += value; }
            remove { this.innerChannel.Closing -= value; }
        }

        /// <summary>
        /// Occurs when the communication object first enters the faulted state.
        /// </summary>
        public event EventHandler Faulted
        {
            add { this.innerChannel.Faulted += value; }
            remove { this.innerChannel.Faulted -= value; }
        }

        /// <summary>
        /// Occurs when the communication object completes its transition from the opening state into the opened state.
        /// </summary>
        public event EventHandler Opened
        {
            add { this.innerChannel.Opened += value; }
            remove { this.innerChannel.Opened -= value; }
        }

        /// <summary>
        /// Occurs when the communication object first enters the opening state.
        /// </summary>
        public event EventHandler Opening
        {
            add { this.innerChannel.Opening += value; }
            remove { this.innerChannel.Opening -= value; }
        }

        /// <summary>
        /// Gets the <see cref="ApplicationDescription"/> of the local application.
        /// </summary>
        public ApplicationDescription LocalDescription
        {
            get { return this.innerChannel.LocalDescription; }
        }

        /// <summary>
        /// Gets the identity of the user. Supports <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> and <see cref="X509Identity"/>.
        /// </summary>
        public IUserIdentity UserIdentity
        {
            get { return this.innerChannel.UserIdentity; }
        }

        /// <summary>
        /// Gets the <see cref="EndpointDescription"/> of the remote application.
        /// </summary>
        public EndpointDescription RemoteEndpoint
        {
            get { return this.innerChannel.RemoteEndpoint; }
        }

        /// <summary>
        /// Gets or sets the requested number of milliseconds that a session may be unused before being closed by the server.
        /// </summary>
        public double SessionTimeout
        {
            get { return this.innerChannel.SessionTimeout; }
            set { this.innerChannel.SessionTimeout = value; }
        }

        /// <summary>
        /// Gets or sets the default number of milliseconds that may elapse before an operation is cancelled by the client.
        /// </summary>
        public uint TimeoutHint
        {
            get { return this.innerChannel.TimeoutHint; }
            set { this.innerChannel.TimeoutHint = value; }
        }

        /// <summary>
        /// Gets or sets the default diagnostics flags to be requested by the client.
        /// </summary>
        public uint DiagnosticsHint
        {
            get { return this.innerChannel.DiagnosticsHint; }
            set { this.innerChannel.DiagnosticsHint = value; }
        }

        /// <summary>
        /// Gets or sets the size of the receive buffer.
        /// </summary>
        public uint LocalReceiveBufferSize
        {
            get { return this.innerChannel.LocalReceiveBufferSize; }
            set { this.innerChannel.LocalReceiveBufferSize = value; }
        }

        /// <summary>
        /// Gets or sets the size of the send buffer.
        /// </summary>
        public uint LocalSendBufferSize
        {
            get { return this.innerChannel.LocalSendBufferSize; }
            set { this.innerChannel.LocalSendBufferSize = value; }
        }

        /// <summary>
        /// Gets or sets the maximum total size of a message.
        /// </summary>
        public uint LocalMaxMessageSize
        {
            get { return this.innerChannel.LocalMaxMessageSize; }
            set { this.innerChannel.LocalMaxMessageSize = value; }
        }

        /// <summary>
        /// Gets or sets the maximum number of message chunks.
        /// </summary>
        public uint LocalMaxChunkCount
        {
            get { return this.innerChannel.LocalMaxChunkCount; }
            set { this.innerChannel.LocalMaxChunkCount = value; }
        }

        /// <summary>
        /// Gets the id of the current session.
        /// </summary>
        public NodeId SessionId
        {
            get { return this.innerChannel.SessionId; }
        }

        /// <summary>
        /// Gets the list of the namespaces of the current session.
        /// </summary>
        public IList<string> NamespaceUris
        {
            get { return this.innerChannel.NamespaceUris; }
        }

        /// <summary>
        /// Gets the list of the server uris of the current session.
        /// </summary>
        public IList<string> ServerUris
        {
            get { return this.innerChannel.ServerUris; }
        }

        /// <summary>
        /// Gets a value that indicates the current state of the communication object.
        /// </summary>
        public CommunicationState State
        {
            get { return this.innerChannel.State; }
        }

        /// <summary>
        /// Sends a service request.
        /// </summary>
        /// <param name="request">An <see cref="IServiceRequest"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns an <see cref="IServiceResponse"/>.</returns>
        public Task<IServiceResponse> RequestAsync(IServiceRequest request)
        {
            if (this.State != CommunicationState.Opened)
            {
                throw new ServiceResultException(StatusCodes.BadServerNotConnected);
            }

            return this.innerChannel.RequestAsync(request);
        }

        /// <summary>
        /// Causes a communication object to transition immediately from its current state into the closed state.
        /// </summary>
        /// <param name="token">The <see cref="T:ConverterSystems.Threading.CancellationToken" /> that notifies when the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task AbortAsync(CancellationToken token = default(CancellationToken))
        {
            await this.semaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                await this.innerChannel.AbortAsync(token).ConfigureAwait(false);
            }
            finally
            {
                this.semaphore.Release();
            }
        }

        /// <summary>
        /// Causes a communication object to transition from its current state into the closed state.
        /// </summary>
        /// <param name="token">The <see cref="T:ConverterSystems.Threading.CancellationToken" /> that notifies when the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task CloseAsync(CancellationToken token = default(CancellationToken))
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
        /// <param name="token">The <see cref="T:ConverterSystems.Threading.CancellationToken" /> that notifies when the task should be canceled.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OpenAsync(CancellationToken token = default(CancellationToken))
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

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        void IDisposable.Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.CloseAsync().GetAwaiter().GetResult();
            }
        }
    }
}