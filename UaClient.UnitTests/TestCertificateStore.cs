using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System;
using System.Linq;
using System.Threading.Tasks;
using Workstation.ServiceModel.Ua;

namespace Workstation.UaClient
{
    public class TestCertificateStore : ITestCertificateStore
    {
        private readonly SecureRandom _rng = new SecureRandom();

        private readonly ApplicationDescription _serverDescription;
        private readonly X509Certificate _serverCertificate;
        private readonly RsaKeyParameters _serverKey;

        private readonly ApplicationDescription _clientDescription;
        private readonly X509Certificate _clientCertificate;
        private readonly RsaKeyParameters _clientKey;

        public byte[] ServerCertificate { get; }
        public byte[] ClientCertificate { get; }


        public TestCertificateStore(ApplicationDescription serverDescription, ApplicationDescription clientDescription)
        {
            _serverDescription = serverDescription;
            (_serverCertificate, _serverKey) = GenerateCertificate(serverDescription);
            ServerCertificate = _serverCertificate.GetEncoded();

            _clientDescription = clientDescription;
            (_clientCertificate, _clientKey) = GenerateCertificate(clientDescription);
            ClientCertificate = _clientCertificate.GetEncoded();
        }

        public Task<(X509Certificate Certificate, RsaKeyParameters Key)> GetLocalCertificateAsync(ApplicationDescription applicationDescription, ILogger logger)
        {
            if (applicationDescription.ApplicationName == _serverDescription.ApplicationName)
            {
                return Task.FromResult((_serverCertificate, _serverKey));
            }
            else if (applicationDescription.ApplicationName == _clientDescription.ApplicationName)
            {
                return Task.FromResult((_clientCertificate, _clientKey));
            }

            throw new InvalidOperationException();
        }

        public Task<bool> ValidateRemoteCertificateAsync(X509Certificate certificate, ILogger logger)
        {
            var cert = certificate.GetEncoded();
            var valid = cert.SequenceEqual(ServerCertificate)
                || cert.SequenceEqual(ClientCertificate);

            return Task.FromResult(valid);
        }

        private (X509Certificate _serverCertificate, RsaKeyParameters _serverKey) GenerateCertificate(ApplicationDescription applicationDescription)
        {
            if (applicationDescription == null)
            {
                throw new ArgumentNullException(nameof(applicationDescription));
            }

            string applicationUri = applicationDescription.ApplicationUri;
            if (string.IsNullOrEmpty(applicationUri))
            {
                throw new ArgumentOutOfRangeException(nameof(applicationDescription), "Expecting ApplicationUri in the form of 'http://{hostname}/{appname}' -or- 'urn:{hostname}:{appname}'.");
            }

            string subjectName = null;
            string hostName = null;

            UriBuilder appUri = new UriBuilder(applicationUri);
            if (appUri.Scheme == "http" && !string.IsNullOrEmpty(appUri.Host))
            {
                var path = appUri.Path.Trim('/');
                if (!string.IsNullOrEmpty(path))
                {
                    hostName = appUri.Host;
                    var appName = path;
                    subjectName = $"CN={appName},DC={hostName}";
                }
            }

            if (appUri.Scheme == "urn")
            {
                var parts = appUri.Path.Split(new[] { ':' }, 2);
                if (parts.Length == 2)
                {
                    hostName = parts[0];
                    var appName = parts[1];
                    subjectName = $"CN={appName},DC={hostName}";
                }
            }

            if (subjectName == null)
            {
                throw new ArgumentOutOfRangeException(nameof(applicationDescription), "Expecting ApplicationUri in the form of 'http://{hostname}/{appname}' -or- 'urn:{hostname}:{appname}'.");
            }

            // Create new certificate
            var subjectDN = new X509Name(subjectName);

            // Create a keypair.
            RsaKeyPairGenerator kg = new RsaKeyPairGenerator();
            kg.Init(new KeyGenerationParameters(_rng, 2048));
            var kp = kg.GenerateKeyPair();

            var key = kp.Private as RsaPrivateCrtKeyParameters;

            // Create a certificate.
            X509V3CertificateGenerator cg = new X509V3CertificateGenerator();
            var subjectSN = BigInteger.ProbablePrime(120, _rng);
            cg.SetSerialNumber(subjectSN);
            cg.SetSubjectDN(subjectDN);
            cg.SetIssuerDN(subjectDN);
            cg.SetNotBefore(DateTime.Now.Date.ToUniversalTime());
            cg.SetNotAfter(DateTime.Now.Date.ToUniversalTime().AddYears(25));
            cg.SetPublicKey(kp.Public);

            cg.AddExtension(
                X509Extensions.BasicConstraints.Id,
                true,
                new BasicConstraints(false));

            cg.AddExtension(
                X509Extensions.SubjectKeyIdentifier.Id,
                false,
                new SubjectKeyIdentifier(SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(kp.Public)));

            cg.AddExtension(
                X509Extensions.AuthorityKeyIdentifier.Id,
                false,
                new AuthorityKeyIdentifier(SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(kp.Public), new GeneralNames(new GeneralName(subjectDN)), subjectSN));

            cg.AddExtension(
                X509Extensions.SubjectAlternativeName,
                false,
                new GeneralNames(new[] { new GeneralName(GeneralName.UniformResourceIdentifier, applicationUri), new GeneralName(GeneralName.DnsName, hostName) }));

            cg.AddExtension(
                X509Extensions.KeyUsage,
                true,
                new KeyUsage(KeyUsage.DataEncipherment | KeyUsage.DigitalSignature | KeyUsage.NonRepudiation | KeyUsage.KeyCertSign | KeyUsage.KeyEncipherment));

            cg.AddExtension(
                X509Extensions.ExtendedKeyUsage,
                true,
                new ExtendedKeyUsage(KeyPurposeID.IdKPClientAuth, KeyPurposeID.IdKPServerAuth));

            var crt = cg.Generate(new Asn1SignatureFactory("SHA256WITHRSA", key, _rng));

            return (crt, key);
        }
    }
}