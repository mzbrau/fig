using System.Security.Cryptography.X509Certificates;
using Fig.Api.Encryption;

namespace Fig.Api.Services;

public interface IEncryptionService
{
    int InputLimit { get; }
    
    EncryptionResultModel Encrypt(string plainText);

    string Decrypt(string encryptedText, string thumbprint);

    void ImportCertificate(X509Certificate2 certificate);

    List<string> GetAllThumbprintsInStore();
    CertificateStatus GetCertificateStatus();
    void MigrateToNewCertificate();
}