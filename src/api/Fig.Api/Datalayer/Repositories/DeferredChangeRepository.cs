using System.Diagnostics;
using Fig.Api.Exceptions;
using Fig.Api.ExtensionMethods;
using Fig.Api.Observability;
using Fig.Api.Services;
using Fig.Datalayer.BusinessEntities;
using NHibernate;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class DeferredChangeRepository : RepositoryBase<DeferredChangeBusinessEntity>, IDeferredChangeRepository
{
    private readonly IEncryptionService _encryptionService;
    private readonly string _appInstance = Guid.NewGuid().ToString("N");

    public DeferredChangeRepository(ISession session, IEncryptionService encryptionService) 
        : base(session)
    {
        _encryptionService = encryptionService;
    }
    
    public async Task Schedule(DeferredChangeBusinessEntity entity)
    {
        entity.SerializeAndEncrypt(_encryptionService);
        await Save(entity);
    }

    public async Task<IEnumerable<DeferredChangeBusinessEntity>> GetChangesToExecute(DateTime evaluationTime)
    {
        var changes = new List<DeferredChangeBusinessEntity>();
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        using var tx = Session.BeginTransaction();
        
        var criteria = Session.CreateCriteria<DeferredChangeBusinessEntity>();
        criteria.Add(Restrictions.Ge(nameof(DeferredChangeBusinessEntity.ExecuteAt), evaluationTime));
        criteria.AddOrder(Order.Desc(nameof(DeferredChangeBusinessEntity.ExecuteAt)));
        var results = await criteria.ListAsync<DeferredChangeBusinessEntity>();

        foreach (var result in results)
        {
            result.DeserializeAndDecrypt(_encryptionService);
            result.HandlingInstance = _appInstance;

            try
            {
                await Update(result);
                await tx.CommitAsync();
            }
            catch (StaleObjectStateException)
            {
                await tx.RollbackAsync();
            }
            
            changes.Add(result);
        }

        return changes;
    }

    public async Task<IEnumerable<DeferredChangeBusinessEntity>> GetAllChanges()
    {
        var changes = new List<DeferredChangeBusinessEntity>();
        var results = await GetAll(false);
        foreach (var result in results)
        {
            result.DeserializeAndDecrypt(_encryptionService);
            changes.Add(result);
        }

        return changes;
    }

    public async Task Reschedule(Guid id, DateTime newExecuteAt)
    {
        var entity = await Get(id, true);
        if (entity == null)
        {
            throw new ChangeNotFoundException($"No deferred change with id {id}");
        }

        entity.ExecuteAt = newExecuteAt;
        await Update(entity);
    }

    public async Task Remove(Guid id)
    {
        var entity = await Get(id, true);
        if (entity is not null)
        {
            await Delete(entity);
        }
    }
}
