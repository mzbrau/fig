namespace Fig.Common.NetStandard.Cryptography;

public interface ICryptography
{
    string Encrypt(string encryptionKey, string plainTextValue);

    string Decrypt(string encryptionKey, string encryptedValue,
        string? fallbackEncryptionKey = null,  bool tryFallbackFirst = false);
}