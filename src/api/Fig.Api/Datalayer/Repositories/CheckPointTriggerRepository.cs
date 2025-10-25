using System.Diagnostics;
using Fig.Api.Observability;
using Fig.Datalayer.BusinessEntities;
using NHibernate;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class CheckPointTriggerRepository : RepositoryBase<CheckPointTriggerBusinessEntity>, ICheckPointTriggerRepository
{
    private readonly string _appInstance = Guid.NewGuid().ToString("N");
    
    public CheckPointTriggerRepository(ISession session) 
        : base(session)
    {
    }

    public async Task AddTrigger(CheckPointTriggerBusinessEntity trigger)
    {
        await Save(trigger);
    }

    public async Task<IEnumerable<CheckPointTriggerBusinessEntity>> GetUnhandledTriggers()
    {
        var changes = new List<CheckPointTriggerBusinessEntity>();
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        
        // First query for triggers
        var criteria = Session.CreateCriteria<CheckPointTriggerBusinessEntity>();
        criteria.Add(Restrictions.IsNull(nameof(CheckPointTriggerBusinessEntity.HandlingInstance)));
        var results = await criteria.ListAsync<CheckPointTriggerBusinessEntity>();

        // Process each result individually with its own transaction
        foreach (var result in results)
        {
            result.HandlingInstance = _appInstance;

            var existingTransaction = Session.GetCurrentTransaction();
            var needsTransaction = existingTransaction == null || !existingTransaction.IsActive;
            var tx = needsTransaction ? Session.BeginTransaction() : null;
            try
            {
                await Update(result);
                if (tx != null)
                    await tx.CommitAsync();
                changes.Add(result);
            }
            catch (StaleObjectStateException)
            {
                if (tx?.IsActive == true)
                    await tx.RollbackAsync();
                // Skip this result as it's being handled by another instance
            }
            catch (Exception)
            {
                if (tx?.IsActive == true)
                    await tx.RollbackAsync();

                throw;
            }
            finally
            {
                tx?.Dispose();
            }
        }

        return changes;
    }

    public async Task DeleteHandledTriggers()
    {
        var hql = @"delete from CheckPointTriggerBusinessEntity 
                where HandlingInstance is not null";

        await Session.CreateQuery(hql)
            .ExecuteUpdateAsync();
    }

    public async Task DeleteAllTriggers()
    {
        var criteria = Session.CreateCriteria<CheckPointTriggerBusinessEntity>();
        var results = await criteria.ListAsync<CheckPointTriggerBusinessEntity>();

        foreach (var result in results)
        {
            await Delete(result);
        }
    }
}