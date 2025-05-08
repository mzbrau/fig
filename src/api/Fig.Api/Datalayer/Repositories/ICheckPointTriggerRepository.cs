using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface ICheckPointTriggerRepository
{
    Task AddTrigger(CheckPointTriggerBusinessEntity trigger);
    
    Task<IEnumerable<CheckPointTriggerBusinessEntity>> GetUnhandledTriggers();
    
    Task DeleteHandledTriggers();
    
    Task DeleteAllTriggers();
}