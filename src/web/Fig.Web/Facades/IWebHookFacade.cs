using Fig.Web.Models.WebHooks;

namespace Fig.Web.Facades;

public interface IWebHookFacade
{
    List<WebHookClientModel> WebHookClients { get; }
    
    List<WebHookModel> WebHooks { get; }
    
    Task LoadAllClients();
    
    Task LoadAllWebHooks();
    
    Task AddClient(WebHookClientModel client);
    
    Task SaveClient(WebHookClientModel client);
    
    Task DeleteClient(WebHookClientModel row);
    
    Task SaveWebHook(WebHookModel webHook);
    
    Task DeleteWebHook(WebHookModel webHook);
}