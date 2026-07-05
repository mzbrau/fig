using System.Collections.Generic;
using System.Runtime.InteropServices;
using Fig.Client.AppSettings;
using Fig.Client.Contracts;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class FigOfflineConfigurationProviderTests
{
    [Test]
    public void Load_DecryptsEncryptedKeysAndExposesPlainKeys()
    {
        var sources = BuildSourcesWithEncryptedValue("Password_FigEncrypted", "enc:mysecret");
        var processor = new TestDpapiProcessor();
        var provider = new FigOfflineConfigurationProvider(sources, processor);

        provider.Load();

        Assert.That(provider.TryGet("Password", out var value), Is.True);
        Assert.That(value, Is.EqualTo("mysecret"));
    }

    [Test]
    public void Load_DoesNotExposeEncryptedKeyAsPlainKey()
    {
        var sources = BuildSourcesWithEncryptedValue("Password_FigEncrypted", "enc:mysecret");
        var processor = new TestDpapiProcessor();
        var provider = new FigOfflineConfigurationProvider(sources, processor);

        provider.Load();

        // The _FigEncrypted key comes from appsettings.json provider, not from the offline provider
        Assert.That(provider.TryGet("Password_FigEncrypted", out _), Is.False,
            "The offline provider should only expose decrypted keys, not the encrypted ones");
    }

    [Test]
    public void Load_IgnoresKeysWithoutEncryptedSuffix()
    {
        var sources = BuildSourcesWithEncryptedValue("RegularSetting", "plainvalue");
        var processor = new TestDpapiProcessor();
        var provider = new FigOfflineConfigurationProvider(sources, processor);

        provider.Load();

        Assert.That(provider.TryGet("RegularSetting", out _), Is.False,
            "Plain settings should not be re-exposed by the offline provider");
    }

    [Test]
    public void Load_SkipsKeyWhenDecryptionFails()
    {
        var sources = BuildSourcesWithEncryptedValue("Password_FigEncrypted", "invalid-encrypted-value");
        var processor = new FailingDpapiProcessor();
        var provider = new FigOfflineConfigurationProvider(sources, processor);

        // Should not throw (it logs to stderr and continues)
        Assert.DoesNotThrow(() => provider.Load());

        Assert.That(provider.TryGet("Password", out _), Is.False,
            "Failed decryption should result in the key being absent");
    }

    [Test]
    public void Load_RespectsHigherPriorityPlainValue()
    {
        // When a plain-text value exists alongside the encrypted one, the plain value takes precedence
        var inMemory = new Dictionary<string, string?>
        {
            ["Password_FigEncrypted"] = "enc:fromFile",
            ["Password"] = "explicitOverride"  // e.g., set via environment variable
        };
        var sources = new List<IConfigurationSource>
        {
            new Microsoft.Extensions.Configuration.Memory.MemoryConfigurationSource
            {
                InitialData = inMemory
            }
        };
        var processor = new TestDpapiProcessor();
        var provider = new FigOfflineConfigurationProvider(sources, processor);

        provider.Load();

        // The offline provider should NOT decrypt because a plain value already exists
        Assert.That(provider.TryGet("Password", out _), Is.False,
            "Offline provider should not override an explicit plain-text value");
    }

    [Test]
    public void Load_DoesNothingWhenDpapiIsNotSupported()
    {
        var sources = BuildSourcesWithEncryptedValue("Password_FigEncrypted", "enc:secret");
        var processor = new TestDpapiProcessor { SupportOverride = false };
        var provider = new FigOfflineConfigurationProvider(sources, processor);

        provider.Load();

        Assert.That(provider.TryGet("Password", out _), Is.False,
            "No decryption should occur on non-Windows platforms");
    }

    [Test]
    public void Load_HandlesMultipleEncryptedSettings()
    {
        var inMemory = new Dictionary<string, string?>
        {
            ["Username_FigEncrypted"] = "enc:admin",
            ["Password_FigEncrypted"] = "enc:secret123",
            ["Host"] = "localhost"
        };
        var sources = new List<IConfigurationSource>
        {
            new Microsoft.Extensions.Configuration.Memory.MemoryConfigurationSource
            {
                InitialData = inMemory
            }
        };
        var processor = new TestDpapiProcessor();
        var provider = new FigOfflineConfigurationProvider(sources, processor);

        provider.Load();

        Assert.That(provider.TryGet("Username", out var username), Is.True);
        Assert.That(username, Is.EqualTo("admin"));
        Assert.That(provider.TryGet("Password", out var password), Is.True);
        Assert.That(password, Is.EqualTo("secret123"));
    }

    [Test]
    public void Load_IsCaseInsensitiveForEncryptedSuffix()
    {
        // Verify the suffix check is case-insensitive
        var inMemory = new Dictionary<string, string?>
        {
            ["Password_FIGENCRYPTED"] = "enc:mysecret"
        };
        var sources = new List<IConfigurationSource>
        {
            new Microsoft.Extensions.Configuration.Memory.MemoryConfigurationSource
            {
                InitialData = inMemory
            }
        };
        var processor = new TestDpapiProcessor();
        var provider = new FigOfflineConfigurationProvider(sources, processor);

        provider.Load();

        Assert.That(provider.TryGet("Password", out var value), Is.True);
        Assert.That(value, Is.EqualTo("mysecret"));
    }

    private static List<IConfigurationSource> BuildSourcesWithEncryptedValue(string key, string value)
    {
        var inMemory = new Dictionary<string, string?> { [key] = value };
        return new List<IConfigurationSource>
        {
            new Microsoft.Extensions.Configuration.Memory.MemoryConfigurationSource
            {
                InitialData = inMemory
            }
        };
    }

    private sealed class TestDpapiProcessor : IAppSettingsEncryptionProvider
    {
        public string Name => "Test";
        public bool SupportOverride { get; set; } = true;

        public bool IsSupported => SupportOverride;

        public string Encrypt(string plainText) => $"enc:{plainText}";

        public string Decrypt(string cipherText) => cipherText.StartsWith("enc:")
            ? cipherText.Substring(4)
            : cipherText;
    }

    private sealed class FailingDpapiProcessor : IAppSettingsEncryptionProvider
    {
        public string Name => "FailingTest";
        public bool IsSupported => true;

        public string Encrypt(string plainText) => throw new System.Exception("Encrypt not expected");

        public string Decrypt(string cipherText) => throw new System.Exception("Decryption failed");
    }
}
