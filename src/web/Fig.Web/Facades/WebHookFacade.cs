using Fig.Common.NetStandard.WebHook;
using Fig.Web.Converters;
using Fig.Web.Models.WebHooks;
using Fig.Web.Services;

namespace Fig.Web.Facades;

public class WebHookFacade : IWebHookFacade
{
    private const string WebHookClientRoute = "webhookclient";
    private const string WebHooksRoute = "webhooks";
    
    private readonly IHttpService _httpService;
    private readonly IWebHookClientConverter _webHookClientConverter;
    private readonly IWebHookConverter _webHookConverter;
    private readonly IWebHookAnalysisService _webHookAnalysisService;

    public WebHookFacade(IHttpService httpService,
        IWebHookClientConverter webHookClientConverter,
        IWebHookConverter webHookConverter,
        IWebHookAnalysisService webHookAnalysisService)
    {
        _httpService = httpService;
        _webHookClientConverter = webHookClientConverter;
        _webHookConverter = webHookConverter;
        _webHookAnalysisService = webHookAnalysisService;
    }

    public List<WebHookClientModel> WebHookClients { get; } = new();

    public List<WebHookModel> WebHooks { get; } = new();
    
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
        var webHooks = await _httpService.Get<List<WebHookDataContract>>(WebHooksRoute);

        WebHooks.Clear();
        
        if (webHooks is null)
            return;
        
        foreach (var client in webHooks.Select(a => _webHookConverter.Convert(a)))
        {
            WebHooks.Add(client);
        }

        foreach (var webHook in WebHooks)
        {
            webHook.MatchingClients = await _webHookAnalysisService.PerformAnalysis(webHook);
        }
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

    public async Task SaveWebHook(WebHookModel webHook)
    {
        webHook.MatchingClients = await _webHookAnalysisService.PerformAnalysis(webHook);
        if (webHook.Id == null)
            await _httpService.Post(WebHooksRoute, _webHookConverter.Convert(webHook));
        else
        {
            var result = await _httpService.Put<WebHookDataContract>($"{WebHooksRoute}/{webHook.Id}", _webHookConverter.Convert(webHook));
            webHook.Id = result.Id;
        }
    }

    public async Task DeleteWebHook(WebHookModel webHook)
    {
        if (webHook.Id is not null)
            await _httpService.Delete($"{WebHooksRoute}/{webHook.Id}");

        WebHooks.Remove(webHook);
    }
}