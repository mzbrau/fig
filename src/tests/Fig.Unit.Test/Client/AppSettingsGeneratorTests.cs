using System.Collections.Generic;
using Fig.Client.AppSettings;
using Fig.Client.Configuration;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class AppSettingsGeneratorTests
{
    private AppSettingsGeneratorTests.TestDpapiValueProcessor _dpapiProcessor = null!;
    private AppSettingsGenerator _generator = null!;

    [SetUp]
    public void SetUp()
    {
        _dpapiProcessor = new TestDpapiValueProcessor();
        _generator = new AppSettingsGenerator(_dpapiProcessor);
    }

    [Test]
    public void GetSettingsDictionary_WritesRegularSettingsWithDefaultValues()
    {
        var settings = new AllSettingsAndTypes();
        var dataContract = settings.CreateDataContract("AllSettingsAndTypes");
        var overrides = new Dictionary<string, string>();

        var result = _generator.GetSettingsDictionary(dataContract, overrides);

        Assert.That(result, Contains.Key("StringSetting"));
        Assert.That(result["StringSetting"], Is.EqualTo("Cat"));
        Assert.That(result, Contains.Key("IntSetting"));
        Assert.That(result["IntSetting"], Is.EqualTo("34"));
        Assert.That(result, Contains.Key("BoolSetting"));
        Assert.That(result["BoolSetting"], Is.EqualTo("True"));
    }

    [Test]
    public void GetSettingsDictionary_WritesEncryptedSuffixForSecretSettings()
    {
        var settings = new AllSettingsAndTypes();
        var dataContract = settings.CreateDataContract("AllSettingsAndTypes");
        var overrides = new Dictionary<string, string>();

        var result = _generator.GetSettingsDictionary(dataContract, overrides);

        Assert.That(result, Does.Not.ContainKey("SecretSetting"),
            "Plain secret key should NOT appear in the output");
        Assert.That(result, Contains.Key("SecretSetting" + AppSettingsGenerator.EncryptedSuffix));
        Assert.That(result["SecretSetting" + AppSettingsGenerator.EncryptedSuffix],
            Is.EqualTo("enc:SecretString"), "Default secret value should be encrypted");
    }

    [Test]
    public void GetSettingsDictionary_AppliesCommandLineOverridesForRegularSettings()
    {
        var settings = new AllSettingsAndTypes();
        var dataContract = settings.CreateDataContract("AllSettingsAndTypes");
        var overrides = new Dictionary<string, string> { { "StringSetting", "overridden" } };

        var result = _generator.GetSettingsDictionary(dataContract, overrides);

        Assert.That(result["StringSetting"], Is.EqualTo("overridden"));
    }

    [Test]
    public void GetSettingsDictionary_AppliesCommandLineOverridesForSecretSettings()
    {
        var settings = new AllSettingsAndTypes();
        var dataContract = settings.CreateDataContract("AllSettingsAndTypes");
        var overrides = new Dictionary<string, string> { { "SecretSetting", "mypassword" } };

        var result = _generator.GetSettingsDictionary(dataContract, overrides);

        Assert.That(result["SecretSetting" + AppSettingsGenerator.EncryptedSuffix],
            Is.EqualTo("enc:mypassword"), "Overridden secret value should be encrypted");
    }

    [Test]
    public void GetSettingsDictionary_OmitsSecretSettingsWhenDpapiUnsupported()
    {
        _dpapiProcessor.SupportOverride = false;
        var settings = new AllSettingsAndTypes();
        var dataContract = settings.CreateDataContract("AllSettingsAndTypes");
        var overrides = new Dictionary<string, string>();

        var result = _generator.GetSettingsDictionary(dataContract, overrides);

        Assert.That(result, Does.Not.ContainKey("SecretSetting" + AppSettingsGenerator.EncryptedSuffix),
            "Secret settings should be omitted when DPAPI is not supported");
        Assert.That(result, Contains.Key("StringSetting"),
            "Non-secret settings should still be included");
    }

    [Test]
    public void GetSettingsDictionary_MatchesOverridesCaseInsensitively()
    {
        var settings = new AllSettingsAndTypes();
        var dataContract = settings.CreateDataContract("AllSettingsAndTypes");
        var overrides = new Dictionary<string, string> { { "stringsetting", "case-insensitive" } };

        var result = _generator.GetSettingsDictionary(dataContract, overrides);

        Assert.That(result["StringSetting"], Is.EqualTo("case-insensitive"));
    }

    [Test]
    public void ParseAppSettingsOverrides_ParsesKeyValuePairs()
    {
        var args = new[] { "--printappsettings", "Password=cat", "Name=John" };

        var result = FigCommandLine.ParseAppSettingsOverrides(args);

        Assert.That(result["Password"], Is.EqualTo("cat"));
        Assert.That(result["Name"], Is.EqualTo("John"));
    }

    [Test]
    public void ParseAppSettingsOverrides_StopsAtNextFlag()
    {
        var args = new[] { "--printappsettings", "Password=cat", "--someother-flag" };

        var result = FigCommandLine.ParseAppSettingsOverrides(args);

        Assert.That(result, Contains.Key("Password"));
        Assert.That(result.Count, Is.EqualTo(1));
    }

    [Test]
    public void ParseAppSettingsOverrides_ReturnsEmptyWhenFlagNotPresent()
    {
        var args = new[] { "--disable-fig=true", "Password=cat" };

        var result = FigCommandLine.ParseAppSettingsOverrides(args);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ParseAppSettingsOverrides_HandlesValuesWithQuotedStrings()
    {
        var args = new[] { "--printappsettings", "Name=\"John Doe\"" };

        var result = FigCommandLine.ParseAppSettingsOverrides(args);

        Assert.That(result["Name"], Is.EqualTo("John Doe"));
    }

    [Test]
    public void ParseAppSettingsOverrides_HandlesNullArgs()
    {
        var result = FigCommandLine.ParseAppSettingsOverrides(null);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void IsFigOffline_ReturnsTrueWhenArgPresent()
    {
        var args = new[] { "--figoffline", "--someother" };

        Assert.That(FigCommandLine.IsFigOffline(args), Is.True);
    }

    [Test]
    public void IsFigOffline_ReturnsFalseWhenArgAbsent()
    {
        var args = new[] { "--disable-fig=true" };

        Assert.That(FigCommandLine.IsFigOffline(args), Is.False);
    }

    [Test]
    public void IsFigOffline_ReturnsFalseForNullArgs()
    {
        Assert.That(FigCommandLine.IsFigOffline(null), Is.False);
    }

    // Test double for DPAPI — avoids real Windows ProtectedData in cross-platform tests
    internal sealed class TestDpapiValueProcessor : IDpapiValueProcessor
    {
        public bool SupportOverride { get; set; } = true;

        public bool IsSupported => SupportOverride;

        public string Encrypt(string plainText) => $"enc:{plainText}";

        public string Decrypt(string cipherText) => cipherText.StartsWith("enc:")
            ? cipherText.Substring(4)
            : cipherText;
    }
}
