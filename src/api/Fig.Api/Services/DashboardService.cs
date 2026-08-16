using Fig.Api.Datalayer.Repositories;
using Fig.Api.Exceptions;
using Fig.Contracts.Authentication;
using Fig.Contracts.Dashboards;
using Fig.Datalayer.BusinessEntities;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Fig.Api.Services;

public class DashboardService : AuthenticatedService, IDashboardService
{
    public const int MinSettingsRefreshSeconds = 600;
    public const int MinStatusRefreshSeconds = 60;

    private static readonly JsonSerializerSettings DefinitionJsonSettings = new()
    {
        TypeNameHandling = TypeNameHandling.None,
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    private readonly IDashboardRepository _dashboardRepository;
    private readonly IConfigurationRepository _configurationRepository;

    public DashboardService(
        IDashboardRepository dashboardRepository,
        IConfigurationRepository configurationRepository)
    {
        _dashboardRepository = dashboardRepository;
        _configurationRepository = configurationRepository;
    }

    public async Task<IEnumerable<DashboardDataContract>> GetAll()
    {
        var user = RequireAuthenticatedUser();
        EnsureCanViewDashboards(user);
        await EnsureJavaScriptEnabled();

        var entities = await _dashboardRepository.GetAllDashboards();
        return entities
            .Select(ConvertToDataContract)
            .Where(d => CanViewDashboard(user, d))
            .ToList();
    }

    public async Task<DashboardDataContract> Get(Guid id)
    {
        var user = RequireAuthenticatedUser();
        EnsureCanViewDashboards(user);
        await EnsureJavaScriptEnabled();

        var entity = await _dashboardRepository.GetDashboard(id)
            ?? throw new KeyNotFoundException($"No dashboard found with id {id}");

        var dashboard = ConvertToDataContract(entity);
        if (!CanViewDashboard(user, dashboard))
            throw new KeyNotFoundException($"No dashboard found with id {id}");

        return dashboard;
    }

    public async Task<DashboardDataContract> Create(DashboardDataContract dashboard)
    {
        RequireAuthenticatedUser();
        await EnsureJavaScriptEnabled();
        await ValidateName(dashboard.Name);
        NormalizeDefinition(dashboard.Definition);

        var now = DateTime.UtcNow;
        var entity = new DashboardBusinessEntity
        {
            Name = dashboard.Name.Trim(),
            Description = dashboard.Description,
            AdminOnly = dashboard.AdminOnly,
            DefinitionJson = SerializeDefinition(dashboard.Definition),
            CreatedAt = now,
            LastModifiedAt = now,
            LastModifiedBy = AuthenticatedUser?.Username
        };

        var id = await _dashboardRepository.AddDashboard(entity);
        dashboard.Id = id;
        dashboard.CreatedAt = entity.CreatedAt;
        dashboard.LastModifiedAt = entity.LastModifiedAt;
        dashboard.LastModifiedBy = entity.LastModifiedBy;
        return dashboard;
    }

    public async Task<DashboardDataContract> Update(Guid id, DashboardDataContract dashboard, bool forceOverwrite = false)
    {
        RequireAuthenticatedUser();
        await EnsureJavaScriptEnabled();

        var entity = await _dashboardRepository.GetDashboard(id, forUpdate: true)
            ?? throw new KeyNotFoundException($"No dashboard found with id {id}");

        if (!forceOverwrite &&
            dashboard.LastModifiedAt != default &&
            TruncateToUtcTicks(entity.LastModifiedAt) != TruncateToUtcTicks(dashboard.LastModifiedAt))
        {
            throw new DashboardConcurrencyException(ConvertToDataContract(entity));
        }

        if (!string.Equals(entity.Name, dashboard.Name, StringComparison.Ordinal))
            await ValidateName(dashboard.Name, excludeId: id);

        NormalizeDefinition(dashboard.Definition);

        entity.Name = dashboard.Name.Trim();
        entity.Description = dashboard.Description;
        entity.AdminOnly = dashboard.AdminOnly;
        entity.DefinitionJson = SerializeDefinition(dashboard.Definition);
        entity.LastModifiedAt = DateTime.UtcNow;
        entity.LastModifiedBy = AuthenticatedUser?.Username;

        await _dashboardRepository.UpdateDashboard(entity);
        return ConvertToDataContract(entity);
    }

    public async Task Delete(Guid id)
    {
        RequireAuthenticatedUser();
        await EnsureJavaScriptEnabled();
        var entity = await _dashboardRepository.GetDashboard(id, forUpdate: true)
            ?? throw new KeyNotFoundException($"No dashboard found with id {id}");
        await _dashboardRepository.DeleteDashboard(entity);
    }

    private async Task EnsureJavaScriptEnabled()
    {
        var configuration = await _configurationRepository.GetConfiguration();
        if (!configuration.AllowDisplayScripts)
        {
            throw new UnauthorizedAccessException(
                "Dashboards are disabled because JavaScript execution is turned off. Enable Allow JavaScript in Fig configuration.");
        }
    }

    private async Task ValidateName(string name, Guid? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Dashboard name is required.");

        var trimmed = name.Trim();
        var existing = await _dashboardRepository.GetDashboardByName(trimmed);
        if (existing is not null && existing.Id != excludeId)
            throw new InvalidOperationException($"A dashboard named '{trimmed}' already exists.");
    }

    private static void NormalizeDefinition(DashboardDefinitionDataContract definition)
    {
        definition.SchemaVersion = definition.SchemaVersion <= 0 ? 1 : definition.SchemaVersion;
        definition.Refresh ??= new DashboardRefreshDataContract();
        definition.Refresh.SettingsSeconds = Math.Max(MinSettingsRefreshSeconds, definition.Refresh.SettingsSeconds);
        definition.Refresh.StatusSeconds = Math.Max(MinStatusRefreshSeconds, definition.Refresh.StatusSeconds);
        definition.Components ??= new List<DashboardComponentInstanceDataContract>();
        definition.Layout ??= new List<DashboardLayoutCellDataContract>();
    }

    private static void EnsureCanViewDashboards(UserDataContract user)
    {
        if (user.Role == Role.LookupService)
            throw new UnauthorizedAccessException("LookupService users cannot access dashboards.");
    }

    private static bool CanViewDashboard(UserDataContract user, DashboardDataContract dashboard)
    {
        if (dashboard.AdminOnly)
            return user.Role == Role.Administrator;

        return user.Role is Role.Administrator or Role.User or Role.ReadOnly or Role.Dashboard;
    }

    private static DashboardDataContract ConvertToDataContract(DashboardBusinessEntity entity)
    {
        return new DashboardDataContract
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            AdminOnly = entity.AdminOnly,
            CreatedAt = entity.CreatedAt,
            LastModifiedAt = entity.LastModifiedAt,
            LastModifiedBy = entity.LastModifiedBy,
            Definition = DeserializeDefinition(entity.DefinitionJson)
        };
    }

    private static DashboardDefinitionDataContract DeserializeDefinition(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new DashboardDefinitionDataContract();

        return JsonConvert.DeserializeObject<DashboardDefinitionDataContract>(json, DefinitionJsonSettings)
               ?? new DashboardDefinitionDataContract();
    }

    private static string SerializeDefinition(DashboardDefinitionDataContract definition)
        => JsonConvert.SerializeObject(definition, DefinitionJsonSettings);

    /// <summary>
    /// NHibernate UtcTicks stores with tick precision; normalize comparisons for round-tripped ISO values.
    /// </summary>
    private static DateTime TruncateToUtcTicks(DateTime value)
        => new(value.Ticks, DateTimeKind.Utc);
}
