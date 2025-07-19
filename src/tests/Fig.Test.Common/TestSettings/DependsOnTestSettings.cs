using System.Collections.Generic;
using Fig.Client.Attributes;
using Microsoft.Extensions.Logging;

namespace Fig.Test.Common.TestSettings;

public class DependsOnTestSettings : TestSettingsBase
{
    public override string ClientName => "DependsOnTestSettings";
    public override string ClientDescription => "Test settings for DependsOn functionality";

    [Setting("Enable features")]
    public bool EnableFeatures { get; set; } = false;

    [Setting("Feature A setting")]
    [DependsOn(nameof(EnableFeatures), true)]
    public string? FeatureASetting { get; set; } = "Default A";

    [Setting("Feature B setting")]
    [DependsOn(nameof(EnableFeatures), true)]
    public int FeatureBSetting { get; set; } = 42;

    [Setting("Connection type")]
    [ValidValues("Database", "File", "Memory")]
    public string ConnectionType { get; set; } = "Memory";

    [Setting("Database connection string")]
    [DependsOn(nameof(ConnectionType), "Database")]
    public string? DatabaseConnection { get; set; }

    [Setting("File path")]
    [DependsOn(nameof(ConnectionType), "File")]
    public string? FilePath { get; set; }

    [Setting("Cache settings")]
    [DependsOn(nameof(ConnectionType), "Database", "File")]
    public bool EnableCaching { get; set; } = true;

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }
}
