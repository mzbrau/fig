using System.Runtime.CompilerServices;
using Fig.Client.Abstractions.CustomActions;
using Fig.Integration.MicrosoftSentinel.Configuration;
using Fig.Integration.MicrosoftSentinel.Services;
using Microsoft.Extensions.Options;

namespace Fig.Integration.MicrosoftSentinel.CustomActions;

public class TestSentinelConnectionAction : ICustomAction
{
    private readonly ISentinelService _sentinelService;
    private readonly IOptionsMonitor<Settings> _settings;
    private readonly ILogger<TestSentinelConnectionAction> _logger;

    public TestSentinelConnectionAction(ISentinelService sentinelService, IOptionsMonitor<Settings> settings, ILogger<TestSentinelConnectionAction> logger)
    {
        _sentinelService = sentinelService;
        _settings = settings;
        _logger = logger;
    }

    public string Name => "TestSentinelConnection";

    public string ButtonName => "Test Connection";

    public string Description => "Tests the connection to Microsoft Sentinel by sending a test log entry";

    public IEnumerable<string> SettingsUsed =>
    [
        nameof(Settings.SentinelWorkspaceId),
        nameof(Settings.SentinelWorkspaceKey),
        nameof(Settings.SentinelLogType),
        nameof(Settings.SentinelApiTimeoutSeconds)
    ];

    public async IAsyncEnumerable<CustomActionResultModel> Execute([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var settings = _settings.CurrentValue;
        
        // Validate settings first
        var validationErrors = settings.GetValidationErrors().ToList();
        if (validationErrors.Any())
        {
            yield return ResultBuilder.CreateFailureResult("Configuration Validation")
                .WithTextResult($"Configuration errors found:\n{string.Join("\n", validationErrors)}");
            yield break;
        }
        
        // Test the connection
        bool success;
        string errorMessage = string.Empty;
        
        try
        {
            success = await _sentinelService.TestConnectionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Sentinel connection test");
            success = false;
            errorMessage = ex.Message;
        }
        
        if (success)
        {
            yield return ResultBuilder.CreateSuccessResult("Connection Test")
                .WithTextResult($"✅ Connection test successful! Test log sent to Microsoft Sentinel.\n\nCheck your Microsoft Sentinel workspace for a log entry in the '{settings.SentinelLogType}_CL' table.");
        }
        else
        {
            var failureMessage = "❌ Connection test failed. Check the logs for more details.\n\nVerify your workspace ID, key, and network connectivity to Microsoft Sentinel.";
            if (!string.IsNullOrEmpty(errorMessage))
            {
                failureMessage += $"\n\nError: {errorMessage}";
            }
            
            yield return ResultBuilder.CreateFailureResult("Connection Test")
                .WithTextResult(failureMessage);
        }
    }
}