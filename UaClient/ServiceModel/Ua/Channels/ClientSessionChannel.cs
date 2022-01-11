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
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.X509;

namespace Workstation.ServiceModel.Ua.Channels
{
    /// <summary>
    /// A session-full, secure channel for communicating with OPC UA servers.
    /// </summary>
    public class ClientSessionChannel : ClientSecureChannel, ISourceBlock<PublishResponse>, IObservable<PublishResponse>
    {
        /// <summary>
        /// The default session timeout.
        /// </summary>
        public const double DefaultSessionTimeout = 120 * 1000; // 2 minutes

        /// <summary>
        /// The default subscription publishing interval.
        /// </summary>
        public const double DefaultPublishingInterval = 1000f;

        /// <summary>
        /// The default subscription keep-alive count.
        /// </summary>
        public const uint DefaultKeepaliveCount = 30;

        /// <summary>
        /// The default subscription lifetime count.
        /// </summary>
        public const uint DefaultLifetimeCount = DefaultKeepaliveCount* 3;

        private const string _rsaSha1Signature = @"http://www.w3.org/2000/09/xmldsig#rsa-sha1";
        private const string _rsaSha256Signature = @"http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
        private const string _rsaPssSha256Signature = @"http://opcfoundation.org/UA/security/rsa-pss-sha2-256";
        private const string _rsaV15KeyWrap = @"http://www.w3.org/2001/04/xmlenc#rsa-1_5";
        private const string _rsaOaepKeyWrap = @"http://www.w3.org/2001/04/xmlenc#rsa-oaep";
        private const string _rsaOaepSha256KeyWrap = @"http://opcfoundation.org/UA/security/rsa-oaep-sha2-256";
        private const int _nonceLength = 32;
        private const uint _publishTimeoutHint = 10 * 60 * 1000; // 10 minutes

        private static readonly SecureRandom _rng = new SecureRandom();
        private readonly X509CertificateParser _certificateParser = new X509CertificateParser();

        private readonly ILoggerFactory? _loggerFactory;
        private readonly ILogger? _logger;
        private readonly BroadcastBlock<PublishResponse> _publishResponses;
        private readonly ActionBlock<PublishResponse> _actionBlock;
        private readonly ClientSessionChannelOptions _options;
        private readonly CancellationTokenSource _stateMachineCts;
        private Task? _stateMachineTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientSessionChannel"/> class.
        /// </summary>
        /// <param name="localDescription">The <see cref="ApplicationDescription"/> of the local application.</param>
        /// <param name="certificateStore">The local certificate store.</param>
        /// <param name="userIdentity">The user identity. Provide an <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> or <see cref="X509Identity"/>.</param>
        /// <param name="remoteEndpoint">The <see cref="EndpointDescription"/> of the remote application. Obtained from a prior call to <see cref="DiscoveryService.GetEndpointsAsync(GetEndpointsRequest, ILoggerFactory?, UaApplicationOptions?, StackProfile?)"/>.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="options">The session channel options.</param>
        public ClientSessionChannel(
            ApplicationDescription localDescription,
            ICertificateStore? certificateStore,
            IUserIdentity userIdentity,
            EndpointDescription remoteEndpoint,
            ILoggerFactory? loggerFactory = null,
            ClientSessionChannelOptions? options = null,
            StackProfile? stackProfile = null)
            : base(localDescription, certificateStore, remoteEndpoint, loggerFactory, options, stackProfile)
        {
            UserIdentity = userIdentity;
            _options = options ?? new ClientSessionChannelOptions();
            _loggerFactory = loggerFactory;
            _logger = loggerFactory?.CreateLogger<ClientSessionChannel>();
            _actionBlock = new ActionBlock<PublishResponse>(pr => OnPublishResponse(pr));
            _stateMachineCts = new CancellationTokenSource();
            _publishResponses = new BroadcastBlock<PublishResponse>(null, new DataflowBlockOptions { CancellationToken = _stateMachineCts.Token });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientSessionChannel"/> class.
        /// </summary>
        /// <param name="localDescription">The <see cref="ApplicationDescription"/> of the local application.</param>
        /// <param name="certificateStore">The local certificate store.</param>
        /// <param name="userIdentity">The user identity. Provide an <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> or <see cref="X509Identity"/>.</param>
        /// <param name="endpointUrl">The url of the endpoint of the remote application</param>
        /// <param name="securityPolicyUri">Optionally, filter by SecurityPolicyUri.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="options">The session channel options.</param>
        public ClientSessionChannel(
            ApplicationDescription localDescription,
            ICertificateStore? certificateStore,
            IUserIdentity? userIdentity,
            string endpointUrl,
            string? securityPolicyUri = null,
            ILoggerFactory? loggerFactory = null,
            ClientSessionChannelOptions? options = null,
            StackProfile? stackProfile = null)
            : base(localDescription, certificateStore, new EndpointDescription { EndpointUrl = endpointUrl, SecurityPolicyUri = securityPolicyUri }, loggerFactory, options, stackProfile)
        {
            UserIdentity = userIdentity;
            _options = options ?? new ClientSessionChannelOptions();
            _loggerFactory = loggerFactory;
            _logger = loggerFactory?.CreateLogger<ClientSessionChannel>();
            _actionBlock = new ActionBlock<PublishResponse>(pr => OnPublishResponse(pr));
            _stateMachineCts = new CancellationTokenSource();
            _publishResponses = new BroadcastBlock<PublishResponse>(null, new DataflowBlockOptions { CancellationToken = _stateMachineCts.Token });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientSessionChannel"/> class.
        /// </summary>
        /// <param name="localDescription">The <see cref="ApplicationDescription"/> of the local application.</param>
        /// <param name="certificateStore">The local certificate store.</param>
        /// <param name="userIdentityProvider">An asynchronous function that provides the user identity. Provide an <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> or <see cref="X509Identity"/>.</param>
        /// <param name="remoteEndpoint">The <see cref="EndpointDescription"/> of the remote application. Obtained from a prior call to <see cref="DiscoveryService.GetEndpointsAsync(GetEndpointsRequest, ILoggerFactory?, UaApplicationOptions?, StackProfile?)" /> .</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="options">The session channel options.</param>
        public ClientSessionChannel(
            ApplicationDescription localDescription,
            ICertificateStore? certificateStore,
            Func<EndpointDescription, Task<IUserIdentity>>? userIdentityProvider,
            EndpointDescription remoteEndpoint,
            ILoggerFactory? loggerFactory = null,
            ClientSessionChannelOptions? options = null,
            StackProfile? stackProfile = null)
            : base(localDescription, certificateStore, remoteEndpoint, loggerFactory, options, stackProfile)
        {
            UserIdentityProvider = userIdentityProvider;
            _options = options ?? new ClientSessionChannelOptions();
            _loggerFactory = loggerFactory;
            _logger = loggerFactory?.CreateLogger<ClientSessionChannel>();
            _actionBlock = new ActionBlock<PublishResponse>(pr => OnPublishResponse(pr));
            _stateMachineCts = new CancellationTokenSource();
            _publishResponses = new BroadcastBlock<PublishResponse>(null, new DataflowBlockOptions { CancellationToken = _stateMachineCts.Token });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientSessionChannel"/> class.
        /// </summary>
        /// <param name="localDescription">The <see cref="ApplicationDescription"/> of the local application.</param>
        /// <param name="certificateStore">The local certificate store.</param>
        /// <param name="userIdentityProvider">An asynchronous function that provides the user identity. Provide an <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> or <see cref="X509Identity"/>.</param>
        /// <param name="endpointUrl">The url of the endpoint of the remote application</param>
        /// <param name="securityPolicyUri">Optionally, filter by SecurityPolicyUri.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="options">The session channel options.</param>
        public ClientSessionChannel(
            ApplicationDescription localDescription,
            ICertificateStore certificateStore,
            Func<EndpointDescription, Task<IUserIdentity>> userIdentityProvider,
            string endpointUrl,
            string? securityPolicyUri = null,
            ILoggerFactory? loggerFactory = null,
            ClientSessionChannelOptions? options = null,
            StackProfile? stackProfile = null)
            : base(localDescription, certificateStore, new EndpointDescription { EndpointUrl = endpointUrl, SecurityPolicyUri = securityPolicyUri }, loggerFactory, options, stackProfile)
        {
            UserIdentityProvider = userIdentityProvider;
            _options = options ?? new ClientSessionChannelOptions();
            _loggerFactory = loggerFactory;
            _logger = loggerFactory?.CreateLogger<ClientSessionChannel>();
            _actionBlock = new ActionBlock<PublishResponse>(pr => OnPublishResponse(pr));
            _stateMachineCts = new CancellationTokenSource();
            _publishResponses = new BroadcastBlock<PublishResponse>(null, new DataflowBlockOptions { CancellationToken = _stateMachineCts.Token });
        }

        /// <summary>
        /// Gets the local certificate.
        /// </summary>
        protected byte[]? LocalCertificate { get; private set; }

        /// <summary>
        /// Gets the local private key.
        /// </summary>
        protected RsaKeyParameters? LocalPrivateKey { get; private set; }

        /// <summary>
        /// Gets the remote public key.
        /// </summary>
        protected RsaKeyParameters? RemotePublicKey { get; private set; }

        /// <summary>
        /// Gets the asynchronous function that provides the user identity. Provide an <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> or <see cref="X509Identity"/>
        /// </summary>
        public Func<EndpointDescription, Task<IUserIdentity>>? UserIdentityProvider { get; }

        /// <summary>
        /// Gets the user identity.
        /// </summary>
        public IUserIdentity? UserIdentity { get; private set; }

        /// <summary>
        /// Gets the session id provided by the server.
        /// </summary>
        public NodeId? SessionId { get; private set; }

        /// <summary>
        /// Gets the remote nonce provided by the server.
        /// </summary>
        public byte[]? RemoteNonce { get; private set; }

        /// <summary>
        /// Gets a Task that represents the asynchronous operation and completion of the channel.
        /// </summary>
        public Task Completion => _publishResponses.Completion;

        /// <inheritdoc/>
        public IDisposable LinkTo(ITargetBlock<PublishResponse> target, DataflowLinkOptions linkOptions)
        {
            return _publishResponses.LinkTo(target, linkOptions);
        }

        /// <inheritdoc/>
        public PublishResponse? ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<PublishResponse> target, out bool messageConsumed)
        {
            return ((ISourceBlock<PublishResponse>)_publishResponses).ConsumeMessage(messageHeader, target, out messageConsumed);
        }

        /// <inheritdoc/>
        public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<PublishResponse> target)
        {
            return ((ISourceBlock<PublishResponse>)_publishResponses).ReserveMessage(messageHeader, target);
        }

        /// <inheritdoc/>
        public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<PublishResponse> target)
        {
            ((ISourceBlock<PublishResponse>)_publishResponses).ReleaseReservation(messageHeader, target);
        }

        /// <inheritdoc/>
        public void Complete()
        {
            _publishResponses.Complete();
        }

        /// <inheritdoc/>
        public void Fault(Exception exception)
        {
            ((ISourceBlock<PublishResponse>)_publishResponses).Fault(exception);
        }

        /// <inheritdoc/>
        public IDisposable Subscribe(IObserver<PublishResponse> observer)
        {
            return this.AsObservable().Subscribe(observer);
        }

        /// <inheritdoc/>
        protected override async Task OnOpeningAsync(CancellationToken token = default)
        {
            if (RemoteEndpoint.Server == null)
            {
                // If specific endpoint is not provided, use discovery to select endpoint with highest
                // security level.
                var endpointUrl = RemoteEndpoint.EndpointUrl;
                var securityPolicyUri = RemoteEndpoint.SecurityPolicyUri;
                try
                {
                    _logger?.LogInformation($"Discovering endpoints of '{endpointUrl}'.");
                    var getEndpointsRequest = new GetEndpointsRequest
                    {
                        EndpointUrl = endpointUrl,
                        ProfileUris = new[] { TransportProfileUris.UaTcpTransport } // We should rethink this line, once we support transport profiles.
                    };
                    var getEndpointsResponse = await DiscoveryService.GetEndpointsAsync(getEndpointsRequest, _loggerFactory, stackProfile: StackProfile).ConfigureAwait(false);
                    if (getEndpointsResponse.Endpoints == null || getEndpointsResponse.Endpoints.Length == 0)
                    {
                        throw new InvalidOperationException($"'{endpointUrl}' returned no endpoints.");
                    }

                    var selectedEndpoint = getEndpointsResponse.Endpoints
                        .OfType<EndpointDescription>()
                        .Where(e => string.IsNullOrEmpty(securityPolicyUri) || e.SecurityPolicyUri == securityPolicyUri)
                        .OrderBy(e => e.SecurityLevel)
                        .LastOrDefault();

                    if (selectedEndpoint is null)
                    {
                        throw new InvalidOperationException($"'{endpointUrl}' returned no endpoint for the requested security policy '{securityPolicyUri}'.");
                    }

                    RemoteEndpoint.Server = selectedEndpoint.Server;
                    RemoteEndpoint.ServerCertificate = selectedEndpoint.ServerCertificate;
                    RemoteEndpoint.SecurityMode = selectedEndpoint.SecurityMode;
                    RemoteEndpoint.SecurityPolicyUri = selectedEndpoint.SecurityPolicyUri;
                    RemoteEndpoint.UserIdentityTokens = selectedEndpoint.UserIdentityTokens;
                    RemoteEndpoint.TransportProfileUri = selectedEndpoint.TransportProfileUri;
                    RemoteEndpoint.SecurityLevel = selectedEndpoint.SecurityLevel;

                    _logger?.LogTrace($"Success discovering endpoints of '{endpointUrl}'.");
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Error discovering endpoints of '{endpointUrl}'. {ex.Message}");
                    throw;
                }
            }

            // Ask for user identity. May show dialog.
            if (UserIdentityProvider != null)
            {
                UserIdentity = await UserIdentityProvider(RemoteEndpoint);
            }

            await base.OnOpeningAsync(token);
        }

        /// <inheritdoc/>
        protected override async Task OnOpenAsync(CancellationToken token = default)
        {
            _logger?.LogInformation($"Opening session channel with endpoint '{RemoteEndpoint.EndpointUrl}'.");
            _logger?.LogInformation($"SecurityPolicy: '{RemoteEndpoint.SecurityPolicyUri}'.");
            _logger?.LogInformation($"SecurityMode: '{RemoteEndpoint.SecurityMode}'.");
            _logger?.LogInformation($"UserIdentity: '{UserIdentity}'.");

            await base.OnOpenAsync(token).ConfigureAwait(false);

            token.ThrowIfCancellationRequested();

            // if SessionId is provided then we skip the CreateSessionRequest and go directly to (re)ActivateSession.
            // requires from previous Session: SessionId, AuthenticationToken, RemoteNonce
            if (SessionId == null)
            {
                var localNonce = GetSessionNonce();

                if (CertificateStore != null)
                {
                    var tuple = await CertificateStore.GetLocalCertificateAsync(LocalDescription, _logger);
                    LocalCertificate = tuple.Certificate?.GetEncoded();
                    LocalPrivateKey = tuple.Key;
                }
            
                var cert = _certificateParser.ReadCertificate(RemoteCertificate);
                RemotePublicKey = cert?.GetPublicKey() as RsaKeyParameters;

                var createSessionRequest = new CreateSessionRequest
                {
                    ClientDescription = LocalDescription,
                    EndpointUrl = RemoteEndpoint.EndpointUrl,
                    SessionName = LocalDescription.ApplicationName,
                    ClientNonce = localNonce,
                    ClientCertificate = LocalCertificate,
                    RequestedSessionTimeout = _options.SessionTimeout,
                    MaxResponseMessageSize = RemoteMaxMessageSize
                };

                var createSessionResponse = await this.CreateSessionAsync(createSessionRequest).ConfigureAwait(false);
                SessionId = createSessionResponse.SessionId;
                AuthenticationToken = createSessionResponse.AuthenticationToken;
                RemoteNonce = createSessionResponse.ServerNonce;

                // verify the server's certificate is the same as the certificate from the selected endpoint.
                ThrowOnInvalidSessionServerCertificate(createSessionResponse.ServerCertificate);

                // verify the server's signature.
                ISigner? verifier = null;
                bool verified = false;

                switch (RemoteEndpoint.SecurityPolicyUri)
                {
                    case SecurityPolicyUris.Basic128Rsa15:
                    case SecurityPolicyUris.Basic256:
                        verifier = SignerUtilities.GetSigner("SHA-1withRSA");
                        verifier.Init(false, RemotePublicKey);
                        verifier.BlockUpdate(LocalCertificate, 0, LocalCertificate!.Length);
                        verifier.BlockUpdate(localNonce, 0, localNonce!.Length);
                        verified = verifier.VerifySignature(createSessionResponse.ServerSignature!.Signature);
                        break;

                    case SecurityPolicyUris.Basic256Sha256:
                    case SecurityPolicyUris.Aes128_Sha256_RsaOaep:
                        verifier = SignerUtilities.GetSigner("SHA-256withRSA");
                        verifier.Init(false, RemotePublicKey);
                        verifier.BlockUpdate(LocalCertificate, 0, LocalCertificate!.Length);
                        verifier.BlockUpdate(localNonce, 0, localNonce!.Length);
                        verified = verifier.VerifySignature(createSessionResponse.ServerSignature!.Signature);
                        break;

                    case SecurityPolicyUris.Aes256_Sha256_RsaPss:
                        verifier = SignerUtilities.GetSigner("SHA-256withRSAandMGF1");
                        verifier.Init(false, RemotePublicKey);
                        verifier.BlockUpdate(LocalCertificate, 0, LocalCertificate!.Length);
                        verifier.BlockUpdate(localNonce, 0, localNonce!.Length);
                        verified = verifier.VerifySignature(createSessionResponse.ServerSignature!.Signature);
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
            SignatureData? clientSignature = null;
            ISigner? signer = null;

            switch (RemoteEndpoint.SecurityPolicyUri)
            {
                case SecurityPolicyUris.Basic128Rsa15:
                case SecurityPolicyUris.Basic256:
                    signer = SignerUtilities.GetSigner("SHA-1withRSA");
                    signer.Init(true, LocalPrivateKey);
                    signer.BlockUpdate(RemoteEndpoint.ServerCertificate, 0, RemoteEndpoint.ServerCertificate!.Length);
                    signer.BlockUpdate(RemoteNonce, 0, RemoteNonce!.Length);
                    clientSignature = new SignatureData
                    {
                        Signature = signer.GenerateSignature(),
                        Algorithm = _rsaSha1Signature,
                    };

                    break;

                case SecurityPolicyUris.Basic256Sha256:
                case SecurityPolicyUris.Aes128_Sha256_RsaOaep:
                    signer = SignerUtilities.GetSigner("SHA-256withRSA");
                    signer.Init(true, LocalPrivateKey);
                    signer.BlockUpdate(RemoteEndpoint.ServerCertificate, 0, RemoteEndpoint.ServerCertificate!.Length);
                    signer.BlockUpdate(RemoteNonce, 0, RemoteNonce!.Length);
                    clientSignature = new SignatureData
                    {
                        Signature = signer.GenerateSignature(),
                        Algorithm = _rsaSha256Signature,
                    };

                    break;

                case SecurityPolicyUris.Aes256_Sha256_RsaPss:
                    signer = SignerUtilities.GetSigner("SHA-256withRSAandMGF1");
                    signer.Init(true, LocalPrivateKey);
                    signer.BlockUpdate(RemoteEndpoint.ServerCertificate, 0, RemoteEndpoint.ServerCertificate!.Length);
                    signer.BlockUpdate(RemoteNonce, 0, RemoteNonce!.Length);
                    clientSignature = new SignatureData
                    {
                        Signature = signer.GenerateSignature(),
                        Algorithm = _rsaPssSha256Signature,
                    };

                    break;

                default:
                    clientSignature = new SignatureData();
                    break;
            }

            signer = null;

            // supported UserIdentityToken types are AnonymousIdentityToken, UserNameIdentityToken, IssuedIdentityToken, X509IdentityToken
            UserIdentityToken? identityToken = null;
            SignatureData? tokenSignature = null;

            // if UserIdentity type is IssuedIdentity
            if (UserIdentity is IssuedIdentity issuedIdentity)
            {
                var tokenPolicy = RemoteEndpoint.UserIdentityTokens.FirstOrDefault(t => t?.TokenType == UserTokenType.IssuedToken);
                if (tokenPolicy == null)
                {
                    throw new ServiceResultException(StatusCodes.BadIdentityTokenRejected);
                }

                int plainTextLength = issuedIdentity.TokenData.Length + RemoteNonce!.Length;
                IBufferedCipher encryptor;
                byte[] cipherText;
                int pos;

                var secPolicyUri = tokenPolicy.SecurityPolicyUri ?? RemoteEndpoint.SecurityPolicyUri;
                switch (secPolicyUri)
                {
                    case SecurityPolicyUris.Basic128Rsa15:
                        encryptor = CipherUtilities.GetCipher("RSA//PKCS1Padding");
                        encryptor.Init(true, RemotePublicKey);
                        cipherText = new byte[encryptor.GetOutputSize(4 + plainTextLength)];
                        pos = encryptor.ProcessBytes(BitConverter.GetBytes(plainTextLength), cipherText, 0);
                        pos = encryptor.ProcessBytes(issuedIdentity.TokenData, cipherText, pos);
                        pos = encryptor.DoFinal(RemoteNonce, cipherText, pos);
                        identityToken = new IssuedIdentityToken
                        {
                            TokenData = cipherText,
                            EncryptionAlgorithm = _rsaV15KeyWrap,
                            PolicyId = tokenPolicy.PolicyId
                        };

                        break;

                    case SecurityPolicyUris.Basic256:
                    case SecurityPolicyUris.Basic256Sha256:
                    case SecurityPolicyUris.Aes128_Sha256_RsaOaep:
                        encryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        encryptor.Init(true, RemotePublicKey);
                        cipherText = new byte[encryptor.GetOutputSize(4 + plainTextLength)];
                        pos = encryptor.ProcessBytes(BitConverter.GetBytes(plainTextLength), cipherText, 0);
                        pos = encryptor.ProcessBytes(issuedIdentity.TokenData, cipherText, pos);
                        pos = encryptor.DoFinal(RemoteNonce, cipherText, pos);
                        identityToken = new IssuedIdentityToken
                        {
                            TokenData = cipherText,
                            EncryptionAlgorithm = _rsaOaepKeyWrap,
                            PolicyId = tokenPolicy.PolicyId
                        };

                        break;

                    case SecurityPolicyUris.Aes256_Sha256_RsaPss:
                        encryptor = CipherUtilities.GetCipher("RSA//OAEPWITHSHA256ANDMGF1PADDING");
                        encryptor.Init(true, RemotePublicKey);
                        cipherText = new byte[encryptor.GetOutputSize(4 + plainTextLength)];
                        pos = encryptor.ProcessBytes(BitConverter.GetBytes(plainTextLength), cipherText, 0);
                        pos = encryptor.ProcessBytes(issuedIdentity.TokenData, cipherText, pos);
                        pos = encryptor.DoFinal(RemoteNonce, cipherText, pos);
                        identityToken = new IssuedIdentityToken
                        {
                            TokenData = cipherText,
                            EncryptionAlgorithm = _rsaOaepSha256KeyWrap,
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
            }

            // if UserIdentity type is X509Identity
            else if (UserIdentity is X509Identity x509Identity)
            {
                var tokenPolicy = RemoteEndpoint.UserIdentityTokens.FirstOrDefault(t => t?.TokenType == UserTokenType.Certificate);
                if (tokenPolicy == null)
                {
                    throw new ServiceResultException(StatusCodes.BadIdentityTokenRejected);
                }

                identityToken = new X509IdentityToken { CertificateData = x509Identity.Certificate?.GetEncoded(), PolicyId = tokenPolicy.PolicyId };

                var secPolicyUri = tokenPolicy.SecurityPolicyUri ?? RemoteEndpoint.SecurityPolicyUri;
                switch (secPolicyUri)
                {

                    case SecurityPolicyUris.Basic128Rsa15:
                    case SecurityPolicyUris.Basic256:
                        signer = SignerUtilities.GetSigner("SHA-1withRSA");
                        signer.Init(true, x509Identity.PrivateKey);
                        signer.BlockUpdate(RemoteEndpoint.ServerCertificate, 0, RemoteEndpoint.ServerCertificate!.Length);
                        signer.BlockUpdate(RemoteNonce, 0, RemoteNonce!.Length);
                        tokenSignature = new SignatureData
                        {
                            Signature = signer.GenerateSignature(),
                            Algorithm = _rsaSha1Signature,
                        };

                        break;

                    case SecurityPolicyUris.Basic256Sha256:
                    case SecurityPolicyUris.Aes128_Sha256_RsaOaep:
                        signer = SignerUtilities.GetSigner("SHA-256withRSA");
                        signer.Init(true, x509Identity.PrivateKey);
                        signer.BlockUpdate(RemoteEndpoint.ServerCertificate, 0, RemoteEndpoint.ServerCertificate!.Length);
                        signer.BlockUpdate(RemoteNonce, 0, RemoteNonce!.Length);
                        tokenSignature = new SignatureData
                        {
                            Signature = signer.GenerateSignature(),
                            Algorithm = _rsaSha256Signature,
                        };
                        break;

                    case SecurityPolicyUris.Aes256_Sha256_RsaPss:
                        signer = SignerUtilities.GetSigner("SHA-256withRSAandMGF1");
                        signer.Init(true, x509Identity.PrivateKey);
                        signer.BlockUpdate(RemoteEndpoint.ServerCertificate, 0, RemoteEndpoint.ServerCertificate!.Length);
                        signer.BlockUpdate(RemoteNonce, 0, RemoteNonce!.Length);
                        tokenSignature = new SignatureData
                        {
                            Signature = signer.GenerateSignature(),
                            Algorithm = _rsaSha256Signature,
                        };
                        break;

                    default:
                        tokenSignature = new SignatureData();
                        break;
                }

                signer = null;
            }

            // if UserIdentity type is UserNameIdentity
            else if (UserIdentity is UserNameIdentity userNameIdentity)
            {
                var tokenPolicy = RemoteEndpoint.UserIdentityTokens.FirstOrDefault(t => t?.TokenType == UserTokenType.UserName);
                if (tokenPolicy == null)
                {
                    throw new ServiceResultException(StatusCodes.BadIdentityTokenRejected);
                }

                byte[] passwordBytes = userNameIdentity.Password != null ? System.Text.Encoding.UTF8.GetBytes(userNameIdentity.Password) : new byte[0];
                int plainTextLength = passwordBytes.Length + RemoteNonce!.Length;
                IBufferedCipher encryptor;
                byte[] cipherText;
                int pos;

                var secPolicyUri = tokenPolicy.SecurityPolicyUri ?? RemoteEndpoint.SecurityPolicyUri;
                switch (secPolicyUri)
                {
                    case SecurityPolicyUris.Basic128Rsa15:
                        encryptor = CipherUtilities.GetCipher("RSA//PKCS1Padding");
                        encryptor.Init(true, RemotePublicKey);
                        cipherText = new byte[encryptor.GetOutputSize(4 + plainTextLength)];
                        pos = encryptor.ProcessBytes(BitConverter.GetBytes(plainTextLength), cipherText, 0);
                        pos = encryptor.ProcessBytes(passwordBytes, cipherText, pos);
                        pos = encryptor.DoFinal(RemoteNonce, cipherText, pos);
                        identityToken = new UserNameIdentityToken
                        {
                            UserName = userNameIdentity.UserName,
                            Password = cipherText,
                            EncryptionAlgorithm = _rsaV15KeyWrap,
                            PolicyId = tokenPolicy.PolicyId
                        };

                        break;

                    case SecurityPolicyUris.Basic256:
                    case SecurityPolicyUris.Basic256Sha256:
                    case SecurityPolicyUris.Aes128_Sha256_RsaOaep:
                        encryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        encryptor.Init(true, RemotePublicKey);
                        cipherText = new byte[encryptor.GetOutputSize(4 + plainTextLength)];
                        pos = encryptor.ProcessBytes(BitConverter.GetBytes(plainTextLength), cipherText, 0);
                        pos = encryptor.ProcessBytes(passwordBytes, cipherText, pos);
                        pos = encryptor.DoFinal(RemoteNonce, cipherText, pos);
                        identityToken = new UserNameIdentityToken
                        {
                            UserName = userNameIdentity.UserName,
                            Password = cipherText,
                            EncryptionAlgorithm = _rsaOaepKeyWrap,
                            PolicyId = tokenPolicy.PolicyId
                        };

                        break;

                    case SecurityPolicyUris.Aes256_Sha256_RsaPss:
                        encryptor = CipherUtilities.GetCipher("RSA//OAEPWITHSHA256ANDMGF1PADDING");
                        encryptor.Init(true, RemotePublicKey);
                        cipherText = new byte[encryptor.GetOutputSize(4 + plainTextLength)];
                        pos = encryptor.ProcessBytes(BitConverter.GetBytes(plainTextLength), cipherText, 0);
                        pos = encryptor.ProcessBytes(passwordBytes, cipherText, pos);
                        pos = encryptor.DoFinal(RemoteNonce, cipherText, pos);
                        identityToken = new UserNameIdentityToken
                        {
                            UserName = userNameIdentity.UserName,
                            Password = cipherText,
                            EncryptionAlgorithm = _rsaOaepSha256KeyWrap,
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
            }

            // if UserIdentity type is AnonymousIdentity or null
            else
            {
                var tokenPolicy = RemoteEndpoint.UserIdentityTokens.FirstOrDefault(t => t?.TokenType == UserTokenType.Anonymous);
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
            RemoteNonce = activateSessionResponse.ServerNonce;

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
            if (readResponse.Results?.Length == 2)
            {
                if (readResponse.Results[0] is { } res0 && StatusCode.IsGood(res0.StatusCode))
                {
                    if (res0.GetValueOrDefault<string[]>() is { } namespaceUris)
                    {
                        NamespaceUris = namespaceUris;
                    }
                }

                if (readResponse.Results[1] is { } res1 && StatusCode.IsGood(res1.StatusCode))
                {
                    if (res1.GetValueOrDefault<string[]>() is { } serverUris)
                    {
                        ServerUris = serverUris;
                    }
                }
            }

            // create the keep alive subscription.
            var subscriptionRequest = new CreateSubscriptionRequest
            {
                RequestedPublishingInterval = 1000f,
                RequestedMaxKeepAliveCount = 5,
                RequestedLifetimeCount = 120,
                PublishingEnabled = true,
            };
            var subscriptionResponse = await this.CreateSubscriptionAsync(subscriptionRequest).ConfigureAwait(false);

            // link up the dataflow blocks
            var id = subscriptionResponse.SubscriptionId;
            var linkToken = this.LinkTo(_actionBlock, pr => pr.SubscriptionId == id);

            // start publishing.
            _stateMachineTask = Task.Run(() => StateMachineAsync(_stateMachineCts.Token));
        }

        /// <inheritdoc/>
        protected override Task OnClosingAsync(CancellationToken token = default)
        {
            _stateMachineCts.Cancel();
            return base.OnClosingAsync(token);
        }

        /// <inheritdoc/>
        protected override async Task OnCloseAsync(CancellationToken token = default)
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
        private async Task PublishAsync(CancellationToken token = default)
        {
            var publishRequest = new PublishRequest
            {
                RequestHeader = new RequestHeader { TimeoutHint = _publishTimeoutHint, ReturnDiagnostics = _options.DiagnosticsHint },
                SubscriptionAcknowledgements = new SubscriptionAcknowledgement[0]
            };
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var publishResponse = await this.PublishAsync(publishRequest, token).ConfigureAwait(false);

                    // post to linked data flow blocks and subscriptions.
                    _publishResponses.Post(publishResponse);

                    publishRequest = new PublishRequest
                    {
                        RequestHeader = new RequestHeader
                        {
                            TimeoutHint = _publishTimeoutHint,
                            ReturnDiagnostics = _options.DiagnosticsHint
                        },
                        SubscriptionAcknowledgements = publishResponse.NotificationMessage?.NotificationData != null
                        ? new[] 
                        { 
                            new SubscriptionAcknowledgement
                            {
                                SequenceNumber = publishResponse.NotificationMessage.SequenceNumber,
                                SubscriptionId = publishResponse.SubscriptionId
                            }
                        }
                        : new SubscriptionAcknowledgement[0]
                    };
                }
                catch (Exception ex)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    _logger?.LogError($"Error publishing subscription. {ex.Message}");
                    Fault(ex);
                    return;
                }
            }
        }

        /// <summary>
        /// The state machine manages the state of the channel.
        /// </summary>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A task.</returns>
        private async Task StateMachineAsync(CancellationToken token = default)
        {
            var tasks = new[]
            {
                PublishAsync(token),
                PublishAsync(token),
                PublishAsync(token),
            };
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }


        /// <summary>
        /// Ensures that the server certificate returned in a <see cref="CreateSessionResponse"/> 
        /// matches the <see cref="EndpointDescription.ServerCertificate"/> for the remote 
        /// endpoint (if required based on security policies).
        /// </summary>
        /// <param name="sessionCertificate">
        /// The server certificate returned in a <see cref="CreateSessionResponse"/>.
        /// </param>
        /// <exception cref="ServiceResultException">
        /// A certificate check is required and the <paramref name="sessionCertificate"/> does not 
        /// match the <see cref="EndpointDescription.ServerCertificate"/> for the remote endpoint.
        /// </exception>
        /// <remarks>
        /// OPC 10000-4 specifies that the client should ignore the server certificate returned by 
        /// a create session response when the security policy for the server is None and none of 
        /// the user token policies requires encryption.
        /// </remarks>
        private void ThrowOnInvalidSessionServerCertificate(byte[]? sessionCertificate) 
        {
            var compareCertificates = false;

            if (!string.Equals(this.RemoteEndpoint.SecurityPolicyUri, SecurityPolicyUris.None)) 
            {
                // Verification required if the security policy for the endpoint is not None.
                compareCertificates = true;
            }
            else if (this.RemoteEndpoint.UserIdentityTokens != null) 
            {
                // Check if any of the user token policies require encryption.
                foreach (var policy in this.RemoteEndpoint.UserIdentityTokens) 
                { 
                    if (policy == null) 
                    {
                        continue;
                    }

                    // If the policy does not define its own security policy, inherit the security 
                    // policy for the endpoint.
                    var securityPolicyUri = string.IsNullOrWhiteSpace(policy.SecurityPolicyUri)
                        ? this.RemoteEndpoint.SecurityPolicyUri
                        : policy.SecurityPolicyUri;

                    if (!string.Equals(securityPolicyUri, SecurityPolicyUris.None)) 
                    {
                        // User token policy requires encryption, so we need to verify the 
                        // session certificate.
                        compareCertificates = true;
                        break;
                    }
                }
            }

            if (compareCertificates) 
            {
                var isValid = this.RemoteEndpoint.ServerCertificate == null || sessionCertificate == null
                    ? this.RemoteEndpoint.ServerCertificate == null && sessionCertificate == null // Valid if both certificates are null
                    : this.RemoteEndpoint.ServerCertificate.SequenceEqual(sessionCertificate); // Valid if both certificates are equal

                if (!isValid) 
                { 
                    throw new ServiceResultException(StatusCodes.BadCertificateInvalid, "Server did not return the same certificate used to create the channel.");
                }
            }
        }

        /// <summary>
        /// Get random nonce.
        /// </summary>
        /// <returns>A nonce.</returns>
        private byte[] GetSessionNonce()
        {
            var nonce = new byte[_nonceLength];
            _rng.NextBytes(nonce);
            return nonce;
        }
    }
}
