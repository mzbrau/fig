using Fig.Api.ExtensionMethods;
using Fig.Contracts.Status;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public class ClientStatusConverter : IClientStatusConverter
{
    public ClientStatusDataContract Convert(ClientStatusBusinessEntity client)
    {
        var runSessions = Convert(client.RunSessions);
        return new ClientStatusDataContract(client.Name,
            client.Instance,
            client.LastRegistration,
            client.LastSettingValueUpdate,
            runSessions);
    }

    private List<ClientRunSessionDataContract> Convert(IEnumerable<ClientRunSessionBusinessEntity> sessions)
    {
        var result = new List<ClientRunSessionDataContract>();
        foreach (var session in sessions.Where(a => !a.IsExpired()))
            result.Add(Convert(session));

        return result;
    }

    private ClientRunSessionDataContract Convert(ClientRunSessionBusinessEntity session)
    {
        return new ClientRunSessionDataContract(session.RunSessionId,
            session.LastSeen,
            session.LiveReload,
            session.PollIntervalMs,
            session.UptimeSeconds,
            session.IpAddress,
            session.Hostname,
            session.FigVersion,
            session.ApplicationVersion,
            session.OfflineSettingsEnabled,
            session.SupportsRestart,
            session.RestartRequested,
            session.RunningUser,
            session.MemoryUsageBytes,
            session.HasConfigurationError,
            session.MemoryAnalysis is null ? null : Convert(session.MemoryAnalysis),
            session.HistoricalMemoryUsage.Select(Convert).ToList());
    }

    private MemoryUsageAnalysisDataContract Convert(MemoryUsageAnalysisBusinessEntity analysis)
    {
        return new MemoryUsageAnalysisDataContract(
            analysis.TimeOfAnalysisUtc,
            analysis.PossibleMemoryLeakDetected,
            analysis.TrendLineSlope,
            analysis.Average,
            analysis.StandardDeviation,
            analysis.StartingAverage,
            analysis.EndingAverage,
            analysis.SecondsAnalyzed,
            analysis.DataPointsAnalyzed);
    }

    private MemoryUsageDataContract Convert(MemoryUsageBusinessEntity dataPoint)
    {
        return new MemoryUsageDataContract(dataPoint.ClientRunTimeSeconds, dataPoint.MemoryUsageBytes);
    }
}