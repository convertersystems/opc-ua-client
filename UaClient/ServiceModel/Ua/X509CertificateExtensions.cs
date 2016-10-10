// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Workstation.Security.Cryptography.X509Certificates;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// X509 Certificate functions.
    /// </summary>
    public static class X509CertificateExtensions
    {
        /// <summary>
        /// Searches the stores for certificate with subject name matching the host and path extracted from the applicationUri.
        /// </summary>
        /// <param name="description">The <see cref="ApplicationDescription"/>.</param>
        /// <param name="createIfNotFound">Creates a new self-signed certificate if one not found.</param>
        /// <returns>The certificate. </returns>
        public static X509Certificate2 GetCertificate(this ApplicationDescription description, bool createIfNotFound = true)
        {
            if (description == null)
            {
                throw new ArgumentNullException(nameof(description));
            }

            if (string.IsNullOrEmpty(description.ApplicationUri))
            {
                throw new ArgumentOutOfRangeException(nameof(description), "Expecting ApplicationUri in the form of 'http://{hostname}/{appname}'.");
            }

            string subjectName = null;

            UriBuilder appUri = new UriBuilder(description.ApplicationUri);
            if (appUri.Scheme == "http" && !string.IsNullOrEmpty(appUri.Host))
            {
                var path = appUri.Path.Trim('/');
                if (!string.IsNullOrEmpty(path))
                {
                    subjectName = $"CN={path}, DC={appUri.Host}";
                }
            }

            if (appUri.Scheme == "urn")
            {
                var parts = appUri.Path.Split(new[] { ':' }, 2);
                if (parts.Length == 2)
                {
                    subjectName = $"CN={parts[1]}, DC={parts[0]}";
                }
            }

            if (subjectName == null)
            {
                throw new ArgumentOutOfRangeException(nameof(description), "Expecting ApplicationUri in the form of 'http://{hostname}/{appname}' -or- 'urn:{hostname}:{appname}'.");
            }

            X509Certificate2 clientCertificate = null;
            X509Store store = null;
            List<X509Certificate2> foundCerts = new List<X509Certificate2>();

            // First check the Local Machine store.
            store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            try
            {
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                var certs = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, subjectName, false);
                if (certs.Count > 0)
                {
                    foundCerts.AddRange(certs.OfType<X509Certificate2>());
                }
            }
            catch (Exception ex)
            {
                EventSource.Log.Error($"Error opening X509Store '{store}'. {ex.Message}");
            }
            finally
            {
                store.Dispose();
            }

            // Then check the Current User store.
            store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            try
            {
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                var certs = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, subjectName, false);
                if (certs.Count > 0)
                {
                    foundCerts.AddRange(certs.OfType<X509Certificate2>());
                }
            }
            catch (Exception ex)
            {
                EventSource.Log.Error($"Error opening X509Store '{store}'. {ex.Message}");
            }
            finally
            {
                store.Dispose();
            }

            // Select the certificate that was created last.
            if (foundCerts.Count > 0)
            {
                clientCertificate = foundCerts.OrderBy(c => c.NotBefore).Last();
                EventSource.Log.Informational($"Found certificate '{subjectName}'.");
                return clientCertificate;
            }

            EventSource.Log.Informational($"Creating new certificate '{subjectName}'.");
            try
            {
                var pfx = CertificateGenerator.CreateSelfSignCertificatePfx(
                    subjectName,
                    DateTime.UtcNow,
                    DateTime.UtcNow.AddYears(25),
                    new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyCertSign, true),
                    new X509EnhancedKeyUsageExtension(new OidCollection { new Oid(EnhancedKeyUsageOids.ServerAuthentication), new Oid(EnhancedKeyUsageOids.ClientAuthentication) }, false),
                    new X509SubjectAlternateNameExtension(new[] { new X509AlternativeName { Type = X509AlternateNameType.Url, Value = description.ApplicationUri } }, true));

                clientCertificate = new X509Certificate2(pfx, (string)null, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.DefaultKeySet);

                // add cert to Current User store.
                store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                try
                {
                    store.Open(OpenFlags.ReadWrite | OpenFlags.OpenExistingOnly);
                    store.Add(clientCertificate);
                }
                finally
                {
                    store.Dispose();
                }
            }
            catch (Exception ex)
            {
                EventSource.Log.Error($"Error creating certificate '{subjectName}'. {ex.Message}");
            }

            return clientCertificate;
        }
    }
}