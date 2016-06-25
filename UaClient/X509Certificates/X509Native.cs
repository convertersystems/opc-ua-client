// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Workstation.Security.Cryptography.X509Certificates
{
    internal static class X509Native
    {
        internal const string ADVAPI32 = "advapi32.dll";
        internal const string CRYPT32 = "crypt32.dll";
        internal const string KERNEL32 = "kernel32.dll";
        internal const string sz_CERT_STORE_PROV_MEMORY = "Memory";

        internal const string OID_RSA_RSA = "1.2.840.113549.1.1.1";
        internal const string OID_RSA_SHA1RSA = "1.2.840.113549.1.1.5";
        internal const string OID_RSA_SHA256RSA = "1.2.840.113549.1.1.11";
        internal const string OID_SUBJECT_ALT_NAME2 = "2.5.29.17";
        internal const string OID_ISSUER_ALT_NAME2 = "2.5.29.18";

        internal const uint PROV_RSA_FULL = 1;
        internal const uint AT_KEYEXCHANGE = 1;
        internal const uint AT_SIGNATURE = 2;

        internal const uint CRYPT_NEWKEYSET = 0x00000008;
        internal const uint CRYPT_DELETEKEYSET = 0x00000010;
        internal const uint CRYPT_VERIFYCONTEXT = 0xF0000000;
        internal const uint CRYPT_EXPORTABLE = 0x00000001;

        internal const uint X509_ASN_ENCODING = 0x00000001;
        internal const uint PKCS_7_ASN_ENCODING = 0x00010000;

        internal const uint ALG_CLASS_HASH = 4 << 13;
        internal const uint ALG_TYPE_ANY = 0;
        internal const uint ALG_SID_SHA1 = 4;
        internal const uint CALG_SHA1 = ALG_CLASS_HASH | ALG_TYPE_ANY | ALG_SID_SHA1;

        internal const uint CERT_X500_NAME_STR = 3;
        internal const uint CERT_NAME_STR_REVERSE_FLAG = 0x02000000;
        internal const uint CERT_STORE_CREATE_NEW_FLAG = 0x00002000;
        internal const uint CERT_STORE_ADD_NEW = 1;
        internal const uint CERT_KEY_PROV_INFO_PROP_ID = 2;
        internal const uint REPORT_NO_PRIVATE_KEY = 0x0001;
        internal const uint REPORT_NOT_ABLE_TO_EXPORT_PRIVATE_KEY = 0x0002;
        internal const uint EXPORT_PRIVATE_KEYS = 0x0004;

        [StructLayout(LayoutKind.Sequential)]
        internal struct SYSTEMTIME
        {
            internal short wYear;
            internal short wMonth;
            internal short wDayOfWeek;
            internal short wDay;
            internal short wHour;
            internal short wMinute;
            internal short wSecond;
            internal short wMilliseconds;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CRYPTOAPI_BLOB
        {
            internal uint cbData;
            internal IntPtr pbData;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CRYPT_BIT_BLOB
        {
            internal uint cbData;
            internal IntPtr pbData;
            internal uint cUnusedBits;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CRYPT_KEY_PROV_INFO
        {
            internal string pwszContainerName;
            internal string pwszProvName;
            internal uint dwProvType;
            internal uint dwFlags;
            internal uint cProvParam;
            internal IntPtr rgProvParam;
            internal uint dwKeySpec;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CRYPT_ALGORITHM_IDENTIFIER
        {
            [MarshalAs(UnmanagedType.LPStr)]
            internal string pszObjId;
            internal CRYPTOAPI_BLOB Parameters;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CERT_ALT_NAME_ENTRY
        {
            internal uint dwAltNameChoice;
            internal CERT_ALT_NAME_ENTRY_UNION Value;
        }

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
        internal struct CERT_ALT_NAME_ENTRY_UNION
        {
            [FieldOffset(0)]
            internal IntPtr pOtherName;
            [FieldOffset(0)]
            internal IntPtr pwszRfc822Name;
            [FieldOffset(0)]
            internal IntPtr pwszDNSName;
            [FieldOffset(0)]
            internal CRYPTOAPI_BLOB DirectoryName;
            [FieldOffset(0)]
            internal IntPtr pwszURL;
            [FieldOffset(0)]
            internal CRYPTOAPI_BLOB IPAddress;
            [FieldOffset(0)]
            internal IntPtr pszRegisteredID;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CERT_ALT_NAME_INFO
        {
            internal uint cAltEntry;
            internal IntPtr rgAltEntry; // PCERT_ALT_NAME_ENTRY
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CERT_EXTENSION
        {
            [MarshalAs(UnmanagedType.LPStr)]
            internal string pszObjId;
            internal bool fCritical;
            internal CRYPTOAPI_BLOB Value;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CERT_EXTENSIONS
        {
            internal uint cExtension;
            internal IntPtr rgExtension;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CERT_PUBLIC_KEY_INFO
        {
            CRYPT_ALGORITHM_IDENTIFIER Algorithm;
            CRYPT_BIT_BLOB PublicKey;
        }

        internal static class NativeMethods
        {
            [DllImport(KERNEL32, SetLastError = true, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool FileTimeToSystemTime(
                [In] ref long lpFileTime,
                out SYSTEMTIME lpSystemTime);

            [DllImport(KERNEL32, SetLastError = true, ExactSpelling = true)]
            internal static extern SafeLocalAllocHandle LocalAlloc([In] int uFlags, [In] UIntPtr sizetdwBytes);

            [DllImport(KERNEL32, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.SysInt)]
            internal static extern IntPtr LocalFree(
                [In, MarshalAs(UnmanagedType.SysInt)] IntPtr hMem);

            [DllImport(ADVAPI32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CryptAcquireContextW(
                out SafeCryptProvHandle providerContext,
                [MarshalAs(UnmanagedType.LPWStr)] string container,
                [MarshalAs(UnmanagedType.LPWStr)] string provider,
                uint providerType,
                uint flags);

            [DllImport(ADVAPI32, SetLastError = true, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CryptReleaseContext(
                IntPtr providerContext,
                uint flags);

            [DllImport(ADVAPI32, SetLastError = true, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CryptGenKey(
                SafeCryptProvHandle providerContext,
                uint algorithmId,
                uint flags,
                out SafeCryptKeyHandle cryptKeyHandle);

            [DllImport(ADVAPI32, SetLastError = true, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CryptDestroyKey(
                IntPtr cryptKeyHandle);

            [DllImport(CRYPT32, SetLastError = true, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CertStrToNameW(
            [In] uint dwCertEncodingType,
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszX500,
            [In] uint dwStrType,
            [In] IntPtr pvReserved,
            [In, Out] IntPtr pbEncoded,
            [In, Out] ref int pcbEncoded,
            [In, Out] IntPtr ppszError);

            [DllImport(CRYPT32, SetLastError = true, ExactSpelling = true)]
            internal static extern SafeCertContextHandle CertCreateSelfSignCertificate(
                SafeCryptProvHandle providerHandle,
                [In] ref CRYPTOAPI_BLOB subjectIssuerBlob,
                uint flags,
                [In] ref CRYPT_KEY_PROV_INFO keyProviderInformation,
                [In] ref CRYPT_ALGORITHM_IDENTIFIER signatureAlgorithm,
                [In] ref SYSTEMTIME startTime,
                [In] ref SYSTEMTIME endTime,
                [In] ref CERT_EXTENSIONS extensions);

            [DllImport(CRYPT32, SetLastError = true, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CertFreeCertificateContext(
                IntPtr certificateContext);

            [DllImport(CRYPT32, SetLastError = true, ExactSpelling = true)]
            internal static extern SafeCertStoreHandle CertOpenStore(
                [MarshalAs(UnmanagedType.LPStr)] string storeProvider,
                uint messageAndCertificateEncodingType,
                IntPtr cryptProvHandle,
                uint flags,
                IntPtr parameters);

            [DllImport(CRYPT32, SetLastError = true, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CertCloseStore(
                IntPtr certificateStoreHandle,
                uint flags);

            [DllImport(CRYPT32, SetLastError = true, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CertAddCertificateContextToStore(
                SafeCertStoreHandle certificateStoreHandle,
                SafeCertContextHandle certificateContext,
                uint addDisposition,
                out SafeCertContextHandle storeContextPtr);

            [DllImport(CRYPT32, SetLastError = true, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CertSetCertificateContextProperty(
                SafeCertContextHandle certificateContext,
                uint propertyId,
                uint flags,
                [In] ref CRYPT_KEY_PROV_INFO data);

            [DllImport(CRYPT32, SetLastError = true, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool PFXExportCertStoreEx(
                SafeCertStoreHandle certificateStoreHandle,
                ref CRYPTOAPI_BLOB pfxBlob,
                IntPtr password,
                IntPtr reserved,
                uint flags);

            [DllImport(CRYPT32, SetLastError = true, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CryptEncodeObjectEx(
                [In] uint dwCertEncodingType,
                [In, MarshalAs(UnmanagedType.LPStr)] string lpszStructType,
                [In] ref CERT_ALT_NAME_INFO pvStructInfo,
                [In] uint dwFlags,
                [In, MarshalAs(UnmanagedType.SysInt)] IntPtr pEncodePara,
                [Out] byte[] pvEncoded,
                [In, Out] ref int pcbEncoded);

            [DllImport(CRYPT32, SetLastError = true, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CryptExportPublicKeyInfoEx(
                [In] SafeCryptProvHandle hCryptProv,
                [In] uint dwKeySpec,
                [In] uint dwCertEncodingType,
                [In, MarshalAs(UnmanagedType.LPStr)] string pszPublicKeyObjId,
                [In] uint dwFlags,
                [In] IntPtr pvAuxInfo,
                [Out] IntPtr pInfo,
                [In, Out] ref int pcbInfo);

            [DllImport(CRYPT32, SetLastError = true, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CryptHashPublicKeyInfo(
                [In] SafeCryptProvHandle hCryptProv,
                [In] uint Algid,
                [In] uint dwFlags,
                [In] uint dwCertEncodingType,
                [In] IntPtr pInfo,
                [Out] byte[] pbComputedHash,
                [In, Out] ref int pcbComputedHash);
        }
    }
}
