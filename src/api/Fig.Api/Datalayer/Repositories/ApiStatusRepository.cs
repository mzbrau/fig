using System.Diagnostics;
using Fig.Api.Observability;
using Fig.Datalayer.BusinessEntities;
using NHibernate;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class ApiStatusRepository : RepositoryBase<ApiStatusBusinessEntity>, IApiStatusRepository
{
    public ApiStatusRepository(ISession session) 
        : base(session)
    {
    }

    public async Task AddOrUpdate(ApiStatusBusinessEntity status)
    {
        if (status.Id == null)
        {
            await Save(status);
        }
        else
        {
            await Update(status);
        }
    }

    public async Task<IList<ApiStatusBusinessEntity>> GetAllActive()
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<ApiStatusBusinessEntity>();
        criteria.Add(Restrictions.Eq("IsActive", true));
        criteria.SetLockMode(LockMode.Upgrade);
        return await criteria.ListAsync<ApiStatusBusinessEntity>();
    }
    
    public async Task<int> DeleteOlderThan(DateTime cutoffDate)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var deleteCount = await Session.CreateQuery(
                "delete from ApiStatusBusinessEntity where LastSeen < :cutoffDate and IsActive = false")
            .SetParameter("cutoffDate", cutoffDate)
            .ExecuteUpdateAsync();
        
        await Session.FlushAsync();
        return deleteCount;
    }
}