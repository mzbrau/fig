using Fig.Client;
using Fig.Client.Abstractions.Attributes;

namespace Fig.Examples.Client.Testing;

/// <summary>
/// Example settings class for testing the enhanced API
/// </summary>
public class ExampleApiSettings : SettingsBase
{
    // Display Script Constants
    private const string HttpsValidationScript = @"
        if (RequireHttps.Value && !BaseUrl.Value.toString().startsWith('https://')) {
            BaseUrl.ValidationExplanation = 'HTTPS is required when RequireHttps is enabled';
            BaseUrl.IsValid = false;
            if (BaseUrl.Value.toString().startsWith('http://')) {
                Port.Value = 443;
            }
        } else {
            BaseUrl.ValidationExplanation = '';
            BaseUrl.IsValid = true;
        }";

    private const string PortValidationScript = @"
        if (RequireHttps.Value && Port.Value === 80) {
            Port.ValidationExplanation = 'HTTPS requires port 443, not 80';
            Port.IsValid = false;
        } else if (RequireHttps.Value && Port.Value === 443) {
            Port.ValidationExplanation = '';
            Port.IsValid = true;
        } else if (!RequireHttps.Value && Port.Value === 443) {
            Port.ValidationExplanation = 'HTTP does not use port 443';
            Port.IsValid = false;
        } else {
            Port.ValidationExplanation = '';
            Port.IsValid = true;
        }";

    private const string DebugVisibilityScript = @"
        if (DebugLogging.Value) {
            DatabaseConnection.IsVisible = true;
            DatabaseConnection.CategoryName = 'Debug';
            DatabaseConnection.CategoryColor = '#FF9800';
        } else {
            DatabaseConnection.IsVisible = false;
        }";

    private const string ApiVersionScript = @"
        var baseUrl = BaseUrl.Value.toString();
        if (!baseUrl.includes('/v1') && !baseUrl.includes('/v2') && !baseUrl.includes('/v3')) {
            BaseUrl.Value = baseUrl.replace(/\/$/, '') + '/' + ApiVersion.Value;
        }
        
        if (ApiVersion.Value === 'v3') {
            TimeoutSeconds.DisplayOrder = 1;
            TimeoutSeconds.CategoryName = 'v3 Settings';
        }";

    private const string TimeoutConversionScript = @"
        var timeoutInSeconds = RequestTimeout.Value / 1000;
        if (timeoutInSeconds > 60) {
            TimeoutSeconds.Value = timeoutInSeconds;
        }";

    public override string ClientDescription => "Example API Settings for testing";

    [Setting("The base URL for the API")]
    [DisplayScript(HttpsValidationScript)]
    public string BaseUrl { get; set; } = "https://api.example.com";

    [Setting("Enable HTTPS requirement", supportsLiveUpdate: true)]
    public bool RequireHttps { get; set; } = true;

    [Setting("API timeout in seconds")]
    [Validation(@"^\d+$", "Must be a positive number")]
    public int TimeoutSeconds { get; set; } = 30;

    [Setting("Server port")]
    [DisplayScript(PortValidationScript)]
    public int Port { get; set; } = 443;

    [Setting("Database connection string")]
    [Secret]
    [DisplayScript(DebugVisibilityScript)]
    public string DatabaseConnection { get; set; } = "Server=localhost;Database=test;";

    [Setting("API version to use")]
    [ValidValues("v1", "v2", "v3")]
    [DisplayScript(ApiVersionScript)]
    public string ApiVersion { get; set; } = "v2";

    [Setting("Enable debug logging")]
    [Advanced]
    public bool DebugLogging { get; set; } = false;

    [Setting("Request timeout")]
    [DisplayScript(TimeoutConversionScript)]
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    [Setting("User roles configuration", defaultValueMethodName: nameof(GetDefaultUserRoles))]
    [Category("Security", "#FF5722")]
    public List<UserRole> UserRoles { get; set; } = new() { new UserRole { Name = "Admin", Permissions = new[] { "Read", "Write" } } };

    // Public methods to expose scripts for testing
    public static string GetHttpsValidationScript() => HttpsValidationScript;
    public static string GetPortValidationScript() => PortValidationScript;
    public static string GetDebugVisibilityScript() => DebugVisibilityScript;
    public static string GetApiVersionScript() => ApiVersionScript;
    public static string GetTimeoutConversionScript() => TimeoutConversionScript;

    public override IEnumerable<string> GetValidationErrors()
    {
        return new List<string>();
    }

    public static List<UserRole> GetDefaultUserRoles()
    {
        return new List<UserRole>
        {
            new UserRole { Name = "Admin", Permissions = new[] { "Read", "Write" } },
            new UserRole { Name = "User", Permissions = new[] { "Read" } }
        };
    }
}