using System.Diagnostics;
using Fig.Api.Observability;
using Fig.Datalayer.BusinessEntities;
using NHibernate;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class DashboardRepository : RepositoryBase<DashboardBusinessEntity>, IDashboardRepository
{
    public DashboardRepository(ISession session)
        : base(session)
    {
    }

    public async Task<IList<DashboardBusinessEntity>> GetAllDashboards()
    {
        return await GetAll(false);
    }

    public async Task<DashboardBusinessEntity?> GetDashboard(Guid id, bool forUpdate = false)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<DashboardBusinessEntity>();
        criteria.Add(Restrictions.Eq(nameof(DashboardBusinessEntity.Id), id));
        if (forUpdate)
            criteria.SetLockMode(LockMode.Upgrade);
        return await criteria.UniqueResultAsync<DashboardBusinessEntity>();
    }

    public async Task<DashboardBusinessEntity?> GetDashboardByName(string name)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<DashboardBusinessEntity>();
        criteria.Add(Restrictions.InsensitiveLike(nameof(DashboardBusinessEntity.Name), name, MatchMode.Exact));
        return await criteria.UniqueResultAsync<DashboardBusinessEntity>();
    }

    public async Task<Guid> AddDashboard(DashboardBusinessEntity dashboard)
    {
        return await Save(dashboard);
    }

    public async Task UpdateDashboard(DashboardBusinessEntity dashboard)
    {
        await Update(dashboard);
    }

    public async Task DeleteDashboard(DashboardBusinessEntity dashboard)
    {
        await Delete(dashboard);
    }
}
