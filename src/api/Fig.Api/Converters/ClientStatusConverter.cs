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
            session.MemoryUsageBytes);
    }
}