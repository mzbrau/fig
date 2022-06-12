using System.Security.Cryptography.X509Certificates;
using Fig.Api.Encryption;
using Fig.Contracts.ImportExport;

namespace Fig.Api.Services;

public interface IEncryptionService
{
    int InputLimit { get; }

    EncryptionResultModel Encrypt(string plainText);

    string Decrypt(string encryptedText, string thumbprint);

    void ImportCertificate(X509Certificate2 certificate);

    List<CertificateMetadataDataContract> GetAllCertificatesInStore();

    CertificateStatus GetCertificateStatus();

    void MigrateToCertificate(string thumbprint);

    byte[]? GetCertificate(string thumbprint);

    byte[] CreateCertificate();
}