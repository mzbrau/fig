using Fig.Api.Encryption;

namespace Fig.Api.Services;

public interface IEncryptionService
{
    int InputLimit { get; }
    
    EncryptionResultModel Encrypt(string plainText);

    string Decrypt(string encryptedText, string thumbprint);

    void UpdateInUseCertificate();
}