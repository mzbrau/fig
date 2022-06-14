namespace Fig.Api.Services;

public interface IEncryptionService
{
    string? Encrypt(string? plainText);

    string? Decrypt(string? encryptedText);
}