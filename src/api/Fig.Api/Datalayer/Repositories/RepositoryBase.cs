using System.Diagnostics;
using Fig.Api.Observability;
using NHibernate;
using NHibernate.Linq;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public abstract class RepositoryBase<T>
{
    protected readonly ISession Session;

    protected RepositoryBase(ISession session)
    {
        Session = session;
    }

    protected async Task<Guid> Save(T entity)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var existingTransaction = Session.GetCurrentTransaction();
        var needsTransaction = existingTransaction == null || !existingTransaction.IsActive;
        var transaction = needsTransaction ? Session.BeginTransaction() : null;
        try
        {
            var id = (Guid) (await Session.SaveAsync(entity));
            if (transaction != null)
                await transaction.CommitAsync();
            await Session.FlushAsync();
            await Session.EvictAsync(entity);

            return id;
        }
        catch
        {
            if (transaction?.IsActive == true)
                await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            transaction?.Dispose();
        }
    }

    protected async Task<T?> Get(Guid id, bool upgradeLock)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        return upgradeLock ? 
            await Session.GetAsync<T>(id, LockMode.Upgrade) : 
            await Session.GetAsync<T>(id);
    }

    protected async Task Update(T entity)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var existingTransaction = Session.GetCurrentTransaction();
        var needsTransaction = existingTransaction == null || !existingTransaction.IsActive;
        var transaction = needsTransaction ? Session.BeginTransaction() : null;
        try
        {
            await Session.UpdateAsync(entity);
            if (transaction != null)
                await transaction.CommitAsync();
            await Session.FlushAsync();
            await Session.EvictAsync(entity);
        }
        catch
        {
            if (transaction?.IsActive == true)
                await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            transaction?.Dispose();
        }
    }

    protected async Task Delete(T entity)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var existingTransaction = Session.GetCurrentTransaction();
        var needsTransaction = existingTransaction == null || !existingTransaction.IsActive;
        var transaction = needsTransaction ? Session.BeginTransaction() : null;
        try
        {
            await Session.DeleteAsync(entity);
            if (transaction != null)
                await transaction.CommitAsync();
            await Session.FlushAsync();
            await Session.EvictAsync(entity);
        }
        catch
        {
            if (transaction?.IsActive == true)
                await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            transaction?.Dispose();
        }
    }

    protected async Task<IList<T>> GetAll(bool upgradeLock)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        if (upgradeLock)
        {
            return await Session.Query<T>()
                .WithLock(LockMode.Upgrade)
                .ToListAsync();
        }

        return await Session.Query<T>()
            .ToListAsync();
    }
}