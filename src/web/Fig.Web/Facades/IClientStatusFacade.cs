using Fig.Web.Models.Clients;

namespace Fig.Web.Facades;

public interface IClientStatusFacade
{
    List<ClientRunSessionModel> ClientRunSessions { get; }
    
    int ConnectedClientsCount => ClientRunSessions.Count;

    Task<bool> Refresh();

    LastSeenModel? GetLastSeen(string clientName, string? instance);

    Task RequestRestart(ClientRunSessionModel client);

    Task SetLiveReload(ClientRunSessionModel client);
}