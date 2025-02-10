using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IWebHookClientRepository
{
    Task<IEnumerable<WebHookClientBusinessEntity>> GetClients(bool upgradeLock);
    
    Task<IList<WebHookClientBusinessEntity>> GetClients(IEnumerable<Guid> clientIds);
    
    Task<Guid> AddClient(WebHookClientBusinessEntity client);

    Task DeleteClient(Guid clientId);
    
    Task<WebHookClientBusinessEntity?> GetClient(Guid dataId);
    
    Task UpdateClient(WebHookClientBusinessEntity client);
}