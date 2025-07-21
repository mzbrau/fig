using Fig.Client.Attributes;

namespace Fig.Test.Common.TestSettings;

public class HeadingTestSettings : TestSettingsBase
{
    public override string ClientName => "HeadingTestSettings";
    public override string ClientDescription => "Test settings for Heading functionality";

    [Heading("Application Configuration")]
    [Setting("Application Name")]
    public string ApplicationName { get; set; } = "Test App";

    [Setting("Application Version")]
    public string ApplicationVersion { get; set; } = "1.0.0";

    [Heading("Database Settings", color: "#0066CC")]
    [Setting("Primary Database Connection")]
    public string PrimaryDbConnection { get; set; } = "Server=localhost;Database=Test";

    [Heading("Connection Pool", color: "#0066CC")]
    [Setting("Max Pool Size")]
    [Indent(1)]
    public int MaxPoolSize { get; set; } = 100;

    [Setting("Min Pool Size")]
    [Indent(1)]
    public int MinPoolSize { get; set; } = 10;

    [Heading("Advanced Database Options", color: "#0066CC")]
    [Advanced]
    [Setting("Enable Query Logging")]
    [Indent(1)]
    public bool EnableQueryLogging { get; set; } = false;

    [Setting("Query Timeout")]
    [Advanced]
    [Indent(1)]
    public int QueryTimeout { get; set; } = 30;

    [Heading("Security Configuration", color: "#FF6600")]
    [Setting("API Key")]
    [Secret]
    public string? ApiKey { get; set; }

    [Setting("Enable HTTPS")]
    public bool EnableHttps { get; set; } = true;

    [Heading("Feature Toggles", color: "#00AA00")]
    [Setting("Enable Feature A")]
    public bool EnableFeatureA { get; set; } = false;

    [Setting("Enable Feature B")]
    public bool EnableFeatureB { get; set; } = true;

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }
}
