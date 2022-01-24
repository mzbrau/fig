namespace Fig.Api.Services;

public interface IEncryptionService
{
    int InputLimit { get; }
    
    string Encrypt(string plainText);

    string Decrypt(string encryptedText);

    void UpdateInUseCertificate();
}