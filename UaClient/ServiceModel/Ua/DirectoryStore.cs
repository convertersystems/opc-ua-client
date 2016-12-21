// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;
using Org.BouncyCastle.Pkix;
using System.IO;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509.Store;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// A certificate store.
    /// </summary>
    public class DirectoryStore : ICertificateStore
    {
        private X509CertificateParser certParser = new X509CertificateParser();
        private SecureRandom rng = new SecureRandom();
        private ILogger logger;
        private string pkiDirectoryPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryStore"/> class.
        /// </summary>
        /// <param name="pkiDirectoryPath">The path to the local pki directory.</param>
        /// <param name="acceptAllRemoteCertificates">Set true to accept all remote certificates.</param>
        /// <param name="createLocalCertificateIfNotExist">Set true to create a local certificate and private key, if the files do not exist.</param>
        /// <param name="loggerFactory">A logger factory.</param>
        public DirectoryStore(string pkiDirectoryPath, bool acceptAllRemoteCertificates = true, bool createLocalCertificateIfNotExist = true, ILoggerFactory loggerFactory = null)
        {
            if (string.IsNullOrEmpty(pkiDirectoryPath))
            {
                throw new ArgumentNullException(nameof(pkiDirectoryPath));
            }
            this.pkiDirectoryPath = pkiDirectoryPath;
            this.AcceptAllRemoteCertificates = acceptAllRemoteCertificates;
            this.CreateLocalCertificateIfNotExist = createLocalCertificateIfNotExist;
            this.logger = loggerFactory?.CreateLogger<DirectoryStore>();
        }

        /// <summary>
        /// Gets a value indicating whether to accept all remote certificates.
        /// </summary>
        public bool AcceptAllRemoteCertificates { get; }

        /// <summary>
        /// Gets a value indicating whether to create a local certificate if it does not exist.
        /// </summary>
        public bool CreateLocalCertificateIfNotExist { get; }

        /// <inheritdoc/>
        public Tuple<X509Certificate, RsaPrivateCrtKeyParameters> GetLocalCertificate(ApplicationDescription applicationDescription)
        {
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
                    subjectName = $"CN={path},DC={appUri.Host}";
                    hostName = appUri.Host;
                }
            }

            if (appUri.Scheme == "urn")
            {
                var parts = appUri.Path.Split(new[] { ':' }, 2);
                if (parts.Length == 2)
                {
                    subjectName = $"CN={parts[1]},DC={parts[0]}";
                    hostName = parts[0];
                }
            }

            if (subjectName == null)
            {
                throw new ArgumentOutOfRangeException(nameof(applicationDescription), "Expecting ApplicationUri in the form of 'http://{hostname}/{appname}' -or- 'urn:{hostname}:{appname}'.");
            }

            var crt = default(X509Certificate);
            var key = default(RsaPrivateCrtKeyParameters);

            var crtInfo = new FileInfo(Path.Combine(this.pkiDirectoryPath, "own", "certs", "client.crt"));
            var keyInfo = new FileInfo(Path.Combine(this.pkiDirectoryPath, "own", "private", "client.key"));
            if (crtInfo.Exists && keyInfo.Exists)
            {
                using (var crtStream = crtInfo.OpenRead())
                {
                    crt = this.certParser.ReadCertificate(crtStream);
                    if (crt != null)
                    {
                        var asn1OctetString = crt.GetExtensionValue(X509Extensions.SubjectAlternativeName);
                        if (asn1OctetString != null)
                        {
                            var asn1Object = X509ExtensionUtilities.FromExtensionValue(asn1OctetString);
                            GeneralNames gns = GeneralNames.GetInstance(asn1Object);
                            if (gns.GetNames().Any(n => n.TagNo == GeneralName.UniformResourceIdentifier && n.Name.ToString() == applicationUri))
                            {
                                using (var keyStream = new StreamReader(keyInfo.OpenRead()))
                                {
                                    var keyReader = new PemReader(keyStream);
                                    var keyPair = keyReader.ReadObject() as AsymmetricCipherKeyPair;
                                    if (keyPair != null)
                                    {
                                        key = keyPair.Private as RsaPrivateCrtKeyParameters;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (crt != null && key != null)
            {
                this.logger?.LogTrace($"Found certificate with subject alt name '{applicationUri}'.");
                return new Tuple<X509Certificate, RsaPrivateCrtKeyParameters>(crt, key);
            }

            if (!this.CreateLocalCertificateIfNotExist)
            {
                return null;
            }

            // Create new certificate
            var subjectDN = new X509Name(subjectName);

            // Create a keypair.
            RsaKeyPairGenerator kg = new RsaKeyPairGenerator();
            kg.Init(new KeyGenerationParameters(this.rng, 2048));
            AsymmetricCipherKeyPair kp = kg.GenerateKeyPair();
            key = kp.Private as RsaPrivateCrtKeyParameters;

            // Create a certificate.
            X509V3CertificateGenerator cg = new X509V3CertificateGenerator();
            var subjectSN = BigInteger.ProbablePrime(120, this.rng);
            cg.SetSerialNumber(subjectSN);
            cg.SetSubjectDN(subjectDN);
            cg.SetIssuerDN(subjectDN);
            cg.SetNotBefore(DateTime.Now);
            cg.SetNotAfter(DateTime.Now.AddYears(25));
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

            crt = cg.Generate(new Asn1SignatureFactory("SHA256WITHRSA", key, this.rng));

            this.logger?.LogTrace($"Created certificate with subject alt name '{applicationUri}'.");

            if (!keyInfo.Directory.Exists)
            {
                Directory.CreateDirectory(keyInfo.DirectoryName);
            }
            else if (keyInfo.Exists)
            {
                keyInfo.Delete();
            }

            using (var keystream = new StreamWriter(keyInfo.OpenWrite()))
            {
                var pemwriter = new PemWriter(keystream);
                pemwriter.WriteObject(key);
            }

            if (!crtInfo.Directory.Exists)
            {
                Directory.CreateDirectory(crtInfo.DirectoryName);
            }
            else if (crtInfo.Exists)
            {
                crtInfo.Delete();
            }

            using (var crtstream = new StreamWriter(crtInfo.OpenWrite()))
            {
                var pemwriter = new PemWriter(crtstream);
                pemwriter.WriteObject(crt);
            }

            return new Tuple<X509Certificate, RsaPrivateCrtKeyParameters>(crt, key);
        }

        /// <inheritdoc/>
        public bool ValidateRemoteCertificate(X509Certificate target)
        {
            if (this.AcceptAllRemoteCertificates)
            {
                return true;
            }

            var trustedCerts = new Org.BouncyCastle.Utilities.Collections.HashSet();
            var intermediateCerts = new Org.BouncyCastle.Utilities.Collections.HashSet();
            var trustedCertsInfo = new DirectoryInfo(Path.Combine(this.pkiDirectoryPath, "trusted", "certs"));
            if (trustedCertsInfo.Exists)
            {
                foreach (var info in trustedCertsInfo.EnumerateFiles())
                {
                    using (var crtStream = info.OpenRead())
                    {
                        var crt = this.certParser.ReadCertificate(crtStream);
                        if (crt != null)
                        {
                            trustedCerts.Add(crt);
                        }
                    }
                }
            }

            var intermediateCertsInfo = new DirectoryInfo(Path.Combine(this.pkiDirectoryPath, "issuer", "certs"));
            if (intermediateCertsInfo.Exists)
            {
                foreach (var info in intermediateCertsInfo.EnumerateFiles())
                {
                    using (var crtStream = info.OpenRead())
                    {
                        var crt = this.certParser.ReadCertificate(crtStream);
                        if (crt != null)
                        {
                            intermediateCerts.Add(crt);
                        }
                    }
                }
            }

            if (IsSelfSigned(target))
            {
                // Create the selector that specifies the starting certificate
                var selector = new X509CertStoreSelector()
                {
                    Certificate = target
                };
                IX509Store trustedCertStore = X509StoreFactory.Create("Certificate/Collection", new X509CollectionStoreParameters(trustedCerts));
                return trustedCertStore.GetMatches(selector).Count > 0;
            }

            try
            {
                var res = VerifyCertificate(target, trustedCerts, intermediateCerts);
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        private static PkixCertPathBuilderResult VerifyCertificate(X509Certificate target, Org.BouncyCastle.Utilities.Collections.HashSet trustedRootCerts, Org.BouncyCastle.Utilities.Collections.HashSet intermediateCerts)
        {
            intermediateCerts.Add(target);

            // Create the selector that specifies the starting certificate
            var selector = new X509CertStoreSelector()
            {
                Certificate = target
            };

            // Create the trust anchors (set of root CA certificates)
            var trustAnchors = new Org.BouncyCastle.Utilities.Collections.HashSet();
            foreach (X509Certificate trustedRootCert in trustedRootCerts)
            {
                trustAnchors.Add(new TrustAnchor(trustedRootCert, null));
            }

            PkixBuilderParameters pkixParams = new PkixBuilderParameters(trustAnchors, selector)
            {

                // Disable CRL checks (this is done manually as additional step)
                IsRevocationEnabled = false
            };

            // Specify a list of intermediate certificates
            IX509Store intermediateCertStore = X509StoreFactory.Create("Certificate/Collection", new X509CollectionStoreParameters(intermediateCerts));
            pkixParams.AddStore(intermediateCertStore);

            // Build and verify the certification chain
            PkixCertPathBuilder builder = new PkixCertPathBuilder();
            PkixCertPathBuilderResult result = builder.Build(pkixParams);
            return result;
        }

        /// <summary>
        /// Checks whether given <see cref="X509Certificate"/> is self-signed.
        /// </summary>
        /// <param name="cert">an <see cref="X509Certificate"/>.</param>
        /// <returns>True, if self signed.</returns>
        public static bool IsSelfSigned(X509Certificate cert)
        {
            try
            {
                // Try to verify certificate signature with its own public key
                var key = cert.GetPublicKey();
                cert.Verify(key);
                return true;
            }
            catch (SignatureException)
            {
                // Invalid signature --> not self-signed
                return false;
            }
            catch (InvalidKeyException)
            {
                // Invalid key --> not self-signed
                return false;
            }
        }
    }
}