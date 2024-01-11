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
        using var transaction = Session.BeginTransaction();
        var id = (Guid) Session.Save(entity);
        transaction.Commit();
        Session.Flush();
        Session.Evict(entity);

        return id;
    }

    protected T? Get(Guid id, bool upgradeLock)
    {
        return upgradeLock ? 
            Session.Get<T>(id, LockMode.Upgrade) : 
            Session.Get<T>(id);
    }

    protected void Update(T entity)
    {
        Session.Update(entity);
        Session.Flush();
        Session.Evict(entity);
    }

    protected void Delete(T entity)
    {
        Session.Delete(entity);
        Session.Flush();
        Session.Evict(entity);
    }

    protected IEnumerable<T> GetAll(bool upgradeLock)
    {
        if (upgradeLock)
        {
            return Session.Query<T>()
                .WithLock(LockMode.Upgrade)
                .ToList();
        }
        else
        {
            return Session.Query<T>()
            .ToList();
        }
    }
}