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
            this.LocalDescription = localDescription ?? throw new ArgumentNullException(nameof(localDescription));
            this.CertificateStore = certificateStore;
            this.RemoteCertificate = this.RemoteEndpoint.ServerCertificate;
            this.TimeoutHint = timeoutHint;
            this.DiagnosticsHint = diagnosticsHint;
            this.AuthenticationToken = null;
            this.NamespaceUris = new List<string> { "http://opcfoundation.org/UA/" };
            this.ServerUris = new List<string>();
            this.channelCts = new CancellationTokenSource();
            this.pendingRequests = new ActionBlock<ServiceOperation>(t => this.SendRequestActionAsync(t), new ExecutionDataflowBlockOptions { CancellationToken = this.channelCts.Token });
            this.pendingCompletions = new ConcurrentDictionary<uint, ServiceOperation>();
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

        public Task Completion => this.pendingRequests.Completion;

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
            this.ThrowIfClosedOrNotOpening();
            this.TimestampHeader(request);
            var operation = new ServiceOperation(request);
            using (var timeoutCts = new CancellationTokenSource((int)request.RequestHeader.TimeoutHint))
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, this.channelCts.Token))
            using (var registration = linkedCts.Token.Register(this.CancelRequest, operation, false))
            {
                if (this.pendingRequests.Post(operation))
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

        /// <inheritdoc/>
        protected override async Task OnOpeningAsync(CancellationToken token)
        {
            await base.OnOpeningAsync(token).ConfigureAwait(false);

            if (this.RemoteCertificate != null)
            {
                var cert = this.certificateParser.ReadCertificate(this.RemoteCertificate);
                if (cert != null)
                {
                    if (this.CertificateStore != null)
                    {
                        try
                        {
                            var result = this.CertificateStore.ValidateRemoteCertificate(cert);

                        }
                        catch (Exception ex)
                        {
                        }
                    }

                    this.RemotePublicKey = cert.GetPublicKey() as RsaKeyParameters;
                }
            }

            if (this.RemoteEndpoint.SecurityMode == MessageSecurityMode.SignAndEncrypt)
            {
                if (this.LocalCertificate == null && this.CertificateStore != null)
                {
                    var tuple = this.CertificateStore.GetLocalCertificate(this.LocalDescription);
                    this.LocalCertificate = tuple.Item1.GetEncoded();
                    this.LocalPrivateKey = tuple.Item2;
                }

                if (this.LocalPrivateKey == null)
                {
                    throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed, "LocalPrivateKey is null.");
                }

                if (this.RemotePublicKey == null)
                {
                    throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed, "RemotePublicKey is null.");
                }

                switch (this.RemoteEndpoint.SecurityPolicyUri)
                {
                    case SecurityPolicyUris.Basic128Rsa15:

                        this.asymSigner = SignerUtilities.GetSigner("SHA-1withRSA");
                        this.asymSigner.Init(true, this.LocalPrivateKey);
                        this.asymVerifier = SignerUtilities.GetSigner("SHA-1withRSA");
                        this.asymVerifier.Init(false, this.RemotePublicKey);
                        this.asymEncryptor = CipherUtilities.GetCipher("RSA//PKCS1Padding");
                        this.asymEncryptor.Init(true, this.RemotePublicKey);
                        this.asymDecryptor = CipherUtilities.GetCipher("RSA//PKCS1Padding");
                        this.asymDecryptor.Init(false, this.LocalPrivateKey);
                        this.symSigner = new HMac(new Sha1Digest());
                        this.symVerifier = new HMac(new Sha1Digest());
                        this.symEncryptor = CipherUtilities.GetCipher("AES/CBC/NoPadding");
                        this.symDecryptor = CipherUtilities.GetCipher("AES/CBC/NoPadding");
                        this.asymLocalKeySize = this.LocalPrivateKey.Modulus.BitLength;
                        this.asymRemoteKeySize = this.RemotePublicKey.Modulus.BitLength;
                        this.asymLocalPlainTextBlockSize = Math.Max((this.asymLocalKeySize / 8) - 11, 1);
                        this.asymRemotePlainTextBlockSize = Math.Max((this.asymRemoteKeySize / 8) - 11, 1);
                        this.symSignatureSize = 20;
                        this.symSignatureKeySize = 16;
                        this.symEncryptionBlockSize = 16;
                        this.symEncryptionKeySize = 16;
                        break;

                    case SecurityPolicyUris.Basic256:

                        this.asymSigner = SignerUtilities.GetSigner("SHA-1withRSA");
                        this.asymSigner.Init(true, this.LocalPrivateKey);
                        this.asymVerifier = SignerUtilities.GetSigner("SHA-1withRSA");
                        this.asymVerifier.Init(false, this.RemotePublicKey);
                        this.asymEncryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        this.asymEncryptor.Init(true, this.RemotePublicKey);
                        this.asymDecryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        this.asymDecryptor.Init(false, this.LocalPrivateKey);
                        this.symSigner = new HMac(new Sha1Digest());
                        this.symVerifier = new HMac(new Sha1Digest());
                        this.symEncryptor = CipherUtilities.GetCipher("AES/CBC/NoPadding");
                        this.symDecryptor = CipherUtilities.GetCipher("AES/CBC/NoPadding");
                        this.asymLocalKeySize = this.LocalPrivateKey.Modulus.BitLength;
                        this.asymRemoteKeySize = this.RemotePublicKey.Modulus.BitLength;
                        this.asymLocalPlainTextBlockSize = Math.Max((this.asymLocalKeySize / 8) - 42, 1);
                        this.asymRemotePlainTextBlockSize = Math.Max((this.asymRemoteKeySize / 8) - 42, 1);
                        this.symSignatureSize = 20;
                        this.symSignatureKeySize = 24;
                        this.symEncryptionBlockSize = 16;
                        this.symEncryptionKeySize = 32;
                        break;

                    case SecurityPolicyUris.Basic256Sha256:

                        this.asymSigner = SignerUtilities.GetSigner("SHA-256withRSA");
                        this.asymSigner.Init(true, this.LocalPrivateKey);
                        this.asymVerifier = SignerUtilities.GetSigner("SHA-256withRSA");
                        this.asymVerifier.Init(false, this.RemotePublicKey);
                        this.asymEncryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        this.asymEncryptor.Init(true, this.RemotePublicKey);
                        this.asymDecryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        this.asymDecryptor.Init(false, this.LocalPrivateKey);
                        this.symSigner = new HMac(new Sha256Digest());
                        this.symVerifier = new HMac(new Sha256Digest());
                        this.symEncryptor = CipherUtilities.GetCipher("AES/CBC/NoPadding");
                        this.symDecryptor = CipherUtilities.GetCipher("AES/CBC/NoPadding");
                        this.asymLocalKeySize = this.LocalPrivateKey.Modulus.BitLength;
                        this.asymRemoteKeySize = this.RemotePublicKey.Modulus.BitLength;
                        this.asymLocalPlainTextBlockSize = Math.Max((this.asymLocalKeySize / 8) - 42, 1);
                        this.asymRemotePlainTextBlockSize = Math.Max((this.asymRemoteKeySize / 8) - 42, 1);
                        this.symSignatureSize = 32;
                        this.symSignatureKeySize = 32;
                        this.symEncryptionBlockSize = 16;
                        this.symEncryptionKeySize = 32;
                        break;

                    default:
                        throw new ServiceResultException(StatusCodes.BadSecurityPolicyRejected);
                }

                this.asymIsSigned = this.asymIsEncrypted = true;
                this.symIsSigned = true;
                this.symIsEncrypted = true;
                this.asymLocalSignatureSize = this.asymLocalKeySize / 8;
                this.asymLocalCipherTextBlockSize = Math.Max(this.asymLocalKeySize / 8, 1);
                this.asymRemoteSignatureSize = this.asymRemoteKeySize / 8;
                this.asymRemoteCipherTextBlockSize = Math.Max(this.asymRemoteKeySize / 8, 1);
                this.clientSigningKey = new byte[this.symSignatureKeySize];
                this.clientEncryptingKey = new byte[this.symEncryptionKeySize];
                this.clientInitializationVector = new byte[this.symEncryptionBlockSize];
                this.serverSigningKey = new byte[this.symSignatureKeySize];
                this.serverEncryptingKey = new byte[this.symEncryptionKeySize];
                this.serverInitializationVector = new byte[this.symEncryptionBlockSize];
                this.encryptionBuffer = new byte[this.LocalSendBufferSize];
                this.thumbprintDigest = DigestUtilities.GetDigest("SHA-1");
            }
            else if (this.RemoteEndpoint.SecurityMode == MessageSecurityMode.Sign)
            {
                if (this.LocalCertificate == null && this.CertificateStore != null)
                {
                    var tuple = this.CertificateStore.GetLocalCertificate(this.LocalDescription);
                    this.LocalCertificate = tuple.Item1.GetEncoded();
                    this.LocalPrivateKey = tuple.Item2;
                }

                if (this.LocalPrivateKey == null)
                {
                    throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed, "LocalPrivateKey is null.");
                }

                if (this.RemotePublicKey == null)
                {
                    throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed, "RemotePublicKey is null.");
                }

                this.RemotePublicKey = this.certificateParser.ReadCertificate(this.RemoteCertificate)?.GetPublicKey() as RsaKeyParameters;

                switch (this.RemoteEndpoint.SecurityPolicyUri)
                {
                    case SecurityPolicyUris.Basic128Rsa15:

                        this.asymSigner = SignerUtilities.GetSigner("SHA-1withRSA");
                        this.asymSigner.Init(true, this.LocalPrivateKey);
                        this.asymVerifier = SignerUtilities.GetSigner("SHA-1withRSA");
                        this.asymVerifier.Init(false, this.RemotePublicKey);
                        this.asymEncryptor = CipherUtilities.GetCipher("RSA//PKCS1Padding");
                        this.asymEncryptor.Init(true, this.RemotePublicKey);
                        this.asymDecryptor = CipherUtilities.GetCipher("RSA//PKCS1Padding");
                        this.asymDecryptor.Init(false, this.LocalPrivateKey);
                        this.symSigner = new HMac(new Sha1Digest());
                        this.symVerifier = new HMac(new Sha1Digest());
                        this.asymLocalKeySize = this.LocalPrivateKey.Modulus.BitLength;
                        this.asymRemoteKeySize = this.RemotePublicKey.Modulus.BitLength;
                        this.asymLocalPlainTextBlockSize = Math.Max((this.asymLocalKeySize / 8) - 11, 1);
                        this.asymRemotePlainTextBlockSize = Math.Max((this.asymRemoteKeySize / 8) - 11, 1);
                        this.symSignatureSize = 20;
                        this.symSignatureKeySize = 16;
                        this.symEncryptionBlockSize = 16;
                        this.symEncryptionKeySize = 16;
                        break;

                    case SecurityPolicyUris.Basic256:

                        this.asymSigner = SignerUtilities.GetSigner("SHA-1withRSA");
                        this.asymSigner.Init(true, this.LocalPrivateKey);
                        this.asymVerifier = SignerUtilities.GetSigner("SHA-1withRSA");
                        this.asymVerifier.Init(false, this.RemotePublicKey);
                        this.asymEncryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        this.asymEncryptor.Init(true, this.RemotePublicKey);
                        this.asymDecryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        this.asymDecryptor.Init(false, this.LocalPrivateKey);
                        this.symSigner = new HMac(new Sha1Digest());
                        this.symVerifier = new HMac(new Sha1Digest());
                        this.asymLocalKeySize = this.LocalPrivateKey.Modulus.BitLength;
                        this.asymRemoteKeySize = this.RemotePublicKey.Modulus.BitLength;
                        this.asymLocalPlainTextBlockSize = Math.Max((this.asymLocalKeySize / 8) - 42, 1);
                        this.asymRemotePlainTextBlockSize = Math.Max((this.asymRemoteKeySize / 8) - 42, 1);
                        this.symSignatureSize = 20;
                        this.symSignatureKeySize = 24;
                        this.symEncryptionBlockSize = 16;
                        this.symEncryptionKeySize = 32;
                        break;

                    case SecurityPolicyUris.Basic256Sha256:

                        this.asymSigner = SignerUtilities.GetSigner("SHA-256withRSA");
                        this.asymSigner.Init(true, this.LocalPrivateKey);
                        this.asymVerifier = SignerUtilities.GetSigner("SHA-256withRSA");
                        this.asymVerifier.Init(false, this.RemotePublicKey);
                        this.asymEncryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        this.asymEncryptor.Init(true, this.RemotePublicKey);
                        this.asymDecryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        this.asymDecryptor.Init(false, this.LocalPrivateKey);
                        this.symSigner = new HMac(new Sha256Digest());
                        this.symVerifier = new HMac(new Sha256Digest());
                        this.asymLocalKeySize = this.LocalPrivateKey.Modulus.BitLength;
                        this.asymRemoteKeySize = this.RemotePublicKey.Modulus.BitLength;
                        this.asymLocalPlainTextBlockSize = Math.Max((this.asymLocalKeySize / 8) - 42, 1);
                        this.asymRemotePlainTextBlockSize = Math.Max((this.asymRemoteKeySize / 8) - 42, 1);
                        this.symSignatureSize = 32;
                        this.symSignatureKeySize = 32;
                        this.symEncryptionBlockSize = 16;
                        this.symEncryptionKeySize = 32;
                        break;

                    default:
                        throw new ServiceResultException(StatusCodes.BadSecurityPolicyRejected);
                }

                this.asymIsSigned = this.asymIsEncrypted = true;
                this.symIsSigned = true;
                this.symIsEncrypted = false;
                this.asymLocalSignatureSize = this.asymLocalKeySize / 8;
                this.asymLocalCipherTextBlockSize = Math.Max(this.asymLocalKeySize / 8, 1);
                this.asymRemoteSignatureSize = this.asymRemoteKeySize / 8;
                this.asymRemoteCipherTextBlockSize = Math.Max(this.asymRemoteKeySize / 8, 1);
                this.clientSigningKey = new byte[this.symSignatureKeySize];
                this.clientEncryptingKey = new byte[this.symEncryptionKeySize];
                this.clientInitializationVector = new byte[this.symEncryptionBlockSize];
                this.serverSigningKey = new byte[this.symSignatureKeySize];
                this.serverEncryptingKey = new byte[this.symEncryptionKeySize];
                this.serverInitializationVector = new byte[this.symEncryptionBlockSize];
                this.encryptionBuffer = new byte[this.LocalSendBufferSize];
                this.thumbprintDigest = DigestUtilities.GetDigest("SHA-1");
            }
            else if (this.RemoteEndpoint.SecurityMode == MessageSecurityMode.None)
            {
                this.asymIsSigned = this.asymIsEncrypted = false;
                this.symIsSigned = this.symIsEncrypted = false;
                this.asymLocalKeySize = 0;
                this.asymRemoteKeySize = 0;
                this.asymLocalSignatureSize = 0;
                this.asymLocalCipherTextBlockSize = 1;
                this.asymRemoteSignatureSize = 0;
                this.asymRemoteCipherTextBlockSize = 1;
                this.asymLocalPlainTextBlockSize = 1;
                this.asymRemotePlainTextBlockSize = 1;
                this.symSignatureSize = 0;
                this.symSignatureKeySize = 0;
                this.symEncryptionBlockSize = 1;
                this.symEncryptionKeySize = 0;
                this.encryptionBuffer = null;
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
            this.sendBuffer = new byte[this.LocalSendBufferSize];
            this.receiveBuffer = new byte[this.LocalReceiveBufferSize];

            this.receiveResponsesTask = this.ReceiveResponsesAsync();

            var openSecureChannelRequest = new OpenSecureChannelRequest
            {
                RequestHeader = new RequestHeader { TimeoutHint = this.TimeoutHint, ReturnDiagnostics = this.DiagnosticsHint, Timestamp = DateTime.UtcNow, RequestHandle = this.GetNextHandle() },
                ClientProtocolVersion = ProtocolVersion,
                RequestType = SecurityTokenRequestType.Issue,
                SecurityMode = this.RemoteEndpoint.SecurityMode,
                ClientNonce = this.symIsSigned ? this.LocalNonce = this.GetNextNonce(this.symEncryptionKeySize) : null,
                RequestedLifetime = TokenRequestedLifetime
            };

            var openSecureChannelResponse = (OpenSecureChannelResponse)await this.RequestAsync(openSecureChannelRequest).ConfigureAwait(false);

            if (openSecureChannelResponse.ServerProtocolVersion < ProtocolVersion)
            {
                throw new ServiceResultException(StatusCodes.BadProtocolVersionUnsupported);
            }

            // Schedule token renewal.
            this.tokenRenewalTime = DateTime.UtcNow.AddMilliseconds(0.8 * openSecureChannelResponse.SecurityToken.RevisedLifetime);
        }

        protected override async Task OnCloseAsync(CancellationToken token)
        {
            this.channelCts?.Cancel();
            var closeSecureChannelRequest = new CloseSecureChannelRequest
            {
                RequestHeader = new RequestHeader { TimeoutHint = this.TimeoutHint, ReturnDiagnostics = this.DiagnosticsHint, Timestamp = DateTime.UtcNow, RequestHandle = this.GetNextHandle() },
            };
            await this.SendRequestAsync(new ServiceOperation(closeSecureChannelRequest)).ConfigureAwait(false);

            await base.OnCloseAsync(token).ConfigureAwait(false);
        }

        protected override Task OnFaulted(CancellationToken token = default(CancellationToken))
        {
            this.channelCts?.Cancel();
            return base.OnFaulted(token);
        }

        protected override async Task OnClosedAsync(CancellationToken token)
        {
            if (this.receiveResponsesTask != null && !this.receiveResponsesTask.IsCompleted)
            {
                this.Logger?.LogTrace("Waiting for socket to close.");
                var t = await Task.WhenAny(this.receiveResponsesTask, Task.Delay(2000)).ConfigureAwait(false);
                if (t != this.receiveResponsesTask)
                {
                    this.Logger?.LogError("Timeout while waiting for socket to close.");
                }
            }
            this.channelCts?.Dispose();
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
            var token = this.channelCts.Token;
            try
            {
                if (!operation.Task.IsCompleted)
                {
                    await this.SendRequestAsync(operation, token).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                {
                    await this.FaultAsync(ex).ConfigureAwait(false);
                    await this.AbortAsync().ConfigureAwait(false);
                }
            }
        }

        private async Task SendRequestAsync(ServiceOperation operation, CancellationToken token = default(CancellationToken))
        {
            await this.sendingSemaphore.WaitAsync(token).ConfigureAwait(false);
            var request = operation.Request;
            try
            {
                this.ThrowIfClosedOrNotOpening();

                // Check if time to renew security token.
                if (DateTime.UtcNow > this.tokenRenewalTime)
                {
                    this.tokenRenewalTime = this.tokenRenewalTime.AddMilliseconds(60000);
                    var openSecureChannelRequest = new OpenSecureChannelRequest
                    {
                        RequestHeader = new RequestHeader
                        {
                            TimeoutHint = this.TimeoutHint,
                            ReturnDiagnostics = this.DiagnosticsHint,
                            Timestamp = DateTime.UtcNow,
                            RequestHandle = this.GetNextHandle(),
                            AuthenticationToken = this.AuthenticationToken
                        },
                        ClientProtocolVersion = ProtocolVersion,
                        RequestType = SecurityTokenRequestType.Renew,
                        SecurityMode = this.RemoteEndpoint.SecurityMode,
                        ClientNonce = this.symIsSigned ? this.LocalNonce = this.GetNextNonce(this.symEncryptionKeySize) : null,
                        RequestedLifetime = TokenRequestedLifetime
                    };
                    this.Logger?.LogTrace($"Sending {openSecureChannelRequest.GetType().Name} Handle: {openSecureChannelRequest.RequestHeader.RequestHandle}");
                    this.pendingCompletions.TryAdd(openSecureChannelRequest.RequestHeader.RequestHandle, new ServiceOperation(openSecureChannelRequest));
                    await this.SendOpenSecureChannelRequestAsync(openSecureChannelRequest, token).ConfigureAwait(false);
                }

                request.RequestHeader.RequestHandle = this.GetNextHandle();
                request.RequestHeader.AuthenticationToken = this.AuthenticationToken;

                this.Logger?.LogTrace($"Sending {request.GetType().Name} Handle: {request.RequestHeader.RequestHandle}");
                this.pendingCompletions.TryAdd(request.RequestHeader.RequestHandle, operation);
                if (request is OpenSecureChannelRequest)
                {
                    await this.SendOpenSecureChannelRequestAsync((OpenSecureChannelRequest)request, token).ConfigureAwait(false);
                }
                else if (request is CloseSecureChannelRequest)
                {
                    await this.SendCloseSecureChannelRequestAsync((CloseSecureChannelRequest)request, token).ConfigureAwait(false);
                }
                else
                {
                    await this.SendServiceRequestAsync(request, token).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                this.Logger?.LogError($"Error sending {request.GetType().Name} Handle: {request.RequestHeader.RequestHandle}. {ex.Message}");
                throw;
            }
            finally
            {
                this.sendingSemaphore.Release();
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
                if (bodyStream.Length > this.RemoteMaxMessageSize)
                {
                    throw new ServiceResultException(StatusCodes.BadEncodingLimitsExceeded);
                }

                // write chunks
                int chunkCount = 0;
                int bodyCount = (int)(bodyStream.Length - bodyStream.Position);
                while (bodyCount > 0)
                {
                    chunkCount++;
                    if (this.RemoteMaxChunkCount > 0 && chunkCount > this.RemoteMaxChunkCount)
                    {
                        throw new ServiceResultException(StatusCodes.BadEncodingLimitsExceeded);
                    }

                    var stream = new MemoryStream(this.sendBuffer, 0, (int)this.RemoteReceiveBufferSize, true, true);
                    var encoder = new BinaryEncoder(stream, this);
                    try
                    {
                        // header
                        encoder.WriteUInt32(null, UaTcpMessageTypes.OPNF);
                        encoder.WriteUInt32(null, 0u);
                        encoder.WriteUInt32(null, this.ChannelId);

                        // asymmetric security header
                        encoder.WriteString(null, this.RemoteEndpoint.SecurityPolicyUri);
                        if (this.RemoteEndpoint.SecurityMode != MessageSecurityMode.None)
                        {
                            encoder.WriteByteString(null, this.LocalCertificate);
                            byte[] thumbprint = new byte[this.thumbprintDigest.GetDigestSize()];
                            this.thumbprintDigest.BlockUpdate(this.RemoteCertificate, 0, this.RemoteCertificate.Length);
                            this.thumbprintDigest.DoFinal(thumbprint, 0);
                            encoder.WriteByteString(null, thumbprint);
                        }
                        else
                        {
                            encoder.WriteByteString(null, null);
                            encoder.WriteByteString(null, null);
                        }

                        int plainHeaderSize = encoder.Position;

                        // sequence header
                        encoder.WriteUInt32(null, this.GetNextSequenceNumber());
                        encoder.WriteUInt32(null, request.RequestHeader.RequestHandle);

                        // body
                        int paddingHeaderSize;
                        int maxBodySize;
                        int bodySize;
                        int paddingSize;
                        int chunkSize;
                        if (this.asymIsEncrypted)
                        {
                            paddingHeaderSize = this.asymRemoteCipherTextBlockSize > 256 ? 2 : 1;
                            maxBodySize = ((((int)this.RemoteReceiveBufferSize - plainHeaderSize - this.asymLocalSignatureSize - paddingHeaderSize) / this.asymRemoteCipherTextBlockSize) * this.asymRemotePlainTextBlockSize) - SequenceHeaderSize;
                            if (bodyCount < maxBodySize)
                            {
                                bodySize = bodyCount;
                                paddingSize = (this.asymRemotePlainTextBlockSize - ((SequenceHeaderSize + bodySize + paddingHeaderSize + this.asymLocalSignatureSize) % this.asymRemotePlainTextBlockSize)) % this.asymRemotePlainTextBlockSize;
                            }
                            else
                            {
                                bodySize = maxBodySize;
                                paddingSize = 0;
                            }

                            chunkSize = plainHeaderSize + (((SequenceHeaderSize + bodySize + paddingSize + paddingHeaderSize + this.asymLocalSignatureSize) / this.asymRemotePlainTextBlockSize) * this.asymRemoteCipherTextBlockSize);
                        }
                        else
                        {
                            paddingHeaderSize = 0;
                            paddingSize = 0;
                            maxBodySize = (int)this.RemoteReceiveBufferSize - plainHeaderSize - this.asymLocalSignatureSize - SequenceHeaderSize;
                            if (bodyCount < maxBodySize)
                            {
                                bodySize = bodyCount;
                            }
                            else
                            {
                                bodySize = maxBodySize;
                            }

                            chunkSize = plainHeaderSize + SequenceHeaderSize + bodySize + this.asymLocalSignatureSize;
                        }

                        bodyStream.Read(this.sendBuffer, encoder.Position, bodySize);
                        encoder.Position += bodySize;
                        bodyCount -= bodySize;

                        // padding
                        if (this.asymIsEncrypted)
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
                        if (this.asymIsSigned)
                        {
                            // sign with local private key.
                            this.asymSigner.BlockUpdate(this.sendBuffer, 0, position);
                            byte[] signature = this.asymSigner.GenerateSignature();
                            Debug.Assert(signature.Length == this.asymLocalSignatureSize, nameof(this.asymLocalSignatureSize));
                            encoder.Write(signature, 0, this.asymLocalSignatureSize);
                        }

                        // encrypt
                        if (this.asymIsEncrypted)
                        {
                            position = encoder.Position;
                            Buffer.BlockCopy(this.sendBuffer, 0, this.encryptionBuffer, 0, plainHeaderSize);
                            byte[] plainText = new byte[this.asymRemotePlainTextBlockSize];
                            int jj = plainHeaderSize;
                            for (int ii = plainHeaderSize; ii < position; ii += this.asymRemotePlainTextBlockSize)
                            {
                                Buffer.BlockCopy(this.sendBuffer, ii, plainText, 0, this.asymRemotePlainTextBlockSize);

                                // encrypt with remote public key.
                                byte[] cipherText = this.asymEncryptor.DoFinal(plainText);
                                Debug.Assert(cipherText.Length == this.asymRemoteCipherTextBlockSize, nameof(this.asymRemoteCipherTextBlockSize));
                                Buffer.BlockCopy(cipherText, 0, this.encryptionBuffer, jj, this.asymRemoteCipherTextBlockSize);
                                jj += this.asymRemoteCipherTextBlockSize;
                            }

                            await this.SendAsync(this.encryptionBuffer, 0, jj, token).ConfigureAwait(false);
                            return;
                        }

                        // pass buffer to transport
                        await this.SendAsync(this.sendBuffer, 0, encoder.Position, token).ConfigureAwait(false);
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
                if (bodyStream.Length > this.RemoteMaxMessageSize)
                {
                    throw new ServiceResultException(StatusCodes.BadEncodingLimitsExceeded);
                }

                // write chunks
                int chunkCount = 0;
                int bodyCount = (int)(bodyStream.Length - bodyStream.Position);
                while (bodyCount > 0)
                {
                    chunkCount++;
                    if (this.RemoteMaxChunkCount > 0 && chunkCount > this.RemoteMaxChunkCount)
                    {
                        throw new ServiceResultException(StatusCodes.BadEncodingLimitsExceeded);
                    }

                    var stream = new MemoryStream(this.sendBuffer, 0, (int)this.RemoteReceiveBufferSize, true, true);
                    var encoder = new BinaryEncoder(stream, this);
                    try
                    {
                        // header
                        encoder.WriteUInt32(null, UaTcpMessageTypes.CLOF);
                        encoder.WriteUInt32(null, 0u);
                        encoder.WriteUInt32(null, this.ChannelId);

                        // symmetric security header
                        encoder.WriteUInt32(null, this.TokenId);

                        // detect new TokenId
                        if (this.TokenId != this.currentClientTokenId)
                        {
                            this.currentClientTokenId = this.TokenId;

                            // update signer and encrypter with new symmetric keys
                            if (this.symIsSigned)
                            {
                                this.symSigner.Init(new KeyParameter(this.clientSigningKey));
                                if (this.symIsEncrypted)
                                {
                                    this.symEncryptor.Init(
                                        true,
                                        new ParametersWithIV(new KeyParameter(this.clientEncryptingKey), this.clientInitializationVector));
                                }
                            }
                        }

                        int plainHeaderSize = encoder.Position;

                        // sequence header
                        encoder.WriteUInt32(null, this.GetNextSequenceNumber());
                        encoder.WriteUInt32(null, request.RequestHeader.RequestHandle);

                        // body
                        int paddingHeaderSize;
                        int maxBodySize;
                        int bodySize;
                        int paddingSize;
                        int chunkSize;
                        if (this.symIsEncrypted)
                        {
                            paddingHeaderSize = this.symEncryptionBlockSize > 256 ? 2 : 1;
                            maxBodySize = ((((int)this.RemoteReceiveBufferSize - plainHeaderSize - this.symSignatureSize - paddingHeaderSize) / this.symEncryptionBlockSize) * this.symEncryptionBlockSize) - SequenceHeaderSize;
                            if (bodyCount < maxBodySize)
                            {
                                bodySize = bodyCount;
                                paddingSize = (this.symEncryptionBlockSize - ((SequenceHeaderSize + bodySize + paddingHeaderSize + this.symSignatureSize) % this.symEncryptionBlockSize)) % this.symEncryptionBlockSize;
                            }
                            else
                            {
                                bodySize = maxBodySize;
                                paddingSize = 0;
                            }

                            chunkSize = plainHeaderSize + (((SequenceHeaderSize + bodySize + paddingSize + paddingHeaderSize + this.symSignatureSize) / this.symEncryptionBlockSize) * this.symEncryptionBlockSize);
                        }
                        else
                        {
                            paddingHeaderSize = 0;
                            paddingSize = 0;
                            maxBodySize = (int)this.RemoteReceiveBufferSize - plainHeaderSize - this.symSignatureSize - SequenceHeaderSize;
                            if (bodyCount < maxBodySize)
                            {
                                bodySize = bodyCount;
                            }
                            else
                            {
                                bodySize = maxBodySize;
                            }

                            chunkSize = plainHeaderSize + SequenceHeaderSize + bodySize + this.symSignatureSize;
                        }

                        bodyStream.Read(this.sendBuffer, encoder.Position, bodySize);
                        encoder.Position += bodySize;
                        bodyCount -= bodySize;

                        // padding
                        if (this.symIsEncrypted)
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
                        if (this.symIsSigned)
                        {
                            this.symSigner.BlockUpdate(this.sendBuffer, 0, position);
                            byte[] signature = new byte[this.symSigner.GetMacSize()];
                            this.symSigner.DoFinal(signature, 0);
                            encoder.Write(signature, 0, signature.Length);
                        }

                        // encrypt
                        position = encoder.Position;
                        if (this.symIsEncrypted)
                        {
                            int inputCount = position - plainHeaderSize;
                            Debug.Assert(inputCount % this.symEncryptor.GetBlockSize() == 0, "Input data is not an even number of encryption blocks.");
                            this.symEncryptor.DoFinal(this.sendBuffer, plainHeaderSize, inputCount, this.sendBuffer, plainHeaderSize);
                        }

                        // pass buffer to transport
                        await this.SendAsync(this.sendBuffer, 0, position, token).ConfigureAwait(false);
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

                bodyEncoder.WriteNodeId(null, ExpandedNodeId.ToNodeId(binaryEncodingId, this.NamespaceUris));
                request.Encode(bodyEncoder);
                bodyStream.Position = 0;
                if (bodyStream.Length > this.RemoteMaxMessageSize)
                {
                    throw new ServiceResultException(StatusCodes.BadEncodingLimitsExceeded);
                }

                // write chunks
                int chunkCount = 0;
                int bodyCount = (int)(bodyStream.Length - bodyStream.Position);
                while (bodyCount > 0)
                {
                    chunkCount++;
                    if (this.RemoteMaxChunkCount > 0 && chunkCount > this.RemoteMaxChunkCount)
                    {
                        throw new ServiceResultException(StatusCodes.BadEncodingLimitsExceeded);
                    }

                    var stream = new MemoryStream(this.sendBuffer, 0, (int)this.RemoteReceiveBufferSize, true, true);
                    var encoder = new BinaryEncoder(stream, this);
                    try
                    {
                        // header
                        encoder.WriteUInt32(null, UaTcpMessageTypes.MSGF);
                        encoder.WriteUInt32(null, 0u);
                        encoder.WriteUInt32(null, this.ChannelId);

                        // symmetric security header
                        encoder.WriteUInt32(null, this.TokenId);

                        // detect new TokenId
                        if (this.TokenId != this.currentClientTokenId)
                        {
                            this.currentClientTokenId = this.TokenId;

                            // update signer and encrypter with new symmetric keys
                            if (this.symIsSigned)
                            {
                                this.symSigner.Init(new KeyParameter(this.clientSigningKey));
                                if (this.symIsEncrypted)
                                {
                                    this.symEncryptor.Init(
                                        true,
                                        new ParametersWithIV(new KeyParameter(this.clientEncryptingKey), this.clientInitializationVector));
                                }
                            }
                        }

                        int plainHeaderSize = encoder.Position;

                        // sequence header
                        encoder.WriteUInt32(null, this.GetNextSequenceNumber());
                        encoder.WriteUInt32(null, request.RequestHeader.RequestHandle);

                        // body
                        int paddingHeaderSize;
                        int maxBodySize;
                        int bodySize;
                        int paddingSize;
                        int chunkSize;
                        if (this.symIsEncrypted)
                        {
                            paddingHeaderSize = this.symEncryptionBlockSize > 256 ? 2 : 1;
                            maxBodySize = ((((int)this.RemoteReceiveBufferSize - plainHeaderSize - this.symSignatureSize - paddingHeaderSize) / this.symEncryptionBlockSize) * this.symEncryptionBlockSize) - SequenceHeaderSize;
                            if (bodyCount < maxBodySize)
                            {
                                bodySize = bodyCount;
                                paddingSize = (this.symEncryptionBlockSize - ((SequenceHeaderSize + bodySize + paddingHeaderSize + this.symSignatureSize) % this.symEncryptionBlockSize)) % this.symEncryptionBlockSize;
                            }
                            else
                            {
                                bodySize = maxBodySize;
                                paddingSize = 0;
                            }

                            chunkSize = plainHeaderSize + (((SequenceHeaderSize + bodySize + paddingSize + paddingHeaderSize + this.symSignatureSize) / this.symEncryptionBlockSize) * this.symEncryptionBlockSize);
                        }
                        else
                        {
                            paddingHeaderSize = 0;
                            paddingSize = 0;
                            maxBodySize = (int)this.RemoteReceiveBufferSize - plainHeaderSize - this.symSignatureSize - SequenceHeaderSize;
                            if (bodyCount < maxBodySize)
                            {
                                bodySize = bodyCount;
                            }
                            else
                            {
                                bodySize = maxBodySize;
                            }

                            chunkSize = plainHeaderSize + SequenceHeaderSize + bodySize + this.symSignatureSize;
                        }

                        bodyStream.Read(this.sendBuffer, encoder.Position, bodySize);
                        encoder.Position += bodySize;
                        bodyCount -= bodySize;

                        // padding
                        if (this.symIsEncrypted)
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
                        if (this.symIsSigned)
                        {
                            this.symSigner.BlockUpdate(this.sendBuffer, 0, position);
                            byte[] signature = new byte[this.symSigner.GetMacSize()];
                            this.symSigner.DoFinal(signature, 0);
                            encoder.Write(signature, 0, signature.Length);
                        }

                        // encrypt
                        position = encoder.Position;
                        if (this.symIsEncrypted)
                        {
                            int inputCount = position - plainHeaderSize;
                            Debug.Assert(inputCount % this.symEncryptor.GetBlockSize() == 0, "Input data is not an even number of encryption blocks.");
                            this.symEncryptor.DoFinal(this.sendBuffer, plainHeaderSize, inputCount, this.sendBuffer, plainHeaderSize);
                        }

                        // pass buffer to transport
                        await this.SendAsync(this.sendBuffer, 0, position, token).ConfigureAwait(false);
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
                    var response = await this.ReceiveResponseAsync().ConfigureAwait(false);
                    if (response == null)
                    {
                        // Null response indicates socket closed. This is expected when closing secure channel.
                        if (this.State == CommunicationState.Closed || this.State == CommunicationState.Closing)
                        {
                            return;
                        }

                        throw new ServiceResultException(StatusCodes.BadServerNotConnected);
                    }
                    var header = response.ResponseHeader;
                    if (this.pendingCompletions.TryRemove(header.RequestHandle, out var tcs))
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
                    if (this.State == CommunicationState.Closed || this.State == CommunicationState.Closing)
                    {
                        return;
                    }

                    if (this.State == CommunicationState.Faulted)
                    {
                        return;
                    }
                    await this.FaultAsync(ex).ConfigureAwait(false);
                    await this.AbortAsync().ConfigureAwait(false);
                }
            }
        }

        protected async Task<IServiceResponse> ReceiveResponseAsync(CancellationToken token = default(CancellationToken))
        {
            await this.receivingSemaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                token.ThrowIfCancellationRequested();
                this.ThrowIfClosedOrNotOpening();
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
                        if (this.LocalMaxChunkCount > 0 && chunkCount > this.LocalMaxChunkCount)
                        {
                            throw new ServiceResultException(StatusCodes.BadEncodingLimitsExceeded);
                        }

                        var count = await this.ReceiveAsync(this.receiveBuffer, 0, (int)this.LocalReceiveBufferSize, token).ConfigureAwait(false);
                        if (count == 0)
                        {
                            return null;
                        }

                        var stream = new MemoryStream(this.receiveBuffer, 0, count, true, true);
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
                                    if (channelId != this.ChannelId)
                                    {
                                        throw new ServiceResultException(StatusCodes.BadTcpSecureChannelUnknown);
                                    }

                                    // symmetric security header
                                    var tokenId = decoder.ReadUInt32(null);

                                    // detect new token
                                    if (tokenId != this.currentServerTokenId)
                                    {
                                        this.currentServerTokenId = tokenId;

                                        // update with new keys
                                        if (this.symIsSigned)
                                        {
                                            this.symVerifier.Init(new KeyParameter(this.serverSigningKey));
                                            if (this.symIsEncrypted)
                                            {
                                                this.symDecryptor.Init(
                                                    false,
                                                    new ParametersWithIV(new KeyParameter(this.serverEncryptingKey), this.serverInitializationVector));
                                            }
                                        }

                                        this.Logger?.LogTrace($"Installed new security token {tokenId}.");
                                    }

                                    plainHeaderSize = decoder.Position;

                                    // decrypt
                                    if (this.symIsEncrypted)
                                    {
                                        int inputCount = messageLength - plainHeaderSize;
                                        Debug.Assert(inputCount % this.symDecryptor.GetBlockSize() == 0, "Input data is not an even number of encryption blocks.");
                                        this.symDecryptor.DoFinal(this.receiveBuffer, plainHeaderSize, inputCount, this.receiveBuffer, plainHeaderSize);
                                    }

                                    // verify
                                    if (this.symIsSigned)
                                    {
                                        var datalen = messageLength - this.symSignatureSize;

                                        this.symVerifier.BlockUpdate(this.receiveBuffer, 0, datalen);
                                        byte[] signature = new byte[this.symVerifier.GetMacSize()];
                                        this.symVerifier.DoFinal(signature, 0);

                                        if (!signature.SequenceEqual(this.receiveBuffer.AsArraySegment(datalen, this.symSignatureSize)))
                                        {
                                            throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed);
                                        }
                                    }

                                    // read sequence header
                                    sequenceNumber = decoder.ReadUInt32(null);
                                    requestId = decoder.ReadUInt32(null);

                                    // body
                                    if (this.symIsEncrypted)
                                    {
                                        if (this.symEncryptionBlockSize > 256)
                                        {
                                            paddingHeaderSize = 2;
                                            paddingSize = BitConverter.ToInt16(this.receiveBuffer, messageLength - this.symSignatureSize - paddingHeaderSize);
                                        }
                                        else
                                        {
                                            paddingHeaderSize = 1;
                                            paddingSize = this.receiveBuffer[messageLength - this.symSignatureSize - paddingHeaderSize];
                                        }

                                        bodySize = messageLength - plainHeaderSize - SequenceHeaderSize - paddingSize - paddingHeaderSize - this.symSignatureSize;
                                    }
                                    else
                                    {
                                        bodySize = messageLength - plainHeaderSize - SequenceHeaderSize - this.symSignatureSize;
                                    }

                                    bodyStream.Write(this.receiveBuffer, plainHeaderSize + SequenceHeaderSize, bodySize);
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
                                    if (this.asymIsEncrypted)
                                    {
                                        byte[] cipherTextBlock = new byte[this.asymLocalCipherTextBlockSize];
                                        int jj = plainHeaderSize;
                                        for (int ii = plainHeaderSize; ii < messageLength; ii += this.asymLocalCipherTextBlockSize)
                                        {
                                            Buffer.BlockCopy(this.receiveBuffer, ii, cipherTextBlock, 0, this.asymLocalCipherTextBlockSize);

                                            // decrypt with local private key.
                                            byte[] plainTextBlock = this.asymDecryptor.DoFinal(cipherTextBlock);
                                            Debug.Assert(plainTextBlock.Length == this.asymLocalPlainTextBlockSize, "Decrypted block length was not as expected.");
                                            Buffer.BlockCopy(plainTextBlock, 0, this.receiveBuffer, jj, this.asymLocalPlainTextBlockSize);
                                            jj += this.asymLocalPlainTextBlockSize;
                                        }

                                        messageLength = jj;
                                        decoder.Position = plainHeaderSize;
                                    }

                                    // verify
                                    if (this.asymIsSigned)
                                    {
                                        // verify with remote public key.
                                        var datalen = messageLength - this.asymRemoteSignatureSize;
                                        this.asymVerifier.BlockUpdate(this.receiveBuffer, 0, datalen);
                                        if (!this.asymVerifier.VerifySignature(this.receiveBuffer.AsArraySegment(datalen, this.asymRemoteSignatureSize).ToArray()))
                                        {
                                            throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed);
                                        }
                                    }

                                    // sequence header
                                    sequenceNumber = decoder.ReadUInt32(null);
                                    requestId = decoder.ReadUInt32(null);

                                    // body
                                    if (this.asymIsEncrypted)
                                    {
                                        if (this.asymLocalCipherTextBlockSize > 256)
                                        {
                                            paddingHeaderSize = 2;
                                            paddingSize = BitConverter.ToInt16(this.receiveBuffer, messageLength - this.asymRemoteSignatureSize - paddingHeaderSize);
                                        }
                                        else
                                        {
                                            paddingHeaderSize = 1;
                                            paddingSize = this.receiveBuffer[messageLength - this.asymRemoteSignatureSize - paddingHeaderSize];
                                        }

                                        bodySize = messageLength - plainHeaderSize - SequenceHeaderSize - paddingSize - paddingHeaderSize - this.asymRemoteSignatureSize;
                                    }
                                    else
                                    {
                                        bodySize = messageLength - plainHeaderSize - SequenceHeaderSize - this.asymRemoteSignatureSize;
                                    }

                                    bodyStream.Write(this.receiveBuffer, plainHeaderSize + SequenceHeaderSize, bodySize);
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

                            if (this.LocalMaxMessageSize > 0 && bodyStream.Position > this.LocalMaxMessageSize)
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
                        if (!BinaryEncodingIdToTypeDictionary.TryGetValue(NodeId.ToExpandedNodeId(nodeId, this.NamespaceUris), out var type2))
                        {
                            throw new ServiceResultException(StatusCodes.BadEncodingError, "NodeId not registered in dictionary.");
                        }

                        // create response
                        response = (IServiceResponse)Activator.CreateInstance(type2);
                    }

                    // set properties from message stream
                    response.Decode(bodyDecoder);

                    this.Logger?.LogTrace($"Received {response.GetType().Name} Handle: {response.ResponseHeader.RequestHandle} Result: {response.ResponseHeader.ServiceResult}");
                    // special inline processing for token renewal because we need to
                    // hold both the sending and receiving semaphores to update the security keys.
                    if (response is OpenSecureChannelResponse openSecureChannelResponse && StatusCode.IsGood(openSecureChannelResponse.ResponseHeader.ServiceResult))
                    {
                        this.tokenRenewalTime = DateTime.UtcNow.AddMilliseconds(0.8 * openSecureChannelResponse.SecurityToken.RevisedLifetime);

                        await this.sendingSemaphore.WaitAsync(token).ConfigureAwait(false);
                        try
                        {
                            this.ChannelId = openSecureChannelResponse.SecurityToken.ChannelId;
                            this.TokenId = openSecureChannelResponse.SecurityToken.TokenId;
                            if (this.symIsSigned)
                            {
                                var clientNonce = this.LocalNonce;
                                var serverNonce = openSecureChannelResponse.ServerNonce;

                                // (re)create client security keys for encrypting the next message sent
                                var clientSecurityKey = CalculatePSHA(serverNonce, clientNonce, this.symSignatureKeySize + this.symEncryptionKeySize + this.symEncryptionBlockSize, this.RemoteEndpoint.SecurityPolicyUri);
                                Buffer.BlockCopy(clientSecurityKey, 0, this.clientSigningKey, 0, this.symSignatureKeySize);
                                Buffer.BlockCopy(clientSecurityKey, this.symSignatureKeySize, this.clientEncryptingKey, 0, this.symEncryptionKeySize);
                                Buffer.BlockCopy(clientSecurityKey, this.symSignatureKeySize + this.symEncryptionKeySize, this.clientInitializationVector, 0, this.symEncryptionBlockSize);

                                // (re)create server security keys for decrypting the next message received that has a new TokenId
                                var serverSecurityKey = CalculatePSHA(clientNonce, serverNonce, this.symSignatureKeySize + this.symEncryptionKeySize + this.symEncryptionBlockSize, this.RemoteEndpoint.SecurityPolicyUri);
                                Buffer.BlockCopy(serverSecurityKey, 0, this.serverSigningKey, 0, this.symSignatureKeySize);
                                Buffer.BlockCopy(serverSecurityKey, this.symSignatureKeySize, this.serverEncryptingKey, 0, this.symEncryptionKeySize);
                                Buffer.BlockCopy(serverSecurityKey, this.symSignatureKeySize + this.symEncryptionKeySize, this.serverInitializationVector, 0, this.symEncryptionBlockSize);
                            }
                        }
                        finally
                        {
                            this.sendingSemaphore.Release();
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
                this.receivingSemaphore.Release();
            }
        }

        private void TimestampHeader(IServiceRequest request)
        {
            if (request.RequestHeader == null)
            {
                request.RequestHeader = new RequestHeader { TimeoutHint = this.TimeoutHint, ReturnDiagnostics = this.DiagnosticsHint };
            }

            request.RequestHeader.Timestamp = DateTime.UtcNow;
        }

        private uint GetNextHandle()
        {
            unchecked
            {
                int snapshot = this.handle;
                int value = snapshot + 1;
                if (value == 0)
                {
                    value = 1;
                }

                if (Interlocked.CompareExchange(ref this.handle, value, snapshot) != snapshot)
                {
                    var spinner = default(SpinWait);
                    do
                    {
                        spinner.SpinOnce();
                        snapshot = this.handle;
                        value = snapshot + 1;
                        if (value == 0)
                        {
                            value = 1;
                        }
                    }
                    while (Interlocked.CompareExchange(ref this.handle, value, snapshot) != snapshot);
                }

                return (uint)value;
            }
        }

        private uint GetNextSequenceNumber()
        {
            unchecked
            {
                return (uint)Interlocked.Increment(ref this.sequenceNumber);
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
                this.Logger?.LogTrace($"Canceled {request.GetType().Name} Handle: {request.RequestHeader.RequestHandle}");
            }
        }

        public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, ServiceOperation messageValue, ISourceBlock<ServiceOperation> source, bool consumeToAccept)
        {
            return ((ITargetBlock<ServiceOperation>)this.pendingRequests).OfferMessage(messageHeader, messageValue, source, consumeToAccept);
        }

        public void Complete()
        {
            this.pendingRequests.Complete();
        }

        public void Fault(Exception exception)
        {
            ((IDataflowBlock)this.pendingRequests).Fault(exception);
        }
    }
}