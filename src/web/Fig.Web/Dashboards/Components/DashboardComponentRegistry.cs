using Fig.Web.Dashboards.Components.Badge;
using Fig.Web.Dashboards.Components.Cards;
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
        fig.runSessions: array of { name, instance, runSessionId, applicationVersion, figVersion, hostname, ipAddress, lastSeen, startTimeUtc, runningUser, memoryUsageBytes, health.{status,components[]}, customProperties, uptimePercent24Hr, uptimeHuman }
        Both clients and runSessions are DashboardJsArray with: length, filter, map, groupBy (-> { key, items }), sort, take, distinct, count, sum, average, min, max, first, last, toArray.
        helpers: functional helpers over arrays (same fluent style).
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
            ExpectedScriptShape =
                "object { value | numerator+denominator, label?, subtitle?, trend?, variant?, icon? } or a primitive value",
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
                },
                new DashboardComponentPreset
                {
                    Id = "replica-count-status",
                    DisplayName = "Replica count status",
                    ComponentType = "kpi",
                    Script = """
                        const clientName = 'AspNetApi';
                        const expected = 3;
                        const warningAt = 2;
                        const sessions = fig.runSessions.filter(s => s.name === clientName);
                        const running = sessions.length;
                        const variant = running >= expected ? 'success' : running >= warningAt ? 'warning' : 'danger';
                        const icon = running >= expected ? 'check' : running >= warningAt ? 'warning' : 'error';
                        return {
                          numerator: running,
                          denominator: expected,
                          label: clientName + ' replicas',
                          subtitle: running + ' of ' + expected + ' running',
                          variant: variant,
                          icon: icon
                        };
                        """,
                    DefaultConfig = new JObject { ["title"] = "Replica status" }
                }
            ]
        });

        Register(new DashboardComponentDescriptor
        {
            Type = "text",
            DisplayName = "Text",
            Category = "Content",
            Icon = "notes",
            ExpectedScriptShape = "{ lines: [{ text, size?, color?, align?, weight? }] } (or string / legacy { text, variant })",
            BlazorComponentType = typeof(DashboardText),
            InputContractType = typeof(DashboardTextInput),
            Presets =
            [
                new DashboardComponentPreset
                {
                    Id = "text-client-count",
                    DisplayName = "Client count summary",
                    ComponentType = "text",
                    Script =
                        """
                        return {
                          lines: [
                            { text: fig.clients.length + ' clients registered', size: 'md', align: 'left' }
                          ]
                        };
                        """,
                    DefaultConfig = new JObject { ["title"] = "Summary" }
                },
                new DashboardComponentPreset
                {
                    Id = "text-heading-sessions",
                    DisplayName = "Run sessions heading",
                    ComponentType = "text",
                    Script =
                        """
                        return {
                          lines: [
                            { text: fig.runSessions.length + ' run sessions', size: 'xl', weight: 'bold', align: 'left' }
                          ]
                        };
                        """,
                    DefaultConfig = new JObject { ["title"] = "Heading" }
                },
                new DashboardComponentPreset
                {
                    Id = "text-uptime-24h",
                    DisplayName = "Average uptime (24h)",
                    ComponentType = "text",
                    Script =
                        """
                        const seen = {};
                        let sum = 0;
                        let count = 0;
                        for (let i = 0; i < fig.runSessions.length; i++) {
                          const s = fig.runSessions[i];
                          const key = s.name + '|' + (s.instance || '');
                          if (seen[key]) continue;
                          seen[key] = true;
                          sum += (s.uptimePercent24Hr == null ? 0 : s.uptimePercent24Hr);
                          count++;
                        }
                        const pct = count === 0 ? 0 : sum / count;
                        const color = pct >= 99 ? '#8fd18f' : (pct >= 95 ? '#f5c57a' : '#e89996');
                        return {
                          lines: [
                            { text: pct.toFixed(1) + '%', size: 'xxl', color: color, align: 'center', weight: 'bold' },
                            { text: 'Average client uptime (24h)', size: 'sm', color: '#9aa0a6', align: 'center' }
                          ]
                        };
                        """,
                    DefaultConfig = new JObject { ["title"] = "Uptime" }
                }
            ]
        });

        Register(new DashboardComponentDescriptor
        {
            Type = "badge",
            DisplayName = "Badge",
            Category = "Content",
            Icon = "verified",
            ExpectedScriptShape = "string or { text, variant? } where variant is info|success|warning|danger|muted",
            BlazorComponentType = typeof(DashboardBadge),
            InputContractType = typeof(DashboardBadgeInput),
            Presets =
            [
                new DashboardComponentPreset
                {
                    Id = "badge-fleet-health",
                    DisplayName = "Fleet health badge",
                    ComponentType = "badge",
                    Script = """
                        const unhealthy = fig.runSessions.filter(s => s.health.status !== 'Healthy').length;
                        return {
                          text: unhealthy === 0 ? 'All healthy' : unhealthy + ' unhealthy',
                          variant: unhealthy === 0 ? 'success' : 'warning'
                        };
                        """,
                    DefaultConfig = new JObject { ["title"] = "Health" }
                },
                new DashboardComponentPreset
                {
                    Id = "badge-session-count",
                    DisplayName = "Session count badge",
                    ComponentType = "badge",
                    Script =
                        "return { text: fig.runSessions.length + ' sessions', variant: 'info' };",
                    DefaultConfig = new JObject { ["title"] = "Sessions" }
                }
            ]
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
            InputContractType = typeof(DashboardChartPoint),
            Presets =
            [
                new DashboardComponentPreset
                {
                    Id = "donut-by-application-version",
                    DisplayName = "Share by application version",
                    ComponentType = "donut",
                    Script =
                        "return fig.runSessions.groupBy(s => s.applicationVersion).map(g => ({ label: g.key, value: g.items.length }));",
                    DefaultConfig = new JObject { ["title"] = "App versions" }
                },
                new DashboardComponentPreset
                {
                    Id = "donut-by-health",
                    DisplayName = "Share by health status",
                    ComponentType = "donut",
                    Script =
                        "return fig.runSessions.groupBy(s => s.health.status).map(g => ({ label: g.key, value: g.items.length }));",
                    DefaultConfig = new JObject { ["title"] = "Health mix" }
                }
            ]
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
            InputContractType = typeof(DashboardListInput),
            Presets =
            [
                new DashboardComponentPreset
                {
                    Id = "list-client-names",
                    DisplayName = "Client names",
                    ComponentType = "list",
                    Script = "return fig.clients.map(c => c.name);",
                    DefaultConfig = new JObject { ["title"] = "Clients" }
                },
                new DashboardComponentPreset
                {
                    Id = "list-sessions-with-host",
                    DisplayName = "Sessions with hostname",
                    ComponentType = "list",
                    Script =
                        "return fig.runSessions.map(s => ({ text: s.name, secondary: s.hostname || s.instance || '' }));",
                    DefaultConfig = new JObject { ["title"] = "Run sessions" }
                }
            ]
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
            Presets =
            [
                new DashboardComponentPreset
                {
                    Id = "clients-health",
                    DisplayName = "Clients + health status",
                    ComponentType = "keyValue",
                    Script = """
                        const unhealthy = fig.runSessions.filter(s => s.health.status !== 'Healthy').length;
                        return {
                          statusIcon: unhealthy > 0 ? 'warning' : 'check',
                          statusColor: unhealthy > 0 ? '#f5c57a' : '#8fd18f',
                          items: [
                            { key: 'clients', value: fig.clients.length },
                            { key: 'runSessions', value: fig.runSessions.length },
                            { key: 'unhealthy', value: unhealthy }
                          ]
                        };
                        """
                },
                new DashboardComponentPreset
                {
                    Id = "master-and-last-sync",
                    DisplayName = "Master + last sync",
                    ComponentType = "keyValue",
                    Script = """
                        const clientName = 'AspNetApi';
                        const sessions = fig.runSessions.filter(s => s.name === clientName);
                        const master = sessions.first(s => s.customProperties && s.customProperties.isMaster === true);
                        const syncTimes = sessions
                          .map(s => s.customProperties && s.customProperties.lastSyncTime)
                          .filter(t => t != null && t !== '');
                        const latestSync = syncTimes.length === 0 ? null : syncTimes.sort().last();
                        return {
                          statusIcon: master ? 'check' : 'help',
                          statusColor: master ? '#8fd18f' : '#6c757d',
                          items: [
                            { key: 'Client', value: clientName },
                            { key: 'Running', value: sessions.length },
                            { key: 'Master', value: master ? (master.hostname || master.instance || 'yes') : '—' },
                            { key: 'Last sync', value: latestSync || '—' }
                          ]
                        };
                        """
                }
            ]
        });

        Register(new DashboardComponentDescriptor
        {
            Type = "cards",
            DisplayName = "Cards",
            Category = "Data",
            Icon = "dashboard",
            ExpectedScriptShape =
                "array of { title?, value, variant?, icon?, rows?: [{ key, value }] }",
            BlazorComponentType = typeof(DashboardCards),
            InputContractType = typeof(DashboardCardsInput),
            Presets =
            [
                new DashboardComponentPreset
                {
                    Id = "all-clients-overview",
                    DisplayName = "All clients overview",
                    ComponentType = "cards",
                    Script =
                        """
                        return fig.clients.groupBy(c => c.name).map(g => {
                          const instances = g.items;
                          const expected = instances.length;
                          const sessions = fig.runSessions.filter(s => s.name === g.key);
                          const matched = instances.map(inst =>
                            sessions.first(s => (s.instance || '') === (inst.instance || ''))
                          ).filter(s => s != null);
                          const running = matched.length;
                          const appVersions = matched.map(s => s.applicationVersion).filter(v => !!v).distinct();
                          const figVersions = matched.map(s => s.figVersion).filter(v => !!v).distinct();
                          const longest = matched.sort(s => s.startTimeUtc).first();
                          let uptimeSum = 0;
                          for (let i = 0; i < matched.length; i++) {
                            const pct = matched[i].uptimePercent24Hr;
                            uptimeSum += (pct == null ? 0 : pct);
                          }
                          const variant = running >= expected ? 'success' : running > 0 ? 'warning' : 'danger';
                          return {
                            title: g.key,
                            value: running + '/' + expected,
                            variant: variant,
                            icon: running >= expected ? 'check' : running > 0 ? 'warning' : 'error',
                            rows: [
                              { key: 'App version', value: appVersions.length === 0 ? '—' : appVersions.length === 1 ? appVersions[0] : 'Multiple' },
                              { key: 'Runtime', value: longest && longest.uptimeHuman ? longest.uptimeHuman : '—' },
                              { key: 'Fig version', value: figVersions.length === 0 ? '—' : figVersions.length === 1 ? figVersions[0] : 'Multiple' },
                              { key: 'Uptime %', value: matched.length === 0 ? '—' : (uptimeSum / matched.length).toFixed(1) + '%' }
                            ]
                          };
                        });
                        """,
                    DefaultConfig = new JObject { ["title"] = "Clients" }
                },
                new DashboardComponentPreset
                {
                    Id = "all-clients-uptime",
                    DisplayName = "All clients uptime",
                    ComponentType = "cards",
                    Script =
                        """
                        return fig.clients.groupBy(c => c.name).map(g => {
                          const instances = g.items;
                          const expected = instances.length;
                          const sessions = fig.runSessions.filter(s => s.name === g.key);
                          const matched = instances.map(inst =>
                            sessions.first(s => (s.instance || '') === (inst.instance || ''))
                          ).filter(s => s != null);
                          const running = matched.length;
                          const appVersions = matched.map(s => s.applicationVersion).filter(v => !!v).distinct();
                          const figVersions = matched.map(s => s.figVersion).filter(v => !!v).distinct();
                          const longest = matched.sort(s => s.startTimeUtc).first();
                          let uptimeSum = 0;
                          for (let i = 0; i < instances.length; i++) {
                            const inst = instances[i];
                            const session = sessions.first(s => (s.instance || '') === (inst.instance || ''));
                            const pct = session && session.uptimePercent24Hr != null ? session.uptimePercent24Hr : 0;
                            uptimeSum += pct;
                          }
                          const avg = instances.length === 0 ? 0 : uptimeSum / instances.length;
                          const variant = avg >= 100 ? 'success' : avg >= 90 ? 'warning' : 'danger';
                          const icon = avg >= 100 ? 'check' : avg >= 90 ? 'warning' : 'error';
                          return {
                            title: g.key,
                            value: avg.toFixed(1) + '%',
                            variant: variant,
                            icon: icon,
                            rows: [
                              { key: 'App version', value: appVersions.length === 0 ? '—' : appVersions.length === 1 ? appVersions[0] : 'Multiple' },
                              { key: 'Runtime', value: longest && longest.uptimeHuman ? longest.uptimeHuman : '—' },
                              { key: 'Fig version', value: figVersions.length === 0 ? '—' : figVersions.length === 1 ? figVersions[0] : 'Multiple' },
                              { key: 'Running', value: running + '/' + expected }
                            ]
                          };
                        });
                        """,
                    DefaultConfig = new JObject { ["title"] = "Uptime" }
                }
            ]
        });
    }

    private void Register(DashboardComponentDescriptor descriptor)
    {
        _byType[descriptor.Type] = descriptor;
        foreach (var preset in descriptor.Presets)
            _presets[preset.Id] = preset;
    }
}
