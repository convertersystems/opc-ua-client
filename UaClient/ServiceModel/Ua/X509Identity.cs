// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkix;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;
using Org.BouncyCastle.X509.Store;

namespace Workstation.ServiceModel.Ua
{
    public class X509Identity : IUserIdentity
    {
        public X509Identity(X509Certificate certificate, RsaKeyParameters privateKey)
        {
            this.Certificate = certificate;
            this.PrivateKey = privateKey;
        }

        public X509Certificate Certificate { get; }

        public RsaKeyParameters PrivateKey { get; }
    }
}
