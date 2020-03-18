// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using System.Collections.Generic;

namespace Workstation.ServiceModel.Ua.Channels
{
    /// <summary>
    /// A session-full, secure channel for communicating with OPC UA servers using the UA TCP transport profile.
    /// </summary>
    public class UaTcpSessionChannel : UaTcpSecureChannel, ISourceBlock<PublishResponse>, IObservable<PublishResponse>
    {
        /// <summary>
        /// The default session timeout.
        /// </summary>
        public const double DefaultSessionTimeout = 120 * 1000; // 2 minutes

        /// <summary>
        /// The default publishing interval.
        /// </summary>
        public const double DefaultPublishingInterval = 1000f;

        /// <summary>
        /// The default keep alive count.
        /// </summary>
        public const uint DefaultKeepaliveCount = 30;

        private const string RsaSha1Signature = @"http://www.w3.org/2000/09/xmldsig#rsa-sha1";
        private const string RsaSha256Signature = @"http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
        private const string RsaPssSha256Signature = @"http://opcfoundation.org/UA/security/rsa-pss-sha2-256";
        private const string RsaV15KeyWrap = @"http://www.w3.org/2001/04/xmlenc#rsa-1_5";
        private const string RsaOaepKeyWrap = @"http://www.w3.org/2001/04/xmlenc#rsa-oaep";
        private const string RsaOaepSha256KeyWrap = @"http://opcfoundation.org/UA/security/rsa-oaep-sha2-256";
        private const int NonceLength = 32;
        private const uint PublishTimeoutHint = 10 * 60 * 1000; // 10 minutes

        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger logger;
        private readonly BroadcastBlock<PublishResponse> publishResponses;
        private readonly ActionBlock<PublishResponse> actionBlock;
        private readonly UaTcpSessionChannelOptions options;
        private readonly CancellationTokenSource stateMachineCts;
        private Task stateMachineTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="UaTcpSessionChannel"/> class.
        /// </summary>
        /// <param name="localDescription">The <see cref="ApplicationDescription"/> of the local application.</param>
        /// <param name="certificateStore">The local certificate store.</param>
        /// <param name="userIdentity">The user identity. Provide an <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> or <see cref="X509Identity"/>.</param>
        /// <param name="remoteEndpoint">The <see cref="EndpointDescription"/> of the remote application. Obtained from a prior call to UaTcpDiscoveryClient.GetEndpoints.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="options">The session channel options.</param>
        /// <param name="additionalTypes">Any additional types to be registered with encoder.</param>
        public UaTcpSessionChannel(
            ApplicationDescription localDescription,
            ICertificateStore certificateStore,
            IUserIdentity userIdentity,
            EndpointDescription remoteEndpoint,
            ILoggerFactory loggerFactory = null,
            UaTcpSessionChannelOptions options = null,
            IEnumerable<Type> additionalTypes = null)
            : base(localDescription, certificateStore, remoteEndpoint, loggerFactory, options, additionalTypes)
        {
            this.UserIdentity = userIdentity;
            this.options = options ?? new UaTcpSessionChannelOptions();
            this.loggerFactory = loggerFactory;
            this.logger = loggerFactory?.CreateLogger<UaTcpSessionChannel>();
            this.actionBlock = new ActionBlock<PublishResponse>(pr => this.OnPublishResponse(pr));
            this.stateMachineCts = new CancellationTokenSource();
            this.publishResponses = new BroadcastBlock<PublishResponse>(null, new DataflowBlockOptions { CancellationToken = this.stateMachineCts.Token });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UaTcpSessionChannel"/> class.
        /// </summary>
        /// <param name="localDescription">The <see cref="ApplicationDescription"/> of the local application.</param>
        /// <param name="certificateStore">The local certificate store.</param>
        /// <param name="userIdentity">The user identity. Provide an <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> or <see cref="X509Identity"/>.</param>
        /// <param name="remoteEndpoint">The <see cref="EndpointDescription"/> of the remote application. Obtained from a prior call to UaTcpDiscoveryClient.GetEndpoints.</param>
        /// <param name="uaTcpSessionChannelReconnectParameter">Provides SessionId, RemoteNonce and AuthenticationToken for reconnecting to a session</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="options">The session channel options.</param>
        /// <param name="additionalTypes">Any additional types to be registered with encoder.</param>
        public UaTcpSessionChannel(
            ApplicationDescription localDescription,
            ICertificateStore certificateStore,
            IUserIdentity userIdentity,
            EndpointDescription remoteEndpoint,
            UaTcpSessionChannelReconnectParameter uaTcpSessionChannelReconnectParameter,
            ILoggerFactory loggerFactory = null,
            UaTcpSessionChannelOptions options = null,
            IEnumerable<Type> additionalTypes = null)
            : base(localDescription, certificateStore, remoteEndpoint, loggerFactory, options, additionalTypes)
        {
            this.UserIdentity = userIdentity;
            this.SessionId = uaTcpSessionChannelReconnectParameter.SessionId;
            this.RemoteNonce = uaTcpSessionChannelReconnectParameter.RemoteNonce;
            this.AuthenticationToken = uaTcpSessionChannelReconnectParameter.AuthenticationToken;
            this.options = options ?? new UaTcpSessionChannelOptions();
            this.loggerFactory = loggerFactory;
            this.logger = loggerFactory?.CreateLogger<UaTcpSessionChannel>();
            this.actionBlock = new ActionBlock<PublishResponse>(pr => this.OnPublishResponse(pr));
            this.stateMachineCts = new CancellationTokenSource();
            this.publishResponses = new BroadcastBlock<PublishResponse>(null, new DataflowBlockOptions { CancellationToken = this.stateMachineCts.Token });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UaTcpSessionChannel"/> class.
        /// </summary>
        /// <param name="localDescription">The <see cref="ApplicationDescription"/> of the local application.</param>
        /// <param name="certificateStore">The local certificate store.</param>
        /// <param name="userIdentity">The user identity. Provide an <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> or <see cref="X509Identity"/>.</param>
        /// <param name="endpointUrl">The url of the endpoint of the remote application</param>
        /// <param name="securityPolicyUri">Optionally, filter by SecurityPolicyUri.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="options">The session channel options.</param>
        /// <param name="additionalTypes">Any additional types to be registered with encoder.</param>
        public UaTcpSessionChannel(
            ApplicationDescription localDescription,
            ICertificateStore certificateStore,
            IUserIdentity userIdentity,
            string endpointUrl,
            string securityPolicyUri = null,
            ILoggerFactory loggerFactory = null,
            UaTcpSessionChannelOptions options = null,
            IEnumerable<Type> additionalTypes = null)
            : base(localDescription, certificateStore, new EndpointDescription { EndpointUrl = endpointUrl, SecurityPolicyUri = securityPolicyUri }, loggerFactory, options, additionalTypes)
        {
            this.UserIdentity = userIdentity;
            this.options = options ?? new UaTcpSessionChannelOptions();
            this.loggerFactory = loggerFactory;
            this.logger = loggerFactory?.CreateLogger<UaTcpSessionChannel>();
            this.actionBlock = new ActionBlock<PublishResponse>(pr => this.OnPublishResponse(pr));
            this.stateMachineCts = new CancellationTokenSource();
            this.publishResponses = new BroadcastBlock<PublishResponse>(null, new DataflowBlockOptions { CancellationToken = this.stateMachineCts.Token });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UaTcpSessionChannel"/> class.
        /// </summary>
        /// <param name="localDescription">The <see cref="ApplicationDescription"/> of the local application.</param>
        /// <param name="certificateStore">The local certificate store.</param>
        /// <param name="userIdentity">The user identity. Provide an <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> or <see cref="X509Identity"/>.</param>
        /// <param name="endpointUrl">The url of the endpoint of the remote application</param>
        /// <param name="uaTcpSessionChannelReconnectParameter">Provides SessionId, RemoteNonce and AuthenticationToken for reconnecting to a session</param>
        /// <param name="securityPolicyUri">Optionally, filter by SecurityPolicyUri.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="options">The session channel options.</param>
        /// <param name="additionalTypes">Any additional types to be registered with encoder.</param>
        public UaTcpSessionChannel(
            ApplicationDescription localDescription,
            ICertificateStore certificateStore,
            IUserIdentity userIdentity,
            string endpointUrl,
            UaTcpSessionChannelReconnectParameter uaTcpSessionChannelReconnectParameter,
            string securityPolicyUri = null,
            ILoggerFactory loggerFactory = null,
            UaTcpSessionChannelOptions options = null,
            IEnumerable<Type> additionalTypes = null)
            : base(localDescription, certificateStore, new EndpointDescription { EndpointUrl = endpointUrl, SecurityPolicyUri = securityPolicyUri }, loggerFactory, options, additionalTypes)
        {
            this.UserIdentity = userIdentity;
            this.SessionId = uaTcpSessionChannelReconnectParameter.SessionId;
            this.RemoteNonce = uaTcpSessionChannelReconnectParameter.RemoteNonce;
            this.AuthenticationToken = uaTcpSessionChannelReconnectParameter.AuthenticationToken;
            this.options = options ?? new UaTcpSessionChannelOptions();
            this.loggerFactory = loggerFactory;
            this.logger = loggerFactory?.CreateLogger<UaTcpSessionChannel>();
            this.actionBlock = new ActionBlock<PublishResponse>(pr => this.OnPublishResponse(pr));
            this.stateMachineCts = new CancellationTokenSource();
            this.publishResponses = new BroadcastBlock<PublishResponse>(null, new DataflowBlockOptions { CancellationToken = this.stateMachineCts.Token });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UaTcpSessionChannel"/> class.
        /// </summary>
        /// <param name="localDescription">The <see cref="ApplicationDescription"/> of the local application.</param>
        /// <param name="certificateStore">The local certificate store.</param>
        /// <param name="userIdentityProvider">An asynchronous function that provides the user identity. Provide an <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> or <see cref="X509Identity"/>.</param>
        /// <param name="remoteEndpoint">The <see cref="EndpointDescription"/> of the remote application. Obtained from a prior call to UaTcpDiscoveryClient.GetEndpoints.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="options">The session channel options.</param>
        /// <param name="additionalTypes">Any additional types to be registered with encoder.</param>
        public UaTcpSessionChannel(
            ApplicationDescription localDescription,
            ICertificateStore certificateStore,
            Func<EndpointDescription, Task<IUserIdentity>> userIdentityProvider,
            EndpointDescription remoteEndpoint,
            ILoggerFactory loggerFactory = null,
            UaTcpSessionChannelOptions options = null,
            IEnumerable<Type> additionalTypes = null)
            : base(localDescription, certificateStore, remoteEndpoint, loggerFactory, options, additionalTypes)
        {
            this.UserIdentityProvider = userIdentityProvider;
            this.options = options ?? new UaTcpSessionChannelOptions();
            this.loggerFactory = loggerFactory;
            this.logger = loggerFactory?.CreateLogger<UaTcpSessionChannel>();
            this.actionBlock = new ActionBlock<PublishResponse>(pr => this.OnPublishResponse(pr));
            this.stateMachineCts = new CancellationTokenSource();
            this.publishResponses = new BroadcastBlock<PublishResponse>(null, new DataflowBlockOptions { CancellationToken = this.stateMachineCts.Token });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UaTcpSessionChannel"/> class.
        /// </summary>
        /// <param name="localDescription">The <see cref="ApplicationDescription"/> of the local application.</param>
        /// <param name="certificateStore">The local certificate store.</param>
        /// <param name="userIdentityProvider">An asynchronous function that provides the user identity. Provide an <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> or <see cref="X509Identity"/>.</param>
        /// <param name="remoteEndpoint">The <see cref="EndpointDescription"/> of the remote application. Obtained from a prior call to UaTcpDiscoveryClient.GetEndpoints.</param>
        /// <param name="uaTcpSessionChannelReconnectParameter">Provides SessionId, RemoteNonce and AuthenticationToken for reconnecting to a session</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="options">The session channel options.</param>
        /// <param name="additionalTypes">Any additional types to be registered with encoder.</param>
        public UaTcpSessionChannel(
            ApplicationDescription localDescription,
            ICertificateStore certificateStore,
            Func<EndpointDescription, Task<IUserIdentity>> userIdentityProvider,
            EndpointDescription remoteEndpoint,
            UaTcpSessionChannelReconnectParameter uaTcpSessionChannelReconnectParameter,
            ILoggerFactory loggerFactory = null,
            UaTcpSessionChannelOptions options = null,
            IEnumerable<Type> additionalTypes = null)
            : base(localDescription, certificateStore, remoteEndpoint, loggerFactory, options, additionalTypes)
        {
            this.UserIdentityProvider = userIdentityProvider;
            this.SessionId = uaTcpSessionChannelReconnectParameter.SessionId;
            this.RemoteNonce = uaTcpSessionChannelReconnectParameter.RemoteNonce;
            this.AuthenticationToken = uaTcpSessionChannelReconnectParameter.AuthenticationToken;
            this.options = options ?? new UaTcpSessionChannelOptions();
            this.loggerFactory = loggerFactory;
            this.logger = loggerFactory?.CreateLogger<UaTcpSessionChannel>();
            this.actionBlock = new ActionBlock<PublishResponse>(pr => this.OnPublishResponse(pr));
            this.stateMachineCts = new CancellationTokenSource();
            this.publishResponses = new BroadcastBlock<PublishResponse>(null, new DataflowBlockOptions { CancellationToken = this.stateMachineCts.Token });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UaTcpSessionChannel"/> class.
        /// </summary>
        /// <param name="localDescription">The <see cref="ApplicationDescription"/> of the local application.</param>
        /// <param name="certificateStore">The local certificate store.</param>
        /// <param name="userIdentityProvider">An asynchronous function that provides the user identity. Provide an <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> or <see cref="X509Identity"/>.</param>
        /// <param name="endpointUrl">The url of the endpoint of the remote application</param>
        /// <param name="securityPolicyUri">Optionally, filter by SecurityPolicyUri.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="options">The session channel options.</param>
        /// <param name="additionalTypes">Any additional types to be registered with encoder.</param>
        public UaTcpSessionChannel(
            ApplicationDescription localDescription,
            ICertificateStore certificateStore,
            Func<EndpointDescription, Task<IUserIdentity>> userIdentityProvider,
            string endpointUrl,
            string securityPolicyUri = null,
            ILoggerFactory loggerFactory = null,
            UaTcpSessionChannelOptions options = null,
            IEnumerable<Type> additionalTypes = null)
            : base(localDescription, certificateStore, new EndpointDescription { EndpointUrl = endpointUrl, SecurityPolicyUri = securityPolicyUri }, loggerFactory, options, additionalTypes)
        {
            this.UserIdentityProvider = userIdentityProvider;
            this.options = options ?? new UaTcpSessionChannelOptions();
            this.loggerFactory = loggerFactory;
            this.logger = loggerFactory?.CreateLogger<UaTcpSessionChannel>();
            this.actionBlock = new ActionBlock<PublishResponse>(pr => this.OnPublishResponse(pr));
            this.stateMachineCts = new CancellationTokenSource();
            this.publishResponses = new BroadcastBlock<PublishResponse>(null, new DataflowBlockOptions { CancellationToken = this.stateMachineCts.Token });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UaTcpSessionChannel"/> class.
        /// </summary>
        /// <param name="localDescription">The <see cref="ApplicationDescription"/> of the local application.</param>
        /// <param name="certificateStore">The local certificate store.</param>
        /// <param name="userIdentityProvider">An asynchronous function that provides the user identity. Provide an <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> or <see cref="X509Identity"/>.</param>
        /// <param name="endpointUrl">The url of the endpoint of the remote application</param>
        /// <param name="uaTcpSessionChannelReconnectParameter">Provides SessionId, RemoteNonce and AuthenticationToken for reconnecting to a session</param>
        /// <param name="securityPolicyUri">Optionally, filter by SecurityPolicyUri.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="options">The session channel options.</param>
        /// <param name="additionalTypes">Any additional types to be registered with encoder.</param>
        public UaTcpSessionChannel(
            ApplicationDescription localDescription,
            ICertificateStore certificateStore,
            Func<EndpointDescription, Task<IUserIdentity>> userIdentityProvider,
            string endpointUrl,
            UaTcpSessionChannelReconnectParameter uaTcpSessionChannelReconnectParameter,
            string securityPolicyUri = null,
            ILoggerFactory loggerFactory = null,
            UaTcpSessionChannelOptions options = null,
            IEnumerable<Type> additionalTypes = null)
            : base(localDescription, certificateStore, new EndpointDescription { EndpointUrl = endpointUrl, SecurityPolicyUri = securityPolicyUri }, loggerFactory, options, additionalTypes)
        {
            this.UserIdentityProvider = userIdentityProvider;
            this.SessionId = uaTcpSessionChannelReconnectParameter.SessionId;
            this.RemoteNonce = uaTcpSessionChannelReconnectParameter.RemoteNonce;
            this.AuthenticationToken = uaTcpSessionChannelReconnectParameter.AuthenticationToken;
            this.options = options ?? new UaTcpSessionChannelOptions();
            this.loggerFactory = loggerFactory;
            this.logger = loggerFactory?.CreateLogger<UaTcpSessionChannel>();
            this.actionBlock = new ActionBlock<PublishResponse>(pr => this.OnPublishResponse(pr));
            this.stateMachineCts = new CancellationTokenSource();
            this.publishResponses = new BroadcastBlock<PublishResponse>(null, new DataflowBlockOptions { CancellationToken = this.stateMachineCts.Token });
        }



        /// <summary>
        /// Gets the asynchronous function that provides the user identity. Provide an <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> or <see cref="X509Identity"/>
        /// </summary>
        public Func<EndpointDescription, Task<IUserIdentity>> UserIdentityProvider { get; }

        /// <summary>
        /// Gets the user identity.
        /// </summary>
        public IUserIdentity UserIdentity { get; private set; }

        /// <summary>
        /// Gets the session id provided by the server.
        /// </summary>
        public NodeId SessionId { get; private set; }

        /// <summary>
        /// Gets the remote nonce provided by the server.
        /// </summary>
        public byte[] RemoteNonce { get; private set; }

        /// <summary>
        /// Gets a Task that represents the asynchronous operation and completion of the channel.
        /// </summary>
        public Task Completion => this.publishResponses.Completion;

        /// <inheritdoc/>
        public IDisposable LinkTo(ITargetBlock<PublishResponse> target, DataflowLinkOptions linkOptions)
        {
            return this.publishResponses.LinkTo(target, linkOptions);
        }

        /// <inheritdoc/>
        public PublishResponse ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<PublishResponse> target, out bool messageConsumed)
        {
            return ((ISourceBlock<PublishResponse>)this.publishResponses).ConsumeMessage(messageHeader, target, out messageConsumed);
        }

        /// <inheritdoc/>
        public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<PublishResponse> target)
        {
            return ((ISourceBlock<PublishResponse>)this.publishResponses).ReserveMessage(messageHeader, target);
        }

        /// <inheritdoc/>
        public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<PublishResponse> target)
        {
            ((ISourceBlock<PublishResponse>)this.publishResponses).ReleaseReservation(messageHeader, target);
        }

        /// <inheritdoc/>
        public void Complete()
        {
            this.publishResponses.Complete();
        }

        /// <inheritdoc/>
        public void Fault(Exception exception)
        {
            ((ISourceBlock<PublishResponse>)this.publishResponses).Fault(exception);
        }

        /// <inheritdoc/>
        public IDisposable Subscribe(IObserver<PublishResponse> observer)
        {
            return this.AsObservable().Subscribe(observer);
        }

        /// <inheritdoc/>
        protected override async Task OnOpeningAsync(CancellationToken token = default(CancellationToken))
        {
            if (this.RemoteEndpoint.Server == null)
            {
                // If specific endpoint is not provided, use discovery to select endpoint with highest
                // security level.
                var endpointUrl = this.RemoteEndpoint.EndpointUrl;
                var securityPolicyUri = this.RemoteEndpoint.SecurityPolicyUri;
                try
                {
                    this.logger?.LogInformation($"Discovering endpoints of '{endpointUrl}'.");
                    var getEndpointsRequest = new GetEndpointsRequest
                    {
                        EndpointUrl = endpointUrl,
                        ProfileUris = new[] { TransportProfileUris.UaTcpTransport }
                    };
                    var getEndpointsResponse = await UaTcpDiscoveryService.GetEndpointsAsync(getEndpointsRequest, this.loggerFactory).ConfigureAwait(false);
                    if (getEndpointsResponse.Endpoints == null || getEndpointsResponse.Endpoints.Length == 0)
                    {
                        throw new InvalidOperationException($"'{endpointUrl}' returned no endpoints.");
                    }

                    var selectedEndpoint = getEndpointsResponse.Endpoints
                        .Where(e => string.IsNullOrEmpty(securityPolicyUri) || e.SecurityPolicyUri == securityPolicyUri)
                        .OrderBy(e => e.SecurityLevel).Last();

                    this.RemoteEndpoint.Server = selectedEndpoint.Server;
                    this.RemoteEndpoint.ServerCertificate = selectedEndpoint.ServerCertificate;
                    this.RemoteEndpoint.SecurityMode = selectedEndpoint.SecurityMode;
                    this.RemoteEndpoint.SecurityPolicyUri = selectedEndpoint.SecurityPolicyUri;
                    this.RemoteEndpoint.UserIdentityTokens = selectedEndpoint.UserIdentityTokens;
                    this.RemoteEndpoint.TransportProfileUri = selectedEndpoint.TransportProfileUri;
                    this.RemoteEndpoint.SecurityLevel = selectedEndpoint.SecurityLevel;

                    this.logger?.LogTrace($"Success discovering endpoints of '{endpointUrl}'.");
                }
                catch (Exception ex)
                {
                    this.logger?.LogError($"Error discovering endpoints of '{endpointUrl}'. {ex.Message}");
                    throw;
                }
            }

            // Ask for user identity. May show dialog.
            if (this.UserIdentityProvider != null)
            {
                this.UserIdentity = await this.UserIdentityProvider(this.RemoteEndpoint);
            }

            await base.OnOpeningAsync(token);
        }

        /// <inheritdoc/>
        protected override async Task OnOpenAsync(CancellationToken token = default(CancellationToken))
        {
            this.logger?.LogInformation($"Opening session channel with endpoint '{this.RemoteEndpoint.EndpointUrl}'.");
            this.logger?.LogInformation($"SecurityPolicy: '{this.RemoteEndpoint.SecurityPolicyUri}'.");
            this.logger?.LogInformation($"SecurityMode: '{this.RemoteEndpoint.SecurityMode}'.");
            this.logger?.LogInformation($"UserIdentity: '{this.UserIdentity}'.");

            await base.OnOpenAsync(token).ConfigureAwait(false);

            token.ThrowIfCancellationRequested();

            // if SessionId is provided then we skip the CreateSessionRequest and go directly to (re)ActivateSession.
            // requires from previous Session: SessionId, AuthenticationToken, RemoteNonce
            if (this.SessionId == null)
            {
                var localNonce = this.GetNextNonce(NonceLength);
                var localCertificate = this.LocalCertificate;
                var createSessionRequest = new CreateSessionRequest
                {
                    ClientDescription = this.LocalDescription,
                    EndpointUrl = this.RemoteEndpoint.EndpointUrl,
                    SessionName = this.LocalDescription.ApplicationName,
                    ClientNonce = localNonce,
                    ClientCertificate = localCertificate,
                    RequestedSessionTimeout = this.options.SessionTimeout,
                    MaxResponseMessageSize = this.RemoteMaxMessageSize
                };

                var createSessionResponse = await this.CreateSessionAsync(createSessionRequest).ConfigureAwait(false);
                this.SessionId = createSessionResponse.SessionId;
                this.AuthenticationToken = createSessionResponse.AuthenticationToken;
                this.RemoteNonce = createSessionResponse.ServerNonce;

                // verify the server's certificate is the same as the certificate from the selected endpoint.
                if (this.RemoteEndpoint.ServerCertificate != null && !this.RemoteEndpoint.ServerCertificate.SequenceEqual(createSessionResponse.ServerCertificate))
                {
                    throw new ServiceResultException(StatusCodes.BadCertificateInvalid, "Server did not return the same certificate used to create the channel.");
                }

                // verify the server's signature.
                ISigner verifier = null;
                bool verified = false;

                switch (this.RemoteEndpoint.SecurityPolicyUri)
                {
                    case SecurityPolicyUris.Basic128Rsa15:
                    case SecurityPolicyUris.Basic256:
                        verifier = SignerUtilities.GetSigner("SHA-1withRSA");
                        verifier.Init(false, this.RemotePublicKey);
                        verifier.BlockUpdate(localCertificate, 0, localCertificate.Length);
                        verifier.BlockUpdate(localNonce, 0, localNonce.Length);
                        verified = verifier.VerifySignature(createSessionResponse.ServerSignature.Signature);
                        break;

                    case SecurityPolicyUris.Basic256Sha256:
                    case SecurityPolicyUris.Aes128_Sha256_RsaOaep:
                        verifier = SignerUtilities.GetSigner("SHA-256withRSA");
                        verifier.Init(false, this.RemotePublicKey);
                        verifier.BlockUpdate(localCertificate, 0, localCertificate.Length);
                        verifier.BlockUpdate(localNonce, 0, localNonce.Length);
                        verified = verifier.VerifySignature(createSessionResponse.ServerSignature.Signature);
                        break;

                    case SecurityPolicyUris.Aes256_Sha256_RsaPss:
                        verifier = SignerUtilities.GetSigner("SHA-256withRSAandMGF1");
                        verifier.Init(false, this.RemotePublicKey);
                        verifier.BlockUpdate(localCertificate, 0, localCertificate.Length);
                        verifier.BlockUpdate(localNonce, 0, localNonce.Length);
                        verified = verifier.VerifySignature(createSessionResponse.ServerSignature.Signature);
                        break;

                    default:
                        verified = true;
                        break;
                }

                verifier = null;
                if (!verified)
                {
                    throw new ServiceResultException(StatusCodes.BadApplicationSignatureInvalid, "Server did not provide a correct signature for the nonce data provided by the client.");
                }
            }

            // create client signature
            SignatureData clientSignature = null;
            ISigner signer = null;

            switch (this.RemoteEndpoint.SecurityPolicyUri)
            {
                case SecurityPolicyUris.Basic128Rsa15:
                case SecurityPolicyUris.Basic256:
                    signer = SignerUtilities.GetSigner("SHA-1withRSA");
                    signer.Init(true, this.LocalPrivateKey);
                    signer.BlockUpdate(this.RemoteEndpoint.ServerCertificate, 0, this.RemoteEndpoint.ServerCertificate.Length);
                    signer.BlockUpdate(this.RemoteNonce, 0, this.RemoteNonce.Length);
                    clientSignature = new SignatureData
                    {
                        Signature = signer.GenerateSignature(),
                        Algorithm = RsaSha1Signature,
                    };

                    break;

                case SecurityPolicyUris.Basic256Sha256:
                case SecurityPolicyUris.Aes128_Sha256_RsaOaep:
                    signer = SignerUtilities.GetSigner("SHA-256withRSA");
                    signer.Init(true, this.LocalPrivateKey);
                    signer.BlockUpdate(this.RemoteEndpoint.ServerCertificate, 0, this.RemoteEndpoint.ServerCertificate.Length);
                    signer.BlockUpdate(this.RemoteNonce, 0, this.RemoteNonce.Length);
                    clientSignature = new SignatureData
                    {
                        Signature = signer.GenerateSignature(),
                        Algorithm = RsaSha256Signature,
                    };

                    break;

                case SecurityPolicyUris.Aes256_Sha256_RsaPss:
                    signer = SignerUtilities.GetSigner("SHA-256withRSAandMGF1");
                    signer.Init(true, this.LocalPrivateKey);
                    signer.BlockUpdate(this.RemoteEndpoint.ServerCertificate, 0, this.RemoteEndpoint.ServerCertificate.Length);
                    signer.BlockUpdate(this.RemoteNonce, 0, this.RemoteNonce.Length);
                    clientSignature = new SignatureData
                    {
                        Signature = signer.GenerateSignature(),
                        Algorithm = RsaPssSha256Signature,
                    };

                    break;

                default:
                    clientSignature = new SignatureData();
                    break;
            }

            signer = null;

            // supported UserIdentityToken types are AnonymousIdentityToken, UserNameIdentityToken, IssuedIdentityToken, X509IdentityToken
            UserIdentityToken identityToken = null;
            SignatureData tokenSignature = null;

            // if UserIdentity type is IssuedIdentity
            if (this.UserIdentity is IssuedIdentity)
            {
                var tokenPolicy = this.RemoteEndpoint.UserIdentityTokens.FirstOrDefault(t => t.TokenType == UserTokenType.IssuedToken);
                if (tokenPolicy == null)
                {
                    throw new ServiceResultException(StatusCodes.BadIdentityTokenRejected);
                }

                var issuedIdentity = (IssuedIdentity)this.UserIdentity;
                int plainTextLength = issuedIdentity.TokenData.Length + this.RemoteNonce.Length;
                IBufferedCipher encryptor;
                byte[] cipherText;
                int pos;

                var secPolicyUri = tokenPolicy.SecurityPolicyUri ?? this.RemoteEndpoint.SecurityPolicyUri;
                switch (secPolicyUri)
                {
                    case SecurityPolicyUris.Basic128Rsa15:
                        encryptor = CipherUtilities.GetCipher("RSA//PKCS1Padding");
                        encryptor.Init(true, this.RemotePublicKey);
                        cipherText = new byte[encryptor.GetOutputSize(4 + plainTextLength)];
                        pos = encryptor.ProcessBytes(BitConverter.GetBytes(plainTextLength), cipherText, 0);
                        pos = encryptor.ProcessBytes(issuedIdentity.TokenData, cipherText, pos);
                        pos = encryptor.DoFinal(this.RemoteNonce, cipherText, pos);
                        identityToken = new IssuedIdentityToken
                        {
                            TokenData = cipherText,
                            EncryptionAlgorithm = RsaV15KeyWrap,
                            PolicyId = tokenPolicy.PolicyId
                        };

                        break;

                    case SecurityPolicyUris.Basic256:
                    case SecurityPolicyUris.Basic256Sha256:
                    case SecurityPolicyUris.Aes128_Sha256_RsaOaep:
                        encryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        encryptor.Init(true, this.RemotePublicKey);
                        cipherText = new byte[encryptor.GetOutputSize(4 + plainTextLength)];
                        pos = encryptor.ProcessBytes(BitConverter.GetBytes(plainTextLength), cipherText, 0);
                        pos = encryptor.ProcessBytes(issuedIdentity.TokenData, cipherText, pos);
                        pos = encryptor.DoFinal(this.RemoteNonce, cipherText, pos);
                        identityToken = new IssuedIdentityToken
                        {
                            TokenData = cipherText,
                            EncryptionAlgorithm = RsaOaepKeyWrap,
                            PolicyId = tokenPolicy.PolicyId
                        };

                        break;

                    case SecurityPolicyUris.Aes256_Sha256_RsaPss:
                        encryptor = CipherUtilities.GetCipher("RSA//OAEPWITHSHA256ANDMGF1PADDING");
                        encryptor.Init(true, this.RemotePublicKey);
                        cipherText = new byte[encryptor.GetOutputSize(4 + plainTextLength)];
                        pos = encryptor.ProcessBytes(BitConverter.GetBytes(plainTextLength), cipherText, 0);
                        pos = encryptor.ProcessBytes(issuedIdentity.TokenData, cipherText, pos);
                        pos = encryptor.DoFinal(this.RemoteNonce, cipherText, pos);
                        identityToken = new IssuedIdentityToken
                        {
                            TokenData = cipherText,
                            EncryptionAlgorithm = RsaOaepSha256KeyWrap,
                            PolicyId = tokenPolicy.PolicyId
                        };

                        break;

                    default:
                        identityToken = new IssuedIdentityToken
                        {
                            TokenData = issuedIdentity.TokenData,
                            EncryptionAlgorithm = null,
                            PolicyId = tokenPolicy.PolicyId
                        };
                        break;
                }

                tokenSignature = new SignatureData();
                encryptor = null;
                cipherText = null;
            }

            // if UserIdentity type is X509Identity
            else if (this.UserIdentity is X509Identity)
            {
                var tokenPolicy = this.RemoteEndpoint.UserIdentityTokens.FirstOrDefault(t => t.TokenType == UserTokenType.Certificate);
                if (tokenPolicy == null)
                {
                    throw new ServiceResultException(StatusCodes.BadIdentityTokenRejected);
                }

                var x509Identity = (X509Identity)this.UserIdentity;
                identityToken = new X509IdentityToken { CertificateData = x509Identity.Certificate?.GetEncoded(), PolicyId = tokenPolicy.PolicyId };

                var secPolicyUri = tokenPolicy.SecurityPolicyUri ?? this.RemoteEndpoint.SecurityPolicyUri;
                switch (secPolicyUri)
                {

                    case SecurityPolicyUris.Basic128Rsa15:
                    case SecurityPolicyUris.Basic256:
                        signer = SignerUtilities.GetSigner("SHA-1withRSA");
                        signer.Init(true, x509Identity.PrivateKey);
                        signer.BlockUpdate(this.RemoteEndpoint.ServerCertificate, 0, this.RemoteEndpoint.ServerCertificate.Length);
                        signer.BlockUpdate(this.RemoteNonce, 0, this.RemoteNonce.Length);
                        tokenSignature = new SignatureData
                        {
                            Signature = signer.GenerateSignature(),
                            Algorithm = RsaSha1Signature,
                        };

                        break;

                    case SecurityPolicyUris.Basic256Sha256:
                    case SecurityPolicyUris.Aes128_Sha256_RsaOaep:
                        signer = SignerUtilities.GetSigner("SHA-256withRSA");
                        signer.Init(true, x509Identity.PrivateKey);
                        signer.BlockUpdate(this.RemoteEndpoint.ServerCertificate, 0, this.RemoteEndpoint.ServerCertificate.Length);
                        signer.BlockUpdate(this.RemoteNonce, 0, this.RemoteNonce.Length);
                        tokenSignature = new SignatureData
                        {
                            Signature = signer.GenerateSignature(),
                            Algorithm = RsaSha256Signature,
                        };
                        break;

                    case SecurityPolicyUris.Aes256_Sha256_RsaPss:
                        signer = SignerUtilities.GetSigner("SHA-256withRSAandMGF1");
                        signer.Init(true, x509Identity.PrivateKey);
                        signer.BlockUpdate(this.RemoteEndpoint.ServerCertificate, 0, this.RemoteEndpoint.ServerCertificate.Length);
                        signer.BlockUpdate(this.RemoteNonce, 0, this.RemoteNonce.Length);
                        tokenSignature = new SignatureData
                        {
                            Signature = signer.GenerateSignature(),
                            Algorithm = RsaSha256Signature,
                        };
                        break;

                    default:
                        tokenSignature = new SignatureData();
                        break;
                }

                signer = null;
            }

            // if UserIdentity type is UserNameIdentity
            else if (this.UserIdentity is UserNameIdentity)
            {
                var tokenPolicy = this.RemoteEndpoint.UserIdentityTokens.FirstOrDefault(t => t.TokenType == UserTokenType.UserName);
                if (tokenPolicy == null)
                {
                    throw new ServiceResultException(StatusCodes.BadIdentityTokenRejected);
                }

                var userNameIdentity = (UserNameIdentity)this.UserIdentity;
                byte[] passwordBytes = userNameIdentity.Password != null ? System.Text.Encoding.UTF8.GetBytes(userNameIdentity.Password) : new byte[0];
                int plainTextLength = passwordBytes.Length + this.RemoteNonce.Length;
                IBufferedCipher encryptor;
                byte[] cipherText;
                int pos;

                var secPolicyUri = tokenPolicy.SecurityPolicyUri ?? this.RemoteEndpoint.SecurityPolicyUri;
                switch (secPolicyUri)
                {
                    case SecurityPolicyUris.Basic128Rsa15:
                        encryptor = CipherUtilities.GetCipher("RSA//PKCS1Padding");
                        encryptor.Init(true, this.RemotePublicKey);
                        cipherText = new byte[encryptor.GetOutputSize(4 + plainTextLength)];
                        pos = encryptor.ProcessBytes(BitConverter.GetBytes(plainTextLength), cipherText, 0);
                        pos = encryptor.ProcessBytes(passwordBytes, cipherText, pos);
                        pos = encryptor.DoFinal(this.RemoteNonce, cipherText, pos);
                        identityToken = new UserNameIdentityToken
                        {
                            UserName = userNameIdentity.UserName,
                            Password = cipherText,
                            EncryptionAlgorithm = RsaV15KeyWrap,
                            PolicyId = tokenPolicy.PolicyId
                        };

                        break;

                    case SecurityPolicyUris.Basic256:
                    case SecurityPolicyUris.Basic256Sha256:
                    case SecurityPolicyUris.Aes128_Sha256_RsaOaep:
                        encryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        encryptor.Init(true, this.RemotePublicKey);
                        cipherText = new byte[encryptor.GetOutputSize(4 + plainTextLength)];
                        pos = encryptor.ProcessBytes(BitConverter.GetBytes(plainTextLength), cipherText, 0);
                        pos = encryptor.ProcessBytes(passwordBytes, cipherText, pos);
                        pos = encryptor.DoFinal(this.RemoteNonce, cipherText, pos);
                        identityToken = new UserNameIdentityToken
                        {
                            UserName = userNameIdentity.UserName,
                            Password = cipherText,
                            EncryptionAlgorithm = RsaOaepKeyWrap,
                            PolicyId = tokenPolicy.PolicyId
                        };

                        break;

                    case SecurityPolicyUris.Aes256_Sha256_RsaPss:
                        encryptor = CipherUtilities.GetCipher("RSA//OAEPWITHSHA256ANDMGF1PADDING");
                        encryptor.Init(true, this.RemotePublicKey);
                        cipherText = new byte[encryptor.GetOutputSize(4 + plainTextLength)];
                        pos = encryptor.ProcessBytes(BitConverter.GetBytes(plainTextLength), cipherText, 0);
                        pos = encryptor.ProcessBytes(passwordBytes, cipherText, pos);
                        pos = encryptor.DoFinal(this.RemoteNonce, cipherText, pos);
                        identityToken = new UserNameIdentityToken
                        {
                            UserName = userNameIdentity.UserName,
                            Password = cipherText,
                            EncryptionAlgorithm = RsaOaepSha256KeyWrap,
                            PolicyId = tokenPolicy.PolicyId
                        };

                        break;

                    default:
                        identityToken = new UserNameIdentityToken
                        {
                            UserName = userNameIdentity.UserName,
                            Password = passwordBytes,
                            EncryptionAlgorithm = null,
                            PolicyId = tokenPolicy.PolicyId
                        };
                        break;
                }

                tokenSignature = new SignatureData();
                passwordBytes = null;
                encryptor = null;
                cipherText = null;
            }

            // if UserIdentity type is AnonymousIdentity or null
            else
            {
                var tokenPolicy = this.RemoteEndpoint.UserIdentityTokens.FirstOrDefault(t => t.TokenType == UserTokenType.Anonymous);
                if (tokenPolicy == null)
                {
                    throw new ServiceResultException(StatusCodes.BadIdentityTokenRejected);
                }

                identityToken = new AnonymousIdentityToken { PolicyId = tokenPolicy.PolicyId };
                tokenSignature = new SignatureData();
            }

            var activateSessionRequest = new ActivateSessionRequest
            {
                ClientSignature = clientSignature,
                LocaleIds = new[] { CultureInfo.CurrentUICulture.TwoLetterISOLanguageName },
                UserIdentityToken = identityToken,
                UserTokenSignature = tokenSignature
            };
            var activateSessionResponse = await this.ActivateSessionAsync(activateSessionRequest).ConfigureAwait(false);
            this.RemoteNonce = activateSessionResponse.ServerNonce;

            // fetch namespace array, etc.
            var readValueIds = new ReadValueId[]
            {
                new ReadValueId
                {
                    NodeId = NodeId.Parse(VariableIds.Server_NamespaceArray),
                    AttributeId = AttributeIds.Value
                },
                new ReadValueId
                {
                    NodeId = NodeId.Parse(VariableIds.Server_ServerArray),
                    AttributeId = AttributeIds.Value
                }
            };
            var readRequest = new ReadRequest
            {
                NodesToRead = readValueIds
            };

            var readResponse = await this.ReadAsync(readRequest).ConfigureAwait(false);
            if (readResponse.Results.Length == 2)
            {
                if (StatusCode.IsGood(readResponse.Results[0].StatusCode))
                {
                    this.NamespaceUris.Clear();
                    this.NamespaceUris.AddRange(readResponse.Results[0].GetValueOrDefault<string[]>());
                }

                if (StatusCode.IsGood(readResponse.Results[1].StatusCode))
                {
                    this.ServerUris.Clear();
                    this.ServerUris.AddRange(readResponse.Results[1].GetValueOrDefault<string[]>());
                }
            }

            // create the keep alive subscription.
            var subscriptionRequest = new CreateSubscriptionRequest
            {
                RequestedPublishingInterval = DefaultPublishingInterval,
                RequestedMaxKeepAliveCount = DefaultKeepaliveCount,
                RequestedLifetimeCount = DefaultKeepaliveCount * 3,
                PublishingEnabled = true,
            };
            var subscriptionResponse = await this.CreateSubscriptionAsync(subscriptionRequest).ConfigureAwait(false);

            // link up the dataflow blocks
            var id = subscriptionResponse.SubscriptionId;
            var linkToken = this.LinkTo(this.actionBlock, pr => pr.SubscriptionId == id);

            // start publishing.
            this.stateMachineTask = Task.Run(() => this.StateMachineAsync(this.stateMachineCts.Token));
        }

        /// <inheritdoc/>
        protected override Task OnClosingAsync(CancellationToken token = default(CancellationToken))
        {
            this.stateMachineCts.Cancel();
            return base.OnClosingAsync(token);
        }

        /// <inheritdoc/>
        protected override async Task OnCloseAsync(CancellationToken token = default(CancellationToken))
        {
            await this.CloseSessionAsync(new CloseSessionRequest { DeleteSubscriptions = true }).ConfigureAwait(false);
            await Task.Delay(1000).ConfigureAwait(false);
            await base.OnCloseAsync(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Handle PublishResponse message.
        /// </summary>
        /// <param name="publishResponse">The publish response.</param>
        private void OnPublishResponse(PublishResponse publishResponse)
        {
            // handle the internal subscription (keep-alive)
        }

        /// <summary>
        /// Sends publish requests to the server.
        /// </summary>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A task.</returns>
        private async Task PublishAsync(CancellationToken token = default(CancellationToken))
        {
            var publishRequest = new PublishRequest
            {
                RequestHeader = new RequestHeader { TimeoutHint = PublishTimeoutHint, ReturnDiagnostics = this.options.DiagnosticsHint },
                SubscriptionAcknowledgements = new SubscriptionAcknowledgement[0]
            };
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var publishResponse = await this.PublishAsync(publishRequest).WithCancellation(token).ConfigureAwait(false);

                    // post to linked data flow blocks and subscriptions.
                    this.publishResponses.Post(publishResponse);

                    publishRequest = new PublishRequest
                    {
                        RequestHeader = new RequestHeader { TimeoutHint = PublishTimeoutHint, ReturnDiagnostics = this.options.DiagnosticsHint },
                        SubscriptionAcknowledgements = publishResponse.NotificationMessage.NotificationData != null ? new[] { new SubscriptionAcknowledgement { SequenceNumber = publishResponse.NotificationMessage.SequenceNumber, SubscriptionId = publishResponse.SubscriptionId } } : new SubscriptionAcknowledgement[0]
                    };
                }
                catch (Exception ex)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    this.logger?.LogError($"Error publishing subscription. {ex.Message}");
                    this.Fault(ex);
                    return;
                }
            }
        }

        /// <summary>
        /// The state machine manages the state of the channel.
        /// </summary>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A task.</returns>
        private async Task StateMachineAsync(CancellationToken token = default(CancellationToken))
        {
            var tasks = new[]
            {
                this.PublishAsync(token),
                this.PublishAsync(token),
                this.PublishAsync(token),
            };
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }
}