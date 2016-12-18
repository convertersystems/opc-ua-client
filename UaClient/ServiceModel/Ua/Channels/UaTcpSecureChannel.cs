// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Misc;
using Org.BouncyCastle.Asn1.Utilities;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Security.Certificates;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.Utilities.Encoders;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;

namespace Workstation.ServiceModel.Ua.Channels
{

    /// <summary>
    /// A channel that opens a secure channel.
    /// </summary>
    public class UaTcpSecureChannel : UaTcpTransportChannel, IRequestChannel, ITargetBlock<ServiceOperation>
    {
        public const uint DefaultTimeoutHint = 15 * 1000; // 15 seconds
        public const uint DefaultDiagnosticsHint = (uint)DiagnosticFlags.None;
        private const int SequenceHeaderSize = 8;
        private const int TokenRequestedLifetime = 60 * 60 * 1000; // 1 hour

        private static readonly NodeId OpenSecureChannelRequestNodeId = NodeId.Parse(ObjectIds.OpenSecureChannelRequest_Encoding_DefaultBinary);
        private static readonly NodeId CloseSecureChannelRequestNodeId = NodeId.Parse(ObjectIds.CloseSecureChannelRequest_Encoding_DefaultBinary);
        private static readonly NodeId ReadResponseNodeId = NodeId.Parse(ObjectIds.ReadResponse_Encoding_DefaultBinary);
        private static readonly NodeId PublishResponseNodeId = NodeId.Parse(ObjectIds.PublishResponse_Encoding_DefaultBinary);
        private static readonly SecureRandom Rng = new SecureRandom();

        private readonly CancellationTokenSource channelCts;
        private readonly SemaphoreSlim sendingSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim receivingSemaphore = new SemaphoreSlim(1, 1);
        internal readonly ActionBlock<ServiceOperation> pendingRequests;
        private readonly ConcurrentDictionary<uint, ServiceOperation> pendingCompletions;
        private readonly X509CertificateParser certificateParser = new X509CertificateParser();

        private int handle;
        private int sequenceNumber;
        private uint currentClientTokenId;
        private uint currentServerTokenId;
        private byte[] clientSigningKey;
        private byte[] clientEncryptingKey;
        private byte[] clientInitializationVector;
        private byte[] serverSigningKey;
        private byte[] serverEncryptingKey;
        private byte[] serverInitializationVector;
        private byte[] encryptionBuffer;
        private Task receiveResponsesTask;
        private int asymLocalKeySize;
        private int asymRemoteKeySize;
        private int asymLocalPlainTextBlockSize;
        private int asymLocalCipherTextBlockSize;
        private int asymLocalSignatureSize;
        private int asymRemotePlainTextBlockSize;
        private int asymRemoteCipherTextBlockSize;
        private int asymRemoteSignatureSize;
        private bool asymIsSigned;
        private bool asymIsEncrypted;
        private int symEncryptionBlockSize;
        private int symEncryptionKeySize;
        private int symSignatureSize;
        private bool symIsSigned;
        private bool symIsEncrypted;
        private int symSignatureKeySize;
        private byte[] sendBuffer;
        private byte[] receiveBuffer;

        private IBufferedCipher asymEncryptor;
        private IBufferedCipher asymDecryptor;
        private IBufferedCipher symEncryptor;
        private IBufferedCipher symDecryptor;
        private ISigner asymSigner;
        private ISigner asymVerifier;
        private IMac symSigner;
        private IMac symVerifier;
        private DateTime tokenRenewalTime = DateTime.MaxValue;
        private IDigest thumbprintDigest;

        static UaTcpSecureChannel()
        {
            BinaryEncodingIdToTypeDictionary = new Dictionary<ExpandedNodeId, Type>();
            TypeToBinaryEncodingIdDictionary = new Dictionary<Type, ExpandedNodeId>();
            DataTypeIdToTypeDictionary = new Dictionary<ExpandedNodeId, Type>()
            {
                [ExpandedNodeId.Parse(DataTypeIds.Boolean)] = typeof(bool),
                [ExpandedNodeId.Parse(DataTypeIds.SByte)] = typeof(sbyte),
                [ExpandedNodeId.Parse(DataTypeIds.Byte)] = typeof(byte),
                [ExpandedNodeId.Parse(DataTypeIds.Int16)] = typeof(short),
                [ExpandedNodeId.Parse(DataTypeIds.UInt16)] = typeof(ushort),
                [ExpandedNodeId.Parse(DataTypeIds.Int32)] = typeof(int),
                [ExpandedNodeId.Parse(DataTypeIds.UInt32)] = typeof(uint),
                [ExpandedNodeId.Parse(DataTypeIds.Int64)] = typeof(long),
                [ExpandedNodeId.Parse(DataTypeIds.UInt64)] = typeof(ulong),
                [ExpandedNodeId.Parse(DataTypeIds.Float)] = typeof(float),
                [ExpandedNodeId.Parse(DataTypeIds.Double)] = typeof(double),
                [ExpandedNodeId.Parse(DataTypeIds.String)] = typeof(string),
                [ExpandedNodeId.Parse(DataTypeIds.DateTime)] = typeof(DateTime),
                [ExpandedNodeId.Parse(DataTypeIds.Guid)] = typeof(Guid),
                [ExpandedNodeId.Parse(DataTypeIds.ByteString)] = typeof(byte[]),
                [ExpandedNodeId.Parse(DataTypeIds.XmlElement)] = typeof(XElement),
                [ExpandedNodeId.Parse(DataTypeIds.NodeId)] = typeof(NodeId),
                [ExpandedNodeId.Parse(DataTypeIds.ExpandedNodeId)] = typeof(ExpandedNodeId),
                [ExpandedNodeId.Parse(DataTypeIds.StatusCode)] = typeof(StatusCode),
                [ExpandedNodeId.Parse(DataTypeIds.QualifiedName)] = typeof(QualifiedName),
                [ExpandedNodeId.Parse(DataTypeIds.LocalizedText)] = typeof(LocalizedText),
                [ExpandedNodeId.Parse(DataTypeIds.Enumeration)] = typeof(int),
                [ExpandedNodeId.Parse(DataTypeIds.UtcTime)] = typeof(DateTime),
            };
            RegisterEncodables(typeof(OpenSecureChannelRequest).GetTypeInfo().Assembly);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UaTcpSecureChannel"/> class.
        /// </summary>
        /// <param name="localDescription">The local description.</param>
        /// <param name="certificateStore">The local certificate store.</param>
        /// <param name="remoteEndpoint">The remote endpoint</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="timeoutHint">The default number of milliseconds that may elapse before an operation is cancelled by the service.</param>
        /// <param name="diagnosticsHint">The default diagnostics flags to be requested by the service.</param>
        /// <param name="localReceiveBufferSize">The size of the receive buffer.</param>
        /// <param name="localSendBufferSize">The size of the send buffer.</param>
        /// <param name="localMaxMessageSize">The maximum total size of a message.</param>
        /// <param name="localMaxChunkCount">The maximum number of message chunks.</param>
        public UaTcpSecureChannel(
            ApplicationDescription localDescription,
            ICertificateStore certificateStore,
            EndpointDescription remoteEndpoint,
            ILoggerFactory loggerFactory = null,
            uint timeoutHint = DefaultTimeoutHint,
            uint diagnosticsHint = DefaultDiagnosticsHint,
            uint localReceiveBufferSize = DefaultBufferSize,
            uint localSendBufferSize = DefaultBufferSize,
            uint localMaxMessageSize = DefaultMaxMessageSize,
            uint localMaxChunkCount = DefaultMaxChunkCount)
            : base(remoteEndpoint, loggerFactory, localReceiveBufferSize, localSendBufferSize, localMaxMessageSize, localMaxChunkCount)
        {
            LocalDescription = localDescription ?? throw new ArgumentNullException(nameof(localDescription));
            CertificateStore = certificateStore;
            RemoteCertificate = RemoteEndpoint.ServerCertificate;
            TimeoutHint = timeoutHint;
            DiagnosticsHint = diagnosticsHint;
            AuthenticationToken = null;
            NamespaceUris = new List<string> { "http://opcfoundation.org/UA/" };
            ServerUris = new List<string>();
            channelCts = new CancellationTokenSource();
            pendingRequests = new ActionBlock<ServiceOperation>(t => SendRequestActionAsync(t), new ExecutionDataflowBlockOptions { CancellationToken = channelCts.Token });
            pendingCompletions = new ConcurrentDictionary<uint, ServiceOperation>();
        }

        public static Dictionary<ExpandedNodeId, Type> BinaryEncodingIdToTypeDictionary { get; }

        public static Dictionary<Type, ExpandedNodeId> TypeToBinaryEncodingIdDictionary { get; }

        public static Dictionary<ExpandedNodeId, Type> DataTypeIdToTypeDictionary { get; }

        public ApplicationDescription LocalDescription { get; }

        public ICertificateStore CertificateStore { get; }

        protected byte[] LocalCertificate { get; set; }

        protected byte[] RemoteCertificate { get; set; }

        protected RsaKeyParameters LocalPrivateKey { get; set; }

        protected RsaKeyParameters RemotePublicKey { get; set; }

        protected byte[] LocalNonce { get; private set; }

        public uint TimeoutHint { get; }

        public uint DiagnosticsHint { get; }

        public uint ChannelId { get; protected set; }

        public uint TokenId { get; protected set; }

        public NodeId AuthenticationToken { get; protected set; }

        public List<string> NamespaceUris { get; protected set; }

        public List<string> ServerUris { get; protected set; }

        public Task Completion => pendingRequests.Completion;

        public static void RegisterEncodables(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            var types = assembly.ExportedTypes.Where(t => t.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IEncodable))).ToArray();
            foreach (var type in types)
            {
                RegisterEncodable(type);
            }
        }

        public static void RegisterEncodable(Type type)
        {
            var attr = type.GetTypeInfo().GetCustomAttribute<BinaryEncodingIdAttribute>(false);
            if (attr != null)
            {
                var binaryEncodingId = attr.NodeId;
                BinaryEncodingIdToTypeDictionary.Add(binaryEncodingId, type);
                TypeToBinaryEncodingIdDictionary.Add(type, binaryEncodingId);
            }

            var attr2 = type.GetTypeInfo().GetCustomAttribute<DataTypeIdAttribute>(false);
            if (attr2 != null)
            {
                var dataTypeId = attr2.NodeId;
                DataTypeIdToTypeDictionary.Add(dataTypeId, type);
            }
        }

        public virtual async Task<IServiceResponse> RequestAsync(IServiceRequest request)
        {
            ThrowIfClosedOrNotOpening();
            TimestampHeader(request);
            var operation = new ServiceOperation(request);
            using (var timeoutCts = new CancellationTokenSource((int)request.RequestHeader.TimeoutHint))
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, channelCts.Token))
            using (var registration = linkedCts.Token.Register(CancelRequest, operation, false))
            {
                if (pendingRequests.Post(operation))
                {
                    return await operation.Task.ConfigureAwait(false);
                }
                throw new ServiceResultException(StatusCodes.BadSecureChannelClosed);
            }
        }

        protected static byte[] Concat(byte[] a, byte[] b)
        {
            if (a == null && b == null)
            {
                return new byte[0];
            }

            if (a == null)
            {
                return b;
            }

            if (b == null)
            {
                return a;
            }

            var result = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, result, 0, a.Length);
            Buffer.BlockCopy(b, 0, result, a.Length, b.Length);
            return result;
        }

        protected override async Task OnOpeningAsync(CancellationToken token)
        {
            await base.OnOpeningAsync(token).ConfigureAwait(false);

            if (RemoteCertificate != null)
            {
                var cert = certificateParser.ReadCertificate(RemoteCertificate);
                if (cert != null)
                {
                    if (CertificateStore != null)
                    {
                        try
                        {
                            var result = CertificateStore.ValidateRemoteCertificate(cert);

                        }
                        catch (Exception ex)
                        {
                        }
                    }

                    RemotePublicKey = cert.GetPublicKey() as RsaKeyParameters;
                }
            }

            if (RemoteEndpoint.SecurityMode == MessageSecurityMode.SignAndEncrypt)
            {
                if (LocalCertificate == null && CertificateStore != null)
                {
                    var tuple = CertificateStore.GetLocalCertificate(LocalDescription);
                    LocalCertificate = tuple.Item1.GetEncoded();
                    LocalPrivateKey = tuple.Item2;
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

                        asymSigner = SignerUtilities.GetSigner("SHA-1withRSA");
                        asymSigner.Init(true, LocalPrivateKey);
                        asymVerifier = SignerUtilities.GetSigner("SHA-1withRSA");
                        asymVerifier.Init(false, RemotePublicKey);
                        asymEncryptor = CipherUtilities.GetCipher("RSA//PKCS1Padding");
                        asymEncryptor.Init(true, RemotePublicKey);
                        asymDecryptor = CipherUtilities.GetCipher("RSA//PKCS1Padding");
                        asymDecryptor.Init(false, LocalPrivateKey);
                        symSigner = new HMac(new Sha1Digest());
                        symVerifier = new HMac(new Sha1Digest());
                        symEncryptor = CipherUtilities.GetCipher("AES/CBC/NoPadding");
                        symDecryptor = CipherUtilities.GetCipher("AES/CBC/NoPadding");
                        asymLocalKeySize = LocalPrivateKey.Modulus.BitLength;
                        asymRemoteKeySize = RemotePublicKey.Modulus.BitLength;
                        asymLocalPlainTextBlockSize = Math.Max((asymLocalKeySize / 8) - 11, 1);
                        asymRemotePlainTextBlockSize = Math.Max((asymRemoteKeySize / 8) - 11, 1);
                        symSignatureSize = 20;
                        symSignatureKeySize = 16;
                        symEncryptionBlockSize = 16;
                        symEncryptionKeySize = 16;
                        break;

                    case SecurityPolicyUris.Basic256:

                        asymSigner = SignerUtilities.GetSigner("SHA-1withRSA");
                        asymSigner.Init(true, LocalPrivateKey);
                        asymVerifier = SignerUtilities.GetSigner("SHA-1withRSA");
                        asymVerifier.Init(false, RemotePublicKey);
                        asymEncryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        asymEncryptor.Init(true, RemotePublicKey);
                        asymDecryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        asymDecryptor.Init(false, LocalPrivateKey);
                        symSigner = new HMac(new Sha1Digest());
                        symVerifier = new HMac(new Sha1Digest());
                        symEncryptor = CipherUtilities.GetCipher("AES/CBC/NoPadding");
                        symDecryptor = CipherUtilities.GetCipher("AES/CBC/NoPadding");
                        asymLocalKeySize = LocalPrivateKey.Modulus.BitLength;
                        asymRemoteKeySize = RemotePublicKey.Modulus.BitLength;
                        asymLocalPlainTextBlockSize = Math.Max((asymLocalKeySize / 8) - 42, 1);
                        asymRemotePlainTextBlockSize = Math.Max((asymRemoteKeySize / 8) - 42, 1);
                        symSignatureSize = 20;
                        symSignatureKeySize = 24;
                        symEncryptionBlockSize = 16;
                        symEncryptionKeySize = 32;
                        break;

                    case SecurityPolicyUris.Basic256Sha256:

                        asymSigner = SignerUtilities.GetSigner("SHA-256withRSA");
                        asymSigner.Init(true, LocalPrivateKey);
                        asymVerifier = SignerUtilities.GetSigner("SHA-256withRSA");
                        asymVerifier.Init(false, RemotePublicKey);
                        asymEncryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        asymEncryptor.Init(true, RemotePublicKey);
                        asymDecryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        asymDecryptor.Init(false, LocalPrivateKey);
                        symSigner = new HMac(new Sha256Digest());
                        symVerifier = new HMac(new Sha256Digest());
                        symEncryptor = CipherUtilities.GetCipher("AES/CBC/NoPadding");
                        symDecryptor = CipherUtilities.GetCipher("AES/CBC/NoPadding");
                        asymLocalKeySize = LocalPrivateKey.Modulus.BitLength;
                        asymRemoteKeySize = RemotePublicKey.Modulus.BitLength;
                        asymLocalPlainTextBlockSize = Math.Max((asymLocalKeySize / 8) - 42, 1);
                        asymRemotePlainTextBlockSize = Math.Max((asymRemoteKeySize / 8) - 42, 1);
                        symSignatureSize = 32;
                        symSignatureKeySize = 32;
                        symEncryptionBlockSize = 16;
                        symEncryptionKeySize = 32;
                        break;

                    default:
                        throw new ServiceResultException(StatusCodes.BadSecurityPolicyRejected);
                }

                asymIsSigned = asymIsEncrypted = true;
                symIsSigned = true;
                symIsEncrypted = true;
                asymLocalSignatureSize = asymLocalKeySize / 8;
                asymLocalCipherTextBlockSize = Math.Max(asymLocalKeySize / 8, 1);
                asymRemoteSignatureSize = asymRemoteKeySize / 8;
                asymRemoteCipherTextBlockSize = Math.Max(asymRemoteKeySize / 8, 1);
                clientSigningKey = new byte[symSignatureKeySize];
                clientEncryptingKey = new byte[symEncryptionKeySize];
                clientInitializationVector = new byte[symEncryptionBlockSize];
                serverSigningKey = new byte[symSignatureKeySize];
                serverEncryptingKey = new byte[symEncryptionKeySize];
                serverInitializationVector = new byte[symEncryptionBlockSize];
                encryptionBuffer = new byte[LocalSendBufferSize];
                thumbprintDigest = DigestUtilities.GetDigest("SHA-1");
            }
            else if (RemoteEndpoint.SecurityMode == MessageSecurityMode.Sign)
            {
                if (LocalCertificate == null && CertificateStore != null)
                {
                    var tuple = CertificateStore.GetLocalCertificate(LocalDescription);
                    LocalCertificate = tuple.Item1.GetEncoded();
                    LocalPrivateKey = tuple.Item2;
                }

                if (LocalPrivateKey == null)
                {
                    throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed, "LocalPrivateKey is null.");
                }

                if (RemotePublicKey == null)
                {
                    throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed, "RemotePublicKey is null.");
                }

                RemotePublicKey = certificateParser.ReadCertificate(RemoteCertificate)?.GetPublicKey() as RsaKeyParameters;

                switch (RemoteEndpoint.SecurityPolicyUri)
                {
                    case SecurityPolicyUris.Basic128Rsa15:

                        asymSigner = SignerUtilities.GetSigner("SHA-1withRSA");
                        asymSigner.Init(true, LocalPrivateKey);
                        asymVerifier = SignerUtilities.GetSigner("SHA-1withRSA");
                        asymVerifier.Init(false, RemotePublicKey);
                        asymEncryptor = CipherUtilities.GetCipher("RSA//PKCS1Padding");
                        asymEncryptor.Init(true, RemotePublicKey);
                        asymDecryptor = CipherUtilities.GetCipher("RSA//PKCS1Padding");
                        asymDecryptor.Init(false, LocalPrivateKey);
                        symSigner = new HMac(new Sha1Digest());
                        symVerifier = new HMac(new Sha1Digest());
                        asymLocalKeySize = LocalPrivateKey.Modulus.BitLength;
                        asymRemoteKeySize = RemotePublicKey.Modulus.BitLength;
                        asymLocalPlainTextBlockSize = Math.Max((asymLocalKeySize / 8) - 11, 1);
                        asymRemotePlainTextBlockSize = Math.Max((asymRemoteKeySize / 8) - 11, 1);
                        symSignatureSize = 20;
                        symSignatureKeySize = 16;
                        symEncryptionBlockSize = 16;
                        symEncryptionKeySize = 16;
                        break;

                    case SecurityPolicyUris.Basic256:

                        asymSigner = SignerUtilities.GetSigner("SHA-1withRSA");
                        asymSigner.Init(true, LocalPrivateKey);
                        asymVerifier = SignerUtilities.GetSigner("SHA-1withRSA");
                        asymVerifier.Init(false, RemotePublicKey);
                        asymEncryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        asymEncryptor.Init(true, RemotePublicKey);
                        asymDecryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        asymDecryptor.Init(false, LocalPrivateKey);
                        symSigner = new HMac(new Sha1Digest());
                        symVerifier = new HMac(new Sha1Digest());
                        asymLocalKeySize = LocalPrivateKey.Modulus.BitLength;
                        asymRemoteKeySize = RemotePublicKey.Modulus.BitLength;
                        asymLocalPlainTextBlockSize = Math.Max((asymLocalKeySize / 8) - 42, 1);
                        asymRemotePlainTextBlockSize = Math.Max((asymRemoteKeySize / 8) - 42, 1);
                        symSignatureSize = 20;
                        symSignatureKeySize = 24;
                        symEncryptionBlockSize = 16;
                        symEncryptionKeySize = 32;
                        break;

                    case SecurityPolicyUris.Basic256Sha256:

                        asymSigner = SignerUtilities.GetSigner("SHA-256withRSA");
                        asymSigner.Init(true, LocalPrivateKey);
                        asymVerifier = SignerUtilities.GetSigner("SHA-256withRSA");
                        asymVerifier.Init(false, RemotePublicKey);
                        asymEncryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        asymEncryptor.Init(true, RemotePublicKey);
                        asymDecryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        asymDecryptor.Init(false, LocalPrivateKey);
                        symSigner = new HMac(new Sha256Digest());
                        symVerifier = new HMac(new Sha256Digest());
                        asymLocalKeySize = LocalPrivateKey.Modulus.BitLength;
                        asymRemoteKeySize = RemotePublicKey.Modulus.BitLength;
                        asymLocalPlainTextBlockSize = Math.Max((asymLocalKeySize / 8) - 42, 1);
                        asymRemotePlainTextBlockSize = Math.Max((asymRemoteKeySize / 8) - 42, 1);
                        symSignatureSize = 32;
                        symSignatureKeySize = 32;
                        symEncryptionBlockSize = 16;
                        symEncryptionKeySize = 32;
                        break;

                    default:
                        throw new ServiceResultException(StatusCodes.BadSecurityPolicyRejected);
                }

                asymIsSigned = asymIsEncrypted = true;
                symIsSigned = true;
                symIsEncrypted = false;
                asymLocalSignatureSize = asymLocalKeySize / 8;
                asymLocalCipherTextBlockSize = Math.Max(asymLocalKeySize / 8, 1);
                asymRemoteSignatureSize = asymRemoteKeySize / 8;
                asymRemoteCipherTextBlockSize = Math.Max(asymRemoteKeySize / 8, 1);
                clientSigningKey = new byte[symSignatureKeySize];
                clientEncryptingKey = new byte[symEncryptionKeySize];
                clientInitializationVector = new byte[symEncryptionBlockSize];
                serverSigningKey = new byte[symSignatureKeySize];
                serverEncryptingKey = new byte[symEncryptionKeySize];
                serverInitializationVector = new byte[symEncryptionBlockSize];
                encryptionBuffer = new byte[LocalSendBufferSize];
                thumbprintDigest = DigestUtilities.GetDigest("SHA-1");
            }
            else if (RemoteEndpoint.SecurityMode == MessageSecurityMode.None)
            {
                asymIsSigned = asymIsEncrypted = false;
                symIsSigned = symIsEncrypted = false;
                asymLocalKeySize = 0;
                asymRemoteKeySize = 0;
                asymLocalSignatureSize = 0;
                asymLocalCipherTextBlockSize = 1;
                asymRemoteSignatureSize = 0;
                asymRemoteCipherTextBlockSize = 1;
                asymLocalPlainTextBlockSize = 1;
                asymRemotePlainTextBlockSize = 1;
                symSignatureSize = 0;
                symSignatureKeySize = 0;
                symEncryptionBlockSize = 1;
                symEncryptionKeySize = 0;
                encryptionBuffer = null;
            }
            else
            {
                throw new ServiceResultException(StatusCodes.BadSecurityModeRejected);
            }
        }

        protected override async Task OnOpenAsync(CancellationToken token)
        {
            await base.OnOpenAsync(token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            sendBuffer = new byte[LocalSendBufferSize];
            receiveBuffer = new byte[LocalReceiveBufferSize];

            receiveResponsesTask = ReceiveResponsesAsync();

            var openSecureChannelRequest = new OpenSecureChannelRequest
            {
                RequestHeader = new RequestHeader { TimeoutHint = TimeoutHint, ReturnDiagnostics = DiagnosticsHint, Timestamp = DateTime.UtcNow, RequestHandle = GetNextHandle() },
                ClientProtocolVersion = ProtocolVersion,
                RequestType = SecurityTokenRequestType.Issue,
                SecurityMode = RemoteEndpoint.SecurityMode,
                ClientNonce = symIsSigned ? LocalNonce = GetNextNonce(symEncryptionKeySize) : null,
                RequestedLifetime = TokenRequestedLifetime
            };

            var openSecureChannelResponse = (OpenSecureChannelResponse)await RequestAsync(openSecureChannelRequest).ConfigureAwait(false);

            if (openSecureChannelResponse.ServerProtocolVersion < ProtocolVersion)
            {
                throw new ServiceResultException(StatusCodes.BadProtocolVersionUnsupported);
            }

            // Schedule token renewal.
            tokenRenewalTime = DateTime.UtcNow.AddMilliseconds(0.8 * openSecureChannelResponse.SecurityToken.RevisedLifetime);
        }

        protected override async Task OnCloseAsync(CancellationToken token)
        {
            channelCts?.Cancel();
            var closeSecureChannelRequest = new CloseSecureChannelRequest
            {
                RequestHeader = new RequestHeader { TimeoutHint = TimeoutHint, ReturnDiagnostics = DiagnosticsHint, Timestamp = DateTime.UtcNow, RequestHandle = GetNextHandle() },
            };
            await SendRequestAsync(new ServiceOperation(closeSecureChannelRequest)).ConfigureAwait(false);

            await base.OnCloseAsync(token).ConfigureAwait(false);
        }

        protected override Task OnFaulted(CancellationToken token = default(CancellationToken))
        {
            channelCts?.Cancel();
            return base.OnFaulted(token);
        }

        protected override async Task OnClosedAsync(CancellationToken token)
        {
            if (receiveResponsesTask != null && !receiveResponsesTask.IsCompleted)
            {
                Logger?.LogTrace("Waiting for socket to close.");
                var t = await Task.WhenAny(receiveResponsesTask, Task.Delay(2000)).ConfigureAwait(false);
                if (t != receiveResponsesTask)
                {
                    Logger?.LogError("Timeout while waiting for socket to close.");
                }
            }
            channelCts?.Dispose();
            await base.OnClosedAsync(token);
        }

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

        private async Task SendRequestActionAsync(ServiceOperation operation)
        {
            var token = channelCts.Token;
            try
            {
                if (!operation.Task.IsCompleted)
                {
                    await SendRequestAsync(operation, token).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                {
                    await FaultAsync(ex).ConfigureAwait(false);
                    await AbortAsync().ConfigureAwait(false);
                }
            }
        }

        private async Task SendRequestAsync(ServiceOperation operation, CancellationToken token = default(CancellationToken))
        {
            await sendingSemaphore.WaitAsync(token).ConfigureAwait(false);
            var request = operation.Request;
            try
            {
                ThrowIfClosedOrNotOpening();

                // Check if time to renew security token.
                if (DateTime.UtcNow > tokenRenewalTime)
                {
                    tokenRenewalTime = tokenRenewalTime.AddMilliseconds(60000);
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
                        ClientNonce = symIsSigned ? LocalNonce = GetNextNonce(symEncryptionKeySize) : null,
                        RequestedLifetime = TokenRequestedLifetime
                    };
                    Logger?.LogTrace($"Sending {openSecureChannelRequest.GetType().Name} Handle: {openSecureChannelRequest.RequestHeader.RequestHandle}");
                    pendingCompletions.TryAdd(openSecureChannelRequest.RequestHeader.RequestHandle, new ServiceOperation(openSecureChannelRequest));
                    await SendOpenSecureChannelRequestAsync(openSecureChannelRequest, token).ConfigureAwait(false);
                }

                request.RequestHeader.RequestHandle = GetNextHandle();
                request.RequestHeader.AuthenticationToken = AuthenticationToken;

                Logger?.LogTrace($"Sending {request.GetType().Name} Handle: {request.RequestHeader.RequestHandle}");
                pendingCompletions.TryAdd(request.RequestHeader.RequestHandle, operation);
                if (request is OpenSecureChannelRequest)
                {
                    await SendOpenSecureChannelRequestAsync((OpenSecureChannelRequest)request, token).ConfigureAwait(false);
                }
                else if (request is CloseSecureChannelRequest)
                {
                    await SendCloseSecureChannelRequestAsync((CloseSecureChannelRequest)request, token).ConfigureAwait(false);
                }
                else
                {
                    await SendServiceRequestAsync(request, token).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError($"Error sending {request.GetType().Name} Handle: {request.RequestHeader.RequestHandle}. {ex.Message}");
                throw;
            }
            finally
            {
                sendingSemaphore.Release();
            }
        }

        private async Task SendOpenSecureChannelRequestAsync(OpenSecureChannelRequest request, CancellationToken token)
        {
            var bodyStream = RecyclableMemoryStreamManager.Default.GetStream("SendOpenSecureChannelRequestAsync");
            var bodyEncoder = new BinaryEncoder(bodyStream, this);
            try
            {
                bodyEncoder.WriteNodeId(null, OpenSecureChannelRequestNodeId);
                request.Encode(bodyEncoder);
                bodyStream.Position = 0;
                if (bodyStream.Length > RemoteMaxMessageSize)
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

                    var stream = new MemoryStream(sendBuffer, 0, (int)RemoteReceiveBufferSize, true, true);
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
                            byte[] thumbprint = new byte[thumbprintDigest.GetDigestSize()];
                            thumbprintDigest.BlockUpdate(RemoteCertificate, 0, RemoteCertificate.Length);
                            thumbprintDigest.DoFinal(thumbprint, 0);
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
                        encoder.WriteUInt32(null, request.RequestHeader.RequestHandle);

                        // body
                        int paddingHeaderSize;
                        int maxBodySize;
                        int bodySize;
                        int paddingSize;
                        int chunkSize;
                        if (asymIsEncrypted)
                        {
                            paddingHeaderSize = asymRemoteCipherTextBlockSize > 256 ? 2 : 1;
                            maxBodySize = ((((int)RemoteReceiveBufferSize - plainHeaderSize - asymLocalSignatureSize - paddingHeaderSize) / asymRemoteCipherTextBlockSize) * asymRemotePlainTextBlockSize) - SequenceHeaderSize;
                            if (bodyCount < maxBodySize)
                            {
                                bodySize = bodyCount;
                                paddingSize = (asymRemotePlainTextBlockSize - ((SequenceHeaderSize + bodySize + paddingHeaderSize + asymLocalSignatureSize) % asymRemotePlainTextBlockSize)) % asymRemotePlainTextBlockSize;
                            }
                            else
                            {
                                bodySize = maxBodySize;
                                paddingSize = 0;
                            }

                            chunkSize = plainHeaderSize + (((SequenceHeaderSize + bodySize + paddingSize + paddingHeaderSize + asymLocalSignatureSize) / asymRemotePlainTextBlockSize) * asymRemoteCipherTextBlockSize);
                        }
                        else
                        {
                            paddingHeaderSize = 0;
                            paddingSize = 0;
                            maxBodySize = (int)RemoteReceiveBufferSize - plainHeaderSize - asymLocalSignatureSize - SequenceHeaderSize;
                            if (bodyCount < maxBodySize)
                            {
                                bodySize = bodyCount;
                            }
                            else
                            {
                                bodySize = maxBodySize;
                            }

                            chunkSize = plainHeaderSize + SequenceHeaderSize + bodySize + asymLocalSignatureSize;
                        }

                        bodyStream.Read(sendBuffer, encoder.Position, bodySize);
                        encoder.Position += bodySize;
                        bodyCount -= bodySize;

                        // padding
                        if (asymIsEncrypted)
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
                        if (asymIsSigned)
                        {
                            // sign with local private key.
                            asymSigner.BlockUpdate(sendBuffer, 0, position);
                            byte[] signature = asymSigner.GenerateSignature();
                            Debug.Assert(signature.Length == asymLocalSignatureSize, nameof(asymLocalSignatureSize));
                            encoder.Write(signature, 0, asymLocalSignatureSize);
                        }

                        // encrypt
                        if (asymIsEncrypted)
                        {
                            position = encoder.Position;
                            Buffer.BlockCopy(sendBuffer, 0, encryptionBuffer, 0, plainHeaderSize);
                            byte[] plainText = new byte[asymRemotePlainTextBlockSize];
                            int jj = plainHeaderSize;
                            for (int ii = plainHeaderSize; ii < position; ii += asymRemotePlainTextBlockSize)
                            {
                                Buffer.BlockCopy(sendBuffer, ii, plainText, 0, asymRemotePlainTextBlockSize);

                                // encrypt with remote public key.
                                byte[] cipherText = asymEncryptor.DoFinal(plainText);
                                Debug.Assert(cipherText.Length == asymRemoteCipherTextBlockSize, nameof(asymRemoteCipherTextBlockSize));
                                Buffer.BlockCopy(cipherText, 0, encryptionBuffer, jj, asymRemoteCipherTextBlockSize);
                                jj += asymRemoteCipherTextBlockSize;
                            }

                            await SendAsync(encryptionBuffer, 0, jj, token).ConfigureAwait(false);
                            return;
                        }

                        // pass buffer to transport
                        await SendAsync(sendBuffer, 0, encoder.Position, token).ConfigureAwait(false);
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

        private async Task SendCloseSecureChannelRequestAsync(CloseSecureChannelRequest request, CancellationToken token)
        {
            var bodyStream = RecyclableMemoryStreamManager.Default.GetStream("SendCloseSecureChannelRequestAsync");
            var bodyEncoder = new BinaryEncoder(bodyStream, this);
            try
            {
                bodyEncoder.WriteNodeId(null, CloseSecureChannelRequestNodeId);
                request.Encode(bodyEncoder);
                bodyStream.Position = 0;
                if (bodyStream.Length > RemoteMaxMessageSize)
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

                    var stream = new MemoryStream(sendBuffer, 0, (int)RemoteReceiveBufferSize, true, true);
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
                        if (TokenId != currentClientTokenId)
                        {
                            currentClientTokenId = TokenId;

                            // update signer and encrypter with new symmetric keys
                            if (symIsSigned)
                            {
                                symSigner.Init(new KeyParameter(clientSigningKey));
                                if (symIsEncrypted)
                                {
                                    symEncryptor.Init(
                                        true,
                                        new ParametersWithIV(new KeyParameter(clientEncryptingKey), clientInitializationVector));
                                }
                            }
                        }

                        int plainHeaderSize = encoder.Position;

                        // sequence header
                        encoder.WriteUInt32(null, GetNextSequenceNumber());
                        encoder.WriteUInt32(null, request.RequestHeader.RequestHandle);

                        // body
                        int paddingHeaderSize;
                        int maxBodySize;
                        int bodySize;
                        int paddingSize;
                        int chunkSize;
                        if (symIsEncrypted)
                        {
                            paddingHeaderSize = symEncryptionBlockSize > 256 ? 2 : 1;
                            maxBodySize = ((((int)RemoteReceiveBufferSize - plainHeaderSize - symSignatureSize - paddingHeaderSize) / symEncryptionBlockSize) * symEncryptionBlockSize) - SequenceHeaderSize;
                            if (bodyCount < maxBodySize)
                            {
                                bodySize = bodyCount;
                                paddingSize = (symEncryptionBlockSize - ((SequenceHeaderSize + bodySize + paddingHeaderSize + symSignatureSize) % symEncryptionBlockSize)) % symEncryptionBlockSize;
                            }
                            else
                            {
                                bodySize = maxBodySize;
                                paddingSize = 0;
                            }

                            chunkSize = plainHeaderSize + (((SequenceHeaderSize + bodySize + paddingSize + paddingHeaderSize + symSignatureSize) / symEncryptionBlockSize) * symEncryptionBlockSize);
                        }
                        else
                        {
                            paddingHeaderSize = 0;
                            paddingSize = 0;
                            maxBodySize = (int)RemoteReceiveBufferSize - plainHeaderSize - symSignatureSize - SequenceHeaderSize;
                            if (bodyCount < maxBodySize)
                            {
                                bodySize = bodyCount;
                            }
                            else
                            {
                                bodySize = maxBodySize;
                            }

                            chunkSize = plainHeaderSize + SequenceHeaderSize + bodySize + symSignatureSize;
                        }

                        bodyStream.Read(sendBuffer, encoder.Position, bodySize);
                        encoder.Position += bodySize;
                        bodyCount -= bodySize;

                        // padding
                        if (symIsEncrypted)
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
                        if (symIsSigned)
                        {
                            symSigner.BlockUpdate(sendBuffer, 0, position);
                            byte[] signature = new byte[symSigner.GetMacSize()];
                            symSigner.DoFinal(signature, 0);
                            encoder.Write(signature, 0, signature.Length);
                        }

                        // encrypt
                        position = encoder.Position;
                        if (symIsEncrypted)
                        {
                            int inputCount = position - plainHeaderSize;
                            Debug.Assert(inputCount % symEncryptor.GetBlockSize() == 0, "Input data is not an even number of encryption blocks.");
                            symEncryptor.DoFinal(sendBuffer, plainHeaderSize, inputCount, sendBuffer, plainHeaderSize);
                        }

                        // pass buffer to transport
                        await SendAsync(sendBuffer, 0, position, token).ConfigureAwait(false);
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

        private async Task SendServiceRequestAsync(IServiceRequest request, CancellationToken token)
        {
            var bodyStream = RecyclableMemoryStreamManager.Default.GetStream("SendServiceRequestAsync");
            var bodyEncoder = new BinaryEncoder(bodyStream, this);
            try
            {
                if (!TypeToBinaryEncodingIdDictionary.TryGetValue(request.GetType(), out var binaryEncodingId))
                {
                    throw new ServiceResultException(StatusCodes.BadDataTypeIdUnknown);
                }

                bodyEncoder.WriteNodeId(null, ExpandedNodeId.ToNodeId(binaryEncodingId, NamespaceUris));
                request.Encode(bodyEncoder);
                bodyStream.Position = 0;
                if (bodyStream.Length > RemoteMaxMessageSize)
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

                    var stream = new MemoryStream(sendBuffer, 0, (int)RemoteReceiveBufferSize, true, true);
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
                        if (TokenId != currentClientTokenId)
                        {
                            currentClientTokenId = TokenId;

                            // update signer and encrypter with new symmetric keys
                            if (symIsSigned)
                            {
                                symSigner.Init(new KeyParameter(clientSigningKey));
                                if (symIsEncrypted)
                                {
                                    symEncryptor.Init(
                                        true,
                                        new ParametersWithIV(new KeyParameter(clientEncryptingKey), clientInitializationVector));
                                }
                            }
                        }

                        int plainHeaderSize = encoder.Position;

                        // sequence header
                        encoder.WriteUInt32(null, GetNextSequenceNumber());
                        encoder.WriteUInt32(null, request.RequestHeader.RequestHandle);

                        // body
                        int paddingHeaderSize;
                        int maxBodySize;
                        int bodySize;
                        int paddingSize;
                        int chunkSize;
                        if (symIsEncrypted)
                        {
                            paddingHeaderSize = symEncryptionBlockSize > 256 ? 2 : 1;
                            maxBodySize = ((((int)RemoteReceiveBufferSize - plainHeaderSize - symSignatureSize - paddingHeaderSize) / symEncryptionBlockSize) * symEncryptionBlockSize) - SequenceHeaderSize;
                            if (bodyCount < maxBodySize)
                            {
                                bodySize = bodyCount;
                                paddingSize = (symEncryptionBlockSize - ((SequenceHeaderSize + bodySize + paddingHeaderSize + symSignatureSize) % symEncryptionBlockSize)) % symEncryptionBlockSize;
                            }
                            else
                            {
                                bodySize = maxBodySize;
                                paddingSize = 0;
                            }

                            chunkSize = plainHeaderSize + (((SequenceHeaderSize + bodySize + paddingSize + paddingHeaderSize + symSignatureSize) / symEncryptionBlockSize) * symEncryptionBlockSize);
                        }
                        else
                        {
                            paddingHeaderSize = 0;
                            paddingSize = 0;
                            maxBodySize = (int)RemoteReceiveBufferSize - plainHeaderSize - symSignatureSize - SequenceHeaderSize;
                            if (bodyCount < maxBodySize)
                            {
                                bodySize = bodyCount;
                            }
                            else
                            {
                                bodySize = maxBodySize;
                            }

                            chunkSize = plainHeaderSize + SequenceHeaderSize + bodySize + symSignatureSize;
                        }

                        bodyStream.Read(sendBuffer, encoder.Position, bodySize);
                        encoder.Position += bodySize;
                        bodyCount -= bodySize;

                        // padding
                        if (symIsEncrypted)
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
                        if (symIsSigned)
                        {
                            symSigner.BlockUpdate(sendBuffer, 0, position);
                            byte[] signature = new byte[symSigner.GetMacSize()];
                            symSigner.DoFinal(signature, 0);
                            encoder.Write(signature, 0, signature.Length);
                        }

                        // encrypt
                        position = encoder.Position;
                        if (symIsEncrypted)
                        {
                            int inputCount = position - plainHeaderSize;
                            Debug.Assert(inputCount % symEncryptor.GetBlockSize() == 0, "Input data is not an even number of encryption blocks.");
                            symEncryptor.DoFinal(sendBuffer, plainHeaderSize, inputCount, sendBuffer, plainHeaderSize);
                        }

                        // pass buffer to transport
                        await SendAsync(sendBuffer, 0, position, token).ConfigureAwait(false);
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

        private async Task ReceiveResponsesAsync(CancellationToken token = default(CancellationToken))
        {
            while (true)
            {
                try
                {
                    var response = await ReceiveResponseAsync().ConfigureAwait(false);
                    if (response == null)
                    {
                        // Null response indicates socket closed. This is expected when closing secure channel.
                        if (State == CommunicationState.Closed || State == CommunicationState.Closing)
                        {
                            return;
                        }

                        throw new ServiceResultException(StatusCodes.BadServerNotConnected);
                    }
                    var header = response.ResponseHeader;
                    if (pendingCompletions.TryRemove(header.RequestHandle, out var tcs))
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
                    }
                }
                catch (Exception ex)
                {
                    if (State == CommunicationState.Closed || State == CommunicationState.Closing)
                    {
                        return;
                    }

                    if (State == CommunicationState.Faulted)
                    {
                        return;
                    }
                    await FaultAsync(ex).ConfigureAwait(false);
                    await AbortAsync().ConfigureAwait(false);
                }
            }
        }

        protected async Task<IServiceResponse> ReceiveResponseAsync(CancellationToken token = default(CancellationToken))
        {
            await receivingSemaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                ThrowIfClosedOrNotOpening();
                uint sequenceNumber;
                uint requestId;
                int paddingHeaderSize;
                int plainHeaderSize;
                int bodySize;
                int paddingSize;

                var bodyStream = RecyclableMemoryStreamManager.Default.GetStream("ReceiveResponseAsync");
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

                        var count = await ReceiveAsync(receiveBuffer, 0, (int)LocalReceiveBufferSize, token).ConfigureAwait(false);
                        if (count == 0)
                        {
                            return null;
                        }

                        var stream = new MemoryStream(receiveBuffer, 0, count, true, true);
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
                                    if (tokenId != currentServerTokenId)
                                    {
                                        currentServerTokenId = tokenId;

                                        // update with new keys
                                        if (symIsSigned)
                                        {
                                            symVerifier.Init(new KeyParameter(serverSigningKey));
                                            if (symIsEncrypted)
                                            {
                                                symDecryptor.Init(
                                                    false,
                                                    new ParametersWithIV(new KeyParameter(serverEncryptingKey), serverInitializationVector));
                                            }
                                        }

                                        Logger?.LogTrace($"Installed new security token {tokenId}.");
                                    }

                                    plainHeaderSize = decoder.Position;

                                    // decrypt
                                    if (symIsEncrypted)
                                    {
                                        int inputCount = messageLength - plainHeaderSize;
                                        Debug.Assert(inputCount % symDecryptor.GetBlockSize() == 0, "Input data is not an even number of encryption blocks.");
                                        symDecryptor.DoFinal(receiveBuffer, plainHeaderSize, inputCount, receiveBuffer, plainHeaderSize);
                                    }

                                    // verify
                                    if (symIsSigned)
                                    {
                                        var datalen = messageLength - symSignatureSize;

                                        symVerifier.BlockUpdate(receiveBuffer, 0, datalen);
                                        byte[] signature = new byte[symVerifier.GetMacSize()];
                                        symVerifier.DoFinal(signature, 0);

                                        if (!signature.SequenceEqual(receiveBuffer.AsArraySegment(datalen, symSignatureSize)))
                                        {
                                            throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed);
                                        }
                                    }

                                    // read sequence header
                                    sequenceNumber = decoder.ReadUInt32(null);
                                    requestId = decoder.ReadUInt32(null);

                                    // body
                                    if (symIsEncrypted)
                                    {
                                        if (symEncryptionBlockSize > 256)
                                        {
                                            paddingHeaderSize = 2;
                                            paddingSize = BitConverter.ToInt16(receiveBuffer, messageLength - symSignatureSize - paddingHeaderSize);
                                        }
                                        else
                                        {
                                            paddingHeaderSize = 1;
                                            paddingSize = receiveBuffer[messageLength - symSignatureSize - paddingHeaderSize];
                                        }

                                        bodySize = messageLength - plainHeaderSize - SequenceHeaderSize - paddingSize - paddingHeaderSize - symSignatureSize;
                                    }
                                    else
                                    {
                                        bodySize = messageLength - plainHeaderSize - SequenceHeaderSize - symSignatureSize;
                                    }

                                    bodyStream.Write(receiveBuffer, plainHeaderSize + SequenceHeaderSize, bodySize);
                                    isFinal = messageType == UaTcpMessageTypes.MSGF;
                                    break;

                                case UaTcpMessageTypes.OPNF:
                                case UaTcpMessageTypes.OPNC:
                                    // header
                                    channelId = decoder.ReadUInt32(null);

                                    // asymmetric header
                                    var securityPolicyUri = decoder.ReadString(null);
                                    var serverCertificateByteString = decoder.ReadByteString(null);
                                    var clientThumbprint = decoder.ReadByteString(null);
                                    plainHeaderSize = decoder.Position;

                                    // decrypt
                                    if (asymIsEncrypted)
                                    {
                                        byte[] cipherTextBlock = new byte[asymLocalCipherTextBlockSize];
                                        int jj = plainHeaderSize;
                                        for (int ii = plainHeaderSize; ii < messageLength; ii += asymLocalCipherTextBlockSize)
                                        {
                                            Buffer.BlockCopy(receiveBuffer, ii, cipherTextBlock, 0, asymLocalCipherTextBlockSize);

                                            // decrypt with local private key.
                                            byte[] plainTextBlock = asymDecryptor.DoFinal(cipherTextBlock);
                                            Debug.Assert(plainTextBlock.Length == asymLocalPlainTextBlockSize, "Decrypted block length was not as expected.");
                                            Buffer.BlockCopy(plainTextBlock, 0, receiveBuffer, jj, asymLocalPlainTextBlockSize);
                                            jj += asymLocalPlainTextBlockSize;
                                        }

                                        messageLength = jj;
                                        decoder.Position = plainHeaderSize;
                                    }

                                    // verify
                                    if (asymIsSigned)
                                    {
                                        // verify with remote public key.
                                        var datalen = messageLength - asymRemoteSignatureSize;
                                        asymVerifier.BlockUpdate(receiveBuffer, 0, datalen);
                                        if (!asymVerifier.VerifySignature(receiveBuffer.AsArraySegment(datalen, asymRemoteSignatureSize).ToArray()))
                                        {
                                            throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed);
                                        }
                                    }

                                    // sequence header
                                    sequenceNumber = decoder.ReadUInt32(null);
                                    requestId = decoder.ReadUInt32(null);

                                    // body
                                    if (asymIsEncrypted)
                                    {
                                        if (asymLocalCipherTextBlockSize > 256)
                                        {
                                            paddingHeaderSize = 2;
                                            paddingSize = BitConverter.ToInt16(receiveBuffer, messageLength - asymRemoteSignatureSize - paddingHeaderSize);
                                        }
                                        else
                                        {
                                            paddingHeaderSize = 1;
                                            paddingSize = receiveBuffer[messageLength - asymRemoteSignatureSize - paddingHeaderSize];
                                        }

                                        bodySize = messageLength - plainHeaderSize - SequenceHeaderSize - paddingSize - paddingHeaderSize - asymRemoteSignatureSize;
                                    }
                                    else
                                    {
                                        bodySize = messageLength - plainHeaderSize - SequenceHeaderSize - asymRemoteSignatureSize;
                                    }

                                    bodyStream.Write(receiveBuffer, plainHeaderSize + SequenceHeaderSize, bodySize);
                                    isFinal = messageType == UaTcpMessageTypes.OPNF;
                                    break;

                                case UaTcpMessageTypes.ERRF:
                                case UaTcpMessageTypes.MSGA:
                                case UaTcpMessageTypes.OPNA:
                                case UaTcpMessageTypes.CLOA:
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
                    var nodeId = bodyDecoder.ReadNodeId(null);
                    IServiceResponse response;

                    // fast path
                    if (nodeId == PublishResponseNodeId)
                    {
                        response = new PublishResponse();
                    }
                    else if (nodeId == ReadResponseNodeId)
                    {
                        response = new ReadResponse();
                    }
                    else
                    {
                        // find node in dictionary
                        if (!BinaryEncodingIdToTypeDictionary.TryGetValue(NodeId.ToExpandedNodeId(nodeId, NamespaceUris), out var type2))
                        {
                            throw new ServiceResultException(StatusCodes.BadEncodingError, "NodeId not registered in dictionary.");
                        }

                        // create response
                        response = (IServiceResponse)Activator.CreateInstance(type2);
                    }

                    // set properties from message stream
                    response.Decode(bodyDecoder);

                    Logger?.LogTrace($"Received {response.GetType().Name} Handle: {response.ResponseHeader.RequestHandle} Result: {response.ResponseHeader.ServiceResult}");
                    // special inline processing for token renewal because we need to
                    // hold both the sending and receiving semaphores to update the security keys.
                    if (response is OpenSecureChannelResponse openSecureChannelResponse && StatusCode.IsGood(openSecureChannelResponse.ResponseHeader.ServiceResult))
                    {
                        tokenRenewalTime = DateTime.UtcNow.AddMilliseconds(0.8 * openSecureChannelResponse.SecurityToken.RevisedLifetime);

                        await sendingSemaphore.WaitAsync(token).ConfigureAwait(false);
                        try
                        {
                            ChannelId = openSecureChannelResponse.SecurityToken.ChannelId;
                            TokenId = openSecureChannelResponse.SecurityToken.TokenId;
                            if (symIsSigned)
                            {
                                var clientNonce = LocalNonce;
                                var serverNonce = openSecureChannelResponse.ServerNonce;

                                // (re)create client security keys for encrypting the next message sent
                                var clientSecurityKey = CalculatePSHA(serverNonce, clientNonce, symSignatureKeySize + symEncryptionKeySize + symEncryptionBlockSize, RemoteEndpoint.SecurityPolicyUri);
                                Buffer.BlockCopy(clientSecurityKey, 0, clientSigningKey, 0, symSignatureKeySize);
                                Buffer.BlockCopy(clientSecurityKey, symSignatureKeySize, clientEncryptingKey, 0, symEncryptionKeySize);
                                Buffer.BlockCopy(clientSecurityKey, symSignatureKeySize + symEncryptionKeySize, clientInitializationVector, 0, symEncryptionBlockSize);

                                // (re)create server security keys for decrypting the next message received that has a new TokenId
                                var serverSecurityKey = CalculatePSHA(clientNonce, serverNonce, symSignatureKeySize + symEncryptionKeySize + symEncryptionBlockSize, RemoteEndpoint.SecurityPolicyUri);
                                Buffer.BlockCopy(serverSecurityKey, 0, serverSigningKey, 0, symSignatureKeySize);
                                Buffer.BlockCopy(serverSecurityKey, symSignatureKeySize, serverEncryptingKey, 0, symEncryptionKeySize);
                                Buffer.BlockCopy(serverSecurityKey, symSignatureKeySize + symEncryptionKeySize, serverInitializationVector, 0, symEncryptionBlockSize);
                            }
                        }
                        finally
                        {
                            sendingSemaphore.Release();
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
                receivingSemaphore.Release();
            }
        }

        private void TimestampHeader(IServiceRequest request)
        {
            if (request.RequestHeader == null)
            {
                request.RequestHeader = new RequestHeader { TimeoutHint = TimeoutHint, ReturnDiagnostics = DiagnosticsHint };
            }

            request.RequestHeader.Timestamp = DateTime.UtcNow;
        }

        private uint GetNextHandle()
        {
            unchecked
            {
                int snapshot = handle;
                int value = snapshot + 1;
                if (value == 0)
                {
                    value = 1;
                }

                if (Interlocked.CompareExchange(ref handle, value, snapshot) != snapshot)
                {
                    var spinner = default(SpinWait);
                    do
                    {
                        spinner.SpinOnce();
                        snapshot = handle;
                        value = snapshot + 1;
                        if (value == 0)
                        {
                            value = 1;
                        }
                    }
                    while (Interlocked.CompareExchange(ref handle, value, snapshot) != snapshot);
                }

                return (uint)value;
            }
        }

        private uint GetNextSequenceNumber()
        {
            unchecked
            {
                return (uint)Interlocked.Increment(ref sequenceNumber);
            }
        }

        protected byte[] GetNextNonce(int length)
        {
            var nonce = new byte[length];
            Rng.NextBytes(nonce);
            return nonce;
        }

        private void CancelRequest(object o)
        {
            var operation = (ServiceOperation)o;
            if (operation.TrySetException(new ServiceResultException(StatusCodes.BadRequestTimeout)))
            {
                var request = operation.Request;
                Logger?.LogTrace($"Canceled {request.GetType().Name} Handle: {request.RequestHeader.RequestHandle}");
            }
        }

        public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, ServiceOperation messageValue, ISourceBlock<ServiceOperation> source, bool consumeToAccept)
        {
            return ((ITargetBlock<ServiceOperation>)pendingRequests).OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        }

        public void Complete()
        {
            pendingRequests.Complete();
        }

        public void Fault(Exception exception)
        {
            ((IDataflowBlock)pendingRequests).Fault(exception);
        }
    }
}