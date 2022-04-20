using Fig.Contracts.Status;
using Fig.Web.Converters;
using Fig.Web.Models.Clients;
using Fig.Web.Services;

namespace Fig.Web.Facades;

public class ClientStatusFacade : IClientStatusFacade
{
    private readonly IHttpService _httpService;
    private readonly IClientRunSessionConverter _clientRunSessionConverter;

    public ClientStatusFacade(IHttpService httpService, IClientRunSessionConverter clientRunSessionConverter)
    {
        _httpService = httpService;
        _clientRunSessionConverter = clientRunSessionConverter;
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
        
        Console.WriteLine($"Loaded {ClientRunSessions.Count} client run sessions");
    }
}