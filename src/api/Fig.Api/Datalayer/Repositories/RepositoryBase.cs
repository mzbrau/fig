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
        using var transaction = Session.BeginTransaction();
        var id = (Guid) (await Session.SaveAsync(entity));
        await transaction.CommitAsync();
        await Session.FlushAsync();
        await Session.EvictAsync(entity);

        return id;
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
        await Session.UpdateAsync(entity);
        await Session.FlushAsync();
        await Session.EvictAsync(entity);
    }

    protected async Task Delete(T entity)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        await Session.DeleteAsync(entity);
        await Session.FlushAsync();
        await Session.EvictAsync(entity);
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