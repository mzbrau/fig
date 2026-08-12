using Fig.Client.Abstractions.Data;
using Fig.Web.Models.Clients;
using Fig.Web.Models.Setting;

namespace Fig.Web.Dashboards.Runtime;

/// <summary>
/// Root object injected into dashboard Jint scripts as <c>fig</c>.
/// </summary>
public class DashboardFigRoot
{
    public DashboardJsArray clients { get; set; } = DashboardJsArray.Empty;

    public DashboardJsArray runSessions { get; set; } = DashboardJsArray.Empty;
}

public class DashboardRunSessionJsModel
{
    public string name { get; set; } = string.Empty;

    public string? instance { get; set; }

    public string runSessionId { get; set; } = string.Empty;

    public string? applicationVersion { get; set; }

    public string? figVersion { get; set; }

    public string? hostname { get; set; }

    public string? ipAddress { get; set; }

    /// <summary>ISO-8601 UTC timestamp, or null when unknown.</summary>
    public string? lastSeen { get; set; }

    /// <summary>ISO-8601 UTC timestamp.</summary>
    public string startTimeUtc { get; set; } = string.Empty;

    public string runningUser { get; set; } = string.Empty;

    public long memoryUsageBytes { get; set; }

    public DashboardHealthJsModel health { get; set; } = new();

    public Dictionary<string, object?> customProperties { get; set; } = new();
}

public class DashboardHealthJsModel
{
    public string status { get; set; } = string.Empty;

    public List<DashboardComponentHealthJsModel> components { get; set; } = new();
}

public class DashboardComponentHealthJsModel
{
    public string name { get; set; } = string.Empty;

    public string status { get; set; } = string.Empty;

    public string? message { get; set; }
}

public class DashboardClientJsModel
{
    public string name { get; set; } = string.Empty;

    public string? instance { get; set; }

    public string description { get; set; } = string.Empty;

    /// <summary>Setting name → value (secrets excluded; classification-filtered).</summary>
    public Dictionary<string, object?> settings { get; set; } = new();
}

public static class DashboardFigApiMapper
{
    public static DashboardRunSessionJsModel ToJsModel(ClientRunSessionModel session)
    {
        return new DashboardRunSessionJsModel
        {
            name = session.Name,
            instance = session.Instance,
            runSessionId = session.RunSessionId.ToString(),
            applicationVersion = session.ApplicationVersion,
            figVersion = session.FigVersion,
            hostname = session.Hostname,
            ipAddress = session.IpAddress,
            lastSeen = ToIsoUtc(session.LastSeen),
            startTimeUtc = ToIsoUtc(session.StartTimeUtc) ?? string.Empty,
            runningUser = session.RunningUser,
            memoryUsageBytes = session.MemoryUsageBytes,
            health = new DashboardHealthJsModel
            {
                status = session.Health.Status.ToString(),
                components = session.Health.Components
                    .Select(c => new DashboardComponentHealthJsModel
                    {
                        name = c.Name,
                        status = c.Status.ToString(),
                        message = c.Message
                    })
                    .ToList()
            },
            customProperties = session.CustomProperties.ToDictionary(
                p => p.Name,
                p => p.Value,
                StringComparer.OrdinalIgnoreCase)
        };
    }

    public static DashboardClientJsModel ToJsModel(
        SettingClientConfigurationModel client,
        IReadOnlyCollection<Classification>? allowedClassifications)
    {
        var settings = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var setting in client.Settings)
        {
            if (setting.IsSecret)
                continue;

            if (allowedClassifications is not null &&
                allowedClassifications.Count > 0 &&
                !allowedClassifications.Contains(setting.Classification))
                continue;

            settings[setting.Name] = setting.GetValue(true);
        }

        return new DashboardClientJsModel
        {
            name = client.Name,
            instance = client.Instance,
            description = client.Description,
            settings = settings
        };
    }

    public static DashboardFigRoot CreateRoot(
        IEnumerable<SettingClientConfigurationModel> clients,
        IEnumerable<ClientRunSessionModel> runSessions,
        IReadOnlyCollection<Classification>? allowedClassifications)
    {
        var clientModels = clients
            .Where(c => !c.IsGroup)
            .Select(c => ToJsModel(c, allowedClassifications))
            .Cast<object?>()
            .ToList();

        var sessionModels = runSessions
            .Select(ToJsModel)
            .Cast<object?>()
            .ToList();

        return new DashboardFigRoot
        {
            clients = new DashboardJsArray(clientModels),
            runSessions = new DashboardJsArray(sessionModels)
        };
    }

    private static string? ToIsoUtc(DateTime? value)
    {
        if (value is null)
            return null;

        var utc = value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
        };

        return utc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
    }
}
