using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Fig.Common.NetStandard.Cryptography;

public class Cryptography : ICryptography
{
    private readonly byte[] _salt =
        {0x10, 0x46, 0x55, 0x2e, 0x55, 0x2a, 0x10, 0xee, 0x11, 0x5c, 0xcc, 0xaa, 0x24, 0x1a, 0x6c, 0x23, 0x44, 0xbb};
    
    // Default number of iterations for PBKDF2
    private const int DefaultIterationCount = 1000;
    
    // Cache for derived keys to avoid expensive PBKDF2 operations
    private readonly ConcurrentDictionary<string, KeyIvPair> _keyCache = new();
    
    // Struct to store key and IV pairs
    private readonly struct KeyIvPair
    {
        public readonly byte[] Key;
        public readonly byte[] IV;

        public KeyIvPair(byte[] key, byte[] iv)
        {
            Key = key;
            IV = iv;
        }
    }

    public string Encrypt(string encryptionKey, string plainTextValue)
    {
        if (string.IsNullOrEmpty(plainTextValue))
            return string.Empty;
            
        // Get bytes from the input string
        var clearBytes = Encoding.Unicode.GetBytes(plainTextValue);
        
        // Get or create key/IV pair
        var keyPair = GetOrCreateKeyPair(encryptionKey);
        
        using var encryptor = Aes.Create();
        encryptor.Key = keyPair.Key;
        encryptor.IV = keyPair.IV;
        
        // Estimate the size of the encrypted output (typically larger than input due to padding)
        int estimatedSize = clearBytes.Length + 32; // Add padding buffer
        
        // Use ArrayPool to rent a buffer instead of allocating a new one
        byte[] outputBuffer = ArrayPool<byte>.Shared.Rent(estimatedSize);
        try
        {
            int bytesWritten;
            using (var ms = new MemoryStream(outputBuffer))
            using (var cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
            {
                cs.Write(clearBytes, 0, clearBytes.Length);
                cs.FlushFinalBlock();
                bytesWritten = (int)ms.Position;
            }
            
            // Convert only the actual bytes written to Base64
            return Convert.ToBase64String(outputBuffer, 0, bytesWritten);
        }
        finally
        {
            // Return the buffer to the pool when done
            ArrayPool<byte>.Shared.Return(outputBuffer);
        }
    }

    public string Decrypt(string encryptionKey, string encryptedValue,
        string? fallbackEncryptionKey = null, bool tryFallbackFirst = false)
    {
        if (string.IsNullOrEmpty(encryptedValue))
            return string.Empty;
            
        var encryptionKeys = GetEncryptionKeys(encryptionKey, fallbackEncryptionKey, tryFallbackFirst);
        foreach (var key in encryptionKeys)
        {
            try
            {
                return DecryptInternal(key, encryptedValue);
            }
            catch (CryptographicException)
            {
                if (encryptionKeys.Count == 1)
                    throw;
            }
        }

        throw new ApplicationException("No valid encryption key was found to decrypt value. " +
                                       "You may need to perform an 'API Secret Encryption Migration' available from the configuration page.");
    }

    private string DecryptInternal(string encryptionKey, string encryptedValue)
    {
        // Get the encrypted bytes from the Base64 string
        byte[] cipherBytes = Convert.FromBase64String(encryptedValue);
        
        // Get or create key/IV pair
        var keyPair = GetOrCreateKeyPair(encryptionKey);
        
        using var encryptor = Aes.Create();
        encryptor.Key = keyPair.Key;
        encryptor.IV = keyPair.IV;
        
        // Use ArrayPool to rent a buffer for output (decrypted size is always <= encrypted size)
        byte[] outputBuffer = ArrayPool<byte>.Shared.Rent(cipherBytes.Length);
        try
        {
            int bytesWritten;
            using (var ms = new MemoryStream(outputBuffer))
            using (var cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
            {
                cs.Write(cipherBytes, 0, cipherBytes.Length);
                cs.FlushFinalBlock();
                bytesWritten = (int)ms.Position;
            }
            
            // Convert only the actual bytes written to string
            return Encoding.Unicode.GetString(outputBuffer, 0, bytesWritten);
        }
        finally
        {
            // Return the buffer to the pool when done
            ArrayPool<byte>.Shared.Return(outputBuffer);
        }
    }

    private KeyIvPair GetOrCreateKeyPair(string encryptionKey)
    {
        // Use the cache to avoid expensive PBKDF2 operations
        return _keyCache.GetOrAdd(encryptionKey, key => 
        {
            using var pdb = new Rfc2898DeriveBytes(key, _salt, DefaultIterationCount);
            return new KeyIvPair(
                pdb.GetBytes(32), // Key
                pdb.GetBytes(16)  // IV
            );
        });
    }

    private List<string> GetEncryptionKeys(
        string encryptionKey, 
        string? fallbackEncryptionKey = null,
        bool useFallbackFirst = false)
    {
        // Initialize with capacity to avoid resizing
        List<string> keys = new List<string>(2);

        if (useFallbackFirst && !string.IsNullOrWhiteSpace(fallbackEncryptionKey))
        {
            keys.Add(fallbackEncryptionKey!);
        }

        keys.Add(encryptionKey);

        if (!useFallbackFirst && !string.IsNullOrWhiteSpace(fallbackEncryptionKey))
        {
            keys.Add(fallbackEncryptionKey!);
        }

        return keys;
    }
}