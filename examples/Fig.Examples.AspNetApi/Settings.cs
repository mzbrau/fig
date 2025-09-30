using Fig.Client.Abstractions.Attributes;
using Fig.Client.Abstractions.Enums;
using Fig.Client.Abstractions.Validation;
using Serilog.Events;
using SettingsBase = Fig.Client.SettingsBase;

namespace Fig.Examples.AspNetApi;

public class Settings : SettingsBase
{
    public override string ClientDescription => "AspNetApi Example";
    
    [Setting("Enable database related settings")]
    [DisplayScript("""
                   if (EnableDatabaseSettings.Value == true) {
                       Database.Name.IsVisible = true;
                       } else {
                       Database.Name.IsVisible = false;
                   }
                   """)]
    public bool EnableDatabaseSettings { get; set; }
    
    [NestedSetting]
    public DatabaseSettings? Database { get; set; }
    
    // First setting with Database category - should get a "Database" divider automatically
    [Setting("Primary database connection string")]
    [Category(Category.Database)]
    [Secret]
    public string PrimaryDbConnectionString { get; set; } = "Server=localhost;Database=MyApp;";

    // Second setting with Database category - should NOT get a divider (not first)
    [Setting("Read-only database connection string")]
    [Category(Category.Database)]
    [Secret]
    public string ReadOnlyDbConnectionString { get; set; } = "Server=localhost;Database=MyAppReadOnly;";

    // First setting with Logging category - should get a "Logging" divider automatically
    [Setting("Minimum log level")]
    [Category(Category.Logging)]
    [ValidValues(typeof(LogEventLevel))]
    public LogEventLevel MinLogLevel { get; set; } = LogEventLevel.Information;

    // Second setting with Logging category - should NOT get a divider (not first)
    [Setting("Log file path")]
    [Category(Category.Logging)]
    [Validation(ValidationType.NotEmpty)]
    public string LogFilePath { get; set; } = "/var/log/myapp.log";

    // First setting with ApiIntegration category - should get an "API Integration" divider automatically
    [Setting("External API base URL")]
    [Category(Category.ApiIntegration)]
    [Validation(ValidationType.NotEmpty)]
    public string ExternalApiUrl { get; set; } = "https://api.example.com";

    // Setting with manual divider - should keep the manual divider (not be overridden)
    [Setting("API timeout in seconds")]
    [Category(Category.ApiIntegration)]
    [Heading("API Configuration", CategoryColor.Red)]
    [Validation(ValidationType.GreaterThanZero)]
    public int ApiTimeoutSeconds { get; set; } = 30;

    // First setting with custom category name/color - should get a divider with custom name/color
    [Setting("Custom cache expiration")]
    [Category("Custom Cache", CategoryColor.Purple)]
    public int CacheExpirationMinutes { get; set; } = 60;

    // Setting without category - should NOT get any divider
    [Setting("Application version")]
    public string AppVersion { get; set; } = "1.0.0";

    // Second setting without category - should NOT get any divider
    [Setting("Environment name")]
    public string EnvironmentName { get; set; } = "Development";

    [Setting("Override for system logs")]
    [ConfigurationSectionOverride("Serilog:Override", "System")]
    [ValidValues(typeof(LogEventLevel))]
    public LogEventLevel SystemLogOverride { get; set; } = LogEventLevel.Warning;
    
    [Setting("Location of the application")]
    public string? Location { get; set; }

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
