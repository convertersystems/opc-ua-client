using Workstation.ServiceModel.Ua;

namespace Workstation.UaClient
{
    public interface ITestCertificateStore : ICertificateStore
    {
        byte[] ServerCertificate { get; }
        byte[] ClientCertificate { get; }
    }
}
