using Fig.Common.WebHook;

namespace Fig.Api.Services;

public interface IWebHookService
{
    IEnumerable<WebHookClientDataContract> GetClients();
    
    WebHookClientDataContract AddClient(WebHookClientDataContract data);

    void DeleteClient(Guid clientId);
    
    WebHookClientDataContract UpdateClient(Guid clientId, WebHookClientDataContract data);
}