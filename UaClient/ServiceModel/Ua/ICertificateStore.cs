// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Pkix;
using Org.BouncyCastle.X509;

namespace Workstation.ServiceModel.Ua
{
    public interface ICertificateStore
    {
        /// <summary>
        /// Gets the local certificate and private key.
        /// </summary>
        /// <returns>The local certificate and private key.</returns>
        Tuple<X509Certificate, RsaPrivateCrtKeyParameters> GetLocalCertificate(ApplicationDescription applicationDescription);

        /// <summary>
        /// Validates the remote certificate.
        /// </summary>
        /// <param name="certificate">the remote certificate.</param>
        /// <returns>The validator result.</returns>
        bool ValidateRemoteCertificate(X509Certificate certificate);
    }
}
