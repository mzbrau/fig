using NHibernate;
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

    protected T? Get(Guid id)
    {
        return Session.Get<T>(id);
    }

    protected void Update(T entity)
    {
        using var transaction = Session.BeginTransaction();
        Session.Update(entity);
        transaction.Commit();
        Session.Flush();
        Session.Evict(entity);
    }

    protected void Delete(T entity)
    {
        using var transaction = Session.BeginTransaction();
        Session.Delete(entity);
        transaction.Commit();
        Session.Flush();
        Session.Evict(entity);
    }

    protected IEnumerable<T> GetAll()
    {
        return Session.Query<T>().ToList();
    }
}