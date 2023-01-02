using Fig.Api.Converters;
using Fig.Api.Datalayer.Repositories;
using Fig.Common.WebHook;

namespace Fig.Api.Services;

public class WebHookService : IWebHookService
{
    private readonly IWebHookClientRepository _webHookClientRepository;
    private readonly IWebHookClientConverter _webHookClientConverter;

    public WebHookService(IWebHookClientRepository webHookClientRepository, IWebHookClientConverter webHookClientConverter)
    {
        _webHookClientRepository = webHookClientRepository;
        _webHookClientConverter = webHookClientConverter;
    }
    
    public IEnumerable<WebHookClientDataContract> GetClients()
    {
        var clients = _webHookClientRepository.GetClients();
        return clients.Select(a => _webHookClientConverter.Convert(a));
    }

    public WebHookClientDataContract AddClient(WebHookClientDataContract data)
    {
        var requestedClient = _webHookClientConverter.Convert(data);
        requestedClient.Secret = $"{Guid.NewGuid()}{Guid.NewGuid()}";
        requestedClient.HashedSecret = BCrypt.Net.BCrypt.EnhancedHashPassword(requestedClient.Secret);
        _webHookClientRepository.AddClient(requestedClient);
        data.HashedSecret = requestedClient.HashedSecret;
        return data;
    }

    public void DeleteClient(string clientName)
    {
        _webHookClientRepository.DeleteClient(clientName);
    }
    
    public WebHookClientDataContract UpdateClient(WebHookClientDataContract data)
    {
        var client = _webHookClientRepository.GetClient(data.Id);

        if (client is null)
            throw new KeyNotFoundException($"No web hook client with id {data.Id}");

        client.Name = data.Name;
        client.BaseUri = data.BaseUri.ToString();

        _webHookClientRepository.UpdateClient(client);

        return _webHookClientConverter.Convert(client);
    }
}