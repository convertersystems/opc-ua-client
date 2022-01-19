using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.X509;
using System;
using System.Threading;
using System.Threading.Tasks;
using Workstation.ServiceModel.Ua;

namespace Workstation.UaClient
{
    public class ThrowingTestCertificateStore : ITestCertificateStore
    {
        public byte[] ServerCertificate => null;

        public byte[] ClientCertificate => null;

        public Task<(X509Certificate Certificate, RsaKeyParameters Key)> GetLocalCertificateAsync(ApplicationDescription applicationDescription, ILogger logger, CancellationToken token)
            => throw new NotImplementedException();

        public Task<bool> ValidateRemoteCertificateAsync(X509Certificate certificate, ILogger logger, CancellationToken token)
            => throw new NotImplementedException();
    }
}