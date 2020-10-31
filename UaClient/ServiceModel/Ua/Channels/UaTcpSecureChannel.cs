// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace Workstation.ServiceModel.Ua.Channels
{
    /// <summary>
    /// A secure channel for communicating with OPC UA servers using the UA TCP transport profile.
    /// </summary>
    public class UaTcpSecureChannel : UaTcpTransportChannel, IRequestChannel, IEncodingContext
    {
        /// <summary>
        /// The default timeout for requests.
        /// </summary>
        public const uint DefaultTimeoutHint = 15 * 1000; // 15 seconds

        /// <summary>
        /// the default diagnostic flags for requests.
        /// </summary>
        public const uint DefaultDiagnosticsHint = (uint)DiagnosticFlags.None;
        private const int _sequenceHeaderSize = 8;
        private const int _tokenRequestedLifetime = 60 * 60 * 1000; // 60 minutes

        private static readonly SecureRandom _rng = new SecureRandom();
        private static readonly RecyclableMemoryStreamManager _streamManager = new RecyclableMemoryStreamManager();

        private readonly CancellationTokenSource _channelCts;
        private readonly ILogger? _logger;
        private readonly SemaphoreSlim _sendingSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _receivingSemaphore = new SemaphoreSlim(1, 1);
        private readonly ActionBlock<ServiceOperation> _pendingRequests;
        private readonly ConcurrentDictionary<uint, ServiceOperation> _pendingCompletions;
        private readonly X509CertificateParser _certificateParser = new X509CertificateParser();

        private int _handle;
        private int _sequenceNumber;
        private uint _currentClientTokenId;
        private uint _currentServerTokenId;
        private byte[]? _clientSigningKey;
        private byte[]? _clientEncryptingKey;
        private byte[]? _clientInitializationVector;
        private byte[]? _serverSigningKey;
        private byte[]? _serverEncryptingKey;
        private byte[]? _serverInitializationVector;
        private byte[]? _encryptionBuffer;
        private Task? _receiveResponsesTask;
        private int _asymLocalKeySize;
        private int _asymRemoteKeySize;
        private int _asymLocalPlainTextBlockSize;
        private int _asymLocalCipherTextBlockSize;
        private int _asymLocalSignatureSize;
        private int _asymRemotePlainTextBlockSize;
        private int _asymRemoteCipherTextBlockSize;
        private int _asymRemoteSignatureSize;
        private bool _asymIsSigned;
        private bool _asymIsEncrypted;
        private int _symEncryptionBlockSize;
        private int _symEncryptionKeySize;
        private int _symSignatureSize;
        private bool _symIsSigned;
        private bool _symIsEncrypted;
        private int _symSignatureKeySize;
        private int _nonceSize;
        private byte[]? _sendBuffer;
        private byte[]? _receiveBuffer;

        private IBufferedCipher? _asymEncryptor;
        private IBufferedCipher? _asymDecryptor;
        private IBufferedCipher? _symEncryptor;
        private IBufferedCipher? _symDecryptor;
        private ISigner? _asymSigner;
        private ISigner? _asymVerifier;
        private IMac? _symSigner;
        private IMac? _symVerifier;
        private DateTime _tokenRenewalTime = DateTime.MaxValue;
        private IDigest? _thumbprintDigest;

        /// <summary>
        /// Initializes a new instance of the <see cref="UaTcpSecureChannel"/> class.
        /// </summary>
        /// <param name="localDescription">The local description.</param>
        /// <param name="certificateStore">The local certificate store.</param>
        /// <param name="remoteEndpoint">The remote endpoint</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="options">The secure channel options.</param>
        public UaTcpSecureChannel(
            ApplicationDescription localDescription,
            ICertificateStore? certificateStore,
            EndpointDescription remoteEndpoint,
            ILoggerFactory? loggerFactory = null,
            UaTcpSecureChannelOptions? options = null)
            : base(remoteEndpoint, loggerFactory, options)
        {
            LocalDescription = localDescription ?? throw new ArgumentNullException(nameof(localDescription));
            CertificateStore = certificateStore;
            TimeoutHint = options?.TimeoutHint ?? DefaultTimeoutHint;
            DiagnosticsHint = options?.DiagnosticsHint ?? DefaultDiagnosticsHint;

            _logger = loggerFactory?.CreateLogger<UaTcpSecureChannel>();

            AuthenticationToken = null;
            NamespaceUris = new List<string> { "http://opcfoundation.org/UA/" };
            ServerUris = new List<string>();
            _channelCts = new CancellationTokenSource();
            _pendingRequests = new ActionBlock<ServiceOperation>(t => SendRequestActionAsync(t), new ExecutionDataflowBlockOptions { CancellationToken = _channelCts.Token });
            _pendingCompletions = new ConcurrentDictionary<uint, ServiceOperation>();
        }

        /// <summary>
        /// Gets the local description.
        /// </summary>
        public ApplicationDescription LocalDescription { get; }

        /// <summary>
        /// Gets the certificate store.
        /// </summary>
        public ICertificateStore? CertificateStore { get; }

        /// <summary>
        /// Gets the default number of milliseconds that may elapse before an operation is cancelled by the service.
        /// </summary>
        public uint TimeoutHint { get; }

        /// <summary>
        /// Gets the default diagnostics flags to be requested by the service.
        /// </summary>
        public uint DiagnosticsHint { get; }

        /// <summary>
        /// Gets the local certificate.
        /// </summary>
        protected byte[]? LocalCertificate { get; private set; }

        /// <summary>
        /// Gets the remote certificate.
        /// </summary>
        protected byte[]? RemoteCertificate => RemoteEndpoint?.ServerCertificate;

        /// <summary>
        /// Gets the local private key.
        /// </summary>
        protected RsaKeyParameters? LocalPrivateKey { get; private set; }

        /// <summary>
        /// Gets the remote public key.
        /// </summary>
        protected RsaKeyParameters? RemotePublicKey { get; private set; }

        /// <summary>
        /// Gets the local nonce.
        /// </summary>
        protected byte[]? LocalNonce { get; private set; }

        /// <summary>
        /// Gets or sets the channel id.
        /// </summary>
        public uint ChannelId { get; protected set; }

        /// <summary>
        /// Gets or sets the token id.
        /// </summary>
        public uint TokenId { get; protected set; }

        /// <summary>
        /// Gets or sets the authentication token.
        /// </summary>
        public NodeId? AuthenticationToken { get; protected set; }

        /// <summary>
        /// Gets or sets the namespace uris.
        /// </summary>
        public IReadOnlyList<string> NamespaceUris { get; protected set; }

        /// <summary>
        /// Gets or sets the server uris.
        /// </summary>
        public IReadOnlyList<string> ServerUris { get; protected set; }

        public int MaxStringLength => 65535;

        public int MaxArrayLength => 65535;

        public int MaxByteStringLength => 65535;

        /// <summary>
        /// Sends a <see cref="T:Workstation.ServiceModel.Ua.IServiceRequest"/> to the server.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public virtual async Task<IServiceResponse> RequestAsync(IServiceRequest request, CancellationToken token = default)
        {
            ThrowIfClosedOrNotOpening();
            TimestampHeader(request);
            var operation = new ServiceOperation(request);
            // TimestampHeader takes care that the RequestHeader property will not be null
            using (var timeoutCts = new CancellationTokenSource((int)request.RequestHeader!.TimeoutHint))
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, _channelCts.Token, token))
            using (var registration = linkedCts.Token.Register(o => ((ServiceOperation)o!).TrySetException(new ServiceResultException(StatusCodes.BadRequestTimeout)), operation, false))
            {
                if (_pendingRequests.Post(operation))
                {
                    return await operation.Task.ConfigureAwait(false);
                }

                throw new ServiceResultException(StatusCodes.BadSecureChannelClosed);
            }
        }

        /// <inheritdoc/>
        protected override async Task OnOpeningAsync(CancellationToken token)
        {
            await base.OnOpeningAsync(token).ConfigureAwait(false);

            if (RemoteCertificate != null)
            {
                var cert = _certificateParser.ReadCertificate(RemoteCertificate);
                if (cert != null)
                {
                    if (CertificateStore != null)
                    {
                        var result = await CertificateStore.ValidateRemoteCertificateAsync(cert, _logger);
                        if (!result)
                        {
                            throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed, "Remote certificate is untrusted.");
                        }
                    }

                    RemotePublicKey = cert.GetPublicKey() as RsaKeyParameters;
                }
            }

            if (RemoteEndpoint.SecurityMode == MessageSecurityMode.SignAndEncrypt)
            {
                if (LocalCertificate == null && CertificateStore != null)
                {
                    var tuple = await CertificateStore.GetLocalCertificateAsync(LocalDescription, _logger);
                    LocalCertificate = tuple.Certificate?.GetEncoded();
                    LocalPrivateKey = tuple.Key;
                }

                if (LocalPrivateKey == null)
                {
                    throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed, "LocalPrivateKey is null.");
                }

                if (RemotePublicKey == null)
                {
                    throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed, "RemotePublicKey is null.");
                }

                switch (RemoteEndpoint.SecurityPolicyUri)
                {
                    case SecurityPolicyUris.Basic128Rsa15:

                        _asymSigner = SignerUtilities.GetSigner("SHA-1withRSA");
                        _asymSigner.Init(true, LocalPrivateKey);
                        _asymVerifier = SignerUtilities.GetSigner("SHA-1withRSA");
                        _asymVerifier.Init(false, RemotePublicKey);
                        _asymEncryptor = CipherUtilities.GetCipher("RSA//PKCS1Padding");
                        _asymEncryptor.Init(true, RemotePublicKey);
                        _asymDecryptor = CipherUtilities.GetCipher("RSA//PKCS1Padding");
                        _asymDecryptor.Init(false, LocalPrivateKey);
                        _symSigner = new HMac(new Sha1Digest());
                        _symVerifier = new HMac(new Sha1Digest());
                        _symEncryptor = CipherUtilities.GetCipher("AES/CBC/NoPadding");
                        _symDecryptor = CipherUtilities.GetCipher("AES/CBC/NoPadding");
                        _asymLocalKeySize = LocalPrivateKey.Modulus.BitLength;
                        _asymRemoteKeySize = RemotePublicKey.Modulus.BitLength;
                        _asymLocalPlainTextBlockSize = Math.Max((_asymLocalKeySize / 8) - 11, 1);
                        _asymRemotePlainTextBlockSize = Math.Max((_asymRemoteKeySize / 8) - 11, 1);
                        _symSignatureSize = 20;
                        _symSignatureKeySize = 16;
                        _symEncryptionBlockSize = 16;
                        _symEncryptionKeySize = 16;
                        _nonceSize = 16;
                        break;

                    case SecurityPolicyUris.Basic256:

                        _asymSigner = SignerUtilities.GetSigner("SHA-1withRSA");
                        _asymSigner.Init(true, LocalPrivateKey);
                        _asymVerifier = SignerUtilities.GetSigner("SHA-1withRSA");
                        _asymVerifier.Init(false, RemotePublicKey);
                        _asymEncryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        _asymEncryptor.Init(true, RemotePublicKey);
                        _asymDecryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        _asymDecryptor.Init(false, LocalPrivateKey);
                        _symSigner = new HMac(new Sha1Digest());
                        _symVerifier = new HMac(new Sha1Digest());
                        _symEncryptor = CipherUtilities.GetCipher("AES/CBC/NoPadding");
                        _symDecryptor = CipherUtilities.GetCipher("AES/CBC/NoPadding");
                        _asymLocalKeySize = LocalPrivateKey.Modulus.BitLength;
                        _asymRemoteKeySize = RemotePublicKey.Modulus.BitLength;
                        _asymLocalPlainTextBlockSize = Math.Max((_asymLocalKeySize / 8) - 42, 1);
                        _asymRemotePlainTextBlockSize = Math.Max((_asymRemoteKeySize / 8) - 42, 1);
                        _symSignatureSize = 20;
                        _symSignatureKeySize = 24;
                        _symEncryptionBlockSize = 16;
                        _symEncryptionKeySize = 32;
                        _nonceSize = 32;
                        break;

                    case SecurityPolicyUris.Basic256Sha256:

                        _asymSigner = SignerUtilities.GetSigner("SHA-256withRSA");
                        _asymSigner.Init(true, LocalPrivateKey);
                        _asymVerifier = SignerUtilities.GetSigner("SHA-256withRSA");
                        _asymVerifier.Init(false, RemotePublicKey);
                        _asymEncryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        _asymEncryptor.Init(true, RemotePublicKey);
                        _asymDecryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        _asymDecryptor.Init(false, LocalPrivateKey);
                        _symSigner = new HMac(new Sha256Digest());
                        _symVerifier = new HMac(new Sha256Digest());
                        _symEncryptor = CipherUtilities.GetCipher("AES/CBC/NoPadding");
                        _symDecryptor = CipherUtilities.GetCipher("AES/CBC/NoPadding");
                        _asymLocalKeySize = LocalPrivateKey.Modulus.BitLength;
                        _asymRemoteKeySize = RemotePublicKey.Modulus.BitLength;
                        _asymLocalPlainTextBlockSize = Math.Max((_asymLocalKeySize / 8) - 42, 1);
                        _asymRemotePlainTextBlockSize = Math.Max((_asymRemoteKeySize / 8) - 42, 1);
                        _symSignatureSize = 32;
                        _symSignatureKeySize = 32;
                        _symEncryptionBlockSize = 16;
                        _symEncryptionKeySize = 32;
                        _nonceSize = 32;
                        break;

                    case SecurityPolicyUris.Aes128_Sha256_RsaOaep:

                        _asymSigner = SignerUtilities.GetSigner("SHA-256withRSA");
                        _asymSigner.Init(true, LocalPrivateKey);
                        _asymVerifier = SignerUtilities.GetSigner("SHA-256withRSA");
                        _asymVerifier.Init(false, RemotePublicKey);
                        _asymEncryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        _asymEncryptor.Init(true, RemotePublicKey);
                        _asymDecryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        _asymDecryptor.Init(false, LocalPrivateKey);
                        _symSigner = new HMac(new Sha256Digest());
                        _symVerifier = new HMac(new Sha256Digest());
                        _symEncryptor = CipherUtilities.GetCipher("AES/CBC/NoPadding");
                        _symDecryptor = CipherUtilities.GetCipher("AES/CBC/NoPadding");
                        _asymLocalKeySize = LocalPrivateKey.Modulus.BitLength;
                        _asymRemoteKeySize = RemotePublicKey.Modulus.BitLength;
                        _asymLocalPlainTextBlockSize = Math.Max((_asymLocalKeySize / 8) - 42, 1);
                        _asymRemotePlainTextBlockSize = Math.Max((_asymRemoteKeySize / 8) - 42, 1);
                        _symSignatureSize = 32;
                        _symSignatureKeySize = 32;
                        _symEncryptionBlockSize = 16;
                        _symEncryptionKeySize = 16;
                        _nonceSize = 32;
                        break;

                    case SecurityPolicyUris.Aes256_Sha256_RsaPss:

                        _asymSigner = SignerUtilities.GetSigner("SHA-256withRSAandMGF1");
                        _asymSigner.Init(true, LocalPrivateKey);
                        _asymVerifier = SignerUtilities.GetSigner("SHA-256withRSAandMGF1");
                        _asymVerifier.Init(false, RemotePublicKey);
                        _asymEncryptor = CipherUtilities.GetCipher("RSA//OAEPWITHSHA256ANDMGF1PADDING");
                        _asymEncryptor.Init(true, RemotePublicKey);
                        _asymDecryptor = CipherUtilities.GetCipher("RSA//OAEPWITHSHA256ANDMGF1PADDING");
                        _asymDecryptor.Init(false, LocalPrivateKey);
                        _symSigner = new HMac(new Sha256Digest());
                        _symVerifier = new HMac(new Sha256Digest());
                        _symEncryptor = CipherUtilities.GetCipher("AES/CBC/NoPadding");
                        _symDecryptor = CipherUtilities.GetCipher("AES/CBC/NoPadding");
                        _asymLocalKeySize = LocalPrivateKey.Modulus.BitLength;
                        _asymRemoteKeySize = RemotePublicKey.Modulus.BitLength;
                        _asymLocalPlainTextBlockSize = Math.Max((_asymLocalKeySize / 8) - 66, 1);
                        _asymRemotePlainTextBlockSize = Math.Max((_asymRemoteKeySize / 8) - 66, 1);
                        _symSignatureSize = 32;
                        _symSignatureKeySize = 32;
                        _symEncryptionBlockSize = 16;
                        _symEncryptionKeySize = 32;
                        _nonceSize = 32;
                        break;

                    default:
                        throw new ServiceResultException(StatusCodes.BadSecurityPolicyRejected);
                }

                _asymIsSigned = _asymIsEncrypted = true;
                _symIsSigned = true;
                _symIsEncrypted = true;
                _asymLocalSignatureSize = _asymLocalKeySize / 8;
                _asymLocalCipherTextBlockSize = Math.Max(_asymLocalKeySize / 8, 1);
                _asymRemoteSignatureSize = _asymRemoteKeySize / 8;
                _asymRemoteCipherTextBlockSize = Math.Max(_asymRemoteKeySize / 8, 1);
                _clientSigningKey = new byte[_symSignatureKeySize];
                _clientEncryptingKey = new byte[_symEncryptionKeySize];
                _clientInitializationVector = new byte[_symEncryptionBlockSize];
                _serverSigningKey = new byte[_symSignatureKeySize];
                _serverEncryptingKey = new byte[_symEncryptionKeySize];
                _serverInitializationVector = new byte[_symEncryptionBlockSize];
                _encryptionBuffer = new byte[LocalSendBufferSize];
                _thumbprintDigest = DigestUtilities.GetDigest("SHA-1");
            }
            else if (RemoteEndpoint.SecurityMode == MessageSecurityMode.Sign)
            {
                if (LocalCertificate == null && CertificateStore != null)
                {
                    var tuple = await CertificateStore.GetLocalCertificateAsync(LocalDescription, _logger);
                    LocalCertificate = tuple.Certificate?.GetEncoded();
                    LocalPrivateKey = tuple.Key;
                }

                if (LocalPrivateKey == null)
                {
                    throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed, "LocalPrivateKey is null.");
                }

                if (RemotePublicKey == null)
                {
                    throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed, "RemotePublicKey is null.");
                }

                switch (RemoteEndpoint.SecurityPolicyUri)
                {
                    case SecurityPolicyUris.Basic128Rsa15:

                        _asymSigner = SignerUtilities.GetSigner("SHA-1withRSA");
                        _asymSigner.Init(true, LocalPrivateKey);
                        _asymVerifier = SignerUtilities.GetSigner("SHA-1withRSA");
                        _asymVerifier.Init(false, RemotePublicKey);
                        _asymEncryptor = CipherUtilities.GetCipher("RSA//PKCS1Padding");
                        _asymEncryptor.Init(true, RemotePublicKey);
                        _asymDecryptor = CipherUtilities.GetCipher("RSA//PKCS1Padding");
                        _asymDecryptor.Init(false, LocalPrivateKey);
                        _symSigner = new HMac(new Sha1Digest());
                        _symVerifier = new HMac(new Sha1Digest());
                        _asymLocalKeySize = LocalPrivateKey.Modulus.BitLength;
                        _asymRemoteKeySize = RemotePublicKey.Modulus.BitLength;
                        _asymLocalPlainTextBlockSize = Math.Max((_asymLocalKeySize / 8) - 11, 1);
                        _asymRemotePlainTextBlockSize = Math.Max((_asymRemoteKeySize / 8) - 11, 1);
                        _symSignatureSize = 20;
                        _symSignatureKeySize = 16;
                        _symEncryptionBlockSize = 16;
                        _symEncryptionKeySize = 16;
                        _nonceSize = 16;
                        break;

                    case SecurityPolicyUris.Basic256:

                        _asymSigner = SignerUtilities.GetSigner("SHA-1withRSA");
                        _asymSigner.Init(true, LocalPrivateKey);
                        _asymVerifier = SignerUtilities.GetSigner("SHA-1withRSA");
                        _asymVerifier.Init(false, RemotePublicKey);
                        _asymEncryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        _asymEncryptor.Init(true, RemotePublicKey);
                        _asymDecryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        _asymDecryptor.Init(false, LocalPrivateKey);
                        _symSigner = new HMac(new Sha1Digest());
                        _symVerifier = new HMac(new Sha1Digest());
                        _asymLocalKeySize = LocalPrivateKey.Modulus.BitLength;
                        _asymRemoteKeySize = RemotePublicKey.Modulus.BitLength;
                        _asymLocalPlainTextBlockSize = Math.Max((_asymLocalKeySize / 8) - 42, 1);
                        _asymRemotePlainTextBlockSize = Math.Max((_asymRemoteKeySize / 8) - 42, 1);
                        _symSignatureSize = 20;
                        _symSignatureKeySize = 24;
                        _symEncryptionBlockSize = 16;
                        _symEncryptionKeySize = 32;
                        _nonceSize = 32;
                        break;

                    case SecurityPolicyUris.Basic256Sha256:

                        _asymSigner = SignerUtilities.GetSigner("SHA-256withRSA");
                        _asymSigner.Init(true, LocalPrivateKey);
                        _asymVerifier = SignerUtilities.GetSigner("SHA-256withRSA");
                        _asymVerifier.Init(false, RemotePublicKey);
                        _asymEncryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        _asymEncryptor.Init(true, RemotePublicKey);
                        _asymDecryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        _asymDecryptor.Init(false, LocalPrivateKey);
                        _symSigner = new HMac(new Sha256Digest());
                        _symVerifier = new HMac(new Sha256Digest());
                        _asymLocalKeySize = LocalPrivateKey.Modulus.BitLength;
                        _asymRemoteKeySize = RemotePublicKey.Modulus.BitLength;
                        _asymLocalPlainTextBlockSize = Math.Max((_asymLocalKeySize / 8) - 42, 1);
                        _asymRemotePlainTextBlockSize = Math.Max((_asymRemoteKeySize / 8) - 42, 1);
                        _symSignatureSize = 32;
                        _symSignatureKeySize = 32;
                        _symEncryptionBlockSize = 16;
                        _symEncryptionKeySize = 32;
                        _nonceSize = 32;
                        break;

                    case SecurityPolicyUris.Aes128_Sha256_RsaOaep:

                        _asymSigner = SignerUtilities.GetSigner("SHA-256withRSA");
                        _asymSigner.Init(true, LocalPrivateKey);
                        _asymVerifier = SignerUtilities.GetSigner("SHA-256withRSA");
                        _asymVerifier.Init(false, RemotePublicKey);
                        _asymEncryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        _asymEncryptor.Init(true, RemotePublicKey);
                        _asymDecryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        _asymDecryptor.Init(false, LocalPrivateKey);
                        _symSigner = new HMac(new Sha256Digest());
                        _symVerifier = new HMac(new Sha256Digest());
                        _asymLocalKeySize = LocalPrivateKey.Modulus.BitLength;
                        _asymRemoteKeySize = RemotePublicKey.Modulus.BitLength;
                        _asymLocalPlainTextBlockSize = Math.Max((_asymLocalKeySize / 8) - 42, 1);
                        _asymRemotePlainTextBlockSize = Math.Max((_asymRemoteKeySize / 8) - 42, 1);
                        _symSignatureSize = 32;
                        _symSignatureKeySize = 32;
                        _symEncryptionBlockSize = 16;
                        _symEncryptionKeySize = 16;
                        _nonceSize = 32;
                        break;

                    case SecurityPolicyUris.Aes256_Sha256_RsaPss:

                        _asymSigner = SignerUtilities.GetSigner("SHA-256withRSAandMGF1");
                        _asymSigner.Init(true, LocalPrivateKey);
                        _asymVerifier = SignerUtilities.GetSigner("SHA-256withRSAandMGF1");
                        _asymVerifier.Init(false, RemotePublicKey);
                        _asymEncryptor = CipherUtilities.GetCipher("RSA//OAEPWITHSHA256ANDMGF1PADDING");
                        _asymEncryptor.Init(true, RemotePublicKey);
                        _asymDecryptor = CipherUtilities.GetCipher("RSA//OAEPWITHSHA256ANDMGF1PADDING");
                        _asymDecryptor.Init(false, LocalPrivateKey);
                        _symSigner = new HMac(new Sha256Digest());
                        _symVerifier = new HMac(new Sha256Digest());
                        _symEncryptor = CipherUtilities.GetCipher("AES/CBC/NoPadding");
                        _symDecryptor = CipherUtilities.GetCipher("AES/CBC/NoPadding");
                        _asymLocalKeySize = LocalPrivateKey.Modulus.BitLength;
                        _asymRemoteKeySize = RemotePublicKey.Modulus.BitLength;
                        _asymLocalPlainTextBlockSize = Math.Max((_asymLocalKeySize / 8) - 66, 1);
                        _asymRemotePlainTextBlockSize = Math.Max((_asymRemoteKeySize / 8) - 66, 1);
                        _symSignatureSize = 32;
                        _symSignatureKeySize = 32;
                        _symEncryptionBlockSize = 16;
                        _symEncryptionKeySize = 32;
                        _nonceSize = 32;
                        break;

                    default:
                        throw new ServiceResultException(StatusCodes.BadSecurityPolicyRejected);
                }

                _asymIsSigned = _asymIsEncrypted = true;
                _symIsSigned = true;
                _symIsEncrypted = false;
                _asymLocalSignatureSize = _asymLocalKeySize / 8;
                _asymLocalCipherTextBlockSize = Math.Max(_asymLocalKeySize / 8, 1);
                _asymRemoteSignatureSize = _asymRemoteKeySize / 8;
                _asymRemoteCipherTextBlockSize = Math.Max(_asymRemoteKeySize / 8, 1);
                _clientSigningKey = new byte[_symSignatureKeySize];
                _clientEncryptingKey = new byte[_symEncryptionKeySize];
                _clientInitializationVector = new byte[_symEncryptionBlockSize];
                _serverSigningKey = new byte[_symSignatureKeySize];
                _serverEncryptingKey = new byte[_symEncryptionKeySize];
                _serverInitializationVector = new byte[_symEncryptionBlockSize];
                _encryptionBuffer = new byte[LocalSendBufferSize];
                _thumbprintDigest = DigestUtilities.GetDigest("SHA-1");
            }
            else if (RemoteEndpoint.SecurityMode == MessageSecurityMode.None)
            {
                _asymIsSigned = _asymIsEncrypted = false;
                _symIsSigned = _symIsEncrypted = false;
                _asymLocalKeySize = 0;
                _asymRemoteKeySize = 0;
                _asymLocalSignatureSize = 0;
                _asymLocalCipherTextBlockSize = 1;
                _asymRemoteSignatureSize = 0;
                _asymRemoteCipherTextBlockSize = 1;
                _asymLocalPlainTextBlockSize = 1;
                _asymRemotePlainTextBlockSize = 1;
                _symSignatureSize = 0;
                _symSignatureKeySize = 0;
                _symEncryptionBlockSize = 1;
                _symEncryptionKeySize = 0;
                _nonceSize = 0;
                _encryptionBuffer = null;
            }
            else
            {
                throw new ServiceResultException(StatusCodes.BadSecurityModeRejected);
            }
        }

        /// <inheritdoc/>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part4/5.5.2/">OPC UA specification Part 4: Services, 5.5.2</seealso>
        protected override async Task OnOpenAsync(CancellationToken token = default)
        {
            await base.OnOpenAsync(token).ConfigureAwait(false);

            token.ThrowIfCancellationRequested();

            _sendBuffer = new byte[LocalSendBufferSize];
            _receiveBuffer = new byte[LocalReceiveBufferSize];

            _receiveResponsesTask = ReceiveResponsesAsync(_channelCts.Token);

            var openSecureChannelRequest = new OpenSecureChannelRequest
            {
                ClientProtocolVersion = ProtocolVersion,
                RequestType = SecurityTokenRequestType.Issue,
                SecurityMode = RemoteEndpoint.SecurityMode,
                ClientNonce = _symIsSigned ? LocalNonce = GetNextNonce(_nonceSize) : null,
                RequestedLifetime = _tokenRequestedLifetime
            };

            var openSecureChannelResponse = (OpenSecureChannelResponse)await RequestAsync(openSecureChannelRequest).ConfigureAwait(false);

            if (openSecureChannelResponse.ServerProtocolVersion < ProtocolVersion)
            {
                throw new ServiceResultException(StatusCodes.BadProtocolVersionUnsupported);
            }
        }

        /// <inheritdoc/>
        /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part4/5.5.3/">OPC UA specification Part 4: Services, 5.5.3</seealso>
        protected override async Task OnCloseAsync(CancellationToken token = default)
        {
            try
            {
                var request = new CloseSecureChannelRequest { RequestHeader = new RequestHeader { TimeoutHint = TimeoutHint, ReturnDiagnostics = DiagnosticsHint } };
                await RequestAsync(request).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error closing secure channel. {ex.Message}");
            }

            await base.OnCloseAsync(token).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        protected override async Task OnAbortAsync(CancellationToken token = default)
        {
            await base.OnAbortAsync(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Calculate the pseudo random function.
        /// </summary>
        /// <param name="secret">The secret.</param>
        /// <param name="seed">The seed.</param>
        /// <param name="sizeBytes">The size in bytes.</param>
        /// <param name="securityPolicyUri">The securityPolicyUri.</param>
        /// <returns>A array of bytes.</returns>
        private static byte[] CalculatePSHA(byte[] secret, byte[] seed, int sizeBytes, string securityPolicyUri)
        {
            IDigest digest;
            switch (securityPolicyUri)
            {
                case SecurityPolicyUris.Basic128Rsa15:
                case SecurityPolicyUris.Basic256:
                    digest = new Sha1Digest();
                    break;

                case SecurityPolicyUris.Basic256Sha256:
                case SecurityPolicyUris.Aes128_Sha256_RsaOaep:
                case SecurityPolicyUris.Aes256_Sha256_RsaPss:
                    digest = new Sha256Digest();
                    break;

                default:
                    digest = new Sha1Digest();
                    break;
            }

            HMac mac = new HMac(digest);
            byte[] output = new byte[sizeBytes];
            mac.Init(new KeyParameter(secret));
            byte[] a = seed;
            int size = digest.GetDigestSize();
            int iterations = (output.Length + size - 1) / size;
            byte[] buf = new byte[mac.GetMacSize()];
            byte[] buf2 = new byte[mac.GetMacSize()];
            for (int i = 0; i < iterations; i++)
            {
                mac.BlockUpdate(a, 0, a.Length);
                mac.DoFinal(buf, 0);
                a = buf;
                mac.BlockUpdate(a, 0, a.Length);
                mac.BlockUpdate(seed, 0, seed.Length);
                mac.DoFinal(buf2, 0);
                Array.Copy(buf2, 0, output, size * i, Math.Min(size, output.Length - (size * i)));
            }

            return output;
        }

        /// <summary>
        /// Send service request on transport channel.
        /// </summary>
        /// <param name="operation">A service operation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task SendRequestActionAsync(ServiceOperation operation)
        {
            try
            {
                if (operation.Task.Status == TaskStatus.WaitingForActivation)
                {
                    await SendRequestAsync(operation, _channelCts.Token).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error sending request. {ex.Message}");
                await FaultAsync(ex).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Send service request on transport channel.
        /// </summary>
        /// <param name="operation">A service operation.</param>
        /// <param name="token">A cancellation token</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task SendRequestAsync(ServiceOperation operation, CancellationToken token = default)
        {
            await _sendingSemaphore.WaitAsync(token).ConfigureAwait(false);
            var request = operation.Request;
            try
            {
                ThrowIfClosedOrNotOpening();

                // Check if time to renew security token.
                if (DateTime.UtcNow > _tokenRenewalTime)
                {
                    _tokenRenewalTime = _tokenRenewalTime.AddMilliseconds(60000);
                    var openSecureChannelRequest = new OpenSecureChannelRequest
                    {
                        RequestHeader = new RequestHeader
                        {
                            TimeoutHint = TimeoutHint,
                            ReturnDiagnostics = DiagnosticsHint,
                            Timestamp = DateTime.UtcNow,
                            RequestHandle = GetNextHandle(),
                            AuthenticationToken = AuthenticationToken
                        },
                        ClientProtocolVersion = ProtocolVersion,
                        RequestType = SecurityTokenRequestType.Renew,
                        SecurityMode = RemoteEndpoint.SecurityMode,
                        ClientNonce = _symIsSigned ? LocalNonce = GetNextNonce(_nonceSize) : null,
                        RequestedLifetime = _tokenRequestedLifetime
                    };
                    _logger?.LogTrace($"Sending {openSecureChannelRequest.GetType().Name}, Handle: {openSecureChannelRequest.RequestHeader.RequestHandle}");
                    _pendingCompletions.TryAdd(openSecureChannelRequest.RequestHeader.RequestHandle, new ServiceOperation(openSecureChannelRequest));
                    await SendOpenSecureChannelRequestAsync(openSecureChannelRequest, token).ConfigureAwait(false);
                }

                // RequestAsync takes care that every request has a non-null header
                var header = request.RequestHeader!;
                header.RequestHandle = GetNextHandle();
                header.AuthenticationToken = AuthenticationToken;

                _logger?.LogTrace($"Sending {request.GetType().Name}, Handle: {header.RequestHandle}");
                _pendingCompletions.TryAdd(header.RequestHandle, operation);
                if (request is OpenSecureChannelRequest)
                {
                    await SendOpenSecureChannelRequestAsync((OpenSecureChannelRequest)request, token).ConfigureAwait(false);
                }
                else if (request is CloseSecureChannelRequest)
                {
                    await SendCloseSecureChannelRequestAsync((CloseSecureChannelRequest)request, token).ConfigureAwait(false);
                    operation.TrySetResult(new CloseSecureChannelResponse { ResponseHeader = new ResponseHeader { RequestHandle = header.RequestHandle, Timestamp = DateTime.UtcNow } });
                }
                else
                {
                    await SendServiceRequestAsync(request, token).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error sending {request.GetType().Name}, Handle: {request.RequestHeader!.RequestHandle}. {ex.Message}");
                throw;
            }
            finally
            {
                _sendingSemaphore.Release();
            }
        }

        /// <summary>
        /// Send open secure channel service request on transport channel.
        /// </summary>
        /// <param name="request">A service request</param>
        /// <param name="token">A cancellation token</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task SendOpenSecureChannelRequestAsync(OpenSecureChannelRequest request, CancellationToken token)
        {
            var bodyStream = _streamManager.GetStream("SendOpenSecureChannelRequestAsync");
            var bodyEncoder = new BinaryEncoder(bodyStream, this);
            try
            {
                bodyEncoder.WriteRequest(request);
                bodyStream.Position = 0;
                if (RemoteMaxMessageSize > 0 && bodyStream.Length > RemoteMaxMessageSize)
                {
                    throw new ServiceResultException(StatusCodes.BadEncodingLimitsExceeded);
                }

                // write chunks
                int chunkCount = 0;
                int bodyCount = (int)(bodyStream.Length - bodyStream.Position);
                while (bodyCount > 0)
                {
                    chunkCount++;
                    if (RemoteMaxChunkCount > 0 && chunkCount > RemoteMaxChunkCount)
                    {
                        throw new ServiceResultException(StatusCodes.BadEncodingLimitsExceeded);
                    }

                    var stream = new MemoryStream(_sendBuffer!, 0, (int)RemoteReceiveBufferSize, true, true);
                    var encoder = new BinaryEncoder(stream, this);
                    try
                    {
                        // header
                        encoder.WriteUInt32(null, UaTcpMessageTypes.OPNF);
                        encoder.WriteUInt32(null, 0u);
                        encoder.WriteUInt32(null, ChannelId);

                        // asymmetric security header
                        encoder.WriteString(null, RemoteEndpoint.SecurityPolicyUri);
                        if (RemoteEndpoint.SecurityMode != MessageSecurityMode.None)
                        {
                            encoder.WriteByteString(null, LocalCertificate);
                            byte[] thumbprint = new byte[_thumbprintDigest!.GetDigestSize()];
                            _thumbprintDigest.BlockUpdate(RemoteCertificate, 0, RemoteCertificate!.Length);
                            _thumbprintDigest.DoFinal(thumbprint, 0);
                            encoder.WriteByteString(null, thumbprint);
                        }
                        else
                        {
                            encoder.WriteByteString(null, null);
                            encoder.WriteByteString(null, null);
                        }

                        int plainHeaderSize = encoder.Position;

                        // sequence header
                        encoder.WriteUInt32(null, GetNextSequenceNumber());
                        encoder.WriteUInt32(null, request.RequestHeader!.RequestHandle);

                        // body
                        int paddingHeaderSize;
                        int maxBodySize;
                        int bodySize;
                        int paddingSize;
                        int chunkSize;
                        if (_asymIsEncrypted)
                        {
                            paddingHeaderSize = _asymRemoteCipherTextBlockSize > 256 ? 2 : 1;
                            maxBodySize = ((((int)RemoteReceiveBufferSize - plainHeaderSize) / _asymRemoteCipherTextBlockSize) * _asymRemotePlainTextBlockSize) - _sequenceHeaderSize - paddingHeaderSize - _asymLocalSignatureSize;
                            if (bodyCount < maxBodySize)
                            {
                                bodySize = bodyCount;
                                paddingSize = (_asymRemotePlainTextBlockSize - ((_sequenceHeaderSize + bodySize + paddingHeaderSize + _asymLocalSignatureSize) % _asymRemotePlainTextBlockSize)) % _asymRemotePlainTextBlockSize;
                            }
                            else
                            {
                                bodySize = maxBodySize;
                                paddingSize = 0;
                            }

                            chunkSize = plainHeaderSize + (((_sequenceHeaderSize + bodySize + paddingSize + paddingHeaderSize + _asymLocalSignatureSize) / _asymRemotePlainTextBlockSize) * _asymRemoteCipherTextBlockSize);
                        }
                        else
                        {
                            paddingHeaderSize = 0;
                            paddingSize = 0;
                            maxBodySize = (int)RemoteReceiveBufferSize - plainHeaderSize - _sequenceHeaderSize - _asymLocalSignatureSize;
                            if (bodyCount < maxBodySize)
                            {
                                bodySize = bodyCount;
                            }
                            else
                            {
                                bodySize = maxBodySize;
                            }

                            chunkSize = plainHeaderSize + _sequenceHeaderSize + bodySize + _asymLocalSignatureSize;
                        }

                        bodyStream.Read(_sendBuffer!, encoder.Position, bodySize);
                        encoder.Position += bodySize;
                        bodyCount -= bodySize;

                        // padding
                        if (_asymIsEncrypted)
                        {
                            byte paddingByte = (byte)(paddingSize & 0xFF);
                            encoder.WriteByte(null, paddingByte);
                            for (int i = 0; i < paddingSize; i++)
                            {
                                encoder.WriteByte(null, paddingByte);
                            }

                            if (paddingHeaderSize == 2)
                            {
                                byte extraPaddingByte = (byte)((paddingSize >> 8) & 0xFF);
                                encoder.WriteByte(null, extraPaddingByte);
                            }
                        }

                        // update message type and (encrypted) length
                        var position = encoder.Position;
                        encoder.Position = 3;
                        encoder.WriteByte(null, bodyCount > 0 ? (byte)'C' : (byte)'F');
                        encoder.WriteUInt32(null, (uint)chunkSize);
                        encoder.Position = position;

                        // sign
                        if (_asymIsSigned)
                        {
                            // sign with local private key.
                            _asymSigner!.BlockUpdate(_sendBuffer, 0, position);
                            byte[] signature = _asymSigner.GenerateSignature();
                            Debug.Assert(signature.Length == _asymLocalSignatureSize, nameof(_asymLocalSignatureSize));
                            encoder.Write(signature, 0, _asymLocalSignatureSize);
                        }

                        // encrypt
                        if (_asymIsEncrypted)
                        {
                            position = encoder.Position;
                            Buffer.BlockCopy(_sendBuffer!, 0, _encryptionBuffer!, 0, plainHeaderSize);
                            byte[] plainText = new byte[_asymRemotePlainTextBlockSize];
                            int jj = plainHeaderSize;
                            for (int ii = plainHeaderSize; ii < position; ii += _asymRemotePlainTextBlockSize)
                            {
                                Buffer.BlockCopy(_sendBuffer!, ii, plainText, 0, _asymRemotePlainTextBlockSize);

                                // encrypt with remote public key.
                                byte[] cipherText = _asymEncryptor!.DoFinal(plainText);
                                Debug.Assert(cipherText.Length == _asymRemoteCipherTextBlockSize, nameof(_asymRemoteCipherTextBlockSize));
                                Buffer.BlockCopy(cipherText, 0, _encryptionBuffer!, jj, _asymRemoteCipherTextBlockSize);
                                jj += _asymRemoteCipherTextBlockSize;
                            }

                            await SendAsync(_encryptionBuffer!, 0, jj, token).ConfigureAwait(false);
                            return;
                        }

                        // pass buffer to transport
                        await SendAsync(_sendBuffer!, 0, encoder.Position, token).ConfigureAwait(false);
                    }
                    finally
                    {
                        encoder.Dispose();
                    }
                }
            }
            finally
            {
                bodyEncoder.Dispose(); // also disposes stream.
            }
        }

        /// <summary>
        /// Send close secure channel request on transport channel.
        /// </summary>
        /// <param name="request">A service request</param>
        /// <param name="token">A cancellation token</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task SendCloseSecureChannelRequestAsync(CloseSecureChannelRequest request, CancellationToken token)
        {
            var bodyStream = _streamManager.GetStream("SendCloseSecureChannelRequestAsync");
            var bodyEncoder = new BinaryEncoder(bodyStream, this);
            try
            {
                bodyEncoder.WriteRequest(request);
                bodyStream.Position = 0;
                if (RemoteMaxMessageSize > 0 && bodyStream.Length > RemoteMaxMessageSize)
                {
                    throw new ServiceResultException(StatusCodes.BadEncodingLimitsExceeded);
                }

                // write chunks
                int chunkCount = 0;
                int bodyCount = (int)(bodyStream.Length - bodyStream.Position);
                while (bodyCount > 0)
                {
                    chunkCount++;
                    if (RemoteMaxChunkCount > 0 && chunkCount > RemoteMaxChunkCount)
                    {
                        throw new ServiceResultException(StatusCodes.BadEncodingLimitsExceeded);
                    }

                    var stream = new MemoryStream(_sendBuffer!, 0, (int)RemoteReceiveBufferSize, true, true);
                    var encoder = new BinaryEncoder(stream, this);
                    try
                    {
                        // header
                        encoder.WriteUInt32(null, UaTcpMessageTypes.CLOF);
                        encoder.WriteUInt32(null, 0u);
                        encoder.WriteUInt32(null, ChannelId);

                        // symmetric security header
                        encoder.WriteUInt32(null, TokenId);

                        // detect new TokenId
                        if (TokenId != _currentClientTokenId)
                        {
                            _currentClientTokenId = TokenId;

                            // update signer and encrypter with new symmetric keys
                            if (_symIsSigned)
                            {
                                _symSigner!.Init(new KeyParameter(_clientSigningKey));
                                if (_symIsEncrypted)
                                {
                                    _symEncryptor!.Init(
                                        true,
                                        new ParametersWithIV(new KeyParameter(_clientEncryptingKey), _clientInitializationVector));
                                }
                            }
                        }

                        int plainHeaderSize = encoder.Position;

                        // sequence header
                        encoder.WriteUInt32(null, GetNextSequenceNumber());
                        encoder.WriteUInt32(null, request.RequestHeader!.RequestHandle);

                        // body
                        int paddingHeaderSize;
                        int maxBodySize;
                        int bodySize;
                        int paddingSize;
                        int chunkSize;
                        if (_symIsEncrypted)
                        {
                            paddingHeaderSize = _symEncryptionBlockSize > 256 ? 2 : 1;
                            maxBodySize = ((((int)RemoteReceiveBufferSize - plainHeaderSize) / _symEncryptionBlockSize) * _symEncryptionBlockSize) - _sequenceHeaderSize - paddingHeaderSize - _symSignatureSize;
                            if (bodyCount < maxBodySize)
                            {
                                bodySize = bodyCount;
                                paddingSize = (_symEncryptionBlockSize - ((_sequenceHeaderSize + bodySize + paddingHeaderSize + _symSignatureSize) % _symEncryptionBlockSize)) % _symEncryptionBlockSize;
                            }
                            else
                            {
                                bodySize = maxBodySize;
                                paddingSize = 0;
                            }

                            chunkSize = plainHeaderSize + _sequenceHeaderSize + bodySize + paddingSize + paddingHeaderSize + _symSignatureSize;
                        }
                        else
                        {
                            paddingHeaderSize = 0;
                            paddingSize = 0;
                            maxBodySize = (int)RemoteReceiveBufferSize - plainHeaderSize - _sequenceHeaderSize - _symSignatureSize;
                            if (bodyCount < maxBodySize)
                            {
                                bodySize = bodyCount;
                            }
                            else
                            {
                                bodySize = maxBodySize;
                            }

                            chunkSize = plainHeaderSize + _sequenceHeaderSize + bodySize + _symSignatureSize;
                        }

                        bodyStream.Read(_sendBuffer!, encoder.Position, bodySize);
                        encoder.Position += bodySize;
                        bodyCount -= bodySize;

                        // padding
                        if (_symIsEncrypted)
                        {
                            var paddingByte = (byte)(paddingSize & 0xFF);
                            encoder.WriteByte(null, paddingByte);
                            for (int i = 0; i < paddingSize; i++)
                            {
                                encoder.WriteByte(null, paddingByte);
                            }

                            if (paddingHeaderSize == 2)
                            {
                                var extraPaddingByte = (byte)((paddingSize >> 8) & 0xFF);
                                encoder.WriteByte(null, extraPaddingByte);
                            }
                        }

                        // update message type and (encrypted) length
                        var position = encoder.Position;
                        encoder.Position = 3;
                        encoder.WriteByte(null, bodyCount > 0 ? (byte)'C' : (byte)'F');
                        encoder.WriteUInt32(null, (uint)chunkSize);
                        encoder.Position = position;

                        // signature
                        if (_symIsSigned)
                        {
                            _symSigner!.BlockUpdate(_sendBuffer, 0, position);
                            byte[] signature = new byte[_symSigner.GetMacSize()];
                            _symSigner.DoFinal(signature, 0);
                            encoder.Write(signature, 0, signature.Length);
                        }

                        // encrypt
                        position = encoder.Position;
                        if (_symIsEncrypted)
                        {
                            int inputCount = position - plainHeaderSize;
                            Debug.Assert(inputCount % _symEncryptor!.GetBlockSize() == 0, "Input data is not an even number of encryption blocks.");
                            _symEncryptor.DoFinal(_sendBuffer, plainHeaderSize, inputCount, _sendBuffer, plainHeaderSize);
                        }

                        // pass buffer to transport
                        await SendAsync(_sendBuffer!, 0, position, token).ConfigureAwait(false);
                    }
                    finally
                    {
                        encoder.Dispose();
                    }
                }
            }
            finally
            {
                bodyEncoder.Dispose(); // also disposes stream.
            }
        }

        /// <summary>
        /// Send service request on transport channel.
        /// </summary>
        /// <param name="request">A service request</param>
        /// <param name="token">A cancellation token</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task SendServiceRequestAsync(IServiceRequest request, CancellationToken token)
        {
            var bodyStream = _streamManager.GetStream("SendServiceRequestAsync");
            var bodyEncoder = new BinaryEncoder(bodyStream, this);
            try
            {
                bodyEncoder.WriteRequest(request);
                bodyStream.Position = 0;
                if (RemoteMaxMessageSize > 0 && bodyStream.Length > RemoteMaxMessageSize)
                {
                    throw new ServiceResultException(StatusCodes.BadEncodingLimitsExceeded);
                }

                // write chunks
                int chunkCount = 0;
                int bodyCount = (int)(bodyStream.Length - bodyStream.Position);
                while (bodyCount > 0)
                {
                    chunkCount++;
                    if (RemoteMaxChunkCount > 0 && chunkCount > RemoteMaxChunkCount)
                    {
                        throw new ServiceResultException(StatusCodes.BadEncodingLimitsExceeded);
                    }

                    var stream = new MemoryStream(_sendBuffer!, 0, (int)RemoteReceiveBufferSize, true, true);
                    var encoder = new BinaryEncoder(stream, this);
                    try
                    {
                        // header
                        encoder.WriteUInt32(null, UaTcpMessageTypes.MSGF);
                        encoder.WriteUInt32(null, 0u);
                        encoder.WriteUInt32(null, ChannelId);

                        // symmetric security header
                        encoder.WriteUInt32(null, TokenId);

                        // detect new TokenId
                        if (TokenId != _currentClientTokenId)
                        {
                            _currentClientTokenId = TokenId;

                            // update signer and encrypter with new symmetric keys
                            if (_symIsSigned)
                            {
                                _symSigner!.Init(new KeyParameter(_clientSigningKey));
                                if (_symIsEncrypted)
                                {
                                    _symEncryptor!.Init(
                                        true,
                                        new ParametersWithIV(new KeyParameter(_clientEncryptingKey), _clientInitializationVector));
                                }
                            }
                        }

                        int plainHeaderSize = encoder.Position;

                        // sequence header
                        encoder.WriteUInt32(null, GetNextSequenceNumber());
                        encoder.WriteUInt32(null, request.RequestHeader!.RequestHandle);

                        // body
                        int paddingHeaderSize;
                        int maxBodySize;
                        int bodySize;
                        int paddingSize;
                        int chunkSize;
                        if (_symIsEncrypted)
                        {
                            paddingHeaderSize = _symEncryptionBlockSize > 256 ? 2 : 1;
                            maxBodySize = ((((int)RemoteReceiveBufferSize - plainHeaderSize) / _symEncryptionBlockSize) * _symEncryptionBlockSize) - _sequenceHeaderSize - paddingHeaderSize - _symSignatureSize;
                            if (bodyCount < maxBodySize)
                            {
                                bodySize = bodyCount;
                                paddingSize = (_symEncryptionBlockSize - ((_sequenceHeaderSize + bodySize + paddingHeaderSize + _symSignatureSize) % _symEncryptionBlockSize)) % _symEncryptionBlockSize;
                            }
                            else
                            {
                                bodySize = maxBodySize;
                                paddingSize = 0;
                            }

                            chunkSize = plainHeaderSize + _sequenceHeaderSize + bodySize + paddingSize + paddingHeaderSize + _symSignatureSize;
                        }
                        else
                        {
                            paddingHeaderSize = 0;
                            paddingSize = 0;
                            maxBodySize = (int)RemoteReceiveBufferSize - plainHeaderSize - _sequenceHeaderSize - _symSignatureSize;
                            if (bodyCount < maxBodySize)
                            {
                                bodySize = bodyCount;
                            }
                            else
                            {
                                bodySize = maxBodySize;
                            }

                            chunkSize = plainHeaderSize + _sequenceHeaderSize + bodySize + _symSignatureSize;
                        }

                        bodyStream.Read(_sendBuffer!, encoder.Position, bodySize);
                        encoder.Position += bodySize;
                        bodyCount -= bodySize;

                        // padding
                        if (_symIsEncrypted)
                        {
                            var paddingByte = (byte)(paddingSize & 0xFF);
                            encoder.WriteByte(null, paddingByte);
                            for (int i = 0; i < paddingSize; i++)
                            {
                                encoder.WriteByte(null, paddingByte);
                            }

                            if (paddingHeaderSize == 2)
                            {
                                var extraPaddingByte = (byte)((paddingSize >> 8) & 0xFF);
                                encoder.WriteByte(null, extraPaddingByte);
                            }
                        }

                        // update message type and (encrypted) length
                        var position = encoder.Position;
                        encoder.Position = 3;
                        encoder.WriteByte(null, bodyCount > 0 ? (byte)'C' : (byte)'F');
                        encoder.WriteUInt32(null, (uint)chunkSize);
                        encoder.Position = position;

                        // signature
                        if (_symIsSigned)
                        {
                            _symSigner!.BlockUpdate(_sendBuffer, 0, position);
                            byte[] signature = new byte[_symSigner.GetMacSize()];
                            _symSigner.DoFinal(signature, 0);
                            encoder.Write(signature, 0, signature.Length);
                        }

                        // encrypt
                        position = encoder.Position;
                        if (_symIsEncrypted)
                        {
                            int inputCount = position - plainHeaderSize;
                            Debug.Assert(inputCount % _symEncryptor!.GetBlockSize() == 0, "Input data is not an even number of encryption blocks.");
                            _symEncryptor.DoFinal(_sendBuffer, plainHeaderSize, inputCount, _sendBuffer, plainHeaderSize);
                        }

                        // pass buffer to transport
                        await SendAsync(_sendBuffer!, 0, position, token).ConfigureAwait(false);
                    }
                    finally
                    {
                        encoder.Dispose();
                    }
                }
            }
            finally
            {
                bodyEncoder.Dispose(); // also disposes stream.
            }
        }

        /// <summary>
        /// Start a task to receive service responses from transport channel.
        /// </summary>
        /// <param name="token">A cancellation token</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task ReceiveResponsesAsync(CancellationToken token = default)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var response = await ReceiveResponseAsync().ConfigureAwait(false);
                    if (response == null)
                    {
                        // Null response indicates socket closed. This is expected when closing secure channel.
                        _channelCts.Cancel();
                        if (State == CommunicationState.Closed || State == CommunicationState.Closing)
                        {
                            return;
                        }

                        throw new ServiceResultException(StatusCodes.BadServerNotConnected);
                    }

                    var header = response.ResponseHeader!;
                    if (_pendingCompletions.TryRemove(header.RequestHandle, out var tcs))
                    {
                        if (StatusCode.IsBad(header.ServiceResult))
                        {
                            var ex = new ServiceResultException(new ServiceResult(header.ServiceResult, header.ServiceDiagnostics, header.StringTable));
                            tcs.TrySetException(ex);
                        }
                        else
                        {
                            tcs.TrySetResult(response);
                        }

                        continue;
                    }

                    // TODO: remove when open62541 server corrected.
                    if (header.RequestHandle == 0)
                    {
                        ServiceOperation? tcs2 = null;
                        if (response is OpenSecureChannelResponse)
                        {
                            tcs2 = _pendingCompletions.OrderBy(k => k.Key).Select(k => k.Value).FirstOrDefault(o => o.Request is OpenSecureChannelRequest);
                        }
                        else if (response is CloseSecureChannelResponse)
                        {
                            tcs2 = _pendingCompletions.OrderBy(k => k.Key).Select(k => k.Value).FirstOrDefault(o => o.Request is CloseSecureChannelRequest);
                        }

                        if (tcs2 != null)
                        {
                            _pendingCompletions.TryRemove(tcs2.Request.RequestHeader!.RequestHandle, out _);
                            if (StatusCode.IsBad(header.ServiceResult))
                            {
                                var ex = new ServiceResultException(new ServiceResult(header.ServiceResult, header.ServiceDiagnostics, header.StringTable));
                                tcs2.TrySetException(ex);
                            }
                            else
                            {
                                tcs2.TrySetResult(response);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error receiving response. {ex.Message}");
                await FaultAsync(ex).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Receive next service response from transport channel.
        /// </summary>
        /// <param name="token">A cancellation token</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task<IServiceResponse?> ReceiveResponseAsync(CancellationToken token = default)
        {
            await _receivingSemaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                ThrowIfClosedOrNotOpening();
                uint sequenceNum;
                uint requestId;
                int paddingHeaderSize;
                int plainHeaderSize;
                int bodySize;
                int paddingSize;

                var bodyStream = _streamManager.GetStream("ReceiveResponseAsync");
                var bodyDecoder = new BinaryDecoder(bodyStream, this);
                try
                {
                    // read chunks
                    int chunkCount = 0;
                    bool isFinal = false;
                    do
                    {
                        chunkCount++;
                        if (LocalMaxChunkCount > 0 && chunkCount > LocalMaxChunkCount)
                        {
                            throw new ServiceResultException(StatusCodes.BadEncodingLimitsExceeded);
                        }

                        var count = await ReceiveAsync(_receiveBuffer!, 0, (int)LocalReceiveBufferSize, token).ConfigureAwait(false);
                        if (count == 0)
                        {
                            return null;
                        }

                        var stream = new MemoryStream(_receiveBuffer!, 0, count, true, true);
                        var decoder = new BinaryDecoder(stream, this);
                        try
                        {
                            uint channelId;
                            uint messageType = decoder.ReadUInt32(null);
                            int messageLength = (int)decoder.ReadUInt32(null);
                            Debug.Assert(count == messageLength, "Bytes received not equal to encoded Message length");
                            switch (messageType)
                            {
                                case UaTcpMessageTypes.MSGF:
                                case UaTcpMessageTypes.MSGC:
                                    // header
                                    channelId = decoder.ReadUInt32(null);
                                    if (channelId != ChannelId)
                                    {
                                        throw new ServiceResultException(StatusCodes.BadTcpSecureChannelUnknown);
                                    }

                                    // symmetric security header
                                    var tokenId = decoder.ReadUInt32(null);

                                    // detect new token
                                    if (tokenId != _currentServerTokenId)
                                    {
                                        _currentServerTokenId = tokenId;

                                        // update with new keys
                                        if (_symIsSigned)
                                        {
                                            _symVerifier!.Init(new KeyParameter(_serverSigningKey));
                                            if (_symIsEncrypted)
                                            {
                                                _symDecryptor!.Init(
                                                    false,
                                                    new ParametersWithIV(new KeyParameter(_serverEncryptingKey), _serverInitializationVector));
                                            }
                                        }

                                        _logger?.LogTrace($"Installed new security token {tokenId}.");
                                    }

                                    plainHeaderSize = decoder.Position;

                                    // decrypt
                                    if (_symIsEncrypted)
                                    {
                                        int inputCount = messageLength - plainHeaderSize;
                                        Debug.Assert(inputCount % _symDecryptor!.GetBlockSize() == 0, "Input data is not an even number of encryption blocks.");
                                        _symDecryptor.DoFinal(_receiveBuffer, plainHeaderSize, inputCount, _receiveBuffer, plainHeaderSize);
                                    }

                                    // verify
                                    if (_symIsSigned)
                                    {
                                        var datalen = messageLength - _symSignatureSize;

                                        _symVerifier!.BlockUpdate(_receiveBuffer, 0, datalen);
                                        byte[] signature = new byte[_symVerifier.GetMacSize()];
                                        _symVerifier.DoFinal(signature, 0);

                                        if (!signature.SequenceEqual(_receiveBuffer!.AsArraySegment(datalen, _symSignatureSize)))
                                        {
                                            throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed);
                                        }
                                    }

                                    // read sequence header
                                    sequenceNum = decoder.ReadUInt32(null);
                                    requestId = decoder.ReadUInt32(null);

                                    // body
                                    if (_symIsEncrypted)
                                    {
                                        if (_symEncryptionBlockSize > 256)
                                        {
                                            paddingHeaderSize = 2;
                                            paddingSize = BitConverter.ToInt16(_receiveBuffer!, messageLength - _symSignatureSize - paddingHeaderSize);
                                        }
                                        else
                                        {
                                            paddingHeaderSize = 1;
                                            paddingSize = _receiveBuffer![messageLength - _symSignatureSize - paddingHeaderSize];
                                        }

                                        bodySize = messageLength - plainHeaderSize - _sequenceHeaderSize - paddingSize - paddingHeaderSize - _symSignatureSize;
                                    }
                                    else
                                    {
                                        bodySize = messageLength - plainHeaderSize - _sequenceHeaderSize - _symSignatureSize;
                                    }

                                    bodyStream.Write(_receiveBuffer!, plainHeaderSize + _sequenceHeaderSize, bodySize);
                                    isFinal = messageType == UaTcpMessageTypes.MSGF;
                                    break;

                                case UaTcpMessageTypes.OPNF:
                                    // header
                                    channelId = decoder.ReadUInt32(null);

                                    // asymmetric header
                                    var securityPolicyUri = decoder.ReadString(null);
                                    var serverCertificateByteString = decoder.ReadByteString(null);
                                    var clientThumbprint = decoder.ReadByteString(null);
                                    plainHeaderSize = decoder.Position;

                                    // decrypt
                                    if (_asymIsEncrypted)
                                    {
                                        byte[] cipherTextBlock = new byte[_asymLocalCipherTextBlockSize];
                                        int jj = plainHeaderSize;
                                        for (int ii = plainHeaderSize; ii < messageLength; ii += _asymLocalCipherTextBlockSize)
                                        {
                                            Buffer.BlockCopy(_receiveBuffer!, ii, cipherTextBlock, 0, _asymLocalCipherTextBlockSize);

                                            // decrypt with local private key.
                                            byte[] plainTextBlock = _asymDecryptor!.DoFinal(cipherTextBlock);
                                            Debug.Assert(plainTextBlock.Length == _asymLocalPlainTextBlockSize, "Decrypted block length was not as expected.");
                                            Buffer.BlockCopy(plainTextBlock, 0, _receiveBuffer!, jj, _asymLocalPlainTextBlockSize);
                                            jj += _asymLocalPlainTextBlockSize;
                                        }

                                        messageLength = jj;
                                        decoder.Position = plainHeaderSize;
                                    }

                                    // verify
                                    if (_asymIsSigned)
                                    {
                                        // verify with remote public key.
                                        var datalen = messageLength - _asymRemoteSignatureSize;
                                        _asymVerifier!.BlockUpdate(_receiveBuffer, 0, datalen);
                                        if (!_asymVerifier.VerifySignature(_receiveBuffer!.AsArraySegment(datalen, _asymRemoteSignatureSize).ToArray()))
                                        {
                                            throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed);
                                        }
                                    }

                                    // sequence header
                                    sequenceNum = decoder.ReadUInt32(null);
                                    requestId = decoder.ReadUInt32(null);

                                    // body
                                    if (_asymIsEncrypted)
                                    {
                                        if (_asymLocalCipherTextBlockSize > 256)
                                        {
                                            paddingHeaderSize = 2;
                                            paddingSize = BitConverter.ToInt16(_receiveBuffer!, messageLength - _asymRemoteSignatureSize - paddingHeaderSize);
                                        }
                                        else
                                        {
                                            paddingHeaderSize = 1;
                                            paddingSize = _receiveBuffer![messageLength - _asymRemoteSignatureSize - paddingHeaderSize];
                                        }

                                        bodySize = messageLength - plainHeaderSize - _sequenceHeaderSize - paddingSize - paddingHeaderSize - _asymRemoteSignatureSize;
                                    }
                                    else
                                    {
                                        bodySize = messageLength - plainHeaderSize - _sequenceHeaderSize - _asymRemoteSignatureSize;
                                    }

                                    bodyStream.Write(_receiveBuffer!, plainHeaderSize + _sequenceHeaderSize, bodySize);
                                    isFinal = messageType == UaTcpMessageTypes.OPNF;
                                    break;

                                case UaTcpMessageTypes.ERRF:
                                case UaTcpMessageTypes.MSGA:
                                    var statusCode = (StatusCode)decoder.ReadUInt32(null);
                                    var message = decoder.ReadString(null);
                                    if (message != null)
                                    {
                                        throw new ServiceResultException(statusCode, message);
                                    }

                                    throw new ServiceResultException(statusCode);

                                default:
                                    throw new ServiceResultException(StatusCodes.BadUnknownResponse);
                            }

                            if (LocalMaxMessageSize > 0 && bodyStream.Position > LocalMaxMessageSize)
                            {
                                throw new ServiceResultException(StatusCodes.BadEncodingLimitsExceeded);
                            }
                        }
                        finally
                        {
                            decoder.Dispose(); // also disposes stream.
                        }
                    }
                    while (!isFinal);
                    bodyStream.Seek(0L, SeekOrigin.Begin);
                    var response = (IServiceResponse)bodyDecoder.ReadResponse();

                    _logger?.LogTrace($"Received {response.GetType().Name}, Handle: {response.ResponseHeader!.RequestHandle} Result: {response.ResponseHeader.ServiceResult}");

                    // special inline processing for token renewal because we need to
                    // hold both the sending and receiving semaphores to update the security keys.
                    var openSecureChannelResponse = response as OpenSecureChannelResponse;
                    if (openSecureChannelResponse != null && StatusCode.IsGood(openSecureChannelResponse.ResponseHeader!.ServiceResult))
                    {
                        _tokenRenewalTime = DateTime.UtcNow.AddMilliseconds(0.8 * openSecureChannelResponse.SecurityToken!.RevisedLifetime);

                        await _sendingSemaphore.WaitAsync(token).ConfigureAwait(false);
                        try
                        {
                            ChannelId = openSecureChannelResponse.SecurityToken.ChannelId;
                            TokenId = openSecureChannelResponse.SecurityToken.TokenId;
                            if (_symIsSigned)
                            {
                                var clientNonce = LocalNonce!;
                                var serverNonce = openSecureChannelResponse.ServerNonce!;

                                // (re)create client security keys for encrypting the next message sent
                                var clientSecurityKey = CalculatePSHA(serverNonce, clientNonce, _symSignatureKeySize + _symEncryptionKeySize + _symEncryptionBlockSize, RemoteEndpoint.SecurityPolicyUri!);
                                Buffer.BlockCopy(clientSecurityKey, 0, _clientSigningKey!, 0, _symSignatureKeySize);
                                Buffer.BlockCopy(clientSecurityKey, _symSignatureKeySize, _clientEncryptingKey!, 0, _symEncryptionKeySize);
                                Buffer.BlockCopy(clientSecurityKey, _symSignatureKeySize + _symEncryptionKeySize, _clientInitializationVector!, 0, _symEncryptionBlockSize);

                                // (re)create server security keys for decrypting the next message received that has a new TokenId
                                var serverSecurityKey = CalculatePSHA(clientNonce, serverNonce, _symSignatureKeySize + _symEncryptionKeySize + _symEncryptionBlockSize, RemoteEndpoint.SecurityPolicyUri!);
                                Buffer.BlockCopy(serverSecurityKey, 0, _serverSigningKey!, 0, _symSignatureKeySize);
                                Buffer.BlockCopy(serverSecurityKey, _symSignatureKeySize, _serverEncryptingKey!, 0, _symEncryptionKeySize);
                                Buffer.BlockCopy(serverSecurityKey, _symSignatureKeySize + _symEncryptionKeySize, _serverInitializationVector!, 0, _symEncryptionBlockSize);
                            }
                        }
                        finally
                        {
                            _sendingSemaphore.Release();
                        }
                    }

                    return response;
                }
                finally
                {
                    bodyDecoder.Dispose();
                }
            }
            finally
            {
                _receivingSemaphore.Release();
            }
        }

        /// <summary>
        /// Update request header with current time.
        /// </summary>
        /// <param name="request">The service request.</param>
        private void TimestampHeader(IServiceRequest request)
        {
            if (request.RequestHeader == null)
            {
                request.RequestHeader = new RequestHeader { TimeoutHint = TimeoutHint, ReturnDiagnostics = DiagnosticsHint, Timestamp = DateTime.UtcNow };
                return;
            }

            request.RequestHeader.Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Get next request handle.
        /// </summary>
        /// <returns>A request handle.</returns>
        private uint GetNextHandle()
        {
            unchecked
            {
                int snapshot = _handle;
                int value = snapshot + 1;
                if (value == 0)
                {
                    value = 1;
                }

                if (Interlocked.CompareExchange(ref _handle, value, snapshot) != snapshot)
                {
                    var spinner = default(SpinWait);
                    do
                    {
                        spinner.SpinOnce();
                        snapshot = _handle;
                        value = snapshot + 1;
                        if (value == 0)
                        {
                            value = 1;
                        }
                    }
                    while (Interlocked.CompareExchange(ref _handle, value, snapshot) != snapshot);
                }

                return (uint)value;
            }
        }

        /// <summary>
        /// Get next sequence number for chunk.
        /// </summary>
        /// <returns>The next sequence number.</returns>
        private uint GetNextSequenceNumber()
        {
            unchecked
            {
                return (uint)Interlocked.Increment(ref _sequenceNumber);
            }
        }

        /// <summary>
        /// Get next random nonce of requested length.
        /// </summary>
        /// <param name="length">The requested length.</param>
        /// <returns>An nonce of requested length.</returns>
        protected byte[] GetNextNonce(int length)
        {
            var nonce = new byte[length];
            _rng.NextBytes(nonce);
            return nonce;
        }
    }
}