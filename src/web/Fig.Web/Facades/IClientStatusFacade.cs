using Fig.Web.Models.Clients;

namespace Fig.Web.Facades;

public interface IClientStatusFacade
{
    List<ClientRunSessionModel> ClientRunSessions { get; }
    
    List<MemoryUsageAnalysisModel> PossibleMemoryLeaks { get; }

    Task Refresh();

    Task RequestRestart(ClientRunSessionModel client);

    Task SetLiveReload(ClientRunSessionModel client);
}