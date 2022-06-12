using Fig.Api.ExtensionMethods;
using Fig.Contracts.Status;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public class ClientStatusConverter : IClientStatusConverter
{
    public ClientStatusDataContract Convert(ClientStatusBusinessEntity client)
    {
        return new ClientStatusDataContract
        {
            Name = client.Name,
            Instance = client.Instance,
            LastRegistration = client.LastRegistration,
            LastSettingValueUpdate = client.LastSettingValueUpdate,
            RunSessions = Convert(client.RunSessions)
        };
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
        return new ClientRunSessionDataContract
        {
            RunSessionId = session.RunSessionId,
            LastSeen = session.LastSeen,
            LiveReload = session.LiveReload,
            PollIntervalMs = session.PollIntervalMs,
            UptimeSeconds = session.UptimeSeconds,
            IpAddress = session.IpAddress,
            Hostname = session.Hostname,
            FigVersion = session.FigVersion,
            ApplicationVersion = session.ApplicationVersion,
            OfflineSettingsEnabled = session.OfflineSettingsEnabled,
            SupportsRestart = session.SupportsRestart,
            RestartRequested = session.RestartRequested
        };
    }
}