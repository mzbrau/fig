using Fig.Contracts.Status;
using Fig.Web.Models.Clients;

namespace Fig.Web.Converters;

public class ClientRunSessionConverter : IClientRunSessionConverter
{
    public IEnumerable<ClientRunSessionModel> Convert(List<ClientStatusDataContract> clients)
    {
        foreach (var client in clients)
        foreach (var session in client.RunSessions)
            yield return new ClientRunSessionModel(name: client.Name, instance: client.Instance,
                lastRegistration: client.LastRegistration?.ToLocalTime(),
                lastSettingValueUpdateUtc: client.LastSettingValueUpdate,
                runSessionId: session.RunSessionId, lastSeen: session.LastSeen?.ToLocalTime(),
                liveReload: session.LiveReload, pollIntervalMs: session.PollIntervalMs,
                startTimeUtc: session.StartTimeUtc, ipAddress: session.IpAddress, hostname: session.Hostname,
                figVersion: session.FigVersion, applicationVersion: session.ApplicationVersion,
                offlineSettingsEnabled: session.OfflineSettingsEnabled, supportsRestart: session.SupportsRestart,
                restartRequested: session.RestartRequested,
                restartRequiredToApplySettings: session.RestartRequiredToApplySettings,
                runningUser: session.RunningUser,
                memoryUsageBytes: session.MemoryUsageBytes, hasConfigurationError: session.HasConfigurationError,
                lastSettingLoadUtc: session.LastSettingLoadUtc);
    }
}