namespace Fig.Api.Datalayer.Repositories;
using ISession = NHibernate.ISession;
using ITransaction = NHibernate.ITransaction;

public abstract class RepositoryBase<T>
{
    protected readonly IFigSessionFactory SessionFactory;

    protected RepositoryBase(IFigSessionFactory sessionFactory)
    {
        SessionFactory = sessionFactory;
    }
    
    protected Guid Save(T entity)
    {
        using ISession session = SessionFactory.OpenSession();
        using ITransaction transaction = session.BeginTransaction();
        var id = (Guid)session.Save(entity);
        transaction.Commit();

        return id;
    }

    protected T Get(Guid id)
    {
        using ISession session = SessionFactory.OpenSession();
        return session.Get<T>(id);
    }

    protected void Update(T entity)
    {
        using ISession session = SessionFactory.OpenSession();
        using ITransaction transaction = session.BeginTransaction();
        session.Update(entity);
        transaction.Commit();
    }

    protected void Delete(T entity)
    {
        using ISession session = SessionFactory.OpenSession();
        using ITransaction transaction = session.BeginTransaction();
        session.Delete(entity);
        transaction.Commit();
    }

    protected IEnumerable<T> GetAll()
    {
        using ISession session = SessionFactory.OpenSession();
        return session.Query<T>().ToList();
    }
}