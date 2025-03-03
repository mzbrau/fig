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

    public async Task<LookupTableBusinessEntity?> GetItem(Guid id)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<LookupTableBusinessEntity>();
        criteria.Add(Restrictions.Eq(nameof(LookupTableBusinessEntity.Id), id));
        criteria.SetLockMode(LockMode.Upgrade);
        var item = await criteria.UniqueResultAsync<LookupTableBusinessEntity>();
        return item;
    }

    public async Task<IList<LookupTableBusinessEntity>> GetAllItems()
    {
        return await GetAll(false);
    }

    public async Task SaveItem(LookupTableBusinessEntity item)
    {
        await Save(item);
    }

    public async Task UpdateItem(LookupTableBusinessEntity item)
    {
        await Update(item);
    }

    public async Task DeleteItem(LookupTableBusinessEntity item)
    {
        await Delete(item);
    }
} 