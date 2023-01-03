using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IWebHookClientRepository
{
    IEnumerable<WebHookClientBusinessEntity> GetClients();
    
    Guid AddClient(WebHookClientBusinessEntity client);

    void DeleteClient(Guid clientId);
    
    WebHookClientBusinessEntity? GetClient(Guid dataId);
    
    void UpdateClient(WebHookClientBusinessEntity client);
}