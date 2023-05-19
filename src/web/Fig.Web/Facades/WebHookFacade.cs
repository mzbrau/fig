using Fig.Common.NetStandard.WebHook;
using Fig.Web.Converters;
using Fig.Web.Models.WebHooks;
using Fig.Web.Services;

namespace Fig.Web.Facades;

public class WebHookFacade : IWebHookFacade
{
    private const string WebHookClientRoute = "webhookclient";
    
    private readonly IHttpService _httpService;
    private readonly IWebHookClientConverter _webHookClientConverter;

    public WebHookFacade(IHttpService httpService, IWebHookClientConverter webHookClientConverter)
    {
        _httpService = httpService;
        _webHookClientConverter = webHookClientConverter;
    }

    public List<WebHookClientModel> WebHookClients { get; } = new();
    
    public async Task LoadAllClients()
    {
        var clients = await _httpService.Get<List<WebHookClientDataContract>>(WebHookClientRoute);

        WebHookClients.Clear();
        
        if (clients is null)
            return;
        
        foreach (var client in clients.Select(a => _webHookClientConverter.Convert(a)))
        {
            WebHookClients.Add(client);
        }
    }

    public async Task LoadAllWebHooks()
    {
        // TODO once web hooks are implemented.
    }

    public async Task AddClient(WebHookClientModel client)
    {
        var createdClient =
            await _httpService.Post<WebHookClientDataContract>(WebHookClientRoute,
                _webHookClientConverter.Convert(client));
        client.Id = createdClient?.Id;
    }

    public async Task SaveClient(WebHookClientModel client)
    {
        if (client.Id != null)
            await _httpService.Put($"{WebHookClientRoute}/{client.Id.Value}", _webHookClientConverter.Convert(client));
    }

    public async Task DeleteClient(WebHookClientModel client)
    {
        await _httpService.Delete($"{WebHookClientRoute}/{client.Id}");
        WebHookClients.Remove(client);
    }
}