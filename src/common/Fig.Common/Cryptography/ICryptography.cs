using System.Collections.Generic;
using System.Security;

namespace Fig.Common.Cryptography;

public interface ICryptography
{
    string Encrypt(SecureString encryptionKey, string plainTextValue);

    string Decrypt(SecureString encryptionKey, string encryptedValue,
        List<SecureString>? fallbackEncryptionKeys = null);
}