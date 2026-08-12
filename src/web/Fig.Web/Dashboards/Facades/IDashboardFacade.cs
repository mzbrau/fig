using Fig.Contracts.Dashboards;
using Fig.Web.Services;

namespace Fig.Web.Dashboards.Facades;

public interface IDashboardFacade
{
    IReadOnlyList<DashboardDataContract> Dashboards { get; }

    Task LoadAll();

    Task<DashboardDataContract?> Get(Guid id);

    Task<DashboardDataContract?> Create(DashboardDataContract dashboard);

    Task<DashboardDataContract?> Update(Guid id, DashboardDataContract dashboard, bool forceOverwrite = false);

    Task Delete(Guid id);
}
