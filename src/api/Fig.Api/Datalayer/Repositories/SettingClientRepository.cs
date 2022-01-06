using Fig.Api.Datalayer.BusinessEntities;
using ISession = NHibernate.ISession;
using ITransaction = NHibernate.ITransaction;

namespace Fig.Api.Datalayer.Repositories;

public class SettingClientRepository
{
    public Guid Save(SettingsClientBusinessEntity person)
    {
        Guid id;
        using (ISession session = NHibernateHelper.OpenSession())
        using (ITransaction transaction = session.BeginTransaction())
        {
            id = (Guid)session.Save(person);
            transaction.Commit();
        }

        return id;
    }

    public SettingsClientBusinessEntity Get(Guid id)
    {
        using (ISession session = NHibernateHelper.OpenSession())
            return session.Get<SettingsClientBusinessEntity>(id);
    }

    public void Update(SettingsClientBusinessEntity person)
    {
        using (ISession session = NHibernateHelper.OpenSession())
        using (ITransaction transaction = session.BeginTransaction())
        {
            session.Update(person);
            transaction.Commit();
        }
    }

    public void Delete(SettingsClientBusinessEntity person)
    {
        using (ISession session = NHibernateHelper.OpenSession())
        using (ITransaction transaction = session.BeginTransaction())
        {
            session.Delete(person);
            transaction.Commit();
        }
    }

    public long RowCount()
    {
        using (ISession session = NHibernateHelper.OpenSession())
        {
            return session.QueryOver<SettingsClientBusinessEntity>().RowCountInt64();
        }
    }
}