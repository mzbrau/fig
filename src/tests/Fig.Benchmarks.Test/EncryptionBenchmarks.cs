using System.Security;
using BenchmarkDotNet.Attributes;
using Fig.Common.Cryptography;

namespace Fig.Benchmarks.Test;

[MemoryDiagnoser]
public class EncryptionBenchmarks
{
    private const string Value = "This is a value to encrypt and decrypt";
    
    [Benchmark]
    public void EncryptDecrypt()
    {
        var key = Guid.NewGuid().ToString();
        var crypto = new Cryptography();
        var encryptedValue = crypto.Encrypt(key.ToSecureString(), Value);
        var value = crypto.Decrypt(key.ToSecureString(), encryptedValue);
    }

    [Benchmark]
    public void EncryptDecryptUsingFallback()
    {
        var key1 = Guid.NewGuid().ToString();
        var key2 = Guid.NewGuid().ToString();
        var crypto = new Cryptography();
        var encryptedValue = crypto.Encrypt(key1.ToSecureString(), Value);
        var value = crypto.Decrypt(key2.ToSecureString(), encryptedValue,
            new List<SecureString>() {key1.ToSecureString()});
    }
    
    [Benchmark]
    public void EncryptDecryptUsingTwoFallbacks()
    {
        var key1 = Guid.NewGuid().ToString();
        var key2 = Guid.NewGuid().ToString();
        var key3 = Guid.NewGuid().ToString();
        var crypto = new Cryptography();
        var encryptedValue = crypto.Encrypt(key1.ToSecureString(), Value);
        var value = crypto.Decrypt(key3.ToSecureString(), encryptedValue,
            new List<SecureString>() {key2.ToSecureString(), key1.ToSecureString()});
    }
}