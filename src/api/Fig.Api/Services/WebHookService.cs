using Fig.Api.Converters;
using Fig.Api.Datalayer.Repositories;
using Fig.Common.NetStandard.WebHook;

namespace Fig.Api.Services;

public class WebHookService : IWebHookService
{
    private readonly IWebHookClientRepository _webHookClientRepository;
    private readonly IWebHookClientConverter _webHookClientConverter;
    private readonly IWebHookRepository _webHookRepository;
    private readonly IWebHookConverter _webHookConverter;

    public WebHookService(IWebHookClientRepository webHookClientRepository,
        IWebHookClientConverter webHookClientConverter,
        IWebHookRepository webHookRepository,
        IWebHookConverter webHookConverter)
    {
        _webHookClientRepository = webHookClientRepository;
        _webHookClientConverter = webHookClientConverter;
        _webHookRepository = webHookRepository;
        _webHookConverter = webHookConverter;
    }
    
    public IEnumerable<WebHookClientDataContract> GetClients()
    {
        var clients = _webHookClientRepository.GetClients();
        return clients.Select(a => _webHookClientConverter.Convert(a));
    }

    public WebHookClientDataContract AddClient(WebHookClientDataContract client)
    {
        var requestedClient = _webHookClientConverter.Convert(client);
        client.Id = _webHookClientRepository.AddClient(requestedClient);
        return client;
    }

    public void DeleteClient(Guid clientId)
    {
        _webHookClientRepository.DeleteClient(clientId);
    }
    
    public WebHookClientDataContract UpdateClient(Guid clientId, WebHookClientDataContract data)
    {
        var client = _webHookClientRepository.GetClient(clientId);

        if (client is null)
            throw new KeyNotFoundException($"No web hook client with id {clientId}");

        client.Name = data.Name;
        client.BaseUri = data.BaseUri.ToString();

        _webHookClientRepository.UpdateClient(client);

        return _webHookClientConverter.Convert(client);
    }

    public IEnumerable<WebHookDataContract> GetWebHooks()
    {
        var webHooks = _webHookRepository.GetWebHooks();
        return webHooks.Select(a => _webHookConverter.Convert(a));
    }

    public WebHookDataContract AddWebHook(WebHookDataContract webHook)
    {
        var requestedWebHook = _webHookConverter.Convert(webHook);
        webHook.Id = _webHookRepository.AddWebHook(requestedWebHook);
        return webHook;
    }

    public WebHookDataContract UpdateWebHook(Guid webHookId, WebHookDataContract update)
    {
        var webHook = _webHookRepository.GetWebHook(webHookId);

        if (webHook is null)
            throw new KeyNotFoundException($"No web hook with id {webHookId}");

        webHook.WebHookType = update.WebHookType;
        webHook.ClientNameRegex = update.ClientNameRegex;
        webHook.SettingNameRegex = update.SettingNameRegex;
        webHook.MinSessions = update.MinSessions;

        _webHookRepository.UpdateWebHook(webHook);

        return _webHookConverter.Convert(webHook);
    }

    public void DeleteWebHook(Guid webHookId)
    {
        _webHookRepository.DeleteWebHook(webHookId);
    }
}