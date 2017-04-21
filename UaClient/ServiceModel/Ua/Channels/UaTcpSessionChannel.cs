// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;

namespace Workstation.ServiceModel.Ua.Channels
{
    /// <summary>
    /// A channel that opens a session.
    /// </summary>
    public class UaTcpSessionChannel : UaTcpSecureChannel
    {
        public const double DefaultSessionTimeout = 120 * 1000; // 2 minutes
        public const string RsaSha1Signature = @"http://www.w3.org/2000/09/xmldsig#rsa-sha1";
        // public const string RsaSha256Signature = @"http://www.w3.org/2000/09/xmldsig#rsa-sha256";
        public const string RsaSha256Signature = @"http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
        public const string RsaV15KeyWrap = @"http://www.w3.org/2001/04/xmlenc#rsa-1_5";
        public const string RsaOaepKeyWrap = @"http://www.w3.org/2001/04/xmlenc#rsa-oaep";
        protected const int NonceLength = 32;

        /// <summary>
        /// Initializes a new instance of the <see cref="UaTcpSessionChannel"/> class.
        /// </summary>
        /// <param name="localDescription">The <see cref="ApplicationDescription"/> of the local application.</param>
        /// <param name="certificateStore">The local certificate store.</param>
        /// <param name="userIdentity">The user identity. Provide an <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> or <see cref="X509Identity"/>.</param>
        /// <param name="remoteEndpoint">The <see cref="EndpointDescription"/> of the remote application. Obtained from a prior call to UaTcpDiscoveryClient.GetEndpoints.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="sessionTimeout">The requested number of milliseconds that a session may be unused before being closed by the server.</param>
        /// <param name="timeoutHint">The default number of milliseconds that may elapse before an operation is cancelled by the service.</param>
        /// <param name="diagnosticsHint">The default diagnostics flags to be requested by the service.</param>
        /// <param name="localReceiveBufferSize">The size of the receive buffer.</param>
        /// <param name="localSendBufferSize">The size of the send buffer.</param>
        /// <param name="localMaxMessageSize">The maximum total size of a message.</param>
        /// <param name="localMaxChunkCount">The maximum number of message chunks.</param>
        public UaTcpSessionChannel(
            ApplicationDescription localDescription,
            ICertificateStore certificateStore,
            IUserIdentity userIdentity,
            EndpointDescription remoteEndpoint,
            ILoggerFactory loggerFactory = null,
            double sessionTimeout = DefaultSessionTimeout,
            uint timeoutHint = DefaultTimeoutHint,
            uint diagnosticsHint = DefaultDiagnosticsHint,
            uint localReceiveBufferSize = DefaultBufferSize,
            uint localSendBufferSize = DefaultBufferSize,
            uint localMaxMessageSize = DefaultMaxMessageSize,
            uint localMaxChunkCount = DefaultMaxChunkCount)
            : base(localDescription, certificateStore, remoteEndpoint, loggerFactory, timeoutHint, diagnosticsHint, localReceiveBufferSize, localSendBufferSize, localMaxMessageSize, localMaxChunkCount)
        {
            this.UserIdentity = userIdentity;
            this.SessionTimeout = sessionTimeout;
        }

        public IUserIdentity UserIdentity { get; }

        public double SessionTimeout { get; }

        public NodeId SessionId { get; private set; }

        public byte[] RemoteNonce { get; private set; }

        protected override async Task OnOpenAsync(CancellationToken token)
        {
            this.Logger?.LogInformation($"Opening session channel with endpoint '{this.RemoteEndpoint.EndpointUrl}'.");
            this.Logger?.LogInformation($"SecurityPolicy: '{this.RemoteEndpoint.SecurityPolicyUri}'.");
            this.Logger?.LogInformation($"SecurityMode: '{this.RemoteEndpoint.SecurityMode}'.");
            this.Logger?.LogInformation($"UserIdentityToken: '{this.UserIdentity}'.");

            await base.OnOpenAsync(token).ConfigureAwait(false);

            token.ThrowIfCancellationRequested();

            // if SessionId is provided then we skip the CreateSessionRequest and go directly to (re)ActivateSession.
            // requires from previous Session: SessionId, AuthenticationToken, RemoteNonce
            if (this.SessionId == null)
            {
                var localNonce = this.RemoteEndpoint.SecurityMode != MessageSecurityMode.None ? this.GetNextNonce(NonceLength) : null;
                var localCertificate = this.RemoteEndpoint.SecurityMode != MessageSecurityMode.None ? this.LocalCertificate : null;
                var createSessionRequest = new CreateSessionRequest
                {
                    ClientDescription = this.LocalDescription,
                    EndpointUrl = this.RemoteEndpoint.EndpointUrl,
                    SessionName = this.LocalDescription.ApplicationName,
                    ClientNonce = localNonce,
                    ClientCertificate = localCertificate,
                    RequestedSessionTimeout = this.SessionTimeout,
                    MaxResponseMessageSize = 0
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
                byte[] dataToVerify = null;
                switch (this.RemoteEndpoint.SecurityPolicyUri)
                {
                    case SecurityPolicyUris.Basic128Rsa15:
                    case SecurityPolicyUris.Basic256:
                        dataToVerify = Concat(localCertificate, localNonce);
                        verifier = SignerUtilities.GetSigner("SHA-1withRSA");
                        verifier.Init(false, this.RemotePublicKey);
                        verifier.BlockUpdate(dataToVerify, 0, dataToVerify.Length);
                        if (!verifier.VerifySignature(createSessionResponse.ServerSignature.Signature))
                        {
                            throw new ServiceResultException(StatusCodes.BadApplicationSignatureInvalid, "Server did not provide a correct signature for the nonce data provided by the client.");
                        }

                        break;

                    case SecurityPolicyUris.Basic256Sha256:
                        dataToVerify = Concat(localCertificate, localNonce);
                        verifier = SignerUtilities.GetSigner("SHA-256withRSA");
                        verifier.Init(false, this.RemotePublicKey);
                        verifier.BlockUpdate(dataToVerify, 0, dataToVerify.Length);
                        if (!verifier.VerifySignature(createSessionResponse.ServerSignature.Signature))
                        {
                            throw new ServiceResultException(StatusCodes.BadApplicationSignatureInvalid, "Server did not provide a correct signature for the nonce data provided by the client.");
                        }

                        break;

                    default:
                        break;
                }
            }

            // create client signature
            SignatureData clientSignature = null;
            ISigner signer = null;
            byte[] dataToSign = null;
            switch (this.RemoteEndpoint.SecurityPolicyUri)
            {
                case SecurityPolicyUris.Basic128Rsa15:
                case SecurityPolicyUris.Basic256:
                    dataToSign = Concat(this.RemoteEndpoint.ServerCertificate, this.RemoteNonce);
                    signer = SignerUtilities.GetSigner("SHA-1withRSA");
                    signer.Init(true, this.LocalPrivateKey);
                    signer.BlockUpdate(dataToSign, 0, dataToSign.Length);
                    clientSignature = new SignatureData
                    {
                        Signature = signer.GenerateSignature(),
                        Algorithm = RsaSha1Signature,
                    };

                    break;

                case SecurityPolicyUris.Basic256Sha256:
                    dataToSign = Concat(this.RemoteEndpoint.ServerCertificate, this.RemoteNonce);
                    signer = SignerUtilities.GetSigner("SHA-256withRSA");
                    signer.Init(true, this.LocalPrivateKey);
                    signer.BlockUpdate(dataToSign, 0, dataToSign.Length);
                    clientSignature = new SignatureData
                    {
                        Signature = signer.GenerateSignature(),
                        Algorithm = RsaSha256Signature,
                    };

                    break;

                default:
                    clientSignature = new SignatureData();
                    break;
            }

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
                byte[] plainText = Concat(issuedIdentity.TokenData, this.RemoteNonce);
                var secPolicyUri = tokenPolicy.SecurityPolicyUri ?? this.RemoteEndpoint.SecurityPolicyUri;
                switch (secPolicyUri)
                {
                    case SecurityPolicyUris.Basic128Rsa15:
                        identityToken = new IssuedIdentityToken
                        {
                            TokenData = this.RemotePublicKey.EncryptTokenData(plainText, secPolicyUri),
                            EncryptionAlgorithm = RsaV15KeyWrap,
                            PolicyId = tokenPolicy.PolicyId
                        };

                        break;

                    case SecurityPolicyUris.Basic256:
                    case SecurityPolicyUris.Basic256Sha256:
                        identityToken = new IssuedIdentityToken
                        {
                            TokenData = this.RemotePublicKey.EncryptTokenData(plainText, secPolicyUri),
                            EncryptionAlgorithm = RsaOaepKeyWrap,
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
            else if (this.UserIdentity is X509Identity)
            {
                throw new NotImplementedException("A user identity of X509Identity is not implemented.");
                /*
                var tokenPolicy = this.RemoteEndpoint.UserIdentityTokens.FirstOrDefault(t => t.TokenType == UserTokenType.Certificate);
                if (tokenPolicy == null)
                {
                    throw new ServiceResultException(StatusCodes.BadIdentityTokenRejected);
                }

                var x509Identity = (X509Identity)this.UserIdentity;
                identityToken = new X509IdentityToken { CertificateData = x509Identity.Certificate?.RawData, PolicyId = tokenPolicy.PolicyId };
                var secPolicyUri = tokenPolicy.SecurityPolicyUri ?? this.RemoteEndpoint.SecurityPolicyUri;
                switch (secPolicyUri)
                {
                    case SecurityPolicyUris.Basic128Rsa15:
                    case SecurityPolicyUris.Basic256:
                        var asymSigningKey = x509Identity.Certificate?.GetRSAPrivateKey();
                        if (asymSigningKey != null)
                        {
                            dataToSign = Concat(this.RemoteEndpoint.ServerCertificate, this.RemoteNonce);
                            tokenSignature = new SignatureData
                            {
                                Signature = asymSigningKey.SignData(dataToSign, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1),
                                Algorithm = RsaSha1Signature,
                            };
                            break;
                        }

                        tokenSignature = new SignatureData();
                        break;

                    case SecurityPolicyUris.Basic256Sha256:
                        var asymSigningKey256 = x509Identity.Certificate?.GetRSAPrivateKey();
                        if (asymSigningKey256 != null)
                        {
                            dataToSign = Concat(this.RemoteEndpoint.ServerCertificate, this.RemoteNonce);
                            tokenSignature = new SignatureData
                            {
                                Signature = asymSigningKey256.SignData(dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
                                Algorithm = RsaSha256Signature,
                            };
                            break;
                        }

                        tokenSignature = new SignatureData();
                        break;

                    default:
                        tokenSignature = new SignatureData();
                        break;
                }
                */
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
                byte[] plainText = Concat(System.Text.Encoding.UTF8.GetBytes(userNameIdentity.Password), this.RemoteNonce);
                var secPolicyUri = tokenPolicy.SecurityPolicyUri ?? this.RemoteEndpoint.SecurityPolicyUri;
                switch (secPolicyUri)
                {
                    case SecurityPolicyUris.Basic128Rsa15:
                        identityToken = new UserNameIdentityToken
                        {
                            UserName = userNameIdentity.UserName,
                            Password = this.RemotePublicKey.EncryptTokenData(plainText, secPolicyUri),
                            EncryptionAlgorithm = RsaV15KeyWrap,
                            PolicyId = tokenPolicy.PolicyId
                        };

                        break;

                    case SecurityPolicyUris.Basic256:
                    case SecurityPolicyUris.Basic256Sha256:
                        identityToken = new UserNameIdentityToken
                        {
                            UserName = userNameIdentity.UserName,
                            Password = this.RemotePublicKey.EncryptTokenData(plainText, secPolicyUri),
                            EncryptionAlgorithm = RsaOaepKeyWrap,
                            PolicyId = tokenPolicy.PolicyId
                        };

                        break;

                    default:
                        identityToken = new UserNameIdentityToken
                        {
                            UserName = userNameIdentity.UserName,
                            Password = System.Text.Encoding.UTF8.GetBytes(userNameIdentity.Password),
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
        }

        protected override async Task OnCloseAsync(CancellationToken token)
        {
            var closeSessionRequest = new CloseSessionRequest
            {
                DeleteSubscriptions = true
            };
            var closeSessionResponse = await this.CloseSessionAsync(closeSessionRequest).ConfigureAwait(false);
            await base.OnCloseAsync(token).ConfigureAwait(false);
        }

        protected override Task OnFaulted(CancellationToken token = default(CancellationToken))
        {
            this.Complete();
            return base.OnFaulted(token);
        }
    }
}