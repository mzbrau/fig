using BenchmarkDotNet.Attributes;
using Fig.Common.NetStandard.Cryptography;

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
        var encryptedValue = crypto.Encrypt(key, Value);
        var value = crypto.Decrypt(key, encryptedValue);
    }

    [Benchmark]
    public void EncryptDecryptUsingFallback()
    {
        var key1 = Guid.NewGuid().ToString();
        var key2 = Guid.NewGuid().ToString();
        var crypto = new Cryptography();
        var encryptedValue = crypto.Encrypt(key1, Value);
        var value = crypto.Decrypt(key2, encryptedValue,
            key1);
    }
}