using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Fig.Client.AppSettings;
using Fig.Client.Contracts;
using Fig.Client.ExtensionMethods;
using Fig.Test.Common.TestSettings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Fig.Integration.Test.Client;

/// <summary>
/// Tests for the full offline flow: generating appsettings.json with --printappsettings
/// and using --figoffline to decrypt encrypted settings at application startup.
/// These tests use a mock DPAPI processor so they run on all platforms.
/// </summary>
public class AppSettingsOfflineFlowTests
{
    [Test]
    public void ShallGenerateAppSettingsJsonWithDefaultValues()
    {
        var processor = new TestDpapiProcessor();
        var generator = new AppSettingsGenerator(processor);
        var settings = new AllSettingsAndTypes();
        var dataContract = settings.CreateDataContract("AllSettingsAndTypes");
        var overrides = new Dictionary<string, string>();

        var result = generator.GetSettingsDictionary(dataContract, overrides);

        Assert.That(result["StringSetting"], Is.EqualTo("Cat"));
        Assert.That(result["IntSetting"], Is.EqualTo("34"));
        Assert.That(result["BoolSetting"], Is.EqualTo("True"));
    }

    [Test]
    public void ShallEncryptSecretSettingsWithSuffix()
    {
        var processor = new TestDpapiProcessor();
        var generator = new AppSettingsGenerator(processor);
        var settings = new AllSettingsAndTypes();
        var dataContract = settings.CreateDataContract("AllSettingsAndTypes");
        var overrides = new Dictionary<string, string>();

        var result = generator.GetSettingsDictionary(dataContract, overrides);

        Assert.That(result, Does.Not.ContainKey("SecretSetting"),
            "Secret settings should not appear as plain keys");
        Assert.That(result, Contains.Key("SecretSetting_FigEncrypted"),
            "Secret settings should appear with _FigEncrypted suffix");
        Assert.That(result["SecretSetting_FigEncrypted"],
            Is.EqualTo("enc:SecretString"),
            "The default secret value should be encrypted");
    }

    [Test]
    public void ShallApplyOverrideValues()
    {
        var processor = new TestDpapiProcessor();
        var generator = new AppSettingsGenerator(processor);
        var settings = new AllSettingsAndTypes();
        var dataContract = settings.CreateDataContract("AllSettingsAndTypes");
        var overrides = new Dictionary<string, string>
        {
            { "StringSetting", "CustomValue" },
            { "SecretSetting", "MyPassword" }
        };

        var result = generator.GetSettingsDictionary(dataContract, overrides);

        Assert.That(result["StringSetting"], Is.EqualTo("CustomValue"),
            "Regular setting should use the provided override");
        Assert.That(result["SecretSetting_FigEncrypted"], Is.EqualTo("enc:MyPassword"),
            "Secret setting override should be encrypted");
    }

    [Test]
    public void ShallDecryptEncryptedSettingsInOfflineMode()
    {
        var encryptedSettings = new Dictionary<string, string?>
        {
            ["StringSetting"] = "PlainValue",
            ["SecretSetting_FigEncrypted"] = "enc:MySecret"
        };

        var sources = new List<IConfigurationSource>
        {
            new MemoryConfigurationSource { InitialData = encryptedSettings }
        };

        var processor = new TestDpapiProcessor();
        var provider = new FigOfflineConfigurationProvider(sources, processor);
        provider.Load();

        Assert.That(provider.TryGet("SecretSetting", out var decryptedValue), Is.True);
        Assert.That(decryptedValue, Is.EqualTo("MySecret"),
            "The offline provider should decrypt _FigEncrypted values");
    }

    [Test]
    public void ShallUseDecryptedValuesInOfflineModeConfiguration()
    {
        // Simulate an appsettings.json with encrypted settings
        var encryptedSettings = new Dictionary<string, string?>
        {
            ["StringSetting"] = "ValueFromFile",
            ["IntSetting"] = "99",
            ["BoolSetting"] = "False",
            ["SecretSetting_FigEncrypted"] = "enc:EncryptedPassword"
        };

        var preFigSources = new List<IConfigurationSource>
        {
            new MemoryConfigurationSource { InitialData = encryptedSettings }
        };

        var processor = new TestDpapiProcessor();

        // Simulate what FigConfigurationBuilder does with --figoffline:
        // Add MemorySource (the "appsettings.json") + FigOfflineConfigurationSource
        var configuration = new ConfigurationBuilder()
            .Add(new MemoryConfigurationSource { InitialData = encryptedSettings })
            .Add(new FigOfflineConfigurationSource(preFigSources, processor))
            .Build();

        var serviceCollection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        serviceCollection.Configure<AllSettingsAndTypes>(configuration);
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<AllSettingsAndTypes>>().Value;

        // Non-secret settings from the file
        Assert.That(options.StringSetting, Is.EqualTo("ValueFromFile"));
        Assert.That(options.IntSetting, Is.EqualTo(99));
        Assert.That(options.BoolSetting, Is.False);

        // Secret setting decrypted by offline provider (overrides _FigEncrypted from file)
        Assert.That(options.SecretSetting, Is.EqualTo("EncryptedPassword"),
            "Secret setting should be decrypted and available under its original name");
    }

    [Test]
    public void ShallNotDecryptWhenPlainValueAlreadyExistsAtHigherPriority()
    {
        // When a plain-text value exists at higher priority (e.g., env var), it should win
        var settings = new Dictionary<string, string?>
        {
            ["Password_FigEncrypted"] = "enc:EncryptedValue",
            ["Password"] = "ExplicitPlainTextOverride"  // higher-priority plain override
        };

        var preFigSources = new List<IConfigurationSource>
        {
            new MemoryConfigurationSource { InitialData = settings }
        };

        var processor = new TestDpapiProcessor();
        var provider = new FigOfflineConfigurationProvider(preFigSources, processor);

        provider.Load();

        // Offline provider should NOT override the explicit plain value
        Assert.That(provider.TryGet("Password", out _), Is.False,
            "Offline provider should not inject a decrypted value when a plain override already exists");
    }

    [Test]
    public void ShallNotFailWhenNoEncryptedSettingsPresent()
    {
        var plainSettings = new Dictionary<string, string?>
        {
            ["StringSetting"] = "Hello",
            ["IntSetting"] = "42"
        };

        var preFigSources = new List<IConfigurationSource>
        {
            new MemoryConfigurationSource { InitialData = plainSettings }
        };

        var processor = new TestDpapiProcessor();
        var provider = new FigOfflineConfigurationProvider(preFigSources, processor);

        Assert.DoesNotThrow(() => provider.Load());
        Assert.That(provider.TryGet("StringSetting", out _), Is.False,
            "Offline provider does not re-expose plain settings, only decrypted ones");
    }

    [Test]
    public void ShallGenerateValidJsonFileToCurrentDirectory()
    {
        var originalDir = Directory.GetCurrentDirectory();
        var tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tmpDir);
        Directory.SetCurrentDirectory(tmpDir);

        try
        {
            var processor = new TestDpapiProcessor();
            var generator = new AppSettingsGenerator(processor);
            var settings = new AllSettingsAndTypes();
            var dataContract = settings.CreateDataContract("AllSettingsAndTypes");
            var overrides = new Dictionary<string, string> { { "StringSetting", "CustomValue" } };

            generator.Generate(dataContract, overrides);

            var filePath = Path.Combine(tmpDir, "appsettings.json");
            Assert.That(File.Exists(filePath), Is.True, "appsettings.json should be written");

            var json = File.ReadAllText(filePath);
            Assert.That(json, Does.Contain("\"CustomValue\""), "Override value should appear in the file");
            Assert.That(json, Does.Contain("_FigEncrypted"), "Secret settings should have _FigEncrypted suffix");
            Assert.That(json, Does.Not.Contain("\"SecretSetting\""),
                "Plain secret key should not appear without the suffix");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
            if (Directory.Exists(tmpDir))
                Directory.Delete(tmpDir, true);
        }
    }

    // Test double for encryption — avoids real Windows ProtectedData in cross-platform tests
    private sealed class TestDpapiProcessor : IAppSettingsEncryptionProvider
    {
        public string Name => "Test";
        public bool IsSupported => true;

        public string Encrypt(string plainText) => $"enc:{plainText}";

        public string Decrypt(string cipherText) => cipherText.StartsWith("enc:")
            ? cipherText.Substring(4)
            : cipherText;
    }
}
