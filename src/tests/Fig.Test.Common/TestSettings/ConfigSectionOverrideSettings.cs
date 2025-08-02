using Fig.Client.Attributes;

namespace Fig.Test.Common.TestSettings;

// Helper classes for nested settings
public class AppConfig
{
    public string AppName { get; set; } = "DefaultApp";
    public int AppVersion { get; set; } = 1;
}

public class DatabaseConfig
{
    public string ConnectionString { get; set; } = "Server=localhost;Database=TestDb";
    public int Timeout { get; set; } = 30;
}

public class ConfigurationItem
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public bool Enabled { get; set; } = true;
}

public class ConfigSectionOverrideSettings : TestSettingsBase
{
    public override string ClientName => "ConfigSectionOverrideSettings";
    public override string ClientDescription => "Settings with various configuration section overrides";

    // Simple string with single override
    [Setting("Basic string setting")]
    [ConfigurationSectionOverride("AppSettings")]
    public string BasicSetting { get; set; } = "DefaultValue";

    // Simple setting with multiple overrides
    [Setting("Setting with multiple configuration sections")]
    [ConfigurationSectionOverride("AppSettings", "ApplicationName")]
    [ConfigurationSectionOverride("Configuration", "AppName")]
    public string MultiSectionSetting { get; set; } = "MyApplication";

    // Integer setting with override
    [Setting("Integer setting")]
    [ConfigurationSectionOverride("AppSettings", "MaxConnections")]
    public int MaxConnections { get; set; } = 100;

    // Boolean setting with override
    [Setting("Boolean setting")]
    [ConfigurationSectionOverride("FeatureFlags")]
    public bool EnableFeature { get; set; } = true;

    // DateTime setting with override
    [Setting("DateTime setting")]
    [ConfigurationSectionOverride("AppSettings", "ExpiryDate")]
    public DateTime? ExpiryDate { get; set; } = null;

    // Nested settings class
    [NestedSetting]
    public DatabaseSettings? Database { get; set; }

    // DataGrid (collection) setting with override
    [Setting("Configuration items", defaultValueMethodName:nameof(GetDefaultConfigItems))]
    [ConfigurationSectionOverride("AppSettings", "ConfigItems")]
    [ConfigurationSectionOverride("Configuration", "Items")]
    public required List<ConfigurationItem> ConfigItems { get; set; }

    // JSON setting with override
    [Setting("Application configuration")]
    [ConfigurationSectionOverride("Application", "Config")]
    public AppConfig? ApplicationConfig { get; set; }

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }

    public static List<ConfigurationItem> GetDefaultConfigItems()
    {
        return
        [
            new() { Key = "Setting1", Value = "Value1" },
            new() { Key = "Setting2", Value = "Value2" }
        ];
    }
}

// Nested settings class
public class DatabaseSettings
{
    [Setting("Database connection string")]
    [ConfigurationSectionOverride("ConnectionStrings", "DefaultConnection")]
    public string ConnectionString { get; set; } = "Server=localhost;Database=DefaultDb";

    [Setting("Database timeout in seconds")]
    [ConfigurationSectionOverride("Database", "CommandTimeout")]
    public int Timeout { get; set; } = 30;

    [Setting("Database provider")]
    [ConfigurationSectionOverride("Database", "Provider")]
    [ConfigurationSectionOverride("ConnectionStrings", "ProviderName")]
    public string Provider { get; set; } = "SqlServer";
}