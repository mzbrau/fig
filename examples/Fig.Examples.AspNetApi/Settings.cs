using Fig.Client.Abstractions.Attributes;
using Fig.Client.Abstractions.Data;
using Fig.Client.Abstractions.Enums;
using Fig.Client.Abstractions.Validation;
using Serilog.Events;
using SettingsBase = Fig.Client.SettingsBase;

namespace Fig.Examples.AspNetApi;

public class Settings : SettingsBase
{
    public override string ClientDescription => "AspNetApi Example";

    [Setting("Primary database connection string")]
    [Category(Category.Database)]
    [Secret]
    public string PrimaryDbConnectionString { get; set; } = "Server=localhost;Database=MyApp;";

    [Setting("Read-only database connection string")]
    [Category(Category.Database)]
    [Secret]
    public string ReadOnlyDbConnectionString { get; set; } = "Server=localhost;Database=MyAppReadOnly;";

    [Setting("Minimum log level")]
    [Category(Category.Logging)]
    [ValidValues(typeof(LogEventLevel))]
    public LogEventLevel MinLogLevel { get; set; } = LogEventLevel.Information;

    [Setting("Log file path")]
    [Category(Category.Logging)]
    [Validation(ValidationType.NotEmpty)]
    [DependsOn(nameof(MinLogLevel), "Information")]
    public string LogFilePath { get; set; } = "/var/log/myapp.log";

    [Setting("External API base URL")]
    [Category(Category.ApiIntegration)]
    [Validation(ValidationType.NotEmpty)]
    public string ExternalApiUrl { get; set; } = "https://api.example.com";

    [Setting("API timeout in seconds")]
    [Category(Category.ApiIntegration)]
    [Heading("API Configuration", CategoryColor.Red)]
    [Validation(ValidationType.GreaterThanZero)]
    public int ApiTimeoutSeconds { get; set; } = 30;

    [Setting("Custom cache expiration")]
    [Category("Custom Cache", CategoryColor.Purple)]
    public int CacheExpirationMinutes { get; set; } = 60;

    [Setting("Application version", classification: Classification.Functional)]
    [Category("General", CategoryColor.Green)]
    public string AppVersion { get; set; } = "1.0.0";

    [Setting("Override for system logs")]
    [ConfigurationSectionOverride("Serilog:Override", "System")]
    [ValidValues(typeof(LogEventLevel))]
    [Category("General", CategoryColor.Green)]
    public LogEventLevel SystemLogOverride { get; set; } = LogEventLevel.Warning;
    
    [Setting("Location of the application")]
    [Category("General", CategoryColor.Green)]
    public string Location { get; set; } = string.Empty;

    [Setting("A list of items")]
    [Category("General", CategoryColor.Green)]
    [ValidateCount(Constraint.AtLeast, 3)]
    public List<string>? Items { get; set; }

    [Setting("The Jira Type")]
    [Category("Jira", CategoryColor.Orange)]
    [LookupTable("IssueType", LookupSource.ProviderDefined)]
    public string? Type { get; set; }

    [Setting("The Jira Property")]
    [Category("Jira", CategoryColor.Orange)]
    [LookupTable("IssueProperty", LookupSource.ProviderDefined, "Type")]
    public string? Property { get; set; }

    [Setting("Environment name")]
    [Category<MyCustomCategories>(MyCustomCategories.PaymentProcessing)]
    public string EnvironmentName { get; set; } = "Development";

    public override IEnumerable<string> GetValidationErrors()
    {
        //Perform validation here.
        return [];
    }
}

public class DatabaseSettings
{
    [Setting("Database name")]
    public string Name { get; set; } = "MoyAppDb";
}
