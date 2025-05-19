using Fig.Common.NetStandard.Json;
using Fig.Contracts.Health;
using Fig.Contracts.Status;
using Fig.Datalayer.BusinessEntities;
using Newtonsoft.Json;

namespace Fig.Api.ExtensionMethods;

public static class ClientRunSessionBusinessEntityExtensions
{
    public static bool Update(
        this ClientRunSessionBusinessEntity runSession,
        StatusRequestDataContract statusRequest,
        string? hostname,
        string? ipAddress,
        FigConfigurationBusinessEntity configuration)
    {
        var healthChanged = false;
        runSession.Hostname = hostname;
        runSession.IpAddress = ipAddress;
        runSession.LastSeen = DateTime.UtcNow;
        runSession.FigVersion = statusRequest.FigVersion;
        runSession.ApplicationVersion = statusRequest.ApplicationVersion;
        runSession.OfflineSettingsEnabled = statusRequest.OfflineSettingsEnabled;
        runSession.SupportsRestart = statusRequest.SupportsRestart;
        runSession.RunningUser = statusRequest.RunningUser;
        runSession.MemoryUsageBytes = statusRequest.MemoryUsageBytes;
        runSession.HasConfigurationError = statusRequest.HasConfigurationError;
        runSession.HealthStatus = statusRequest.Health?.Status ?? FigHealthStatus.Unknown;

        if (statusRequest.Health is not null)
        {
            var newJson = JsonConvert.SerializeObject(statusRequest.Health, JsonSettings.FigDefault);
            if (newJson != runSession.HealthReportJson)
                healthChanged = true;

            runSession.HealthReportJson = newJson;
        }
        else
        {
            statusRequest.Health = null;
        }
        
        if (configuration.PollIntervalOverride.HasValue)
            runSession.PollIntervalMs = configuration.PollIntervalOverride.Value;

        return healthChanged;
    }

    public static bool IsExpired(this ClientRunSessionBusinessEntity session)
    {
        var gracePeriodMs = 2 * session.PollIntervalMs + 50;
        var expiryTime = session.LastSeen + TimeSpan.FromMilliseconds(gracePeriodMs);
        var result = expiryTime < DateTime.UtcNow;
        return result;
    }
}