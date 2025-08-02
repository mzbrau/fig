using Fig.Api.ExtensionMethods;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.Health;
using Fig.Contracts.Status;
using Fig.Datalayer.BusinessEntities;
using Newtonsoft.Json;

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
        HealthDataContract? health = null;
        if (session.HealthReportJson is not null)
        {
            health = JsonConvert.DeserializeObject<HealthDataContract>(session.HealthReportJson, JsonSettings.FigDefault);
        }
        
        return new ClientRunSessionDataContract(session.RunSessionId,
            session.LastSeen,
            session.LiveReload,
            session.PollIntervalMs,
            session.StartTimeUtc,
            session.IpAddress,
            session.Hostname,
            session.FigVersion,
            session.ApplicationVersion,
            session.OfflineSettingsEnabled,
            session.SupportsRestart,
            session.RestartRequested,
            session.RestartRequiredToApplySettings,
            session.RunningUser,
            session.MemoryUsageBytes,
            session.LastSettingLoadUtc,
            health);
    }
}