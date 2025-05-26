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
        
        // First query for eligible changes
        var criteria = Session.CreateCriteria<DeferredChangeBusinessEntity>();
        criteria.Add(Restrictions.Le(nameof(DeferredChangeBusinessEntity.ExecuteAtUtc), evaluationTime));
        criteria.AddOrder(Order.Asc(nameof(DeferredChangeBusinessEntity.ExecuteAtUtc)));
        var results = await criteria.ListAsync<DeferredChangeBusinessEntity>();

        // Process each result individually with its own transaction
        foreach (var result in results)
        {
            result.DeserializeAndDecrypt(_encryptionService);
            result.HandlingInstance = _appInstance;

            using var tx = Session.BeginTransaction();
            try
            {
                await Update(result);
                await tx.CommitAsync();
                changes.Add(result);
            }
            catch (StaleObjectStateException)
            {
                await tx.RollbackAsync();
                // Skip this result as it's being handled by another instance
            }
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

    public async Task Remove(Guid id)
    {
        await Session.CreateQuery("delete from DeferredChangeBusinessEntity where Id = :id")
            .SetParameter("id", id)
            .ExecuteUpdateAsync();
    }

    public async Task<DeferredChangeBusinessEntity?> GetById(Guid id)
    {
        var entity = await Get(id, true);
        entity?.DeserializeAndDecrypt(_encryptionService);
        return entity;
    }

    public async Task UpdateDeferredChange(DeferredChangeBusinessEntity existing)
    {
        await Update(existing);
    }
}
