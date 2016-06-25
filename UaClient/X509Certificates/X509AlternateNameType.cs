// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Workstation.Security.Cryptography.X509Certificates
{
    /// <summary>
    /// Types of alternate names that can be applied to an X509 certificate
    /// </summary>
    public enum X509AlternateNameType
    {
        None = 0,

        /// <summary>
        ///     Alternate name that isn't one of the standard alternate name types.  This corresponds to the
        ///     CERT_ALT_NAME_OTHER_NAME type.
        /// </summary>
        OtherName = 1,

        /// <summary>
        ///     Alternate name represented as an email address as defined in RFC 822.  This corresponds to the
        ///     CERT_ALT_NAME_RFC822_NAME type.
        /// </summary>
        Rfc822Name = 2,

        /// <summary>
        ///     Alternate name represented as a DNS name.  This corresponds to the CERT_ALT_NAME_DNS_NAME type.
        /// </summary>
        DnsName = 3,

        /// <summary>
        ///     Alternate name represented as an x400 address.  This corresponds to the
        ///     CERT_ALT_NAME_X400_ADDRESS type.
        /// </summary>
        X400Address = 4,

        /// <summary>
        ///     Alternate name given as a directory name.  This corresponds to the
        ///     CERT_ALT_NAME_DIRECTORY_NAME type.
        /// </summary>
        DirectoryName = 5,

        /// <summary>
        ///     Alternate name given as an EDI party name.  This corresponds to the
        ///     CERT_ALT_NAME_EDI_PARTY_NAME type.
        /// </summary>
        EdiPartyName = 6,

        /// <summary>
        ///     Alternate URL.  This corresponds to the CERT_ALT_NAME_URL type.
        /// </summary>
        Url = 7,

        /// <summary>
        ///     Alternate name as an IP address.  This corresponds to the CERT_ALT_NAME_IP_ADDRESS type.
        /// </summary>
        IPAddress = 8,

        /// <summary>
        ///     Alternate name as a registered ID.  This corresponds to the CERT_ALT_NAME_REGISTERED_ID type.
        /// </summary>
        RegisteredId = 9,
    }
}