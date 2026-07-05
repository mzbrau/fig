using System;
using System.Collections.Generic;
using System.IO;
using Fig.Client.AppSettings;
using Fig.Client.ExtensionMethods;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Fig.Integration.Test.Client;

/// <summary>
/// Tests for the full offline flow: generating appsettings.json with --printappsettings
/// and using --figoffline to decrypt encrypted settings at application startup.
/// These tests use a mock encryption provider so they run on all platforms.
/// </summary>
public class AppSettingsOfflineFlowTests
{
    private readonly TestAppSettingsEncryptionProvider _encryptionProvider = new();

    [Test]
    public void ShallGenerateAppSettingsJsonWithDefaultValues()
    {
        var generator = CreateGenerator();
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
        var generator = CreateGenerator();
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
        var generator = CreateGenerator();
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

        var provider = new FigOfflineConfigurationProvider(sources, _encryptionProvider);
        provider.Load();

        Assert.That(provider.TryGet("SecretSetting", out var decryptedValue), Is.True);
        Assert.That(decryptedValue, Is.EqualTo("MySecret"),
            "The offline provider should decrypt _FigEncrypted values");
    }

    [Test]
    public void ShallUseDecryptedValuesInOfflineModeConfiguration()
    {
        var encryptedSettings = new Dictionary<string, string?>
        {
            ["StringSetting"] = "ValueFromFile",
            ["IntSetting"] = "99",
            ["BoolSetting"] = "False",
            ["SecretSetting_FigEncrypted"] = "enc:EncryptedPassword"
        };

        var options = BindOfflineSettings<AllSettingsAndTypes>(encryptedSettings);

        Assert.That(options.StringSetting, Is.EqualTo("ValueFromFile"));
        Assert.That(options.IntSetting, Is.EqualTo(99));
        Assert.That(options.BoolSetting, Is.False);
        Assert.That(options.SecretSetting, Is.EqualTo("EncryptedPassword"),
            "Secret setting should be decrypted and available under its original name");
    }

    [Test]
    public void ShallFlattenNestedSettingsAndBindInOfflineMode()
    {
        var settings = new ClientWithNestedSettings
        {
            Database = new Database { ConnectionString = "Server=db;", TimeoutMs = 30 },
            MessageBus = new MessageBus
            {
                Uri = "amqp://localhost",
                Auth = new Authorization { Username = "user", Password = "pass" }
            }
        };

        var flat = GenerateFlatSettings(settings, []);
        Assert.That(flat["MessageBus:Auth:Username"], Is.EqualTo("user"));
        Assert.That(flat["MessageBus:Uri"], Is.EqualTo("amqp://localhost"));
        Assert.That(flat["Database:ConnectionString"], Is.EqualTo("Server=db;"));

        var bound = BindOfflineSettings<ClientWithNestedSettings>(flat);
        Assert.That(bound.MessageBus?.Auth?.Username, Is.EqualTo("user"));
        Assert.That(bound.MessageBus?.Uri, Is.EqualTo("amqp://localhost"));
        Assert.That(bound.Database.ConnectionString, Is.EqualTo("Server=db;"));
    }

    [Test]
    public void ShallFlattenDataGridDefaultsAndBindInOfflineMode()
    {
        var settings = new ClientWithDataGridDefaults();
        var flat = GenerateFlatSettings(settings, []);

        Assert.That(flat["SimpleStringList:0"], Is.EqualTo("alpha"));
        Assert.That(flat["SimpleStringList:2"], Is.EqualTo("gamma"));
        Assert.That(flat["MultiColumnComplexList:1:Label"], Is.EqualTo("Second"));
        Assert.That(flat["MultiColumnComplexList:1:IsActive"], Is.EqualTo("False"));

        var bound = BindOfflineSettings<ClientWithDataGridDefaults>(flat);
        Assert.That(bound.SimpleStringList, Is.EqualTo(["alpha", "beta", "gamma"]));
        Assert.That(bound.MultiColumnComplexList[1].Label, Is.EqualTo("Second"));
        Assert.That(bound.MultiColumnComplexList[1].IsActive, Is.False);
    }

    [Test]
    public void ShallFlattenNestedDataGridCombinationAndBindInOfflineMode()
    {
        var settings = new SettingsWithNesting
        {
            School = new School()
        };

        var flat = GenerateFlatSettings(settings, []);
        Assert.That(flat["School:Name"], Is.Null);
        Assert.That(flat["School:Subjects:0:Name"], Is.EqualTo("Math"));
        Assert.That(flat["School:Subjects:0:Grade"], Is.EqualTo("90"));
        Assert.That(flat["School:Subjects:1:Name"], Is.EqualTo("English"));

        var bound = BindOfflineSettings<SettingsWithNesting>(flat);
        Assert.That(bound.School?.Subjects?[0].Name, Is.EqualTo("Math"));
        Assert.That(bound.School?.Subjects?[0].Grade, Is.EqualTo(90));
        Assert.That(bound.School?.Subjects?[1].Name, Is.EqualTo("English"));
    }

    [Test]
    public void ShallEncryptDataGridColumnSecretsAndBindInOfflineMode()
    {
        var settings = new SecretSettings { LoginsWithDefault = SecretSettings.GetDefaultLogins() };
        var flat = GenerateFlatSettings(settings, []);

        Assert.That(flat["LoginsWithDefault:0:Username"], Is.EqualTo("myUser"));
        Assert.That(flat, Does.Not.ContainKey("LoginsWithDefault:0:Password"));
        Assert.That(flat["LoginsWithDefault:0:Password_FigEncrypted"], Is.EqualTo("enc:myPassword"));
        Assert.That(flat["LoginsWithDefault:1:Password_FigEncrypted"], Is.EqualTo("enc:myPassword2"));

        var bound = BindOfflineSettings<SecretSettings>(flat);
        Assert.That(bound.LoginsWithDefault[0].Username, Is.EqualTo("myUser"));
        Assert.That(bound.LoginsWithDefault[0].Password, Is.EqualTo("myPassword"));
        Assert.That(bound.LoginsWithDefault[1].Password, Is.EqualTo("myPassword2"));
    }

    [Test]
    public void ShallApplyOverrideForDataGridColumnSecret()
    {
        var settings = new SecretSettings { LoginsWithDefault = SecretSettings.GetDefaultLogins() };
        var overrides = new Dictionary<string, string>
        {
            { "LoginsWithDefault:0:Password", "NewPass" }
        };

        var flat = GenerateFlatSettings(settings, overrides);
        Assert.That(flat["LoginsWithDefault:0:Password_FigEncrypted"], Is.EqualTo("enc:NewPass"));

        var bound = BindOfflineSettings<SecretSettings>(flat);
        Assert.That(bound.LoginsWithDefault[0].Password, Is.EqualTo("NewPass"));
        Assert.That(bound.LoginsWithDefault[1].Password, Is.EqualTo("myPassword2"));
    }

    [Test]
    public void ShallNotDecryptWhenPlainValueAlreadyExistsAtHigherPriority()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Password_FigEncrypted"] = "enc:EncryptedValue",
            ["Password"] = "ExplicitPlainTextOverride"
        };

        var preFigSources = new List<IConfigurationSource>
        {
            new MemoryConfigurationSource { InitialData = settings }
        };

        var provider = new FigOfflineConfigurationProvider(preFigSources, _encryptionProvider);
        provider.Load();

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

        var provider = new FigOfflineConfigurationProvider(preFigSources, _encryptionProvider);

        Assert.DoesNotThrow(() => provider.Load());
        Assert.That(provider.TryGet("StringSetting", out _), Is.False,
            "Offline provider does not re-expose plain settings, only decrypted ones");
    }

    [Test]
    [NonParallelizable]
    public void ShallGenerateValidJsonFileToCurrentDirectory()
    {
        var originalDir = Directory.GetCurrentDirectory();
        var tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tmpDir);
        Directory.SetCurrentDirectory(tmpDir);

        try
        {
            var generator = CreateGenerator();
            var settings = new AllSettingsAndTypes();
            var dataContract = settings.CreateDataContract("AllSettingsAndTypes");
            var overrides = new Dictionary<string, string> { { "StringSetting", "CustomValue" } };

            generator.Generate(dataContract, overrides);

            var filePath = Path.Combine(tmpDir, "appsettings.fig.json");
            Assert.That(File.Exists(filePath), Is.True, "appsettings.fig.json should be written");

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

    private AppSettingsGenerator CreateGenerator() => new(_encryptionProvider);

    private Dictionary<string, string?> GenerateFlatSettings(
        TestSettingsBase settings,
        Dictionary<string, string> overrides)
    {
        var generator = CreateGenerator();
        var dataContract = settings.CreateDataContract(settings.ClientName);
        return generator.GetSettingsDictionary(dataContract, overrides);
    }

    private T BindOfflineSettings<T>(Dictionary<string, string?> flatSettings) where T : class
    {
        var memorySource = new MemoryConfigurationSource { InitialData = flatSettings };
        var preFigSources = new List<IConfigurationSource> { memorySource };

        var configuration = new ConfigurationBuilder()
            .Add(memorySource)
            .Add(new FigOfflineConfigurationSource(preFigSources, _encryptionProvider))
            .Build();

        var serviceCollection = new ServiceCollection();
        serviceCollection.Configure<T>(configuration);
        return serviceCollection.BuildServiceProvider().GetRequiredService<IOptions<T>>().Value;
    }
}
