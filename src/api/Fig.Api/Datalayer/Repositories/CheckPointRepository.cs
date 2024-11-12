using System.Diagnostics;
using Fig.Api.Observability;
using Fig.Datalayer.BusinessEntities;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class CheckPointRepository : RepositoryBase<CheckPointBusinessEntity>, ICheckPointRepository
{
    public CheckPointRepository(ISession session) : base(session)
    {
    }

    public IEnumerable<CheckPointBusinessEntity> GetCheckPoints(DateTime startDate, DateTime endDate)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<CheckPointBusinessEntity>();
        criteria.Add(Restrictions.Ge(nameof(CheckPointBusinessEntity.Timestamp), startDate));
        criteria.Add(Restrictions.Le(nameof(CheckPointBusinessEntity.Timestamp), endDate));

        criteria.AddOrder(Order.Desc(nameof(CheckPointBusinessEntity.Timestamp)));
        return criteria.List<CheckPointBusinessEntity>();
    }

    public void Add(CheckPointBusinessEntity checkPoint)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        Save(checkPoint);
    }

    public DateTime GetEarliestEntry()
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var result = Session.Query<CheckPointBusinessEntity>().FirstOrDefault();
        return result?.Timestamp ?? DateTime.UtcNow;
    }

    public CheckPointBusinessEntity? GetCheckPoint(Guid id)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<CheckPointBusinessEntity>();
        criteria.Add(Restrictions.Eq(nameof(CheckPointBusinessEntity.Id), id));
        return criteria.UniqueResult<CheckPointBusinessEntity>();
    }

    public void UpdateCheckPoint(CheckPointBusinessEntity checkPoint)
    {
        Update(checkPoint);
    }
}