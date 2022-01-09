// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Workstation.ServiceModel.Ua.Channels
{
    /// <summary>
    /// The <see cref="IConversation"/> interface implementation
    /// for the OPC UA Secure Conversation (UASC).
    /// </summary>
    /// <seealso href="https://reference.opcfoundation.org/v104/Core/docs/Part6/6.7.1/">OPC UA specification Part 6: Mappings, 6.7.1</seealso>
    public class UaSecureConversation : IConversation
    {
        private const int _sequenceHeaderSize = 8;

        private static readonly SecureRandom _rng = new SecureRandom();

        private readonly X509CertificateParser _certificateParser = new X509CertificateParser();

        private readonly ApplicationDescription _localDescription;
        private readonly ICertificateStore? _certificateStore;
        private readonly TransportConnectionOptions _options;
        private readonly byte[] _sendBuffer;
        private readonly byte[] _receiveBuffer;
        private readonly ILogger? _logger;

        private MessageSecurityMode _securityMode;
        private string? _securityPolicyUri;
        private byte[]? _remoteCertificate;
        private byte[]? _localCertificate;
        private byte[]? _localSigningKey;
        private byte[]? _localEncryptingKey;
        private byte[]? _localInitializationVector;
        private byte[]? _remoteSigningKey;
        private byte[]? _remoteEncryptingKey;
        private byte[]? _remoteInitializationVector;
        private byte[]? _encryptionBuffer;

        private RsaKeyParameters? _localPrivateKey;
        private RsaKeyParameters? _remotePublicKey;

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

        private IBufferedCipher? _asymEncryptor;
        private IBufferedCipher? _asymDecryptor;
        private IBufferedCipher? _symEncryptor;
        private IBufferedCipher? _symDecryptor;
        private ISigner? _asymSigner;
        private ISigner? _asymVerifier;
        private IMac? _symSigner;
        private IMac? _symVerifier;
        private IDigest? _thumbprintDigest;

        private byte[]? _localNonce;
        private byte[]? _remoteNonce;
        private int _sequenceNumber;

        private uint _currentLocalTokenId;
        private uint _currentRemoteTokenId;

        /// <inheritdoc />
        public uint ChannelId { get; set; }

        /// <inheritdoc />
        public uint TokenId { get; set; }

        /// <inheritdoc />
        public byte[]? RemoteNonce
        {
            get => _remoteNonce;
            set
            {
                _remoteNonce = value;
                UpdateKeys();
            }
        }

        /// <summary>
        /// Retrieves if the conversation is hold by a server.
        /// </summary>
        public bool IsServer { get; }

        /// <summary>
        /// The security mode of the conversation.
        /// </summary>
        public MessageSecurityMode SecurityMode
        {
            get => _securityMode;
            set
            {
                if (value == MessageSecurityMode.SignAndEncrypt)
                {
                    _symEncryptor = CipherUtilities.GetCipher("AES/CBC/NoPadding");
                    _symDecryptor = CipherUtilities.GetCipher("AES/CBC/NoPadding");
                    _symIsEncrypted = true;
                }
                else
                {
                    _symIsEncrypted = false;
                }
                _securityMode = value;
            }
        }

        /// <summary>
        /// Creates a client conversation instance.
        /// </summary>
        /// <param name="localDescription">The local application description.</param>
        /// <param name="options">The transport connection options to be used.</param>
        /// <param name="certificateStore">The ceritficate store.</param>
        /// <param name="logger">The logger instance.</param>
        public UaSecureConversation(ApplicationDescription localDescription, TransportConnectionOptions options, ICertificateStore? certificateStore, ILogger? logger)
        {
            _localDescription = localDescription;
            _certificateStore = certificateStore;
            _logger = logger;
            _options = options;

            _sendBuffer = new byte[_options.SendBufferSize];
            _receiveBuffer = new byte[_options.ReceiveBufferSize];

            IsServer = false;
        }

        /// <summary>
        /// Creates a server conversation instance.
        /// </summary>
        /// <param name="channelId">The channel ID.</param>
        /// <param name="localDescription">The local application description.</param>
        /// <param name="options">The transport connection options to be used.</param>
        /// <param name="certificateStore">The cerificate store.</param>
        /// <param name="logger">The logger instance.</param>
        public UaSecureConversation(uint channelId, ApplicationDescription localDescription, TransportConnectionOptions options, ICertificateStore? certificateStore, ILogger? logger)
            : this(localDescription, options, certificateStore, logger)
        {
            ChannelId = channelId;
            IsServer = true;
        }

        /// <summary>
        /// Sets the remote certificate.
        /// </summary>
        /// <param name="securityPolicyUri">The security policy URI.</param>
        /// <param name="remoteCertificate">The remote certificate.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SetRemoteCertificateAsync(string? securityPolicyUri, byte[]? remoteCertificate)
        {
            _securityPolicyUri = securityPolicyUri;
            _remoteCertificate = remoteCertificate;

            if (remoteCertificate != null)
            {
                var cert = _certificateParser.ReadCertificate(remoteCertificate);
                if (cert != null)
                {
                    if (_certificateStore != null)
                    {
                        var result = await _certificateStore.ValidateRemoteCertificateAsync(cert, _logger);
                        if (!result)
                        {
                            throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed, "Remote certificate is untrusted.");
                        }
                    }

                    _remotePublicKey = cert.GetPublicKey() as RsaKeyParameters;
                }
            }

            if (securityPolicyUri == SecurityPolicyUris.None)
            {
                _asymIsSigned = _asymIsEncrypted = false;
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
                if (_localCertificate == null && _certificateStore != null)
                {
                    var tuple = await _certificateStore.GetLocalCertificateAsync(_localDescription, _logger);
                    _localCertificate = tuple.Certificate?.GetEncoded();
                    _localPrivateKey = tuple.Key;
                }

                if (_localPrivateKey == null)
                {
                    throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed, "LocalPrivateKey is null.");
                }

                if (_remotePublicKey == null)
                {
                    throw new ServiceResultException(StatusCodes.BadSecurityChecksFailed, "RemotePublicKey is null.");
                }

                switch (securityPolicyUri)
                {
                    case SecurityPolicyUris.Basic128Rsa15:

                        _asymSigner = SignerUtilities.GetSigner("SHA-1withRSA");
                        _asymSigner.Init(true, _localPrivateKey);
                        _asymVerifier = SignerUtilities.GetSigner("SHA-1withRSA");
                        _asymVerifier.Init(false, _remotePublicKey);
                        _asymEncryptor = CipherUtilities.GetCipher("RSA//PKCS1Padding");
                        _asymEncryptor.Init(true, _remotePublicKey);
                        _asymDecryptor = CipherUtilities.GetCipher("RSA//PKCS1Padding");
                        _asymDecryptor.Init(false, _localPrivateKey);
                        _symSigner = new HMac(new Sha1Digest());
                        _symVerifier = new HMac(new Sha1Digest());
                        _asymLocalKeySize = _localPrivateKey.Modulus.BitLength;
                        _asymRemoteKeySize = _remotePublicKey.Modulus.BitLength;
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
                        _asymSigner.Init(true, _localPrivateKey);
                        _asymVerifier = SignerUtilities.GetSigner("SHA-1withRSA");
                        _asymVerifier.Init(false, _remotePublicKey);
                        _asymEncryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        _asymEncryptor.Init(true, _remotePublicKey);
                        _asymDecryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        _asymDecryptor.Init(false, _localPrivateKey);
                        _symSigner = new HMac(new Sha1Digest());
                        _symVerifier = new HMac(new Sha1Digest());
                        _asymLocalKeySize = _localPrivateKey.Modulus.BitLength;
                        _asymRemoteKeySize = _remotePublicKey.Modulus.BitLength;
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
                        _asymSigner.Init(true, _localPrivateKey);
                        _asymVerifier = SignerUtilities.GetSigner("SHA-256withRSA");
                        _asymVerifier.Init(false, _remotePublicKey);
                        _asymEncryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        _asymEncryptor.Init(true, _remotePublicKey);
                        _asymDecryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        _asymDecryptor.Init(false, _localPrivateKey);
                        _symSigner = new HMac(new Sha256Digest());
                        _symVerifier = new HMac(new Sha256Digest());
                        _asymLocalKeySize = _localPrivateKey.Modulus.BitLength;
                        _asymRemoteKeySize = _remotePublicKey.Modulus.BitLength;
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
                        _asymSigner.Init(true, _localPrivateKey);
                        _asymVerifier = SignerUtilities.GetSigner("SHA-256withRSA");
                        _asymVerifier.Init(false, _remotePublicKey);
                        _asymEncryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        _asymEncryptor.Init(true, _remotePublicKey);
                        _asymDecryptor = CipherUtilities.GetCipher("RSA//OAEPPADDING");
                        _asymDecryptor.Init(false, _localPrivateKey);
                        _symSigner = new HMac(new Sha256Digest());
                        _symVerifier = new HMac(new Sha256Digest());
                        _asymLocalKeySize = _localPrivateKey.Modulus.BitLength;
                        _asymRemoteKeySize = _remotePublicKey.Modulus.BitLength;
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
                        _asymSigner.Init(true, _localPrivateKey);
                        _asymVerifier = SignerUtilities.GetSigner("SHA-256withRSAandMGF1");
                        _asymVerifier.Init(false, _remotePublicKey);
                        _asymEncryptor = CipherUtilities.GetCipher("RSA//OAEPWITHSHA256ANDMGF1PADDING");
                        _asymEncryptor.Init(true, _remotePublicKey);
                        _asymDecryptor = CipherUtilities.GetCipher("RSA//OAEPWITHSHA256ANDMGF1PADDING");
                        _asymDecryptor.Init(false, _localPrivateKey);
                        _symSigner = new HMac(new Sha256Digest());
                        _symVerifier = new HMac(new Sha256Digest());
                        _asymLocalKeySize = _localPrivateKey.Modulus.BitLength;
                        _asymRemoteKeySize = _remotePublicKey.Modulus.BitLength;
                        _asymLocalPlainTextBlockSize = Math.Max((_asymLocalKeySize / 8) - 66, 1);
                        _asymRemotePlainTextBlockSize = Math.Max((_asymRemoteKeySize / 8) - 66, 1);
                        _symSignatureSize = 32;
                        _symSignatureKeySize = 32;
                        _symEncryptionBlockSize = 16;
                        _symEncryptionKeySize = 32;
                        _nonceSize = 32;
                        break;

                    case SecurityPolicyUris.None:
                        break;
                }

                _asymIsSigned = true;
                _asymIsEncrypted = true;
                _symIsSigned = true;
                _asymLocalSignatureSize = _asymLocalKeySize / 8;
                _asymLocalCipherTextBlockSize = Math.Max(_asymLocalKeySize / 8, 1);
                _asymRemoteSignatureSize = _asymRemoteKeySize / 8;
                _asymRemoteCipherTextBlockSize = Math.Max(_asymRemoteKeySize / 8, 1);
                _localSigningKey = new byte[_symSignatureKeySize];
                _localEncryptingKey = new byte[_symEncryptionKeySize];
                _localInitializationVector = new byte[_symEncryptionBlockSize];
                _remoteSigningKey = new byte[_symSignatureKeySize];
                _remoteEncryptingKey = new byte[_symEncryptionKeySize];
                _remoteInitializationVector = new byte[_symEncryptionBlockSize];
                _encryptionBuffer = new byte[_options.SendBufferSize];
                _thumbprintDigest = DigestUtilities.GetDigest("SHA-1");
            }
        }

        /// <inheritdoc />
        public byte[]? GetNextNonce()
        {
            return _symIsSigned ? _localNonce = GetNextNonce(_nonceSize) : null;
        }

        /// <inheritdoc />
        public Task EncryptMessageAsync(Stream bodyStream, uint messageType, uint requestHandle, Func<byte[], int, int, CancellationToken, Task> consume, CancellationToken token)
        {
            if (messageType == UaTcpMessageTypes.OPNF)
            {
                return EncryptOpenMessage(bodyStream, messageType, requestHandle, consume, token);
            }
            else
            {
                return EncryptRequestMessage(bodyStream, messageType, requestHandle, consume, token);
            }
        }

        private async Task EncryptRequestMessage(Stream bodyStream, uint messageType, uint requestHandle, Func<byte[], int, int, CancellationToken, Task> consume, CancellationToken token)
        {
            int chunkCount = 0;
            int bodyCount = (int)(bodyStream.Length - bodyStream.Position);
            while (bodyCount > 0)
            {
                chunkCount++;
                if (_options.MaxChunkCount > 0 && chunkCount > _options.MaxChunkCount)
                {
                    throw new ServiceResultException(StatusCodes.BadEncodingLimitsExceeded);
                }

                var stream = new MemoryStream(_sendBuffer, 0, (int)_options.ReceiveBufferSize, true, true);
                var encoder = new BinaryEncoder(stream);
                try
                {
                    // header
                    encoder.WriteUInt32(null, messageType);
                    encoder.WriteUInt32(null, 0u);
                    encoder.WriteUInt32(null, ChannelId);

                    // symmetric security header
                    encoder.WriteUInt32(null, TokenId);

                    // detect new TokenId
                    if (TokenId != _currentLocalTokenId)
                    {
                        _currentLocalTokenId = TokenId;

                        // update signer and encrypter with new symmetric keys
                        if (_symIsSigned)
                        {
                            _symSigner!.Init(new KeyParameter(_localSigningKey));
                            if (_symIsEncrypted)
                            {
                                _symEncryptor!.Init(
                                    true,
                                    new ParametersWithIV(new KeyParameter(_localEncryptingKey), _localInitializationVector));
                            }
                        }
                    }

                    int plainHeaderSize = encoder.Position;

                    // sequence header
                    encoder.WriteUInt32(null, GetNextSequenceNumber());
                    encoder.WriteUInt32(null, requestHandle);

                    // body
                    int paddingHeaderSize;
                    int maxBodySize;
                    int bodySize;
                    int paddingSize;
                    int chunkSize;
                    if (_symIsEncrypted)
                    {
                        paddingHeaderSize = _symEncryptionBlockSize > 256 ? 2 : 1;
                        maxBodySize = ((((int)_options.ReceiveBufferSize - plainHeaderSize) / _symEncryptionBlockSize) * _symEncryptionBlockSize) - _sequenceHeaderSize - paddingHeaderSize - _symSignatureSize;
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
                        maxBodySize = (int)_options.ReceiveBufferSize - plainHeaderSize - _sequenceHeaderSize - _symSignatureSize;
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

                    bodyStream.Read(_sendBuffer, encoder.Position, bodySize);
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
                    await consume(_sendBuffer, 0, position, token).ConfigureAwait(false);
                }
                finally
                {
                    encoder.Dispose();
                }
            }
        }

        private async Task EncryptOpenMessage(Stream bodyStream, uint messageType, uint requestHandle, Func<byte[], int, int, CancellationToken, Task> consume, CancellationToken token)
        {
            int chunkCount = 0;
            int bodyCount = (int)(bodyStream.Length - bodyStream.Position);
            while (bodyCount > 0)
            {
                chunkCount++;
                if (_options.MaxChunkCount > 0 && chunkCount > _options.MaxChunkCount)
                {
                    throw new ServiceResultException(StatusCodes.BadEncodingLimitsExceeded);
                }

                var stream = new MemoryStream(_sendBuffer, 0, (int)_options.ReceiveBufferSize, true, true);
                var encoder = new BinaryEncoder(stream);
                try
                {
                    // header
                    encoder.WriteUInt32(null, messageType);
                    encoder.WriteUInt32(null, 0u);
                    encoder.WriteUInt32(null, ChannelId);

                    // asymmetric security header
                    encoder.WriteString(null, _securityPolicyUri);
                    if (SecurityMode != MessageSecurityMode.None)
                    {
                        encoder.WriteByteString(null, _localCertificate);
                        byte[] thumbprint = new byte[_thumbprintDigest!.GetDigestSize()];
                        var remoteCertificate = _remoteCertificate;
                        _thumbprintDigest.BlockUpdate(remoteCertificate, 0, remoteCertificate!.Length);
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
                    encoder.WriteUInt32(null, requestHandle);

                    // body
                    int paddingHeaderSize;
                    int maxBodySize;
                    int bodySize;
                    int paddingSize;
                    int chunkSize;
                    if (_asymIsEncrypted)
                    {
                        paddingHeaderSize = _asymRemoteCipherTextBlockSize > 256 ? 2 : 1;
                        maxBodySize = ((((int)_options.ReceiveBufferSize - plainHeaderSize) / _asymRemoteCipherTextBlockSize) * _asymRemotePlainTextBlockSize) - _sequenceHeaderSize - paddingHeaderSize - _asymLocalSignatureSize;
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
                        maxBodySize = (int)_options.ReceiveBufferSize - plainHeaderSize - _sequenceHeaderSize - _asymLocalSignatureSize;
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
                        Buffer.BlockCopy(_sendBuffer, 0, _encryptionBuffer!, 0, plainHeaderSize);
                        byte[] plainText = new byte[_asymRemotePlainTextBlockSize];
                        int jj = plainHeaderSize;
                        for (int ii = plainHeaderSize; ii < position; ii += _asymRemotePlainTextBlockSize)
                        {
                            Buffer.BlockCopy(_sendBuffer, ii, plainText, 0, _asymRemotePlainTextBlockSize);

                            // encrypt with remote public key.
                            byte[] cipherText = _asymEncryptor!.DoFinal(plainText);
                            Debug.Assert(cipherText.Length == _asymRemoteCipherTextBlockSize, nameof(_asymRemoteCipherTextBlockSize));
                            Buffer.BlockCopy(cipherText, 0, _encryptionBuffer!, jj, _asymRemoteCipherTextBlockSize);
                            jj += _asymRemoteCipherTextBlockSize;
                        }

                        await consume(_encryptionBuffer!, 0, jj, token).ConfigureAwait(false);
                        return;
                    }

                    // pass buffer to transport
                    await consume(_sendBuffer!, 0, encoder.Position, token).ConfigureAwait(false);
                }
                finally
                {
                    encoder.Dispose();
                }
            }
        }

        /// <inheritdoc />
        public async Task<(uint messageType, uint requestHandle)> DecryptMessageAsync(Stream bodyStream, Func<byte[], int, int, CancellationToken, Task<int>> receive, CancellationToken token)
        {
            uint requestId;
            int paddingHeaderSize;
            int bodySize;
            int paddingSize;

            // read chunks
            int chunkCount = 0;
            bool isFinal = false;
            uint messageType;

            do
            {
                chunkCount++;
                if (_options.MaxChunkCount > 0 && chunkCount > _options.MaxChunkCount)
                {
                    throw new ServiceResultException(StatusCodes.BadEncodingLimitsExceeded);
                }

                var count = await receive(_receiveBuffer, 0, (int)_options.ReceiveBufferSize, token).ConfigureAwait(false);
                if (count == 0)
                {
                    return (0, 0);
                }

                var stream = new MemoryStream(_receiveBuffer, 0, count, true, true);
                var decoder = new BinaryDecoder(stream);
                try
                {
                    uint channelId;
                    messageType = decoder.ReadUInt32(null);
                    int messageLength = (int)decoder.ReadUInt32(null);
                    Debug.Assert(count == messageLength, "Bytes received not equal to encoded Message length");
                    switch (messageType)
                    {
                        case UaTcpMessageTypes.MSGF:
                        case UaTcpMessageTypes.MSGC:
                        case UaTcpMessageTypes.CLOF:
                            // header
                            channelId = decoder.ReadUInt32(null);
                            if (channelId != ChannelId)
                            {
                                throw new ServiceResultException(StatusCodes.BadTcpSecureChannelUnknown);
                            }

                            // symmetric security header
                            var tokenId = decoder.ReadUInt32(null);

                            // detect new token
                            if (tokenId != _currentRemoteTokenId)
                            {
                                _currentRemoteTokenId = tokenId;

                                // update with new keys
                                if (_symIsSigned)
                                {
                                    _symVerifier!.Init(new KeyParameter(_remoteSigningKey));
                                    if (_symIsEncrypted)
                                    {
                                        _symDecryptor!.Init(
                                            false,
                                            new ParametersWithIV(new KeyParameter(_remoteEncryptingKey), _remoteInitializationVector));
                                    }
                                }

                                _logger?.LogTrace($"Installed new security token {tokenId}.");
                            }

                            var plainHeaderSize = decoder.Position;

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
                            var sequenceNum = decoder.ReadUInt32(null);
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
                            isFinal = messageType == UaTcpMessageTypes.MSGF || messageType == UaTcpMessageTypes.CLOF;
                            break;

                        case UaTcpMessageTypes.OPNF:
                            // header
                            channelId = decoder.ReadUInt32(null);
                            if (!IsServer)
                            {
                                ChannelId = channelId;
                            }

                            // asymmetric header
                            var securityPolicyUri = decoder.ReadString(null);
                            var remoteCertificate = decoder.ReadByteString(null);
                            var remoteThumbprint = decoder.ReadByteString(null);

                            if (IsServer)
                            {
                                await SetRemoteCertificateAsync(securityPolicyUri, remoteCertificate).ConfigureAwait(false);
                            }

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

                    if (_options.MaxMessageSize > 0 && bodyStream.Position > _options.MaxMessageSize)
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

            return (messageType, requestId);
        }

        private void UpdateKeys()
        {
            if (_symIsSigned)
            {
                var localNonce = _localNonce!;
                var remoteNonce = RemoteNonce!;

                // (re)create local security keys for encrypting the next message sent
                var localSecurityKey = CalculatePSHA(remoteNonce, localNonce, _symSignatureKeySize + _symEncryptionKeySize + _symEncryptionBlockSize, _securityPolicyUri!);
                Buffer.BlockCopy(localSecurityKey, 0, _localSigningKey!, 0, _symSignatureKeySize);
                Buffer.BlockCopy(localSecurityKey, _symSignatureKeySize, _localEncryptingKey!, 0, _symEncryptionKeySize);
                Buffer.BlockCopy(localSecurityKey, _symSignatureKeySize + _symEncryptionKeySize, _localInitializationVector!, 0, _symEncryptionBlockSize);

                // (re)create remote security keys for decrypting the next message received that has a new TokenId
                var remoteSecurityKey = CalculatePSHA(localNonce, remoteNonce, _symSignatureKeySize + _symEncryptionKeySize + _symEncryptionBlockSize, _securityPolicyUri!);
                Buffer.BlockCopy(remoteSecurityKey, 0, _remoteSigningKey!, 0, _symSignatureKeySize);
                Buffer.BlockCopy(remoteSecurityKey, _symSignatureKeySize, _remoteEncryptingKey!, 0, _symEncryptionKeySize);
                Buffer.BlockCopy(remoteSecurityKey, _symSignatureKeySize + _symEncryptionKeySize, _remoteInitializationVector!, 0, _symEncryptionBlockSize);
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
    }
}
