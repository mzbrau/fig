using Fig.Contracts.WebHook;

namespace Fig.Api.Services;

public interface IWebHookService
{
    IEnumerable<WebHookClientDataContract> GetClients();
    
    WebHookClientDataContract AddClient(WebHookClientDataContract client);

    void DeleteClient(Guid clientId);
    
    WebHookClientDataContract UpdateClient(Guid clientId, WebHookClientDataContract data);
    
    IEnumerable<WebHookDataContract> GetWebHooks();
    
    WebHookDataContract AddWebHook(WebHookDataContract webHook);
    
    WebHookDataContract UpdateWebHook(Guid webHookId, WebHookDataContract webHook);
    
    void DeleteWebHook(Guid webHookId);
    
    Task<WebHookClientTestResultsDataContract> TestClient(Guid clientId);
}