// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.X509;

namespace Workstation.ServiceModel.Ua
{
    /// <summary>
    /// The certificate store interface.
    /// </summary>
    public interface ICertificateStore
    {
        /// <summary>
        /// Gets the local certificate and private key.
        /// </summary>
        /// <param name="applicationDescription">The application description.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The local certificate and private key.</returns>
        Task<(X509Certificate? Certificate, RsaKeyParameters? Key)> GetLocalCertificateAsync(ApplicationDescription applicationDescription, ILogger? logger);

        /// <summary>
        /// Validates the remote certificate.
        /// </summary>
        /// <param name="certificate">The remote certificate.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The validator result.</returns>
        Task<bool> ValidateRemoteCertificateAsync(X509Certificate certificate, ILogger? logger);
    }
}
