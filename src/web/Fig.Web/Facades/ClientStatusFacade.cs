using Fig.Common.Events;
using Fig.Contracts.Status;
using Fig.Web.Converters;
using Fig.Web.Events;
using Fig.Web.Models.Clients;
using Fig.Web.Services;

namespace Fig.Web.Facades;

public class ClientStatusFacade : IClientStatusFacade
{
    private readonly IClientRunSessionConverter _clientRunSessionConverter;
    private readonly IEventDistributor _eventDistributor;
    private readonly IHttpService _httpService;

    public ClientStatusFacade(IHttpService httpService, IClientRunSessionConverter clientRunSessionConverter, IEventDistributor eventDistributor)
    {
        _httpService = httpService;
        _clientRunSessionConverter = clientRunSessionConverter;
        _eventDistributor = eventDistributor;
        eventDistributor.Subscribe(EventConstants.LogoutEvent, () =>
        {
            ClientRunSessions.Clear();
        });
    }

    public List<ClientRunSessionModel> ClientRunSessions { get; } = new();
    
    public async Task Refresh()
    {
        var result = await _httpService.Get<List<ClientStatusDataContract>>("statuses");

        if (result == null)
            return;

        ClientRunSessions.Clear();
        var newSessions = _clientRunSessionConverter.Convert(result);

        foreach (var session in newSessions.OrderBy(a => a.Name).ThenBy(a => a.Hostname))
            ClientRunSessions.Add(session);

        await _eventDistributor.PublishAsync(EventConstants.ClientRunSessionsChanged);
        
        Console.WriteLine($"Loaded {ClientRunSessions.Count} client run sessions");
    }

    public async Task RequestRestart(ClientRunSessionModel client)
    {
        await _httpService.Put(
            $"statuses/{client.RunSessionId}/restart", null);
    }
    
    public async Task SetLiveReload(ClientRunSessionModel client)
    {
        await _httpService.Put(
            $"statuses/{client.RunSessionId}/liveReload?liveReload={client.LiveReload}", null);
    }
}