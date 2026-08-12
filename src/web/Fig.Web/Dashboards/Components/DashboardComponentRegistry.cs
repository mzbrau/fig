using Fig.Web.Dashboards.Components.Badge;
using Fig.Web.Dashboards.Components.Chart;
using Fig.Web.Dashboards.Components.Contracts;
using Fig.Web.Dashboards.Components.KeyValue;
using Fig.Web.Dashboards.Components.Kpi;
using Fig.Web.Dashboards.Components.List;
using Fig.Web.Dashboards.Components.Table;
using Fig.Web.Dashboards.Components.Text;
using Newtonsoft.Json.Linq;

namespace Fig.Web.Dashboards.Components;

public sealed class DashboardComponentPreset
{
    public string Id { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string ComponentType { get; init; } = string.Empty;

    public string Script { get; init; } = string.Empty;

    public JObject DefaultConfig { get; init; } = new();
}

public sealed class DashboardComponentDescriptor
{
    public string Type { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string Icon { get; init; } = "widgets";

    /// <summary>
    /// Short description of the expected inline-script return shape for Fig Assistant.
    /// </summary>
    public string ExpectedScriptShape { get; init; } = string.Empty;

    public Type BlazorComponentType { get; init; } = typeof(object);

    public Type InputContractType { get; init; } = typeof(object);

    public IReadOnlyList<DashboardComponentPreset> Presets { get; init; } =
        Array.Empty<DashboardComponentPreset>();
}

public class DashboardComponentRegistry
{
    private readonly Dictionary<string, DashboardComponentDescriptor> _byType =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, DashboardComponentPreset> _presets =
        new(StringComparer.OrdinalIgnoreCase);

    public DashboardComponentRegistry()
    {
        RegisterDefaults();
    }

    public IReadOnlyCollection<DashboardComponentDescriptor> All => _byType.Values;

    public DashboardComponentDescriptor? Get(string type) =>
        _byType.TryGetValue(type, out var descriptor) ? descriptor : null;

    public DashboardComponentPreset? GetPreset(string presetId) =>
        _presets.TryGetValue(presetId, out var preset) ? preset : null;

    public IEnumerable<DashboardComponentPreset> PresetsFor(string componentType) =>
        _presets.Values.Where(p => string.Equals(p.ComponentType, componentType, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Shared summary of the JavaScript model available to dashboard scripts (for Fig Assistant).
    /// </summary>
    public static string JsModelSummary { get; } =
        """
        fig.clients: array of { name, instance, description, settings (non-secret dict) }
        fig.runSessions: array of { name, instance, runSessionId, applicationVersion, figVersion, hostname, ipAddress, lastSeen, startTimeUtc, runningUser, memoryUsageBytes, health.{status,components[]}, customProperties }
        Both clients and runSessions are DashboardJsArray with: length, filter, map, groupBy (-> { key, items }), sort, take, distinct, count, sum, average, min, max, first, last, toArray.
        helpers: functional helpers over arrays (same fluent style).
        transforms: dictionary of named transform results; named transform ids are also bound as top-level identifiers.
        Scripts may use return { ... } or an expression. Prefer fig.runSessions / fig.clients fluent API; Object.keys and Array.isArray do not work on CLR-backed objects.
        """;

    private void RegisterDefaults()
    {
        Register(new DashboardComponentDescriptor
        {
            Type = "kpi",
            DisplayName = "KPI",
            Category = "Data",
            Icon = "trending_up",
            ExpectedScriptShape = "object { value, label?, trend?, variant? } or a primitive value",
            BlazorComponentType = typeof(DashboardKpi),
            InputContractType = typeof(DashboardKpiInput),
            Presets =
            [
                new DashboardComponentPreset
                {
                    Id = "count-run-sessions",
                    DisplayName = "Count run sessions",
                    ComponentType = "kpi",
                    Script = "return { value: fig.runSessions.length, label: 'Connected run sessions' };",
                    DefaultConfig = new JObject { ["title"] = "Connected run sessions" }
                }
            ]
        });

        Register(new DashboardComponentDescriptor
        {
            Type = "text",
            DisplayName = "Text",
            Category = "Content",
            Icon = "notes",
            ExpectedScriptShape = "string or { text, variant? } where variant is heading|body|muted",
            BlazorComponentType = typeof(DashboardText),
            InputContractType = typeof(DashboardTextInput)
        });

        Register(new DashboardComponentDescriptor
        {
            Type = "badge",
            DisplayName = "Badge",
            Category = "Content",
            Icon = "verified",
            ExpectedScriptShape = "string or { text, variant? } where variant is info|success|warning|danger|muted",
            BlazorComponentType = typeof(DashboardBadge),
            InputContractType = typeof(DashboardBadgeInput)
        });

        Register(new DashboardComponentDescriptor
        {
            Type = "bar",
            DisplayName = "Bar chart",
            Category = "Charts",
            Icon = "bar_chart",
            ExpectedScriptShape = "array of { label, value }",
            BlazorComponentType = typeof(DashboardBarChart),
            InputContractType = typeof(DashboardChartPoint),
            Presets =
            [
                new DashboardComponentPreset
                {
                    Id = "count-by-application-version",
                    DisplayName = "Count by application version",
                    ComponentType = "bar",
                    Script =
                        "return fig.runSessions.groupBy(s => s.applicationVersion).map(g => ({ label: g.key, value: g.items.length }));",
                    DefaultConfig = new JObject { ["title"] = "Run sessions by application version" }
                },
                new DashboardComponentPreset
                {
                    Id = "count-by-name",
                    DisplayName = "Count by client name",
                    ComponentType = "bar",
                    Script =
                        "return fig.runSessions.groupBy(s => s.name).map(g => ({ label: g.key, value: g.items.length }));",
                    DefaultConfig = new JObject { ["title"] = "Run sessions by client name" }
                }
            ]
        });

        Register(new DashboardComponentDescriptor
        {
            Type = "donut",
            DisplayName = "Donut chart",
            Category = "Charts",
            Icon = "donut_large",
            ExpectedScriptShape = "array of { label, value }",
            BlazorComponentType = typeof(DashboardDonutChart),
            InputContractType = typeof(DashboardChartPoint)
        });

        Register(new DashboardComponentDescriptor
        {
            Type = "table",
            DisplayName = "Table",
            Category = "Data",
            Icon = "table_chart",
            ExpectedScriptShape = "array of row objects; columns come from Config.columns or inferred keys",
            BlazorComponentType = typeof(DashboardTable),
            InputContractType = typeof(DashboardTableInput),
            Presets =
            [
                new DashboardComponentPreset
                {
                    Id = "table-run-sessions",
                    DisplayName = "Table of run sessions",
                    ComponentType = "table",
                    Script =
                        """
                        return fig.runSessions.map(s => ({
                            name: s.name,
                            instance: s.instance,
                            applicationVersion: s.applicationVersion,
                            hostname: s.hostname,
                            lastSeen: s.lastSeen,
                            health: s.health && s.health.status
                        }));
                        """,
                    DefaultConfig = JObject.FromObject(new
                    {
                        columns = new[]
                        {
                            new { property = "name", header = "Name" },
                            new { property = "instance", header = "Instance" },
                            new { property = "applicationVersion", header = "Version" },
                            new { property = "hostname", header = "Hostname" },
                            new { property = "lastSeen", header = "Last seen" },
                            new { property = "health", header = "Health" }
                        }
                    })
                }
            ]
        });

        Register(new DashboardComponentDescriptor
        {
            Type = "list",
            DisplayName = "List",
            Category = "Data",
            Icon = "format_list_bulleted",
            ExpectedScriptShape = "array of strings or { text|name, secondary?, variant? }",
            BlazorComponentType = typeof(DashboardList),
            InputContractType = typeof(DashboardListInput)
        });

        Register(new DashboardComponentDescriptor
        {
            Type = "keyValue",
            DisplayName = "Key / value",
            Category = "Data",
            Icon = "terminal",
            ExpectedScriptShape = "[{ key, value }] or { statusIcon?, statusColor?, items: [...] } or a single object (properties become pairs; statusIcon/statusColor reserved)",
            BlazorComponentType = typeof(DashboardKeyValue),
            InputContractType = typeof(DashboardKeyValueInput),
            Presets = new[]
            {
                new DashboardComponentPreset
                {
                    Id = "clients-health",
                    DisplayName = "Clients + health status",
                    ComponentType = "keyValue",
                    Script = """
                        const unhealthy = fig.runSessions.filter(s => s.health.status !== 'Healthy').length;
                        return {
                          statusIcon: unhealthy > 0 ? 'warning' : 'check',
                          statusColor: unhealthy > 0 ? '#f0ad4e' : '#22c55e',
                          items: [
                            { key: 'clients', value: fig.clients.length },
                            { key: 'runSessions', value: fig.runSessions.length },
                            { key: 'unhealthy', value: unhealthy }
                          ]
                        };
                        """
                }
            }
        });
    }

    private void Register(DashboardComponentDescriptor descriptor)
    {
        _byType[descriptor.Type] = descriptor;
        foreach (var preset in descriptor.Presets)
            _presets[preset.Id] = preset;
    }
}
