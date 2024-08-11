using System.Diagnostics;
using Fig.Api.Observability;
using Fig.Datalayer.BusinessEntities;
using NHibernate;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class LookupTablesRepository : RepositoryBase<LookupTableBusinessEntity>, ILookupTablesRepository
{
    public LookupTablesRepository(ISession session) 
        : base(session)
    {
    }

    public LookupTableBusinessEntity? GetItem(Guid id)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<LookupTableBusinessEntity>();
        criteria.Add(Restrictions.Eq("Id", id));
        criteria.SetLockMode(LockMode.Upgrade);
        var item = criteria.UniqueResult<LookupTableBusinessEntity>();
        return item;
    }

    public LookupTableBusinessEntity? GetItem(string name)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<LookupTableBusinessEntity>();
        criteria.Add(Restrictions.Eq("Name", name));
        var item = criteria.UniqueResult<LookupTableBusinessEntity>();
        return item;
    }

    public IList<LookupTableBusinessEntity> GetAllItems()
    {
        return GetAll(false);
    }

    public void SaveItem(LookupTableBusinessEntity item)
    {
        Save(item);
    }

    public void UpdateItem(LookupTableBusinessEntity item)
    {
        Update(item);
    }

    public void DeleteItem(LookupTableBusinessEntity item)
    {
        Delete(item);
    }
} 