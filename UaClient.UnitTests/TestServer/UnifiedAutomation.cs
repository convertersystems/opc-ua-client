using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Workstation.ServiceModel.Ua;

namespace Workstation.UaClient.TestServer
{
    public class UnifiedAutomation : ITestServer
    {
        public string EndpointUrl => "opc.tcp://localhost:48010";
        public TestEndpoint[] TestEndpoints { get; }
        public IUserIdentity[] UserIdentities { get; }

        public UnifiedAutomation(string pkiPath)
        {
            var x509Identity = default(X509Identity);
            var userNameIdentity = new UserNameIdentity("root", "secret");

            // read x509Identity
            var userCert = default(X509Certificate);
            var userKey = default(RsaKeyParameters);

            var certParser = new X509CertificateParser();
            var userCertInfo = new FileInfo(Path.Combine(pkiPath, "user", "certs", "ctt_usrT.der"));
            if (userCertInfo.Exists)
            {
                using (var crtStream = userCertInfo.OpenRead())
                {
                    var c = certParser.ReadCertificate(crtStream);
                    if (c != null)
                    {
                        userCert = c;
                    }
                }
            }
            var userKeyInfo = new FileInfo(Path.Combine(pkiPath, "user", "private", "ctt_usrT.pem"));
            if (userKeyInfo.Exists)
            {
                using (var keyStream = new StreamReader(userKeyInfo.OpenRead()))
                {
                    var keyReader = new PemReader(keyStream);
                    var keyPair = keyReader.ReadObject() as AsymmetricCipherKeyPair;
                    if (keyPair != null)
                    {
                        userKey = keyPair.Private as RsaKeyParameters;
                    }
                }
            }
            
            if (userCert != null && userKey != null)
            {
                x509Identity = new X509Identity(userCert, userKey);
            }

            this.UserIdentities = new IUserIdentity[] { new AnonymousIdentity(), userNameIdentity, x509Identity };

            this.TestEndpoints = new[]
            {
                new TestEndpoint
                {
                    EndpointUrl = EndpointUrl,
                    SecurityPolicyUri =  SecurityPolicyUris.None,
                    SecurityMode = MessageSecurityMode.None,
                    UserIdentity = new AnonymousIdentity()
                },
                new TestEndpoint
                {
                    EndpointUrl = EndpointUrl,
                    SecurityPolicyUri =  SecurityPolicyUris.None,
                    SecurityMode = MessageSecurityMode.None,
                    UserIdentity = userNameIdentity
                },
                new TestEndpoint
                {
                    EndpointUrl = EndpointUrl,
                    SecurityPolicyUri =  SecurityPolicyUris.None,
                    SecurityMode = MessageSecurityMode.None,
                    UserIdentity = x509Identity
                },
                new TestEndpoint
                {
                    EndpointUrl = EndpointUrl,
                    SecurityPolicyUri = SecurityPolicyUris.Basic256Sha256,
                    SecurityMode = MessageSecurityMode.Sign,
                    UserIdentity = new AnonymousIdentity()
                },
                new TestEndpoint
                {
                    EndpointUrl = EndpointUrl,
                    SecurityPolicyUri = SecurityPolicyUris.Basic256Sha256,
                    SecurityMode = MessageSecurityMode.Sign,
                    UserIdentity = userNameIdentity
                },
                new TestEndpoint
                {
                    EndpointUrl = EndpointUrl,
                    SecurityPolicyUri = SecurityPolicyUris.Basic256Sha256,
                    SecurityMode = MessageSecurityMode.Sign,
                    UserIdentity = x509Identity
                },
                new TestEndpoint
                {
                    EndpointUrl = EndpointUrl,
                    SecurityPolicyUri = SecurityPolicyUris.Aes128_Sha256_RsaOaep,
                    SecurityMode = MessageSecurityMode.Sign,
                    UserIdentity = new AnonymousIdentity()
                },
                new TestEndpoint
                {
                    EndpointUrl = EndpointUrl,
                    SecurityPolicyUri = SecurityPolicyUris.Aes128_Sha256_RsaOaep,
                    SecurityMode = MessageSecurityMode.Sign,
                    UserIdentity = userNameIdentity
                },
                new TestEndpoint
                {
                    EndpointUrl = EndpointUrl,
                    SecurityPolicyUri = SecurityPolicyUris.Aes128_Sha256_RsaOaep,
                    SecurityMode = MessageSecurityMode.Sign,
                    UserIdentity = x509Identity
                },
                new TestEndpoint
                {
                    EndpointUrl = EndpointUrl,
                    SecurityPolicyUri = SecurityPolicyUris.Aes256_Sha256_RsaPss,
                    SecurityMode = MessageSecurityMode.Sign,
                    UserIdentity = new AnonymousIdentity()
                },
                new TestEndpoint
                {
                    EndpointUrl = EndpointUrl,
                    SecurityPolicyUri = SecurityPolicyUris.Aes256_Sha256_RsaPss,
                    SecurityMode = MessageSecurityMode.Sign,
                    UserIdentity = userNameIdentity
                },
                new TestEndpoint
                {
                    EndpointUrl = EndpointUrl,
                    SecurityPolicyUri = SecurityPolicyUris.Aes256_Sha256_RsaPss,
                    SecurityMode = MessageSecurityMode.Sign,
                    UserIdentity = x509Identity
                },
                new TestEndpoint
                {
                    EndpointUrl = EndpointUrl,
                    SecurityPolicyUri = SecurityPolicyUris.Basic256Sha256,
                    SecurityMode = MessageSecurityMode.SignAndEncrypt,
                    UserIdentity = new AnonymousIdentity()
                },
                new TestEndpoint
                {
                    EndpointUrl = EndpointUrl,
                    SecurityPolicyUri = SecurityPolicyUris.Basic256Sha256,
                    SecurityMode = MessageSecurityMode.SignAndEncrypt,
                    UserIdentity = userNameIdentity
                },
                new TestEndpoint
                {
                    EndpointUrl = EndpointUrl,
                    SecurityPolicyUri = SecurityPolicyUris.Basic256Sha256,
                    SecurityMode = MessageSecurityMode.SignAndEncrypt,
                    UserIdentity = x509Identity
                },
                new TestEndpoint
                {
                    EndpointUrl = EndpointUrl,
                    SecurityPolicyUri = SecurityPolicyUris.Aes128_Sha256_RsaOaep,
                    SecurityMode = MessageSecurityMode.SignAndEncrypt,
                    UserIdentity = new AnonymousIdentity()
                },
                new TestEndpoint
                {
                    EndpointUrl = EndpointUrl,
                    SecurityPolicyUri = SecurityPolicyUris.Aes128_Sha256_RsaOaep,
                    SecurityMode = MessageSecurityMode.SignAndEncrypt,
                    UserIdentity = userNameIdentity
                },
                new TestEndpoint
                {
                    EndpointUrl = EndpointUrl,
                    SecurityPolicyUri = SecurityPolicyUris.Aes128_Sha256_RsaOaep,
                    SecurityMode = MessageSecurityMode.SignAndEncrypt,
                    UserIdentity = x509Identity
                },
                new TestEndpoint
                {
                    EndpointUrl = EndpointUrl,
                    SecurityPolicyUri = SecurityPolicyUris.Aes256_Sha256_RsaPss,
                    SecurityMode = MessageSecurityMode.SignAndEncrypt,
                    UserIdentity = new AnonymousIdentity()
                },
                new TestEndpoint
                {
                    EndpointUrl = EndpointUrl,
                    SecurityPolicyUri = SecurityPolicyUris.Aes256_Sha256_RsaPss,
                    SecurityMode = MessageSecurityMode.SignAndEncrypt,
                    UserIdentity = userNameIdentity
                },
                new TestEndpoint
                {
                    EndpointUrl = EndpointUrl,
                    SecurityPolicyUri = SecurityPolicyUris.Aes256_Sha256_RsaPss,
                    SecurityMode = MessageSecurityMode.SignAndEncrypt,
                    UserIdentity = x509Identity
                },
            };
        }
    }
}
