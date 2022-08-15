using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace Fig.Common.NetStandard.Cryptography;

public class Cryptography : ICryptography
{
    private readonly byte[] _salt =
        {0x10, 0x46, 0x55, 0x2e, 0x55, 0x2a, 0x10, 0xee, 0x11, 0x5c, 0xcc, 0xaa, 0x24, 0x1a, 0x6c, 0x23, 0x44, 0xbb};

    public string Encrypt(SecureString encryptionKey, string plainTextValue)
    {
        var clearBytes = Encoding.Unicode.GetBytes(plainTextValue);
        using var encryptor = Aes.Create();
        var pdb = new Rfc2898DeriveBytes(encryptionKey.Read(), _salt);
        encryptor.Key = pdb.GetBytes(32);
        encryptor.IV = pdb.GetBytes(16);
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
        {
            cs.Write(clearBytes, 0, clearBytes.Length);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(SecureString encryptionKey, string encryptedValue,
        List<SecureString>? fallbackEncryptionKeys = null)
    {
        if (fallbackEncryptionKeys == null)
            return DecryptInternal(encryptionKey, encryptedValue);

        fallbackEncryptionKeys.Insert(0, encryptionKey);

        foreach (var key in fallbackEncryptionKeys)
            try
            {
                return DecryptInternal(key, encryptedValue);
            }
            catch (CryptographicException)
            {
            }

        throw new ApplicationException("No valid encryption key was found to decrypt value");
    }

    private string DecryptInternal(SecureString encryptionKey, string encryptedValue)
    {
        var cipherBytes = Convert.FromBase64String(encryptedValue);
        using var encryptor = Aes.Create();
        var pdb = new Rfc2898DeriveBytes(encryptionKey.Read(), _salt);
        encryptor.Key = pdb.GetBytes(32);
        encryptor.IV = pdb.GetBytes(16);
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
        {
            cs.Write(cipherBytes, 0, cipherBytes.Length);
        }

        return Encoding.Unicode.GetString(ms.ToArray());
    }
}