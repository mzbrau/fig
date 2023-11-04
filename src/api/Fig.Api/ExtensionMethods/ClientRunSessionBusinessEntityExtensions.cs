using Fig.Contracts.Status;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.ExtensionMethods;

public static class ClientRunSessionBusinessEntityExtensions
{
    public static void Update(
        this ClientRunSessionBusinessEntity runSession,
        StatusRequestDataContract statusRequest,
        string? hostname,
        string? ipAddress,
        FigConfigurationBusinessEntity configuration)
    {
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
        if (configuration.AnalyzeMemoryUsage)
        {
            runSession.HistoricalMemoryUsage.Add(new MemoryUsageBusinessEntity()
            {
                ClientRunTimeSeconds = (DateTime.UtcNow - statusRequest.StartTime).TotalSeconds,
                MemoryUsageBytes = statusRequest.MemoryUsageBytes
            });
        }
        
        if (configuration.PollIntervalOverride.HasValue)
            runSession.PollIntervalMs = configuration.PollIntervalOverride.Value;
    }

    public static bool IsExpired(this ClientRunSessionBusinessEntity session)
    {
        var gracePeriodMs = 2 * session.PollIntervalMs + 50;
        var expiryTime = session.LastSeen + TimeSpan.FromMilliseconds(gracePeriodMs);
        var result = expiryTime < DateTime.UtcNow;
        return result;
    }
}