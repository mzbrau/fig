using Fig.Client;
using Fig.Client.Attributes;
using Fig.Client.Enums;
using Serilog.Events;

namespace Fig.Integration.MicrosoftSentinel.Configuration;

public class Settings : SettingsBase
{
    public override string ClientDescription => "Microsoft Sentinel Integration - Forwards Fig webhook events to Microsoft Sentinel for SIEM monitoring";

    [Setting("The hashed secret provided by Fig when configuring the web hook client.")]
    [Category("Authentication", CategoryColor.Blue)]
    [Secret]
    public string HashedSecret { get; set; } = string.Empty;

    [Setting("Microsoft Sentinel workspace ID (Customer ID from workspace settings)")]
    [Category("Sentinel Configuration", CategoryColor.Green)]
    public string SentinelWorkspaceId { get; set; } = string.Empty;

    [Setting("Microsoft Sentinel workspace primary or secondary key")]
    [Category("Sentinel Configuration", CategoryColor.Green)]
    [Secret]
    public string SentinelWorkspaceKey { get; set; } = string.Empty;

    [Setting("Custom log type name that will appear in Sentinel (will have '_CL' suffix added automatically)")]
    [Category("Sentinel Configuration", CategoryColor.Green)]
    public string SentinelLogType { get; set; } = "FigEvents";

    [Setting("Minimum log level for this integration")]
    [Category("Logging", CategoryColor.Purple)]
    [Advanced]
    public LogEventLevel LogLevel { get; set; } = LogEventLevel.Information;

    [Setting("Maximum number of retry attempts for failed Sentinel API calls")]
    [Category("Advanced Settings", CategoryColor.Red)]
    [Advanced]
    public int MaxRetryAttempts { get; set; } = 3;

    [Setting("Delay in seconds between retry attempts (will use exponential backoff)")]
    [Category("Advanced Settings", CategoryColor.Red)]
    [Advanced]
    public int RetryDelaySeconds { get; set; } = 2;

    [Setting("Timeout in seconds for Sentinel API calls")]
    [Category("Advanced Settings", CategoryColor.Red)]
    [Advanced]
    public int SentinelApiTimeoutSeconds { get; set; } = 30;

    public override IEnumerable<string> GetValidationErrors()
    {
        var errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(HashedSecret))
        {
            errors.Add("HashedSecret must be configured. This should be provided by Fig when configuring the webhook client.");
        }
        
        if (string.IsNullOrWhiteSpace(SentinelWorkspaceId))
        {
            errors.Add("SentinelWorkspaceId must be configured. This is the Customer ID from your Microsoft Sentinel workspace settings.");
        }
        
        if (string.IsNullOrWhiteSpace(SentinelWorkspaceKey))
        {
            errors.Add("SentinelWorkspaceKey must be configured. This is the primary or secondary key from your Microsoft Sentinel workspace settings.");
        }
        
        if (string.IsNullOrWhiteSpace(SentinelLogType))
        {
            errors.Add("SentinelLogType must be configured. This will be the custom log type name in Sentinel.");
        }

        if (MaxRetryAttempts < 0 || MaxRetryAttempts > 10)
        {
            errors.Add("MaxRetryAttempts must be between 0 and 10.");
        }

        if (RetryDelaySeconds < 1 || RetryDelaySeconds > 60)
        {
            errors.Add("RetryDelaySeconds must be between 1 and 60.");
        }

        if (SentinelApiTimeoutSeconds < 5 || SentinelApiTimeoutSeconds > 300)
        {
            errors.Add("SentinelApiTimeoutSeconds must be between 5 and 300.");
        }
        
        return errors;
    }
}