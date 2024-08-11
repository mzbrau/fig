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

    protected Guid Save(T entity)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        using var transaction = Session.BeginTransaction();
        var id = (Guid) Session.Save(entity);
        transaction.Commit();
        Session.Flush();
        Session.Evict(entity);

        return id;
    }

    protected T? Get(Guid id, bool upgradeLock)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        return upgradeLock ? 
            Session.Get<T>(id, LockMode.Upgrade) : 
            Session.Get<T>(id);
    }

    protected void Update(T entity)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        Session.Update(entity);
        Session.Flush();
        Session.Evict(entity);
    }

    protected void Delete(T entity)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        Session.Delete(entity);
        Session.Flush();
        Session.Evict(entity);
    }

    protected IList<T> GetAll(bool upgradeLock)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        if (upgradeLock)
        {
            return Session.Query<T>()
                .WithLock(LockMode.Upgrade)
                .ToList();
        }

        return Session.Query<T>()
            .ToList();
    }
}