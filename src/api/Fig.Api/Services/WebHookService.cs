using Fig.Api.Converters;
using Fig.Api.Datalayer.Repositories;
using Fig.Contracts.WebHook;

namespace Fig.Api.Services;

public class WebHookService : IWebHookService
{
    private readonly IWebHookClientRepository _webHookClientRepository;
    private readonly IWebHookClientConverter _webHookClientConverter;
    private readonly IWebHookRepository _webHookRepository;
    private readonly IWebHookConverter _webHookConverter;
    private readonly IWebHookClientTestingService _webHookClientTestingService;

    public WebHookService(IWebHookClientRepository webHookClientRepository,
        IWebHookClientConverter webHookClientConverter,
        IWebHookRepository webHookRepository,
        IWebHookConverter webHookConverter,
        IWebHookClientTestingService webHookClientTestingService)
    {
        _webHookClientRepository = webHookClientRepository;
        _webHookClientConverter = webHookClientConverter;
        _webHookRepository = webHookRepository;
        _webHookConverter = webHookConverter;
        _webHookClientTestingService = webHookClientTestingService;
    }
    
    public async Task<IEnumerable<WebHookClientDataContract>> GetClients()
    {
        var clients = await _webHookClientRepository.GetClients(false);
        return clients.Select(a => _webHookClientConverter.Convert(a));
    }

    public async Task<WebHookClientDataContract> AddClient(WebHookClientDataContract client)
    {
        var requestedClient = _webHookClientConverter.Convert(client);
        client.Id = await _webHookClientRepository.AddClient(requestedClient);
        return client;
    }

    public async Task DeleteClient(Guid clientId)
    {
        var usages = await _webHookRepository.GetWebHooksForClient(clientId);
        if (usages.Any())
            throw new InvalidOperationException("Cannot delete client as it has active web hooks");
        
        await _webHookClientRepository.DeleteClient(clientId);
    }
    
    public async Task<WebHookClientDataContract> UpdateClient(Guid clientId, WebHookClientDataContract data)
    {
        var client = await _webHookClientRepository.GetClient(clientId);

        if (client is null)
            throw new KeyNotFoundException($"No web hook client with id {clientId}");

        client.Name = data.Name;
        client.BaseUri = data.BaseUri.ToString();

        if (data.Secret is not null)
        {
            client.Secret = data.Secret;
        }

        await _webHookClientRepository.UpdateClient(client);

        return _webHookClientConverter.Convert(client);
    }

    public async Task<IEnumerable<WebHookDataContract>> GetWebHooks()
    {
        var webHooks = await _webHookRepository.GetWebHooks();
        return webHooks.Select(a => _webHookConverter.Convert(a));
    }

    public async Task<WebHookDataContract> AddWebHook(WebHookDataContract webHook)
    {
        var requestedWebHook = _webHookConverter.Convert(webHook);
        webHook.Id = await _webHookRepository.AddWebHook(requestedWebHook);
        return webHook;
    }

    public async Task<WebHookDataContract> UpdateWebHook(Guid webHookId, WebHookDataContract update)
    {
        var webHook = await _webHookRepository.GetWebHook(webHookId);

        if (webHook is null)
            throw new KeyNotFoundException($"No web hook with id {webHookId}");

        webHook.WebHookType = update.WebHookType;
        webHook.ClientNameRegex = update.ClientNameRegex;
        webHook.SettingNameRegex = update.SettingNameRegex;
        webHook.MinSessions = update.MinSessions;

        await _webHookRepository.UpdateWebHook(webHook);

        return _webHookConverter.Convert(webHook);
    }

    public async Task DeleteWebHook(Guid webHookId)
    {
        await _webHookRepository.DeleteWebHook(webHookId);
    }

    public async Task<WebHookClientTestResultsDataContract> TestClient(Guid clientId)
    {
        var client = await _webHookClientRepository.GetClient(clientId);

        if (client is null)
            throw new KeyNotFoundException($"Unknown web hook client with id {client}");

        return await _webHookClientTestingService.PerformTest(client);
    }
}