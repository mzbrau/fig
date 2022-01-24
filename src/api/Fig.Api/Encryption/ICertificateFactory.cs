using System.Security.Cryptography.X509Certificates;

namespace Fig.Api.Encryption;

public interface ICertificateFactory
{
    X509Certificate2 Create(int keySize);
}