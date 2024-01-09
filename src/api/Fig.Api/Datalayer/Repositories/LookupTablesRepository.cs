using Fig.Datalayer.BusinessEntities;
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
        var criteria = Session.CreateCriteria<LookupTableBusinessEntity>();
        criteria.Add(Restrictions.Eq("Id", id));
        var item = criteria.UniqueResult<LookupTableBusinessEntity>();
        return item;
    }

    public LookupTableBusinessEntity? GetItem(string name)
    {
        var criteria = Session.CreateCriteria<LookupTableBusinessEntity>();
        criteria.Add(Restrictions.Eq("Name", name));
        var item = criteria.UniqueResult<LookupTableBusinessEntity>();
        return item;
    }

    public IEnumerable<LookupTableBusinessEntity> GetAllItems()
    {
        return GetAll();
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