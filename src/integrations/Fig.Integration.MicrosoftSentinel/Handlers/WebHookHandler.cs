using Fig.Integration.MicrosoftSentinel.Extensions;
using Fig.Integration.MicrosoftSentinel.Services;
using Fig.WebHooks.Contracts;

namespace Fig.Integration.MicrosoftSentinel.Handlers;

public class WebHookHandler : IWebHookHandler
{
    private readonly ISentinelService _sentinelService;
    private readonly ILogger<WebHookHandler> _logger;

    public WebHookHandler(ISentinelService sentinelService, ILogger<WebHookHandler> logger)
    {
        _sentinelService = sentinelService;
        _logger = logger;
    }

    public async Task<bool> HandleSecurityEventAsync(SecurityEventDataContract securityEvent)
    {
        _logger.LogInformation("Processing security event: {EventType} for user {Username}, Success: {Success}",
            securityEvent.EventType, securityEvent.Username?.Anonymize(), securityEvent.Success);

        // Check if this is a test webhook and return success without sending to Sentinel
        if (securityEvent.IsTest)
        {
            _logger.LogDebug("Detected test webhook for security event, returning success without sending to Sentinel");
            return true;
        }

        var sentinelLog = new
        {
            timestamp = securityEvent.Timestamp,
            eventType = "SecurityEvent",
            figEventType = securityEvent.EventType,
            username = securityEvent.Username?.Anonymize(),
            success = securityEvent.Success,
            ipAddress = securityEvent.IpAddress?.Anonymize(),
            hostname = securityEvent.Hostname,
            failureReason = securityEvent.FailureReason,
            uri = securityEvent.Uri?.ToString(),
            source = "Fig",
            integration = "MicrosoftSentinel",
            severity = securityEvent.Success ? "Informational" : "Medium"
        };

        var success = await _sentinelService.SendLogAsync(sentinelLog);
        
        if (success)
        {
            _logger.LogDebug("Security event sent to Sentinel successfully");
        }
        else
        {
            _logger.LogError("Failed to send security event to Sentinel");
        }

        return success;
    }

    public async Task<bool> HandleSettingValueChangedAsync(SettingValueChangedDataContract settingValueChanged)
    {
        _logger.LogInformation("Processing setting value changed event for client: {ClientName}, Settings: {UpdatedSettings}",
            settingValueChanged.ClientName, string.Join(", ", settingValueChanged.UpdatedSettings));

        // Check if this is a test webhook and return success without sending to Sentinel
        if (settingValueChanged.IsTest)
        {
            _logger.LogDebug("Detected test webhook for setting value changed, returning success without sending to Sentinel");
            return true;
        }

        var sentinelLog = new
        {
            timestamp = DateTime.UtcNow,
            eventType = "SettingValueChanged",
            clientName = settingValueChanged.ClientName,
            instance = settingValueChanged.Instance,
            updatedSettings = settingValueChanged.UpdatedSettings,
            username = settingValueChanged.Username?.Anonymize(),
            changeMessage = settingValueChanged.ChangeMessage,
            source = "Fig",
            integration = "MicrosoftSentinel",
            severity = "Informational"
        };

        var success = await _sentinelService.SendLogAsync(sentinelLog);
        
        if (success)
        {
            _logger.LogDebug("Setting value changed event sent to Sentinel successfully");
        }
        else
        {
            _logger.LogError("Failed to send setting value changed event to Sentinel");
        }

        return success;
    }

    public async Task<bool> HandleClientRegistrationAsync(ClientRegistrationDataContract clientRegistration)
    {
        _logger.LogInformation("Processing client registration event: {ClientName}, Type: {RegistrationType}",
            clientRegistration.ClientName, clientRegistration.RegistrationType);

        // Check if this is a test webhook and return success without sending to Sentinel
        if (clientRegistration.IsTest)
        {
            _logger.LogDebug("Detected test webhook for client registration, returning success without sending to Sentinel");
            return true;
        }

        var sentinelLog = new
        {
            timestamp = DateTime.UtcNow,
            eventType = "ClientRegistration",
            clientName = clientRegistration.ClientName,
            registrationType = clientRegistration.RegistrationType.ToString(),
            source = "Fig",
            integration = "MicrosoftSentinel",
            severity = "Informational"
        };

        var success = await _sentinelService.SendLogAsync(sentinelLog);
        
        if (success)
        {
            _logger.LogDebug("Client registration event sent to Sentinel successfully");
        }
        else
        {
            _logger.LogError("Failed to send client registration event to Sentinel");
        }

        return success;
    }

    public async Task<bool> HandleClientStatusChangedAsync(ClientStatusChangedDataContract clientStatusChanged)
    {
        _logger.LogInformation("Processing client status changed event: {ClientName}, ConnectionEvent: {ConnectionEvent}",
            clientStatusChanged.ClientName, clientStatusChanged.ConnectionEvent);

        // Check if this is a test webhook and return success without sending to Sentinel
        if (clientStatusChanged.IsTest)
        {
            _logger.LogDebug("Detected test webhook for client status changed, returning success without sending to Sentinel");
            return true;
        }

        var sentinelLog = new
        {
            timestamp = DateTime.UtcNow,
            eventType = "ClientStatusChanged",
            clientName = clientStatusChanged.ClientName,
            instance = clientStatusChanged.Instance,
            connectionEvent = clientStatusChanged.ConnectionEvent.ToString(),
            startTime = clientStatusChanged.StartTime,
            ipAddress = clientStatusChanged.IpAddress?.Anonymize(),
            hostname = clientStatusChanged.Hostname,
            figVersion = clientStatusChanged.FigVersion,
            applicationVersion = clientStatusChanged.ApplicationVersion,
            link = clientStatusChanged.Link?.ToString(),
            source = "Fig",
            integration = "MicrosoftSentinel",
            severity = "Informational"
        };

        var success = await _sentinelService.SendLogAsync(sentinelLog);
        
        if (success)
        {
            _logger.LogDebug("Client status changed event sent to Sentinel successfully");
        }
        else
        {
            _logger.LogError("Failed to send client status changed event to Sentinel");
        }

        return success;
    }

    public async Task<bool> HandleClientHealthChangedAsync(ClientHealthChangedDataContract clientHealthChanged)
    {
        _logger.LogInformation("Processing client health changed event: {ClientName}, Status: {HealthStatus}",
            clientHealthChanged.ClientName, clientHealthChanged.HealthDetails.Status);

        // Check if this is a test webhook and return success without sending to Sentinel
        if (clientHealthChanged.IsTest)
        {
            _logger.LogDebug("Detected test webhook for client health changed, returning success without sending to Sentinel");
            return true;
        }

        var sentinelLog = new
        {
            timestamp = DateTime.UtcNow,
            eventType = "ClientHealthChanged",
            clientName = clientHealthChanged.ClientName,
            healthStatus = clientHealthChanged.HealthDetails.Status.ToString(),
            componentDetails = clientHealthChanged.HealthDetails.Components?.Select(c => new
            {
                name = c.Name,
                status = c.Status.ToString(),
                message = c.Message
            }),
            source = "Fig",
            integration = "MicrosoftSentinel",
            severity = clientHealthChanged.HealthDetails.Status == HealthStatus.Healthy ? "Informational" : "Warning"
        };

        var success = await _sentinelService.SendLogAsync(sentinelLog);
        
        if (success)
        {
            _logger.LogDebug("Client health changed event sent to Sentinel successfully");
        }
        else
        {
            _logger.LogError("Failed to send client health changed event to Sentinel");
        }

        return success;
    }

    public async Task<bool> HandleMinRunSessionsAsync(MinRunSessionsDataContract minRunSessions)
    {
        _logger.LogInformation("Processing min run sessions event: {ClientName}, Sessions: {RunSessions}, Event: {Event}",
            minRunSessions.ClientName, minRunSessions.RunSessions, minRunSessions.RunSessionsEvent);

        // Check if this is a test webhook and return success without sending to Sentinel
        if (minRunSessions.IsTest)
        {
            _logger.LogDebug("Detected test webhook for min run sessions, returning success without sending to Sentinel");
            return true;
        }

        var sentinelLog = new
        {
            timestamp = DateTime.UtcNow,
            eventType = "MinRunSessions",
            clientName = minRunSessions.ClientName,
            instance = minRunSessions.Instance,
            runSessions = minRunSessions.RunSessions,
            runSessionsEvent = minRunSessions.RunSessionsEvent.ToString(),
            source = "Fig",
            integration = "MicrosoftSentinel",
            severity = "Warning"
        };

        var success = await _sentinelService.SendLogAsync(sentinelLog);
        
        if (success)
        {
            _logger.LogDebug("Min run sessions event sent to Sentinel successfully");
        }
        else
        {
            _logger.LogError("Failed to send min run sessions event to Sentinel");
        }

        return success;
    }
}