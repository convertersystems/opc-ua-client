// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using static Workstation.Security.Cryptography.X509Certificates.X509Native;

namespace Workstation.Security.Cryptography.X509Certificates
{
    /// <summary>
    /// An x509 certificate generator.
    /// </summary>
    public static class CertificateGenerator
    {
        /// <summary>
        /// Create a self-signed x509 certificate.
        /// </summary>
        /// <param name="subjectName">The distinguished name.</param>
        /// <param name="notBefore">The start time.</param>
        /// <param name="notAfter">the end time.</param>
        /// <param name="extensions">the extensions.</param>
        /// <returns>A byte array containing the certificate and private key encoded as PFX.</returns>
        public static byte[] CreateSelfSignCertificatePfx(
            string subjectName,
            DateTime notBefore,
            DateTime notAfter,
            params X509Extension[] extensions)
        {
            if (subjectName == null)
            {
                subjectName = string.Empty;
            }

            byte[] pfxData;

            SYSTEMTIME startSystemTime = ToSystemTime(notBefore);
            SYSTEMTIME endSystemTime = ToSystemTime(notAfter);
            string containerName = $"Created by Workstation. {Guid.NewGuid().ToString()}";

            GCHandle gcHandle = default(GCHandle);
            var providerContext = SafeCryptProvHandle.InvalidHandle;
            var cryptKey = SafeCryptKeyHandle.InvalidHandle;
            var certContext = SafeCertContextHandle.InvalidHandle;
            var certStore = SafeCertStoreHandle.InvalidHandle;
            var storeCertContext = SafeCertContextHandle.InvalidHandle;

            try
            {
                Check(NativeMethods.CryptAcquireContextW(
                    out providerContext,
                    containerName,
                    null,
                    PROV_RSA_FULL,
                    CRYPT_NEWKEYSET));

                Check(NativeMethods.CryptGenKey(
                    providerContext,
                    AT_KEYEXCHANGE,
                    CRYPT_EXPORTABLE | (2048 << 16), // 2048bit
                    out cryptKey));

                IntPtr pbEncoded = IntPtr.Zero;
                int cbEncoded = 0;

                Check(NativeMethods.CertStrToNameW(
                    X509_ASN_ENCODING | PKCS_7_ASN_ENCODING,
                    subjectName,
                    CERT_X500_NAME_STR | CERT_NAME_STR_REVERSE_FLAG,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    ref cbEncoded,
                    IntPtr.Zero));

                pbEncoded = Marshal.AllocHGlobal(cbEncoded);

                Check(NativeMethods.CertStrToNameW(
                    X509_ASN_ENCODING | PKCS_7_ASN_ENCODING,
                    subjectName,
                    CERT_X500_NAME_STR | CERT_NAME_STR_REVERSE_FLAG,
                    IntPtr.Zero,
                    pbEncoded,
                    ref cbEncoded,
                    IntPtr.Zero));

                var nameBlob = new CRYPTOAPI_BLOB
                {
                    cbData = (uint)cbEncoded,
                    pbData = pbEncoded
                };

                var kpi = new CRYPT_KEY_PROV_INFO
                {
                    pwszContainerName = containerName,
                    dwProvType = PROV_RSA_FULL,
                    dwKeySpec = AT_KEYEXCHANGE
                };

                var signatureAlgorithm = new CRYPT_ALGORITHM_IDENTIFIER
                {
                    pszObjId = OID_RSA_SHA256RSA,
                    Parameters = default(CRYPTOAPI_BLOB)
                };

                IntPtr pInfo = IntPtr.Zero;
                int cbInfo = 0;
                byte[] keyHash = null;
                int cbKeyHash = 0;

                try
                {
                    Check(NativeMethods.CryptExportPublicKeyInfoEx(
                        providerContext,
                        AT_KEYEXCHANGE,
                        X509_ASN_ENCODING | PKCS_7_ASN_ENCODING,
                        OID_RSA_RSA,
                        0,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        ref cbInfo));

                    pInfo = Marshal.AllocHGlobal(cbInfo);

                    Check(NativeMethods.CryptExportPublicKeyInfoEx(
                        providerContext,
                        AT_KEYEXCHANGE,
                        X509_ASN_ENCODING | PKCS_7_ASN_ENCODING,
                        OID_RSA_RSA,
                        0,
                        IntPtr.Zero,
                        pInfo,
                        ref cbInfo));

                    Check(NativeMethods.CryptHashPublicKeyInfo(
                        providerContext,
                        CALG_SHA1,
                        0,
                        X509_ASN_ENCODING | PKCS_7_ASN_ENCODING,
                        pInfo,
                        null,
                        ref cbKeyHash));

                    keyHash = new byte[cbKeyHash];

                    Check(NativeMethods.CryptHashPublicKeyInfo(
                        providerContext,
                        CALG_SHA1,
                        0,
                        X509_ASN_ENCODING | PKCS_7_ASN_ENCODING,
                        pInfo,
                        keyHash,
                        ref cbKeyHash));
                }
                finally
                {
                    if (pInfo != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(pInfo);
                    }
                }

                var safeExtensions = new List<SafeX509Extension>();
                var blob = IntPtr.Zero;
                try
                {
                    foreach (var item in extensions)
                    {
                        safeExtensions.Add(new SafeX509Extension(item));
                    }

                    // adding SubjectKeyIdentifier TODO: AuthKeyIdentifier?
                    safeExtensions.Add(new SafeX509Extension(new X509SubjectKeyIdentifierExtension(keyHash, false)));

                    var structSize = Marshal.SizeOf<CERT_EXTENSION>();
                    blob = Marshal.AllocHGlobal(structSize * safeExtensions.Count);
                    for (int index = 0, offset = 0; index < safeExtensions.Count; index++, offset += structSize)
                    {
                        var marshalX509Extension = safeExtensions[index];
                        Marshal.StructureToPtr(marshalX509Extension.Value, blob + offset, false);
                    }

                    var certExtensions = new CERT_EXTENSIONS { cExtension = (uint)safeExtensions.Count, rgExtension = blob };

                    certContext = NativeMethods.CertCreateSelfSignCertificate(
                        providerContext,
                        ref nameBlob,
                        0,
                        ref kpi,
                        ref signatureAlgorithm,
                        ref startSystemTime,
                        ref endSystemTime,
                        ref certExtensions);
                    Check(!certContext.IsInvalid);
                }
                finally
                {
                    foreach (var safeExtension in safeExtensions)
                    {
                        safeExtension.Dispose();
                    }

                    safeExtensions.Clear();
                    Marshal.FreeHGlobal(blob);
                    Marshal.FreeHGlobal(pbEncoded);
                }

                certStore = NativeMethods.CertOpenStore(
                    sz_CERT_STORE_PROV_MEMORY,
                    0,
                    IntPtr.Zero,
                    CERT_STORE_CREATE_NEW_FLAG,
                    IntPtr.Zero);
                Check(!certStore.IsInvalid);

                Check(NativeMethods.CertAddCertificateContextToStore(
                    certStore,
                    certContext,
                    CERT_STORE_ADD_NEW,
                    out storeCertContext));

                NativeMethods.CertSetCertificateContextProperty(
                    storeCertContext,
                    CERT_KEY_PROV_INFO_PROP_ID,
                    0,
                    ref kpi);

                CRYPTOAPI_BLOB pfxBlob = default(CRYPTOAPI_BLOB);
                Check(NativeMethods.PFXExportCertStoreEx(
                    certStore,
                    ref pfxBlob,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    EXPORT_PRIVATE_KEYS | REPORT_NO_PRIVATE_KEY | REPORT_NOT_ABLE_TO_EXPORT_PRIVATE_KEY));

                pfxData = new byte[pfxBlob.cbData];
                gcHandle = GCHandle.Alloc(pfxData, GCHandleType.Pinned);
                pfxBlob.pbData = gcHandle.AddrOfPinnedObject();

                Check(NativeMethods.PFXExportCertStoreEx(
                    certStore,
                    ref pfxBlob,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    EXPORT_PRIVATE_KEYS | REPORT_NO_PRIVATE_KEY | REPORT_NOT_ABLE_TO_EXPORT_PRIVATE_KEY));

                gcHandle.Free();
            }
            finally
            {
                if (gcHandle.IsAllocated)
                {
                    gcHandle.Free();
                }

                if (!certContext.IsInvalid)
                {
                    certContext.Dispose();
                }

                if (!storeCertContext.IsInvalid)
                {
                    storeCertContext.Dispose();
                }

                if (!certStore.IsInvalid)
                {
                    certStore.Dispose();
                }

                if (!cryptKey.IsInvalid)
                {
                    cryptKey.Dispose();
                }

                if (!providerContext.IsInvalid)
                {
                    providerContext.Dispose();
                    providerContext = SafeCryptProvHandle.InvalidHandle;

                    // Delete generated keyset. Does not return a providerContext
                    NativeMethods.CryptAcquireContextW(
                        out providerContext,
                        containerName,
                        null,
                        PROV_RSA_FULL,
                        CRYPT_DELETEKEYSET);
                }
            }

            return pfxData;
        }

        private static SYSTEMTIME ToSystemTime(DateTime dateTime)
        {
            long fileTime = dateTime.ToFileTime();
            SYSTEMTIME systemTime;
            Check(NativeMethods.FileTimeToSystemTime(ref fileTime, out systemTime));
            return systemTime;
        }

        private static void Check(bool nativeCallSucceeded)
        {
            if (!nativeCallSucceeded)
            {
                int error = Marshal.GetHRForLastWin32Error();
                Marshal.ThrowExceptionForHR(error);
            }
        }
    }
}