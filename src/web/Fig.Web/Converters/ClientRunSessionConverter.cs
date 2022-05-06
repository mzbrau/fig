using Fig.Contracts.Status;
using Fig.Web.Models.Clients;

namespace Fig.Web.Converters;

public class ClientRunSessionConverter : IClientRunSessionConverter
{
    public IEnumerable<ClientRunSessionModel> Convert(List<ClientStatusDataContract> clients)
    {
        foreach (var client in clients)
        {
            foreach (var session in client.RunSessions)
            {
                yield return new ClientRunSessionModel
                {
                    Name = client.Name,
                    Instance = client.Instance,
                    LastRegistration = client.LastRegistration?.ToLocalTime(),
                    LastSettingValueUpdate = client.LastSettingValueUpdate?.ToLocalTime(),
                    RunSessionId = session.RunSessionId,
                    LastSeen = session.LastSeen?.ToLocalTime(),
                    LiveReload = session.LiveReload,
                    PollIntervalMs = session.PollIntervalMs,
                    UptimeSeconds = session.UptimeSeconds,
                    IpAddress = session.IpAddress,
                    Hostname = session.Hostname,
                    FigVersion = session.FigVersion,
                    ApplicationVersion = session.ApplicationVersion
                };
            }
        }
    }
}