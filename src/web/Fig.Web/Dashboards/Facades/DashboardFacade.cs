using Fig.Common.Events;
using Fig.Contracts.Dashboards;
using Fig.Web.Dashboards.Schema;
using Fig.Web.Events;
using Fig.Web.Services;

namespace Fig.Web.Dashboards.Facades;

public class DashboardFacade : IDashboardFacade
{
    private const string Route = "dashboards";
    private readonly IHttpService _httpService;
    private readonly IDashboardSchemaMigrator _schemaMigrator;
    private readonly List<DashboardDataContract> _dashboards = new();

    public DashboardFacade(
        IHttpService httpService,
        IEventDistributor eventDistributor,
        IDashboardSchemaMigrator schemaMigrator)
    {
        _httpService = httpService;
        _schemaMigrator = schemaMigrator;
        eventDistributor.Subscribe(EventConstants.LogoutEvent, () => _dashboards.Clear());
    }

    public IReadOnlyList<DashboardDataContract> Dashboards => _dashboards;

    public async Task LoadAll()
    {
        var result = await _httpService.Get<List<DashboardDataContract>>(Route);
        _dashboards.Clear();
        if (result is null)
            return;

        foreach (var dashboard in result)
            MigrateInPlace(dashboard);

        _dashboards.AddRange(result);
    }

    public async Task<DashboardDataContract?> Get(Guid id)
    {
        var dashboard = await _httpService.Get<DashboardDataContract>($"{Route}/{id}");
        MigrateInPlace(dashboard);
        return dashboard;
    }

    public async Task<DashboardDataContract?> Create(DashboardDataContract dashboard)
    {
        MigrateInPlace(dashboard);
        var created = await _httpService.Post<DashboardDataContract>(Route, dashboard);
        if (created is not null)
        {
            MigrateInPlace(created);
            _dashboards.Add(created);
        }

        return created;
    }

    public async Task<DashboardDataContract?> Update(Guid id, DashboardDataContract dashboard, bool forceOverwrite = false)
    {
        MigrateInPlace(dashboard);
        var uri = $"{Route}/{id}?forceOverwrite={forceOverwrite.ToString().ToLowerInvariant()}";
        // Suppress toast on conflict so the editor can offer reload vs force-overwrite.
        var updated = await _httpService.Put<DashboardDataContract>(uri, dashboard, showNotifications: false);
        if (updated is not null)
        {
            MigrateInPlace(updated);
            var index = _dashboards.FindIndex(d => d.Id == id);
            if (index >= 0)
                _dashboards[index] = updated;
            return updated;
        }

        if (!forceOverwrite)
        {
            var current = await Get(id);
            if (current is not null &&
                TruncateToUtcTicks(current.LastModifiedAt) != TruncateToUtcTicks(dashboard.LastModifiedAt))
            {
                throw new DashboardConcurrencyConflictException(current);
            }
        }

        return null;
    }

    public async Task Delete(Guid id)
    {
        await _httpService.Delete($"{Route}/{id}");
        _dashboards.RemoveAll(d => d.Id == id);
    }

    private void MigrateInPlace(DashboardDataContract? dashboard)
    {
        if (dashboard?.Definition is null)
            return;

        dashboard.Definition = _schemaMigrator.Migrate(dashboard.Definition);
    }

    private static DateTime TruncateToUtcTicks(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();
        return new DateTime(utc.Ticks - utc.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
    }
}
