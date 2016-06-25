// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using static Workstation.Security.Cryptography.X509Certificates.X509Native;

namespace Workstation.Security.Cryptography.X509Certificates
{
    public class X509SubjectAlternateNameExtension : X509Extension
    {
        public X509SubjectAlternateNameExtension(IList<X509AlternativeName> altNames, bool critical)
            : base(new Oid(OID_SUBJECT_ALT_NAME2), EncodeExtension(altNames), critical)
        {
        }

        private static byte[] EncodeExtension(IList<X509AlternativeName> altNames)
        {
            var certAltName = new CERT_ALT_NAME_INFO
            {
                cAltEntry = (uint)altNames.Count
            };
            var structSize = Marshal.SizeOf<CERT_ALT_NAME_ENTRY>();
            var altNamesBuffer = Marshal.AllocHGlobal(structSize * altNames.Count);
            var unionValues = new List<IntPtr>();
            byte[] data = null;
            int dataSize = 0;

            try
            {
                for (int index = 0, offset = 0; index < altNames.Count; index++, offset += structSize)
                {
                    var altName = new CERT_ALT_NAME_ENTRY
                    {
                        dwAltNameChoice = (uint)altNames[index].Type
                    };
                    switch (altNames[index].Type)
                    {
                        case X509AlternateNameType.DnsName:
                            altName.Value = new CERT_ALT_NAME_ENTRY_UNION
                            {
                                pwszDNSName = Marshal.StringToHGlobalUni((string)altNames[index].Value)
                            };
                            unionValues.Add(altName.Value.pwszDNSName);
                            break;
                        case X509AlternateNameType.Url:
                            altName.Value = new CERT_ALT_NAME_ENTRY_UNION
                            {
                                pwszURL = Marshal.StringToHGlobalUni((string)altNames[index].Value)
                            };
                            unionValues.Add(altName.Value.pwszURL);
                            break;
                        case X509AlternateNameType.IPAddress:
                            var ip = (IPAddress)altNames[index].Value;
                            var addressBytes = ip.GetAddressBytes();
                            var ipBytes = Marshal.AllocHGlobal(addressBytes.Length);
                            Marshal.Copy(addressBytes, 0, ipBytes, addressBytes.Length);
                            altName.Value = new CERT_ALT_NAME_ENTRY_UNION
                            {
                                IPAddress = new CRYPTOAPI_BLOB
                                {
                                    cbData = (uint)addressBytes.Length,
                                    pbData = ipBytes
                                }
                            };
                            unionValues.Add(ipBytes);
                            break;
                    }

                    Marshal.StructureToPtr(altName, altNamesBuffer + offset, false);
                }

                certAltName.rgAltEntry = altNamesBuffer;

                if (!NativeMethods.CryptEncodeObjectEx(
                   X509_ASN_ENCODING,
                   OID_SUBJECT_ALT_NAME2,
                   ref certAltName,
                   0,
                   IntPtr.Zero,
                   null,
                   ref dataSize))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                data = new byte[dataSize];

                if (!NativeMethods.CryptEncodeObjectEx(
                    X509_ASN_ENCODING,
                    OID_SUBJECT_ALT_NAME2,
                    ref certAltName,
                    0,
                    IntPtr.Zero,
                    data,
                    ref dataSize))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                return data;
            }
            finally
            {
                Marshal.FreeHGlobal(altNamesBuffer);
                unionValues.ForEach(Marshal.FreeHGlobal);
            }
        }
    }
}
