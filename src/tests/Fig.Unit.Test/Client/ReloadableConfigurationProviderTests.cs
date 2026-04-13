using System.Collections.Generic;
using System.Linq;
using Fig.Client;
using Fig.Client.Abstractions.Attributes;
using Fig.Client.Testing.Extensions;
using Fig.Client.Testing.Integration;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class ReloadableConfigurationProviderTests
{
    #region Test Settings Classes

    private class SimpleSettings : SettingsBase
    {
        public override string ClientDescription => "Simple settings";

        [Setting("A basic value")]
        public string BasicValue { get; set; } = "Hello";

        [Setting("An integer value")]
        public int Count { get; set; } = 42;

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    private class SettingsWithSectionOverride : SettingsBase
    {
        public override string ClientDescription => "Settings with section override";

        [Setting("A setting with configuration section override")]
        [ConfigurationSectionOverride("Serilog:Override", "Microsoft")]
        public string LogLevel { get; set; } = "Warning";

        [Setting("A setting with multiple overrides")]
        [ConfigurationSectionOverride("AppSettings", "ApplicationName")]
        [ConfigurationSectionOverride("Configuration", "AppName")]
        public string AppName { get; set; } = "MyApp";

        [Setting("A setting with override but no name override")]
        [ConfigurationSectionOverride("FeatureFlags")]
        public string EnableLogging { get; set; } = "true";

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    private class NestedSettingsWithOverride : SettingsBase
    {
        public override string ClientDescription => "Nested settings with override";

        [Setting("A top-level setting")]
        public string TopLevel { get; set; } = "TopValue";

        [NestedSetting]
        public NestedDbSettings Database { get; set; } = new();

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    public class NestedDbSettings
    {
        [Setting("Database name")]
        [ConfigurationSectionOverride("ConnectionStrings", "DefaultConnection")]
        public string ConnectionString { get; set; } = "Server=localhost;Database=TestDb";

        [Setting("Database timeout")]
        [ConfigurationSectionOverride("Database", "CommandTimeout")]
        public int Timeout { get; set; } = 30;
    }

    private class NestedMultiOverrideSettings : SettingsBase
    {
        public override string ClientDescription => "Nested settings with multiple overrides";

        [NestedSetting]
        public MultiOverrideDb Database { get; set; } = new();

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    public class MultiOverrideDb
    {
        [Setting("Provider name")]
        [ConfigurationSectionOverride("Database", "Provider")]
        [ConfigurationSectionOverride("ConnectionStrings", "ProviderName")]
        public string Provider { get; set; } = "SqlServer";
    }

    private class NestedWithoutOverrideSettings : SettingsBase
    {
        public override string ClientDescription => "Nested settings without override";

        [NestedSetting]
        public PlainNestedSettings Nested { get; set; } = new();

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    public class PlainNestedSettings
    {
        [Setting("Some value")]
        public string Value { get; set; } = "NestedValue";

        [Setting("Some number")]
        public int Number { get; set; } = 7;
    }

    private class DeepNestedSettings : SettingsBase
    {
        public override string ClientDescription => "Deeply nested settings";

        [NestedSetting]
        public Level1Settings Level1 { get; set; } = new();

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    public class Level1Settings
    {
        [NestedSetting]
        public Level2Settings Level2 { get; set; } = new();
    }

    public class Level2Settings
    {
        [Setting("Deep value")]
        [ConfigurationSectionOverride("DeepSection", "DeepKey")]
        public string DeepValue { get; set; } = "DeepDefault";
    }

    private class InheritedOverrideSettings : SettingsBase
    {
        public override string ClientDescription => "Settings with inherited ConfigurationSectionOverride";

        [NestedSetting]
        [ConfigurationSectionOverride("InheritedSection", "InheritedKey")]
        public InheritableNestedSettings Nested { get; set; } = new();

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    public class InheritableNestedSettings
    {
        [Setting("Child setting")]
        public string ChildValue { get; set; } = "ChildDefault";
    }

    private class NestedNoNameOverrideSettings : SettingsBase
    {
        public override string ClientDescription => "Nested with no setting name override";

        [NestedSetting]
        public NoNameOverrideDb Database { get; set; } = new();

        public override IEnumerable<string> GetValidationErrors() => [];
    }

    public class NoNameOverrideDb
    {
        [Setting("Connection string")]
        [ConfigurationSectionOverride("ConnectionStrings")]
        public string ConnStr { get; set; } = "Server=test";
    }

    #endregion

    #region Helper Methods

    private static IConfigurationRoot BuildConfiguration<T>(T settings, ConfigReloader<T>? reloader = null)
        where T : SettingsBase
    {
        reloader ??= new ConfigReloader<T>();
        return new ConfigurationBuilder()
            .AddIntegrationTestConfiguration(reloader, settings)
            .Build();
    }

    #endregion

    // 1. Non-nested settings load correctly
    [Test]
    public void ShallLoadNonNestedSettingsCorrectly()
    {
        var settings = new SimpleSettings();
        var config = BuildConfiguration(settings);

        Assert.That(config["BasicValue"], Is.EqualTo("Hello"));
        Assert.That(config["Count"], Is.EqualTo("42"));
    }

    // 2. Non-nested setting with ConfigurationSectionOverride populates override section
    [Test]
    public void ShallPopulateOverrideSectionForNonNestedSetting()
    {
        var settings = new SettingsWithSectionOverride();
        var config = BuildConfiguration(settings);

        // Main key is populated
        Assert.That(config["LogLevel"], Is.EqualTo("Warning"));

        // Override section is populated
        Assert.That(config["Serilog:Override:Microsoft"], Is.EqualTo("Warning"));
    }

    // 3. Non-nested setting with multiple ConfigurationSectionOverride attributes
    [Test]
    public void ShallPopulateMultipleOverrideSectionsForNonNestedSetting()
    {
        var settings = new SettingsWithSectionOverride();
        var config = BuildConfiguration(settings);

        Assert.That(config["AppName"], Is.EqualTo("MyApp"));
        Assert.That(config["AppSettings:ApplicationName"], Is.EqualTo("MyApp"));
        Assert.That(config["Configuration:AppName"], Is.EqualTo("MyApp"));
    }

    // 4. Non-nested setting with ConfigurationSectionOverride that has no SettingNameOverride
    [Test]
    public void ShallUsePropertyNameWhenNoSettingNameOverrideForNonNestedSetting()
    {
        var settings = new SettingsWithSectionOverride();
        var config = BuildConfiguration(settings);

        Assert.That(config["EnableLogging"], Is.EqualTo("true"));
        Assert.That(config["FeatureFlags:EnableLogging"], Is.EqualTo("true"));
    }

    // 5. Nested setting with ConfigurationSectionOverride populates override section (THE BUG)
    [Test]
    public void ShallPopulateOverrideSectionForNestedSetting()
    {
        var settings = new NestedSettingsWithOverride();
        var config = BuildConfiguration(settings);

        // Main nested keys are populated
        Assert.That(config["Database:ConnectionString"], Is.EqualTo("Server=localhost;Database=TestDb"));
        Assert.That(config["Database:Timeout"], Is.EqualTo("30"));

        // Override sections are populated
        Assert.That(config["ConnectionStrings:DefaultConnection"], Is.EqualTo("Server=localhost;Database=TestDb"));
        Assert.That(config["Database:CommandTimeout"], Is.EqualTo("30"));
    }

    // 6. Nested setting with multiple ConfigurationSectionOverride attributes
    [Test]
    public void ShallPopulateMultipleOverrideSectionsForNestedSetting()
    {
        var settings = new NestedMultiOverrideSettings();
        var config = BuildConfiguration(settings);

        Assert.That(config["Database:Provider"], Is.EqualTo("SqlServer"));
        Assert.That(config["ConnectionStrings:ProviderName"], Is.EqualTo("SqlServer"));
    }

    // 7. Nested setting WITHOUT ConfigurationSectionOverride still binds correctly
    [Test]
    public void ShallBindNestedSettingsWithoutOverrideCorrectly()
    {
        var settings = new NestedWithoutOverrideSettings();
        var config = BuildConfiguration(settings);

        Assert.That(config["Nested:Value"], Is.EqualTo("NestedValue"));
        Assert.That(config["Nested:Number"], Is.EqualTo("7"));
    }

    // 8. Reload via ConfigReloader updates both main and override keys for nested settings
    [Test]
    public void ShallUpdateOverrideSectionsOnReloadForNestedSettings()
    {
        var settings = new NestedSettingsWithOverride();
        var reloader = new ConfigReloader<SettingsBase>();
        var config = BuildConfiguration(settings, reloader);

        // Verify initial values
        Assert.That(config["ConnectionStrings:DefaultConnection"], Is.EqualTo("Server=localhost;Database=TestDb"));

        // Mutate and reload
        settings.Database.ConnectionString = "Server=prod;Database=ProdDb";
        settings.Database.Timeout = 60;
        reloader.Reload(settings);

        // Verify updated values in both main and override sections
        Assert.That(config["Database:ConnectionString"], Is.EqualTo("Server=prod;Database=ProdDb"));
        Assert.That(config["ConnectionStrings:DefaultConnection"], Is.EqualTo("Server=prod;Database=ProdDb"));
        Assert.That(config["Database:Timeout"], Is.EqualTo("60"));
        Assert.That(config["Database:CommandTimeout"], Is.EqualTo("60"));
    }

    // 9. Reload updates non-nested override sections too
    [Test]
    public void ShallUpdateOverrideSectionsOnReloadForNonNestedSettings()
    {
        var settings = new SettingsWithSectionOverride();
        var reloader = new ConfigReloader<SettingsBase>();
        var config = BuildConfiguration(settings, reloader);

        Assert.That(config["Serilog:Override:Microsoft"], Is.EqualTo("Warning"));

        settings.LogLevel = "Error";
        reloader.Reload(settings);

        Assert.That(config["LogLevel"], Is.EqualTo("Error"));
        Assert.That(config["Serilog:Override:Microsoft"], Is.EqualTo("Error"));
    }

    // 10. Deeply nested settings (2+ levels) with ConfigurationSectionOverride
    [Test]
    public void ShallPopulateOverrideSectionForDeeplyNestedSetting()
    {
        var settings = new DeepNestedSettings();
        var config = BuildConfiguration(settings);

        // Main deep nested key
        Assert.That(config["Level1:Level2:DeepValue"], Is.EqualTo("DeepDefault"));

        // Override section
        Assert.That(config["DeepSection:DeepKey"], Is.EqualTo("DeepDefault"));
    }

    // 11. ConfigurationSectionOverride inherited from parent NestedSetting attribute
    // Note: While ConfigurationSectionOverride is listed as inheritable in SettingsBase,
    // GetConfigurationSections() only reads direct property attributes. Inherited overrides
    // are available in SettingDetails.InheritedAttributes for the data contract but do NOT
    // propagate to the configuration provider's section mapping.
    [Test]
    public void ShallBindNestedSettingWithInheritedOverrideByDirectPath()
    {
        var settings = new InheritedOverrideSettings();
        var config = BuildConfiguration(settings);

        // Main nested key is populated
        Assert.That(config["Nested:ChildValue"], Is.EqualTo("ChildDefault"));
    }

    // 12. Nested ConfigurationSectionOverride with no SettingNameOverride uses leaf property name
    [Test]
    public void ShallUseLeafPropertyNameWhenNoSettingNameOverrideForNestedSetting()
    {
        var settings = new NestedNoNameOverrideSettings();
        var config = BuildConfiguration(settings);

        Assert.That(config["Database:ConnStr"], Is.EqualTo("Server=test"));
        // Should use the leaf name "ConnStr", not the full path "Database:ConnStr"
        Assert.That(config["ConnectionStrings:ConnStr"], Is.EqualTo("Server=test"));
    }

    // 13. Top-level setting is unaffected by nested setting overrides
    [Test]
    public void ShallNotAffectTopLevelSettingsWhenNestedHaveOverrides()
    {
        var settings = new NestedSettingsWithOverride();
        var config = BuildConfiguration(settings);

        Assert.That(config["TopLevel"], Is.EqualTo("TopValue"));
    }

    // 14. All keys present after reload (no stale keys from previous load)
    [Test]
    public void ShallClearAndRepopulateAllKeysOnReload()
    {
        var settings = new NestedSettingsWithOverride();
        var reloader = new ConfigReloader<SettingsBase>();
        var config = BuildConfiguration(settings, reloader);

        // First load
        Assert.That(config["ConnectionStrings:DefaultConnection"], Is.EqualTo("Server=localhost;Database=TestDb"));

        // Change settings and reload
        settings.Database.ConnectionString = "Server=new";
        reloader.Reload(settings);

        // Old value should be gone
        Assert.That(config["ConnectionStrings:DefaultConnection"], Is.EqualTo("Server=new"));
        Assert.That(config["Database:ConnectionString"], Is.EqualTo("Server=new"));
    }
}
