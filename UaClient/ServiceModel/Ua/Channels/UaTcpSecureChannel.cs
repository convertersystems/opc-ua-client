// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Xml.Linq;

namespace Workstation.ServiceModel.Ua.Channels
{
    /// <summary>
    /// A channel that opens a secure channel.
    /// </summary>
    public class UaTcpSecureChannel : UaTcpTransportChannel
    {
        public const uint DefaultTimeoutHint = 15 * 1000; // 15 seconds
        public const uint DefaultDiagnosticsHint = (uint)DiagnosticFlags.None;

        protected const int NonceLength = 32;
        private const int SequenceHeaderSize = 8;
        private const int TokenRequestedLifetime = 60 * 60 * 1000; // 1 hour

        private static readonly NodeId OpenSecureChannelRequestNodeId = NodeId.Parse(ObjectIds.OpenSecureChannelRequest_Encoding_DefaultBinary);
        private static readonly NodeId CloseSecureChannelRequestNodeId = NodeId.Parse(ObjectIds.CloseSecureChannelRequest_Encoding_DefaultBinary);
        private static readonly NodeId ReadResponseNodeId = NodeId.Parse(ObjectIds.ReadResponse_Encoding_DefaultBinary);
        private static readonly NodeId PublishResponseNodeId = NodeId.Parse(ObjectIds.PublishResponse_Encoding_DefaultBinary);
        private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();
        private static readonly MetroLog.ILogger Log = MetroLog.LogManagerFactory.DefaultLogManager.GetLogger<UaTcpSecureChannel>();

        private readonly CancellationTokenSource channelCts;
        private readonly Action<object> cancelRequest;
        private readonly BufferBlock<TaskCompletionSource<IServiceResponse>> pendingRequests;
        private readonly ConcurrentDictionary<uint, TaskCompletionSource<IServiceResponse>> pendingCompletions;
        private int handle;
        private int sequenceNumber;
        private uint currentClientTokenId;
        private uint currentServerTokenId;
        private byte[] clientSigningKey;
        private byte[] clientEncryptingKey;
        private byte[] clientInitializationVector;
        private byte[] currentClientEncryptingKey;
        private byte[] currentClientInitializationVector;
        private byte[] serverSigningKey;
        private byte[] serverEncryptingKey;
        private byte[] serverInitializationVector;
        private byte[] currentServerEncryptingKey;
        private byte[] currentServerInitializationVector;
        private byte[] encryptionBuffer;
        private DateTime tokenRenewalTime;
        private SemaphoreSlim sendingSemaphore;
        private SemaphoreSlim receivingSemaphore;
        private Task sendRequestsTask;
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
        private byte[] localCertificateBlob;
        private byte[] remoteCertificateBlob;
        private byte[] sendBuffer;
        private byte[] receiveBuffer;

        private SymmetricAlgorithm symEncryptionAlgorithm;
        private HMAC symVerifier;
        private HMAC symSigner;
        private ICryptoTransform symEncryptor;
        private ICryptoTransform symDecryptor;
        private RSAEncryptionPadding asymEncryptionPadding;
        private HashAlgorithmName asymSignatureHashAlgorithmName;

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
        /// <param name="localCertificate">The local certificate</param>
        /// <param name="remoteEndpoint">The remote endpoint</param>
        public UaTcpSecureChannel(ApplicationDescription localDescription, X509Certificate2 localCertificate, EndpointDescription remoteEndpoint)
            : base(remoteEndpoint)
        {
            if (localDescription == null)
            {
                throw new ArgumentNullException(nameof(localDescription));
            }

            this.LocalDescription = localDescription;
            this.LocalCertificate = localCertificate;
            this.TimeoutHint = DefaultTimeoutHint;
            this.DiagnosticsHint = DefaultDiagnosticsHint;
            this.AuthenticationToken = null;
            this.NamespaceUris = new List<string> { "http://opcfoundation.org/UA/" };
            this.ServerUris = new List<string>();
            this.channelCts = new CancellationTokenSource();
            this.cancelRequest = new Action<object>(this.CancelRequest);
            this.pendingRequests = new BufferBlock<TaskCompletionSource<IServiceResponse>>();
            this.pendingCompletions = new ConcurrentDictionary<uint, TaskCompletionSource<IServiceResponse>>();
            this.sendingSemaphore = new SemaphoreSlim(1, 1);
            this.receivingSemaphore = new SemaphoreSlim(1, 1);
            this.tokenRenewalTime = DateTime.MaxValue;
        }

        public static Dictionary<ExpandedNodeId, Type> BinaryEncodingIdToTypeDictionary { get; }

        public static Dictionary<Type, ExpandedNodeId> TypeToBinaryEncodingIdDictionary { get; }

        public static Dictionary<ExpandedNodeId, Type> DataTypeIdToTypeDictionary { get; }

        public ApplicationDescription LocalDescription { get; }

        protected X509Certificate2 LocalCertificate { get; set; }

        protected X509Certificate2 RemoteCertificate { get; set; }

        protected RSA LocalPrivateKey { get; set; }

        protected RSA RemotePublicKey { get; set; }

        public uint TimeoutHint { get; set; }

        public uint DiagnosticsHint { get; set; }

        public uint ChannelId { get; protected set; }

        public uint TokenId { get; protected set; }

        public NodeId AuthenticationToken { get; set; }

        public List<string> NamespaceUris { get; protected set; }

        public List<string> ServerUris { get; protected set; }

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
            this.UpdateHeader(request);
            var tcs = new TaskCompletionSource<IServiceResponse>(request);
            using (var timeoutCts = new CancellationTokenSource((int)request.RequestHeader.TimeoutHint))
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, this.channelCts.Token))
            using (var registration = linkedCts.Token.Register(this.cancelRequest, tcs, false))
            {
                this.pendingRequests.Post(tcs);
                return await tcs.Task.ConfigureAwait(false);
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

        protected static byte[] GetThumbprintBytes(string thumbprint)
        {
            if (thumbprint == null)
            {
                return null;
            }

            byte[] array = new byte[thumbprint.Length / 2];
            for (int i = 0; i < thumbprint.Length - 1; i += 2)
            {
                array[i / 2] = Convert.ToByte(thumbprint.Substring(i, 2), 16);
            }

            return array;
        }

        protected override async Task OnOpenAsync(CancellationToken token)
        {
            await base.OnOpenAsync(token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            this.sendBuffer = new byte[this.LocalSendBufferSize];
            this.receiveBuffer = new byte[this.LocalReceiveBufferSize];
            if (this.LocalCertificate != null)
            {
                this.localCertificateBlob = this.LocalCertificate.RawData;
            }

            this.remoteCertificateBlob = this.RemoteEndpoint.ServerCertificate;
            if (this.remoteCertificateBlob != null)
            {
                this.RemoteCertificate = new X509Certificate2(this.remoteCertificateBlob);
            }

            if (this.RemoteEndpoint.SecurityMode == MessageSecurityMode.SignAndEncrypt)
            {
                if (this.LocalCertificate == null)
                {
                    throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed, "LocalCertificate is null.");
                }

                if (this.RemoteCertificate == null)
                {
                    throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed, "RemoteCertificate is null.");
                }

                this.LocalPrivateKey = this.LocalCertificate.GetRSAPrivateKey();
                this.RemotePublicKey = this.RemoteCertificate.GetRSAPublicKey();

                switch (this.RemoteEndpoint.SecurityPolicyUri)
                {
                    case SecurityPolicyUris.Basic128Rsa15:

                        this.asymEncryptionPadding = RSAEncryptionPadding.Pkcs1;
                        this.asymSignatureHashAlgorithmName = HashAlgorithmName.SHA1;
                        this.symSigner = new HMACSHA1();
                        this.symVerifier = new HMACSHA1();
                        this.symEncryptionAlgorithm = Aes.Create();
                        this.symEncryptionAlgorithm.Mode = CipherMode.CBC;
                        this.symEncryptionAlgorithm.Padding = PaddingMode.None;
                        this.asymLocalKeySize = this.LocalPrivateKey.KeySize;
                        this.asymRemoteKeySize = this.RemotePublicKey.KeySize;
                        this.asymLocalPlainTextBlockSize = Math.Max((this.asymLocalKeySize / 8) - 11, 1);
                        this.asymRemotePlainTextBlockSize = Math.Max((this.asymRemoteKeySize / 8) - 11, 1);
                        this.symSignatureSize = 20;
                        this.symSignatureKeySize = 16;
                        this.symEncryptionBlockSize = 16;
                        this.symEncryptionKeySize = 16;
                        break;

                    case SecurityPolicyUris.Basic256:

                        this.asymEncryptionPadding = RSAEncryptionPadding.OaepSHA1;
                        this.asymSignatureHashAlgorithmName = HashAlgorithmName.SHA1;
                        this.symSigner = new HMACSHA1();
                        this.symVerifier = new HMACSHA1();
                        this.symEncryptionAlgorithm = Aes.Create();
                        this.symEncryptionAlgorithm.Mode = CipherMode.CBC;
                        this.symEncryptionAlgorithm.Padding = PaddingMode.None;
                        this.asymLocalKeySize = this.LocalPrivateKey.KeySize;
                        this.asymRemoteKeySize = this.RemotePublicKey.KeySize;
                        this.asymLocalPlainTextBlockSize = Math.Max((this.asymLocalKeySize / 8) - 42, 1);
                        this.asymRemotePlainTextBlockSize = Math.Max((this.asymRemoteKeySize / 8) - 42, 1);
                        this.symSignatureSize = 20;
                        this.symSignatureKeySize = 24;
                        this.symEncryptionBlockSize = 16;
                        this.symEncryptionKeySize = 32;
                        break;

                    case SecurityPolicyUris.Basic256Sha256:

                        this.asymEncryptionPadding = RSAEncryptionPadding.OaepSHA1;
                        this.asymSignatureHashAlgorithmName = HashAlgorithmName.SHA256;
                        this.symSigner = new HMACSHA256();
                        this.symVerifier = new HMACSHA256();
                        this.symEncryptionAlgorithm = Aes.Create();
                        this.symEncryptionAlgorithm.Mode = CipherMode.CBC;
                        this.symEncryptionAlgorithm.Padding = PaddingMode.None;
                        this.asymLocalKeySize = this.LocalPrivateKey.KeySize;
                        this.asymRemoteKeySize = this.RemotePublicKey.KeySize;
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
            }
            else if (this.RemoteEndpoint.SecurityMode == MessageSecurityMode.Sign)
            {
                if (this.LocalCertificate == null)
                {
                    throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed, "LocalCertificate is null.");
                }

                if (this.RemoteCertificate == null)
                {
                    throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed, "RemoteCertificate is null.");
                }

                this.LocalPrivateKey = this.LocalCertificate.GetRSAPrivateKey();
                this.RemotePublicKey = this.RemoteCertificate.GetRSAPublicKey();

                switch (this.RemoteEndpoint.SecurityPolicyUri)
                {
                    case SecurityPolicyUris.Basic128Rsa15:

                        this.asymEncryptionPadding = RSAEncryptionPadding.Pkcs1;
                        this.asymSignatureHashAlgorithmName = HashAlgorithmName.SHA1;
                        this.symSigner = new HMACSHA1();
                        this.symVerifier = new HMACSHA1();
                        this.asymLocalKeySize = this.LocalPrivateKey.KeySize;
                        this.asymRemoteKeySize = this.RemotePublicKey.KeySize;
                        this.asymLocalPlainTextBlockSize = Math.Max((this.asymLocalKeySize / 8) - 11, 1);
                        this.asymRemotePlainTextBlockSize = Math.Max((this.asymRemoteKeySize / 8) - 11, 1);
                        this.symSignatureSize = 20;
                        this.symSignatureKeySize = 16;
                        this.symEncryptionBlockSize = 16;
                        this.symEncryptionKeySize = 16;
                        break;

                    case SecurityPolicyUris.Basic256:

                        this.asymEncryptionPadding = RSAEncryptionPadding.OaepSHA1;
                        this.asymSignatureHashAlgorithmName = HashAlgorithmName.SHA1;
                        this.symSigner = new HMACSHA1();
                        this.symVerifier = new HMACSHA1();
                        this.asymLocalKeySize = this.LocalPrivateKey.KeySize;
                        this.asymRemoteKeySize = this.RemotePublicKey.KeySize;
                        this.asymLocalPlainTextBlockSize = Math.Max((this.asymLocalKeySize / 8) - 42, 1);
                        this.asymRemotePlainTextBlockSize = Math.Max((this.asymRemoteKeySize / 8) - 42, 1);
                        this.symSignatureSize = 20;
                        this.symSignatureKeySize = 24;
                        this.symEncryptionBlockSize = 16;
                        this.symEncryptionKeySize = 32;
                        break;

                    case SecurityPolicyUris.Basic256Sha256:

                        this.asymEncryptionPadding = RSAEncryptionPadding.OaepSHA1;
                        this.asymSignatureHashAlgorithmName = HashAlgorithmName.SHA256;
                        this.symSigner = new HMACSHA256();
                        this.symVerifier = new HMACSHA256();
                        this.asymLocalKeySize = this.LocalPrivateKey.KeySize;
                        this.asymRemoteKeySize = this.RemotePublicKey.KeySize;
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

            this.sendRequestsTask = Task.Run(() => this.SendRequestsAsync(this.channelCts.Token));
            this.receiveResponsesTask = Task.Run(() => this.ReceiveResponsesAsync(this.channelCts.Token));

            var openSecureChannelRequest = new OpenSecureChannelRequest
            {
                ClientProtocolVersion = ProtocolVersion,
                RequestType = SecurityTokenRequestType.Issue,
                SecurityMode = this.RemoteEndpoint.SecurityMode,
                ClientNonce = this.symIsSigned ? this.GetNextNonce() : null,
                RequestedLifetime = TokenRequestedLifetime
            };
            var openSecureChannelResponse = (OpenSecureChannelResponse)await this.RequestAsync(openSecureChannelRequest).ConfigureAwait(false);
            if (openSecureChannelResponse.ServerProtocolVersion < ProtocolVersion)
            {
                throw new ServiceResultException(StatusCodes.BadProtocolVersionUnsupported);
            }

            await this.sendingSemaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                this.ChannelId = openSecureChannelResponse.SecurityToken.ChannelId;
                this.TokenId = openSecureChannelResponse.SecurityToken.TokenId;
                this.tokenRenewalTime = DateTime.UtcNow.AddMilliseconds(0.75 * openSecureChannelResponse.SecurityToken.RevisedLifetime);
                if (this.symIsSigned)
                {
                    var clientNonce = openSecureChannelRequest.ClientNonce;
                    var serverNonce = openSecureChannelResponse.ServerNonce;

                    // (re)create client security keys for encrypting the next message sent
                    var clientSecurityKey = CalculatePSHA(serverNonce, clientNonce, this.symSignatureKeySize + this.symEncryptionKeySize + this.symEncryptionBlockSize, this.asymSignatureHashAlgorithmName);

                    Buffer.BlockCopy(clientSecurityKey, 0, this.clientSigningKey, 0, this.symSignatureKeySize);
                    Buffer.BlockCopy(clientSecurityKey, this.symSignatureKeySize, this.clientEncryptingKey, 0, this.symEncryptionKeySize);
                    Buffer.BlockCopy(clientSecurityKey, this.symSignatureKeySize + this.symEncryptionKeySize, this.clientInitializationVector, 0, this.symEncryptionBlockSize);

                    // (re)create server security keys for decrypting the next message received that has a new TokenId
                    var serverSecurityKey = CalculatePSHA(clientNonce, serverNonce, this.symSignatureKeySize + this.symEncryptionKeySize + this.symEncryptionBlockSize, this.asymSignatureHashAlgorithmName);
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

        protected override async Task OnCloseAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var closeSecureChannelRequest = new CloseSecureChannelRequest();
            await this.RequestAsync(closeSecureChannelRequest).ConfigureAwait(false);

            try
            {
                await Task.WhenAll(this.sendRequestsTask, this.receiveResponsesTask).WithCancellation(token).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
            }

            this.channelCts.Cancel();
            await base.OnCloseAsync(token).ConfigureAwait(false);
        }

        protected override Task OnAbortAsync(CancellationToken token)
        {
            this.channelCts.Cancel();
            return base.OnCloseAsync(token);
        }

        protected override Task OnClosedAsync(CancellationToken token)
        {
            this.channelCts.Dispose();
            this.symSigner?.Dispose();
            this.symVerifier?.Dispose();
            this.symEncryptor?.Dispose();
            this.symDecryptor?.Dispose();
            this.RemotePublicKey?.Dispose();
            this.LocalPrivateKey?.Dispose();
            return base.OnClosedAsync(token);
        }

        private static byte[] CalculatePSHA(byte[] secret, byte[] seed, int sizeBytes, HashAlgorithmName hashName)
        {
            using (var algorithm = (hashName == HashAlgorithmName.SHA256) ? (HMAC)new HMACSHA256() : new HMACSHA1())
            {
                algorithm.Key = secret;
                int hashSize = algorithm.HashSize / 8;
                int bufferSize = hashSize + seed.Length;
                int i = 0;
                byte[] b1 = seed;
                byte[] b2 = new byte[bufferSize];
                byte[] temp = null;
                byte[] psha = new byte[sizeBytes];

                while (i < sizeBytes)
                {
                    b1 = algorithm.ComputeHash(b1);

                    b1.CopyTo(b2, 0);
                    seed.CopyTo(b2, hashSize);

                    temp = algorithm.ComputeHash(b2);

                    for (int j = 0; j < temp.Length; j++)
                    {
                        if (i < sizeBytes)
                        {
                            psha[i] = temp[j];
                            i++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                return psha;
            }
        }

        private async Task SendRequestsAsync(CancellationToken token = default(CancellationToken))
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var tcs = await this.pendingRequests.ReceiveAsync(token).ConfigureAwait(false);
                    var task = tcs.Task;
                    var request = (IServiceRequest)task.AsyncState;
                    if (!task.IsCanceled)
                    {
                        this.pendingCompletions.TryAdd(request.RequestHeader.RequestHandle, tcs);
                        await this.SendRequestAsync(request, token).ConfigureAwait(false);
                    }

                    if (request is CloseSecureChannelRequest)
                    {
                        return; // end of the line
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
        }

        private async Task SendRequestAsync(IServiceRequest request, CancellationToken token = default(CancellationToken))
        {
            await this.sendingSemaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                this.ThrowIfClosedOrNotOpening();
                Log.Trace($"Sending {request.GetType().Name} Handle: {request.RequestHeader.RequestHandle}");
                if (request is OpenSecureChannelRequest)
                {
                    await this.SendOpenSecureChannelRequestAsync((OpenSecureChannelRequest)request, token).ConfigureAwait(false);
                }
                else if (request is CloseSecureChannelRequest)
                {
                    await this.SendCloseSecureChannelRequestAsync((CloseSecureChannelRequest)request, token).ConfigureAwait(false);

                    // in this case, the endpoint does not actually send a response
                    TaskCompletionSource<IServiceResponse> tcs;
                    if (this.pendingCompletions.TryRemove(request.RequestHeader.RequestHandle, out tcs))
                    {
                        tcs.TrySetResult(new CloseSecureChannelResponse());
                    }
                }
                else
                {
                    await this.SendServiceRequestAsync(request, token).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Log.Warn($"Error sending {request.GetType().Name} Handle: {request.RequestHeader.RequestHandle}. {ex.Message}");
                throw;
            }
            finally
            {
                this.sendingSemaphore.Release();
            }
        }

        private async Task SendOpenSecureChannelRequestAsync(OpenSecureChannelRequest request, CancellationToken token)
        {
            var bodyStream = SerializableBytes.CreateWritableStream();
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
                            encoder.WriteByteString(null, this.localCertificateBlob);
                            encoder.WriteByteString(null, this.RemoteCertificate.GetCertHash());
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
                            byte[] signature = this.LocalPrivateKey.SignData(this.sendBuffer, 0, position, this.asymSignatureHashAlgorithmName, RSASignaturePadding.Pkcs1);
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
                                byte[] cipherText = this.RemotePublicKey.Encrypt(plainText, this.asymEncryptionPadding);
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
                bodyEncoder.Dispose();
            }
        }

        private async Task SendCloseSecureChannelRequestAsync(CloseSecureChannelRequest request, CancellationToken token)
        {
            var bodyStream = SerializableBytes.CreateWritableStream();
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
                                this.symSigner.Key = this.clientSigningKey;
                                if (this.symIsEncrypted)
                                {
                                    this.currentClientEncryptingKey = this.clientEncryptingKey;
                                    this.currentClientInitializationVector = this.clientInitializationVector;
                                    this.symEncryptor = this.symEncryptionAlgorithm.CreateEncryptor(this.currentClientEncryptingKey, this.currentClientInitializationVector);
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
                            byte[] signature = this.symSigner.ComputeHash(this.sendBuffer, 0, position);
                            if (signature != null)
                            {
                                encoder.Write(signature, 0, signature.Length);
                            }
                        }

                        // encrypt
                        position = encoder.Position;
                        if (this.symIsEncrypted)
                        {
                            using (var symEncryptor = this.symEncryptionAlgorithm.CreateEncryptor(this.currentClientEncryptingKey, this.currentClientInitializationVector))
                            {
                                int inputCount = position - plainHeaderSize;
                                Debug.Assert(inputCount % symEncryptor.InputBlockSize == 0, "Input data is not an even number of encryption blocks.");
                                symEncryptor.TransformBlock(this.sendBuffer, plainHeaderSize, inputCount, this.sendBuffer, plainHeaderSize);
                                symEncryptor.TransformFinalBlock(this.sendBuffer, position, 0);
                            }
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
                bodyEncoder.Dispose();
            }
        }

        private async Task SendServiceRequestAsync(IServiceRequest request, CancellationToken token)
        {
            var bodyStream = SerializableBytes.CreateWritableStream();
            var bodyEncoder = new BinaryEncoder(bodyStream, this);
            try
            {
                ExpandedNodeId binaryEncodingId;
                if (!TypeToBinaryEncodingIdDictionary.TryGetValue(request.GetType(), out binaryEncodingId))
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
                                this.symSigner.Key = this.clientSigningKey;
                                if (this.symIsEncrypted)
                                {
                                    this.currentClientEncryptingKey = this.clientEncryptingKey;
                                    this.currentClientInitializationVector = this.clientInitializationVector;
                                    this.symEncryptor = this.symEncryptionAlgorithm.CreateEncryptor(this.currentClientEncryptingKey, this.currentClientInitializationVector);
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
                            byte[] signature = this.symSigner.ComputeHash(this.sendBuffer, 0, position);
                            if (signature != null)
                            {
                                encoder.Write(signature, 0, signature.Length);
                            }
                        }

                        // encrypt
                        position = encoder.Position;
                        if (this.symIsEncrypted)
                        {
                            using (var symEncryptor = this.symEncryptionAlgorithm.CreateEncryptor(this.currentClientEncryptingKey, this.currentClientInitializationVector))
                            {
                                int inputCount = position - plainHeaderSize;
                                Debug.Assert(inputCount % symEncryptor.InputBlockSize == 0, "Input data is not an even number of encryption blocks.");
                                symEncryptor.TransformBlock(this.sendBuffer, plainHeaderSize, position - plainHeaderSize, this.sendBuffer, plainHeaderSize);
                                symEncryptor.TransformFinalBlock(this.sendBuffer, position, 0);
                            }
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
                bodyEncoder.Dispose();
            }
        }

        private async Task ReceiveResponsesAsync(CancellationToken token = default(CancellationToken))
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var response = await this.ReceiveResponseAsync(token).ConfigureAwait(false);
                    if (response == null)
                    {
                        // Null response indicates socket closed. This is expected when closing secure channel.
                        if (this.State == CommunicationState.Closing)
                        {
                            return;
                        }

                        throw new ServiceResultException(StatusCodes.BadServerNotConnected);
                    }

                    var header = response.ResponseHeader;
                    TaskCompletionSource<IServiceResponse> tcs;
                    if (this.pendingCompletions.TryRemove(header.RequestHandle, out tcs))
                    {
                        if (StatusCode.IsBad(header.ServiceResult))
                        {
                            var ex = new ServiceResultException(new ServiceResult(header.ServiceResult, header.ServiceDiagnostics, header.StringTable));
                            Log.Warn($"Received {response.GetType().Name} Handle: {response.ResponseHeader.RequestHandle} Code: {header.ServiceResult} - {ex.Message}");
                            tcs.TrySetException(ex);
                        }
                        else
                        {
                            Log.Trace($"Received {response.GetType().Name} Handle: {response.ResponseHeader.RequestHandle}");
                            tcs.TrySetResult(response);
                        }
                    }

                    // check if time to renew token
                    if (this.State == CommunicationState.Opened && DateTime.UtcNow > this.tokenRenewalTime)
                    {
                        this.tokenRenewalTime = this.tokenRenewalTime.AddMilliseconds(60000);
                        var task = Task.Run(() => this.OnRenewAsync(token));
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
        }

        private async Task<IServiceResponse> ReceiveResponseAsync(CancellationToken token = default(CancellationToken))
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

                var bodyStream = SerializableBytes.CreateWritableStream();
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
                                            this.symVerifier.Key = this.serverSigningKey;
                                            if (this.symIsEncrypted)
                                            {
                                                this.currentServerEncryptingKey = this.serverEncryptingKey;
                                                this.currentServerInitializationVector = this.serverInitializationVector;
                                                this.symDecryptor = this.symEncryptionAlgorithm.CreateDecryptor(this.currentServerEncryptingKey, this.currentServerInitializationVector);
                                            }
                                        }
                                    }

                                    plainHeaderSize = decoder.Position;

                                    // decrypt
                                    if (this.symIsEncrypted)
                                    {
                                        using (var symDecryptor = this.symEncryptionAlgorithm.CreateDecryptor(this.currentServerEncryptingKey, this.currentServerInitializationVector))
                                        {
                                            int inputCount = messageLength - plainHeaderSize;
                                            Debug.Assert(inputCount % symDecryptor.InputBlockSize == 0, "Input data is not an even number of encryption blocks.");
                                            symDecryptor.TransformBlock(this.receiveBuffer, plainHeaderSize, inputCount, this.receiveBuffer, plainHeaderSize);
                                            symDecryptor.TransformFinalBlock(this.receiveBuffer, messageLength, 0);
                                        }
                                    }

                                    // verify
                                    if (this.symIsSigned)
                                    {
                                        var datalen = messageLength - this.symSignatureSize;
                                        byte[] signature = this.symVerifier.ComputeHash(this.receiveBuffer, 0, datalen);
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
                                            byte[] plainTextBlock = this.LocalPrivateKey.Decrypt(cipherTextBlock, this.asymEncryptionPadding);
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
                                        if (!this.RemotePublicKey.VerifyData(this.receiveBuffer, 0, datalen, this.receiveBuffer.AsArraySegment(datalen, this.asymRemoteSignatureSize).ToArray(), this.asymSignatureHashAlgorithmName, RSASignaturePadding.Pkcs1))
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
                                    var code = (StatusCode)decoder.ReadUInt32(null);
                                    var message = decoder.ReadString(null);
                                    throw new ServiceResultException(code, message);

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
                            decoder.Dispose();
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
                        Type type2;
                        if (!BinaryEncodingIdToTypeDictionary.TryGetValue(NodeId.ToExpandedNodeId(nodeId, this.NamespaceUris), out type2))
                        {
                            throw new ServiceResultException(StatusCodes.BadEncodingError, "NodeId not registered in dictionary.");
                        }

                        // create response
                        response = (IServiceResponse)Activator.CreateInstance(type2);
                    }

                    // set properties from message stream
                    response.Decode(bodyDecoder);
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

        private async Task OnRenewAsync(CancellationToken token)
        {
            var openSecureChannelRequest = new OpenSecureChannelRequest
            {
                ClientProtocolVersion = ProtocolVersion,
                RequestType = SecurityTokenRequestType.Renew,
                SecurityMode = this.RemoteEndpoint.SecurityMode,
                ClientNonce = this.symIsSigned ? this.GetNextNonce() : null,
                RequestedLifetime = TokenRequestedLifetime
            };
            var openSecureChannelResponse = (OpenSecureChannelResponse)await this.RequestAsync(openSecureChannelRequest).ConfigureAwait(false);
            if (openSecureChannelResponse.ServerProtocolVersion < ProtocolVersion)
            {
                throw new ServiceResultException(StatusCodes.BadProtocolVersionUnsupported);
            }

            await this.sendingSemaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                this.ChannelId = openSecureChannelResponse.SecurityToken.ChannelId;
                this.TokenId = openSecureChannelResponse.SecurityToken.TokenId;
                this.tokenRenewalTime = DateTime.UtcNow.AddMilliseconds(0.75 * openSecureChannelResponse.SecurityToken.RevisedLifetime);
                if (this.symIsSigned)
                {
                    var clientNonce = openSecureChannelRequest.ClientNonce;
                    var serverNonce = openSecureChannelResponse.ServerNonce;

                    // (re)create client security keys for encrypting the next message sent
                    var clientSecurityKey = CalculatePSHA(serverNonce, clientNonce, this.symSignatureKeySize + this.symEncryptionKeySize + this.symEncryptionBlockSize, this.asymSignatureHashAlgorithmName);
                    Buffer.BlockCopy(clientSecurityKey, 0, this.clientSigningKey, 0, this.symSignatureKeySize);
                    Buffer.BlockCopy(clientSecurityKey, this.symSignatureKeySize, this.clientEncryptingKey, 0, this.symEncryptionKeySize);
                    Buffer.BlockCopy(clientSecurityKey, this.symSignatureKeySize + this.symEncryptionKeySize, this.clientInitializationVector, 0, this.symEncryptionBlockSize);

                    // (re)create server security keys for decrypting the next message received that has a new TokenId
                    var serverSecurityKey = CalculatePSHA(clientNonce, serverNonce, this.symSignatureKeySize + this.symEncryptionKeySize + this.symEncryptionBlockSize, this.asymSignatureHashAlgorithmName);
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

        private void UpdateHeader(IServiceRequest request)
        {
            if (request.RequestHeader == null)
            {
                request.RequestHeader = new RequestHeader { TimeoutHint = this.TimeoutHint, ReturnDiagnostics = this.DiagnosticsHint };
            }

            request.RequestHeader.Timestamp = DateTime.UtcNow;
            request.RequestHeader.RequestHandle = this.GetNextHandle();
            request.RequestHeader.AuthenticationToken = this.AuthenticationToken;
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

        protected byte[] GetNextNonce()
        {
            var nonce = new byte[NonceLength];
            Rng.GetBytes(nonce);
            return nonce;
        }

        private void CancelRequest(object o)
        {
            var tcs = (TaskCompletionSource<IServiceResponse>)o;
            if (tcs.TrySetException(new ServiceResultException(StatusCodes.BadRequestTimeout)))
            {
                var request = (IServiceRequest)tcs.Task.AsyncState;
                this.pendingCompletions.TryRemove(request.RequestHeader.RequestHandle, out tcs);
                Log.Trace($"Canceled {request.GetType().Name} Handle: {request.RequestHeader.RequestHandle}");
            }
        }
    }
}