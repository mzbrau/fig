using Fig.Contracts.Dashboards;

namespace Fig.Api.Services;

public interface IDashboardService : IAuthenticatedService
{
    Task<IEnumerable<DashboardDataContract>> GetAll();

    Task<DashboardDataContract> Get(Guid id);

    Task<DashboardDataContract> Create(DashboardDataContract dashboard);

    Task<DashboardDataContract> Update(Guid id, DashboardDataContract dashboard, bool forceOverwrite = false);

    Task Delete(Guid id);
}
