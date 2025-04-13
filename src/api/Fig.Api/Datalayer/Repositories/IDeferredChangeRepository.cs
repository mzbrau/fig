using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IDeferredChangeRepository
{
    Task Schedule(DeferredChangeBusinessEntity entity);

    Task<IEnumerable<DeferredChangeBusinessEntity>> GetChangesToExecute(DateTime evaluationTime);

    Task<IEnumerable<DeferredChangeBusinessEntity>> GetAllChanges();

    Task Reschedule(Guid id, DateTime newExecuteAt);

    Task Remove(Guid id);
}