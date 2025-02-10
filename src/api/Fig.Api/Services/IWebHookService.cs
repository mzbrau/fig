using Fig.Contracts.WebHook;

namespace Fig.Api.Services;

public interface IWebHookService
{
    Task<IEnumerable<WebHookClientDataContract>> GetClients();
    
    Task<WebHookClientDataContract> AddClient(WebHookClientDataContract client);

    Task DeleteClient(Guid clientId);
    
    Task<WebHookClientDataContract> UpdateClient(Guid clientId, WebHookClientDataContract data);
    
    Task<IEnumerable<WebHookDataContract>> GetWebHooks();
    
    Task<WebHookDataContract> AddWebHook(WebHookDataContract webHook);
    
    Task<WebHookDataContract> UpdateWebHook(Guid webHookId, WebHookDataContract webHook);
    
    Task DeleteWebHook(Guid webHookId);
    
    Task<WebHookClientTestResultsDataContract> TestClient(Guid clientId);
}