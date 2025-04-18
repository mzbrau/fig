using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IDeferredChangeRepository
{
    Task Schedule(DeferredChangeBusinessEntity entity);

    Task<IEnumerable<DeferredChangeBusinessEntity>> GetChangesToExecute(DateTime evaluationTime);

    Task<IEnumerable<DeferredChangeBusinessEntity>> GetAllChanges();

    Task Remove(Guid id);
    
    Task<DeferredChangeBusinessEntity?> GetById(Guid deferredChangeId);
    
    Task UpdateDeferredChange(DeferredChangeBusinessEntity existing);
}