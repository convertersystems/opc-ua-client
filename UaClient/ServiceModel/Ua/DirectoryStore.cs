// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkix;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Security.Certificates;
using Org.BouncyCastle.Utilities.Collections;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;
using Org.BouncyCastle.X509.Store;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// A certificate store.
    /// </summary>
    public class DirectoryStore : ICertificateStore
    {
        private readonly string _pkiPath;
        private readonly X509CertificateParser _certParser = new X509CertificateParser();
        private readonly SecureRandom _rng = new SecureRandom();

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryStore"/> class.
        /// </summary>
        /// <param name="path">The path to the local pki directory.</param>
        /// <param name="acceptAllRemoteCertificates">Set true to accept all remote certificates.</param>
        /// <param name="createLocalCertificateIfNotExist">Set true to create a local certificate and private key, if the files do not exist.</param>
        public DirectoryStore(string path, bool acceptAllRemoteCertificates = true, bool createLocalCertificateIfNotExist = true)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            _pkiPath = path;
            AcceptAllRemoteCertificates = acceptAllRemoteCertificates;
            CreateLocalCertificateIfNotExist = createLocalCertificateIfNotExist;
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
        public async Task<(X509Certificate? Certificate, RsaKeyParameters? Key)> GetLocalCertificateAsync(ApplicationDescription applicationDescription, ILogger? logger = null, CancellationToken token = default)
        {
            if (applicationDescription == null)
            {
                throw new ArgumentNullException(nameof(applicationDescription));
            }

            string? applicationUri = applicationDescription.ApplicationUri;
            if (string.IsNullOrEmpty(applicationUri))
            {
                throw new ArgumentOutOfRangeException(nameof(applicationDescription), "Expecting ApplicationUri in the form of 'http://{hostname}/{appname}' -or- 'urn:{hostname}:{appname}'.");
            }

            string? subjectName = null;
            string? hostName = null;
            string? appName = null;

            UriBuilder appUri = new UriBuilder(applicationUri);
            if (appUri.Scheme == "http" && !string.IsNullOrEmpty(appUri.Host))
            {
                var path = appUri.Path.Trim('/');
                if (!string.IsNullOrEmpty(path))
                {
                    hostName = appUri.Host;
                    appName = path;
                    subjectName = $"CN={appName},DC={hostName}";
                }
            }

            if (appUri.Scheme == "urn")
            {
                var parts = appUri.Path.Split(new[] { ':' }, 2);
                if (parts.Length == 2)
                {
                    hostName = parts[0];
                    appName = parts[1];
                    subjectName = $"CN={appName},DC={hostName}";
                }
            }

            if (subjectName == null)
            {
                throw new ArgumentOutOfRangeException(nameof(applicationDescription), "Expecting ApplicationUri in the form of 'http://{hostname}/{appname}' -or- 'urn:{hostname}:{appname}'.");
            }

            var crt = default(X509Certificate);
            var key = default(RsaKeyParameters);

            // Build 'own/certs' certificate store.
            var ownCerts = new HashSet<X509Certificate>();
            var ownCertsInfo = new DirectoryInfo(Path.Combine(_pkiPath, "own", "certs"));
            if (ownCertsInfo.Exists)
            {
                foreach (var info in ownCertsInfo.EnumerateFiles())
                {
                    using (var crtStream = info.OpenRead())
                    {
                        var c = _certParser.ReadCertificate(crtStream);
                        if (c != null)
                        {
                            ownCerts.Add(c);
                        }
                    }
                }
            }

            var ownCertStore = CollectionUtilities.CreateStore(ownCerts);

            // Select the newest certificate that matches by subject name.
            var selector = new X509CertStoreSelector()
            {
                Subject = new X509Name(subjectName)
            };
            crt = ownCertStore.EnumerateMatches(selector).OfType<X509Certificate>().OrderBy(c => c.NotBefore).LastOrDefault();
            if (crt != null)
            {
                // If certificate found, verify alt-name, and retrieve private key.
                var asn1OctetString = crt.GetExtensionValue(X509Extensions.SubjectAlternativeName);
                if (asn1OctetString != null)
                {
                    var asn1Object = X509ExtensionUtilities.FromExtensionValue(asn1OctetString);
                    GeneralNames gns = GeneralNames.GetInstance(asn1Object);
                    if (gns.GetNames().Any(n => n.TagNo == GeneralName.UniformResourceIdentifier && n.Name.ToString() == applicationUri))
                    {
                        var ki = new FileInfo(Path.Combine(_pkiPath, "own", "private", $"{crt.SerialNumber}.key"));
                        if (ki.Exists)
                        {
                            using (var keyStream = new StreamReader(ki.OpenRead()))
                            {
                                var keyReader = new PemReader(keyStream);
                                var keyPair = keyReader.ReadObject() as AsymmetricCipherKeyPair;
                                if (keyPair != null)
                                {
                                    key = keyPair.Private as RsaKeyParameters;
                                }
                            }
                        }
                    }
                }
            }

            // If certificate and key are found, return to caller.
            if (crt != null && key != null)
            {
                logger?.LogTrace("Found certificate with subject alt name '{applicationUri}'.", applicationUri);
                return (crt, key);
            }

            if (!CreateLocalCertificateIfNotExist)
            {
                return (null, null);
            }

            // Create new certificate
            var subjectDN = new X509Name(subjectName);

            // Create a keypair.
            var kp = await Task.Run<AsymmetricCipherKeyPair>(() =>
            {
                RsaKeyPairGenerator kg = new RsaKeyPairGenerator();
                kg.Init(new KeyGenerationParameters(_rng, 2048));
                return kg.GenerateKeyPair();
            });

            key = kp.Private as RsaPrivateCrtKeyParameters;

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
                new ExtendedKeyUsage(KeyPurposeID.id_kp_clientAuth, KeyPurposeID.id_kp_serverAuth));

            crt = cg.Generate(new Asn1SignatureFactory("SHA256WITHRSA", key, _rng));

            logger?.LogTrace("Created certificate with subject alt name '{applicationUri}'.", applicationUri);

            var keyInfo = new FileInfo(Path.Combine(_pkiPath, "own", "private", $"{crt.SerialNumber}.key"));
            if (keyInfo.Exists)
            {
                keyInfo.Delete();
            }
            Directory.CreateDirectory(Path.Combine(_pkiPath, "own", "private"));

            using (var keystream = new StreamWriter(keyInfo.OpenWrite()))
            {
                var pemwriter = new PemWriter(keystream);
                pemwriter.WriteObject(key);
            }

            var crtInfo = new FileInfo(Path.Combine(_pkiPath, "own", "certs", $"{crt.SerialNumber}.crt"));
            if (crtInfo.Exists)
            {
                crtInfo.Delete();
            }
            Directory.CreateDirectory(Path.Combine(_pkiPath, "own", "certs"));

            using (var crtstream = new StreamWriter(crtInfo.OpenWrite()))
            {
                var pemwriter = new PemWriter(crtstream);
                pemwriter.WriteObject(crt);
            }

            return (crt, key);
        }

        /// <inheritdoc/>
        public Task<bool> ValidateRemoteCertificateAsync(X509Certificate target, ILogger? logger = null, CancellationToken token = default)
        {
            if (AcceptAllRemoteCertificates)
            {
                return Task.FromResult(true);
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (!target.IsValidNow)
            {
                logger?.LogError($"Error validatingRemoteCertificate. Certificate is expired or not yet valid.");
                StoreInRejectedFolder(target);
                return Task.FromResult(false);
            }

            var trustedCerts = new HashSet<X509Certificate>();
            var trustedCertsInfo = new DirectoryInfo(Path.Combine(_pkiPath, "trusted"));
            if (!trustedCertsInfo.Exists)
            {
                trustedCertsInfo.Create();
            }

            foreach (var info in trustedCertsInfo.EnumerateFiles())
            {
                using (var crtStream = info.OpenRead())
                {
                    var crt = _certParser.ReadCertificate(crtStream);
                    if (crt != null)
                    {
                        trustedCerts.Add(crt);
                    }
                }
            }

            var intermediateCerts = new HashSet<X509Certificate>();
            var intermediateCertsInfo = new DirectoryInfo(Path.Combine(_pkiPath, "issuer"));
            if (!intermediateCertsInfo.Exists)
            {
                intermediateCertsInfo.Create();
            }

            foreach (var info in intermediateCertsInfo.EnumerateFiles())
            {
                using (var crtStream = info.OpenRead())
                {
                    var crt = _certParser.ReadCertificate(crtStream);
                    if (crt != null)
                    {
                        intermediateCerts.Add(crt);
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
                var trustedCertStore = CollectionUtilities.CreateStore(trustedCerts);
                if (trustedCertStore.EnumerateMatches(selector).Any())
                {
                    return Task.FromResult(true);
                }

                logger?.LogError("Error validatingRemoteCertificate.");
                StoreInRejectedFolder(target);
                return Task.FromResult(false);
            }

            try
            {
                var res = VerifyCertificate(target, trustedCerts, intermediateCerts);
            }
            catch (Exception ex)
            {
                logger?.LogError("Error validatingRemoteCertificate. {Message}", ex.Message);
                StoreInRejectedFolder(target);
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        private static PkixCertPathBuilderResult VerifyCertificate(X509Certificate target, HashSet<X509Certificate> trustedRootCerts, HashSet<X509Certificate> intermediateCerts)
        {
            intermediateCerts.Add(target);

            // Create the selector that specifies the starting certificate
            var selector = new X509CertStoreSelector()
            {
                Certificate = target
            };

            // Create the trust anchors (set of root CA certificates)
            var trustAnchors = new HashSet<TrustAnchor>();
            foreach (X509Certificate? trustedRootCert in trustedRootCerts)
            {
                trustAnchors.Add(new TrustAnchor(trustedRootCert, null));
            }

            PkixBuilderParameters pkixParams = new PkixBuilderParameters(trustAnchors, selector)
            {
                IsRevocationEnabled = false
            };

            // Specify a list of intermediate certificates
            pkixParams.AddStoreCert(CollectionUtilities.CreateStore(intermediateCerts));

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
        private static bool IsSelfSigned(X509Certificate cert)
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

        private void StoreInRejectedFolder(X509Certificate crt)
        {
            var crtInfo = new FileInfo(Path.Combine(_pkiPath, "rejected", $"{crt.SerialNumber}.crt"));
            if (crtInfo.Exists)
            {
                crtInfo.Delete();
            }

            Directory.CreateDirectory(Path.Combine(_pkiPath, "rejected"));

            using (var crtstream = new StreamWriter(crtInfo.OpenWrite()))
            {
                var pemwriter = new PemWriter(crtstream);
                pemwriter.WriteObject(crt);
            }
        }
    }
}