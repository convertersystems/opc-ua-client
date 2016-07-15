// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Workstation.ServiceModel.Ua.Channels
{
    /// <summary>
    /// A channel that opens a session.
    /// </summary>
    public class UaTcpSessionChannel : UaTcpSecureChannel, IRequestChannel
    {
        public const int DefaultSessionTimeout = 120 * 1000; // 2 minutes
        public const string RsaSha1Signature = @"http://www.w3.org/2000/09/xmldsig#rsa-sha1";
        // public const string RsaSha256Signature = @"http://www.w3.org/2000/09/xmldsig#rsa-sha256";
        public const string RsaSha256Signature = @"http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
        public const string RsaV15KeyWrap = @"http://www.w3.org/2001/04/xmlenc#rsa-1_5";
        public const string RsaOaepKeyWrap = @"http://www.w3.org/2001/04/xmlenc#rsa-oaep";

        /// <summary>
        /// Initializes a new instance of the <see cref="UaTcpSessionChannel"/> class.
        /// </summary>
        /// <param name="localDescription">The <see cref="ApplicationDescription"/> of the local application.</param>
        /// <param name="localCertificate">The <see cref="X509Certificate2"/> of the local application.</param>
        /// <param name="userIdentity">The user identity or null if anonymous. Supports <see cref="AnonymousIdentity"/>, <see cref="UserNameIdentity"/>, <see cref="IssuedIdentity"/> and <see cref="X509Identity"/>.</param>
        /// <param name="remoteEndpoint">The <see cref="EndpointDescription"/> of the remote application. Obtained from a prior call to UaTcpDiscoveryClient.GetEndpoints.</param>
        public UaTcpSessionChannel(ApplicationDescription localDescription, X509Certificate2 localCertificate, IUserIdentity userIdentity, EndpointDescription remoteEndpoint)
            : base(localDescription, localCertificate, remoteEndpoint)
        {
            this.UserIdentity = userIdentity;
            this.SessionTimeout = DefaultSessionTimeout;
        }

        public IUserIdentity UserIdentity { get; }

        public double SessionTimeout { get; set; }

        public NodeId SessionId { get; set; }

        public byte[] RemoteNonce { get; set; }

        protected override async Task OnOpenAsync(CancellationToken token)
        {
            await base.OnOpenAsync(token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();

            // if SessionId is provided then we skip the CreateSessionRequest and go directly to (re)ActivateSession.
            // requires from previous Session: SessionId, AuthenticationToken, RemoteNonce
            if (this.SessionId == null)
            {
                var localNonce = this.RemoteEndpoint.SecurityMode != MessageSecurityMode.None ? this.GetNextNonce() : null;
                var localCertificateBlob = this.RemoteEndpoint.SecurityMode != MessageSecurityMode.None ? this.LocalCertificate.RawData : null;
                var createSessionRequest = new CreateSessionRequest
                {
                    RequestHeader = new RequestHeader { TimeoutHint = this.TimeoutHint, ReturnDiagnostics = this.DiagnosticsHint, Timestamp = DateTime.UtcNow },
                    ClientDescription = this.LocalDescription,
                    EndpointUrl = this.RemoteEndpoint.EndpointUrl,
                    SessionName = this.LocalDescription.ApplicationName,
                    ClientNonce = localNonce,
                    ClientCertificate = localCertificateBlob,
                    RequestedSessionTimeout = this.SessionTimeout,
                    MaxResponseMessageSize = this.RemoteMaxMessageSize
                };
                var createSessionResponse = (CreateSessionResponse)await this.RequestAsync(createSessionRequest).ConfigureAwait(false);
                this.SessionId = createSessionResponse.SessionId;
                this.AuthenticationToken = createSessionResponse.AuthenticationToken;
                this.RemoteNonce = createSessionResponse.ServerNonce;

                // verify the server's certificate is the same as the certificate from the selected endpoint.
                if (this.RemoteEndpoint.ServerCertificate != null && !this.RemoteEndpoint.ServerCertificate.SequenceEqual(createSessionResponse.ServerCertificate))
                {
                    throw new ServiceResultException(StatusCodes.BadCertificateInvalid, "Server did not return the same certificate used to create the channel.");
                }

                // verify the server's signature.
                switch (this.RemoteEndpoint.SecurityPolicyUri)
                {
                    case SecurityPolicyUris.Basic128Rsa15:
                    case SecurityPolicyUris.Basic256:
                        byte[] dataToVerify = Concat(localCertificateBlob, localNonce);
                        if (!this.RemotePublicKey.VerifyData(dataToVerify, createSessionResponse.ServerSignature.Signature, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1))
                        {
                            throw new ServiceResultException(StatusCodes.BadApplicationSignatureInvalid, "Server did not provide a correct signature for the nonce data provided by the client.");
                        }

                        break;

                    case SecurityPolicyUris.Basic256Sha256:
                        byte[] dataToVerify256 = Concat(localCertificateBlob, localNonce);
                        if (!this.RemotePublicKey.VerifyData(dataToVerify256, createSessionResponse.ServerSignature.Signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
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
            switch (this.RemoteEndpoint.SecurityPolicyUri)
            {
                case SecurityPolicyUris.Basic128Rsa15:
                case SecurityPolicyUris.Basic256:
                    byte[] dataToSign = Concat(this.RemoteEndpoint.ServerCertificate, this.RemoteNonce);
                    clientSignature = new SignatureData
                    {
                        Signature = this.LocalPrivateKey.SignData(dataToSign, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1),
                        Algorithm = RsaSha1Signature,
                    };

                    break;

                case SecurityPolicyUris.Basic256Sha256:
                    byte[] dataToSign256 = Concat(this.RemoteEndpoint.ServerCertificate, this.RemoteNonce);
                    clientSignature = new SignatureData
                    {
                        Signature = this.LocalPrivateKey.SignData(dataToSign256, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
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
                RSA asymRemoteEncryptionKey;
                switch (secPolicyUri)
                {
                    case SecurityPolicyUris.Basic128Rsa15:
                        asymRemoteEncryptionKey = this.RemoteCertificate.GetRSAPublicKey();
                        identityToken = new IssuedIdentityToken
                        {
                            TokenData = asymRemoteEncryptionKey.EncryptTokenData(plainText, secPolicyUri),
                            EncryptionAlgorithm = RsaV15KeyWrap,
                            PolicyId = tokenPolicy.PolicyId
                        };

                        break;

                    case SecurityPolicyUris.Basic256:
                    case SecurityPolicyUris.Basic256Sha256:
                        asymRemoteEncryptionKey = this.RemoteCertificate.GetRSAPublicKey();
                        identityToken = new IssuedIdentityToken
                        {
                            TokenData = asymRemoteEncryptionKey.EncryptTokenData(plainText, secPolicyUri),
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
                            byte[] dataToSign = Concat(this.RemoteEndpoint.ServerCertificate, this.RemoteNonce);
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
                            byte[] dataToSign256 = Concat(this.RemoteEndpoint.ServerCertificate, this.RemoteNonce);
                            tokenSignature = new SignatureData
                            {
                                Signature = asymSigningKey256.SignData(dataToSign256, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
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
                RSA asymRemoteEncryptionKey;
                switch (secPolicyUri)
                {
                    case SecurityPolicyUris.Basic128Rsa15:
                        asymRemoteEncryptionKey = this.RemoteCertificate.GetRSAPublicKey();
                        identityToken = new UserNameIdentityToken
                        {
                            UserName = userNameIdentity.UserName,
                            Password = asymRemoteEncryptionKey.EncryptTokenData(plainText, secPolicyUri),
                            EncryptionAlgorithm = RsaV15KeyWrap,
                            PolicyId = tokenPolicy.PolicyId
                        };

                        break;

                    case SecurityPolicyUris.Basic256:
                    case SecurityPolicyUris.Basic256Sha256:
                        asymRemoteEncryptionKey = this.RemoteCertificate.GetRSAPublicKey();
                        identityToken = new UserNameIdentityToken
                        {
                            UserName = userNameIdentity.UserName,
                            Password = asymRemoteEncryptionKey.EncryptTokenData(plainText, secPolicyUri),
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
                RequestHeader = new RequestHeader { TimeoutHint = this.TimeoutHint, ReturnDiagnostics = this.DiagnosticsHint, Timestamp = DateTime.UtcNow },
                ClientSignature = clientSignature,
                LocaleIds = new[] { CultureInfo.CurrentUICulture.TwoLetterISOLanguageName },
                UserIdentityToken = identityToken,
                UserTokenSignature = tokenSignature
            };
            var activateSessionResponse = (ActivateSessionResponse)await this.RequestAsync(activateSessionRequest).ConfigureAwait(false);
            this.RemoteNonce = activateSessionResponse.ServerNonce;
            await this.FetchNamespaceTablesAsync().ConfigureAwait(false);
        }

        protected override async Task OnCloseAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var closeSessionRequest = new CloseSessionRequest
            {
                DeleteSubscriptions = true
            };
            await this.RequestAsync(closeSessionRequest).ConfigureAwait(false);
            await base.OnCloseAsync(token).ConfigureAwait(false);
        }

        private async Task FetchNamespaceTablesAsync()
        {
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
            var readResponse = (ReadResponse)await this.RequestAsync(readRequest).ConfigureAwait(false);
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
    }
}