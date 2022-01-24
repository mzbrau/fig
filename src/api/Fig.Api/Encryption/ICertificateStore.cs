using System.Security.Cryptography.X509Certificates;

namespace Fig.Api.Encryption;

public interface ICertificateStore
{
    X509Certificate2? GetCertificate(string thumbprint);

    void SaveCertificate(X509Certificate2 certificate);
}