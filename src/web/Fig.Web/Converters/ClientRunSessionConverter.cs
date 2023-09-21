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
                lastSettingValueUpdate: client.LastSettingValueUpdate?.ToLocalTime(),
                runSessionId: session.RunSessionId, lastSeen: session.LastSeen?.ToLocalTime(),
                liveReload: session.LiveReload, pollIntervalMs: session.PollIntervalMs,
                uptimeSeconds: session.UptimeSeconds, ipAddress: session.IpAddress, hostname: session.Hostname,
                figVersion: session.FigVersion, applicationVersion: session.ApplicationVersion,
                offlineSettingsEnabled: session.OfflineSettingsEnabled, supportsRestart: session.SupportsRestart,
                restartRequested: session.RestartRequested,
                restartRequiredToApplySettings: session.RestartRequiredToApplySettings,
                runningUser: session.RunningUser,
                memoryUsageBytes: session.MemoryUsageBytes, hasConfigurationError: session.HasConfigurationError,
                historicalMemoryUsage: session.HistoricalMemoryUsage.Select(Convert).ToList(),
                possibleMemoryLeakDetected: session.MemoryAnalysis?.PossibleMemoryLeakDetected == true);
    }

    public IEnumerable<MemoryUsageAnalysisModel> ConvertToMemoryAnalysis(List<ClientStatusDataContract> clients)
    {
        foreach (var client in clients)
        {
            foreach (var session in client.RunSessions.Where(a => a.MemoryAnalysis?.PossibleMemoryLeakDetected == true))
            {
                yield return new MemoryUsageAnalysisModel(
                    client.Name,
                    session.Hostname,
                    session.MemoryAnalysis!.TimeOfAnalysisUtc,
                    session.MemoryAnalysis.PossibleMemoryLeakDetected,
                    session.MemoryAnalysis.TrendLineSlope,
                    session.MemoryAnalysis.Average,
                    session.MemoryAnalysis.StandardDeviation,
                    session.MemoryAnalysis.StartingAverage,
                    session.MemoryAnalysis.EndingAverage,
                    session.MemoryAnalysis.SecondsAnalyzed,
                    session.MemoryAnalysis.DataPointsAnalyzed);
            }
        }
    }

    private MemoryUsageModel Convert(MemoryUsageDataContract memoryUsage)
    {
        return new MemoryUsageModel(memoryUsage.ClientRunTimeSeconds, memoryUsage.MemoryUsageBytes);
    }
}