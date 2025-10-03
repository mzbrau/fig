using System.Diagnostics;
using Fig.Api.Observability;
using Fig.Datalayer.BusinessEntities;
using NHibernate.Criterion;
using NHibernate.Linq;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class CheckPointRepository : RepositoryBase<CheckPointBusinessEntity>, ICheckPointRepository
{
    public CheckPointRepository(ISession session) : base(session)
    {
    }

    public async Task<IEnumerable<CheckPointBusinessEntity>> GetCheckPoints(DateTime startDate, DateTime endDate)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<CheckPointBusinessEntity>();
        criteria.Add(Restrictions.Ge(nameof(CheckPointBusinessEntity.Timestamp), startDate));
        criteria.Add(Restrictions.Le(nameof(CheckPointBusinessEntity.Timestamp), endDate));

        criteria.AddOrder(Order.Desc(nameof(CheckPointBusinessEntity.Timestamp)));
        return await criteria.ListAsync<CheckPointBusinessEntity>();
    }

    public async Task Add(CheckPointBusinessEntity checkPoint)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        await Save(checkPoint);
    }

    public async Task<DateTime> GetEarliestEntry()
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var result = await Session.Query<CheckPointBusinessEntity>().FirstOrDefaultAsync();
        return result?.Timestamp ?? DateTime.UtcNow;
    }

    public async Task<CheckPointBusinessEntity?> GetCheckPoint(Guid id)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<CheckPointBusinessEntity>();
        criteria.Add(Restrictions.Eq(nameof(CheckPointBusinessEntity.Id), id));
        return await criteria.UniqueResultAsync<CheckPointBusinessEntity>();
    }

    public async Task UpdateCheckPoint(CheckPointBusinessEntity checkPoint)
    {
        await Update(checkPoint);
    }
    
    public async Task<int> DeleteOlderThan(DateTime cutoffDate)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var deleteCount = await Session.CreateQuery(
                "delete from CheckPointBusinessEntity where Timestamp < :cutoffDate")
            .SetParameter("cutoffDate", cutoffDate)
            .ExecuteUpdateAsync();
        
        await Session.FlushAsync();
        return deleteCount;
    }
}