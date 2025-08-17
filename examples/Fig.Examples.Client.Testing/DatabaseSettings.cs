using Fig.Client;
using Fig.Client.Abstractions.Attributes;

namespace Fig.Examples.Client.Testing;

/// <summary>
/// Additional settings class for testing multiple client scenarios
/// </summary>
public class DatabaseSettings : SettingsBase
{
    private const string ConnectionValidationScript = @"
        if (EnablePooling.Value && MaxPoolSize.Value < 10) {
            MaxPoolSize.ValidationExplanation = 'Pool size should be at least 10 when pooling is enabled';
            MaxPoolSize.IsValid = false;
        } else {
            MaxPoolSize.ValidationExplanation = '';
            MaxPoolSize.IsValid = true;
        }";

    public override string ClientDescription => "Database Configuration Settings";

    [Setting("Database connection string")]
    [Secret]
    public string ConnectionString { get; set; } = "Server=localhost;Database=app;";

    [Setting("Connection timeout in seconds")]
    [Validation(@"^\d+$", "Must be a positive number")]
    public int ConnectionTimeout { get; set; } = 30;

    [Setting("Enable connection pooling")]
    public bool EnablePooling { get; set; } = true;

    [Setting("Maximum pool size")]
    [Advanced]
    [DisplayScript(ConnectionValidationScript)]
    public int MaxPoolSize { get; set; } = 100;

    public static string GetConnectionValidationScript() => ConnectionValidationScript;

    public override IEnumerable<string> GetValidationErrors()
    {
        return new List<string>();
    }
}