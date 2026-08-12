using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IDashboardRepository
{
    Task<IList<DashboardBusinessEntity>> GetAllDashboards();

    Task<DashboardBusinessEntity?> GetDashboard(Guid id, bool forUpdate = false);

    Task<DashboardBusinessEntity?> GetDashboardByName(string name);

    Task<Guid> AddDashboard(DashboardBusinessEntity dashboard);

    Task UpdateDashboard(DashboardBusinessEntity dashboard);

    Task DeleteDashboard(DashboardBusinessEntity dashboard);
}
