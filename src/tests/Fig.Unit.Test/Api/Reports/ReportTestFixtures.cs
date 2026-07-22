using Fig.Api.Reports;
using Fig.Api.Services;
using Fig.Client.Abstractions.Data;
using Fig.Contracts.Authentication;
using Fig.Contracts.Health;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.BusinessEntities.SettingValues;

namespace Fig.Unit.Test.Api.Reports;

public static class ReportTestFixtures
{
    public static UserDataContract CreateAdminUser(string clientFilter = ".*", string username = "admin")
        => new(
            Guid.NewGuid(),
            username,
            "Admin",
            "User",
            Role.Administrator,
            clientFilter,
            Enum.GetValues<Classification>().ToList());

    public static UserDataContract CreateFilteredUser(string clientFilter, string username = "filtered")
        => CreateAdminUser(clientFilter, username);

    public static void Authenticate(AuthenticatedService service, UserDataContract? user = null)
        => service.SetAuthenticatedUser(user ?? CreateAdminUser());

    public static SettingClientBusinessEntity CreateClient(
        string name,
        string? instance = null,
        params SettingBusinessEntity[] settings)
        => new()
        {
            Name = name,
            Instance = instance,
            Description = $"{name} description",
            LastRegistration = DateTime.UtcNow.AddDays(-10),
            Settings = settings.ToList()
        };

    public static SettingBusinessEntity CreateSetting(
        string name,
        string? value = "value",
        bool isSecret = false,
        DateTime? lastChanged = null,
        bool supportsLiveUpdate = true,
        bool isExternallyManaged = false,
        string? category = "General",
        bool? environmentSpecific = false,
        string? lookupTableKey = null)
        => new()
        {
            Name = name,
            Description = $"{name} description",
            IsSecret = isSecret,
            ValueType = typeof(string),
            Value = value is null ? null : new StringSettingBusinessEntity(value),
            LastChanged = lastChanged ?? DateTime.UtcNow.AddDays(-5),
            SupportsLiveUpdate = supportsLiveUpdate,
            IsExternallyManaged = isExternallyManaged,
            CategoryName = category,
            EnvironmentSpecific = environmentSpecific,
            LookupTableKey = lookupTableKey,
            Classification = Classification.Technical
        };

    public static EventLogBusinessEntity CreateEvent(
        string eventType,
        DateTime? timestamp = null,
        string? clientName = null,
        string? instance = null,
        string? settingName = null,
        string? user = "admin",
        string? message = null,
        string? originalValue = null,
        string? newValue = null)
        => new()
        {
            EventType = eventType,
            Timestamp = timestamp ?? DateTime.UtcNow,
            ClientName = clientName,
            Instance = instance,
            SettingName = settingName,
            AuthenticatedUser = user,
            Message = message,
            OriginalValue = originalValue,
            NewValue = newValue
        };

    public static ClientStatusBusinessEntity CreateStatus(
        string name,
        string? instance = null,
        params ClientRunSessionBusinessEntity[] sessions)
        => new()
        {
            Name = name,
            Instance = instance,
            LastRegistration = DateTime.UtcNow.AddDays(-10),
            RunSessions = sessions.ToList()
        };

    public static ClientRunSessionBusinessEntity CreateSession(
        string? hostname = "host-1",
        string applicationVersion = "1.0.0",
        string figVersion = "3.0.0",
        DateTime? startTimeUtc = null,
        DateTime? lastSeen = null,
        bool restartRequired = false,
        bool liveReload = true,
        FigHealthStatus health = FigHealthStatus.Healthy)
    {
        var start = startTimeUtc ?? DateTime.UtcNow.AddHours(-2);
        return new ClientRunSessionBusinessEntity
        {
            RunSessionId = Guid.NewGuid(),
            Hostname = hostname,
            ApplicationVersion = applicationVersion,
            FigVersion = figVersion,
            StartTimeUtc = start,
            LastSeen = lastSeen ?? DateTime.UtcNow,
            RestartRequiredToApplySettings = restartRequired,
            LiveReload = liveReload,
            HealthStatus = health,
            RunningUser = "app",
            PollIntervalMs = 30000
        };
    }

    public static (DateTime From, DateTime To) DefaultRange(int days = 7)
    {
        var to = DateTime.UtcNow;
        return (to.AddDays(-days), to);
    }
}
