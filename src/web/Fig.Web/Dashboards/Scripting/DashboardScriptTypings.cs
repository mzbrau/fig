using System.Text;
using Fig.Web.Dashboards.Runtime;

namespace Fig.Web.Dashboards.Scripting;

/// <summary>
/// Builds ambient TypeScript declarations for Monaco IntelliSense in dashboard inline scripts.
/// </summary>
public static class DashboardScriptTypings
{
    public const string AmbientLibPath = "ts:fig-dashboard-ambient.d.ts";
    public const string DynamicLibPath = "ts:fig-dashboard-dynamic.d.ts";
    public const string ExpectedLibPath = "ts:fig-dashboard-expected.d.ts";

    public static IReadOnlyList<DashboardScriptExtraLib> Build(
        string componentType,
        DashboardFigRoot? fig = null)
    {
        return
        [
            new DashboardScriptExtraLib(AmbientLibPath, BuildAmbient()),
            new DashboardScriptExtraLib(DynamicLibPath, BuildDynamic(fig)),
            new DashboardScriptExtraLib(ExpectedLibPath, BuildExpectedResult(componentType))
        ];
    }

    public static string BuildAmbient() =>
        """
        /** Fluent array wrapper used by fig.clients / fig.runSessions (not a native JS Array). */
        interface DashboardJsArray<T = any> {
            readonly length: number;
            [index: number]: T;
            filter(predicate: (item: T) => boolean): DashboardJsArray<T>;
            map<U>(selector: (item: T) => U): DashboardJsArray<U>;
            groupBy(keySelector: (item: T) => any): DashboardJsArray<DashboardJsGroup<T>>;
            sort(keySelector?: (item: T) => any): DashboardJsArray<T>;
            take(count: number): DashboardJsArray<T>;
            distinct(keySelector?: (item: T) => any): DashboardJsArray<T>;
            count(predicate?: (item: T) => boolean): number;
            sum(selector?: (item: T) => number): number;
            average(selector?: (item: T) => number): number;
            min(selector?: (item: T) => number): number;
            max(selector?: (item: T) => number): number;
            first(predicate?: (item: T) => boolean): T | undefined;
            last(predicate?: (item: T) => boolean): T | undefined;
            toArray(): T[];
        }

        interface DashboardJsGroup<T = any> {
            key: any;
            items: DashboardJsArray<T>;
        }

        interface DashboardComponentHealth {
            name: string;
            status: string;
            message?: string | null;
        }

        interface DashboardHealth {
            status: string;
            components: DashboardComponentHealth[];
        }

        interface DashboardRunSession {
            name: string;
            instance?: string | null;
            runSessionId: string;
            applicationVersion?: string | null;
            figVersion?: string | null;
            hostname?: string | null;
            ipAddress?: string | null;
            lastSeen?: string | null;
            startTimeUtc: string;
            runningUser: string;
            memoryUsageBytes: number;
            health: DashboardHealth;
            customProperties: Record<string, any>;
            /** Approximate client rolling 24h uptime percentage (0–100), or null/undefined before first observation. */
            uptimePercent24Hr?: number | null;
            /** Humanized process runtime since startTimeUtc (e.g. "3 hours"). */
            uptimeHuman: string;
        }

        interface DashboardClient {
            name: string;
            instance?: string | null;
            description: string;
            settings: Record<string, any>;
        }

        interface DashboardFigRoot {
            clients: DashboardJsArray<DashboardClient>;
            runSessions: DashboardJsArray<DashboardRunSession>;
        }

        interface DashboardJsLinq {
            from(source: any): DashboardJsArray;
            filter(source: any, predicate: (item: any) => boolean): DashboardJsArray;
            map(source: any, selector: (item: any) => any): DashboardJsArray;
            groupBy(source: any, keySelector: (item: any) => any): DashboardJsArray;
            sort(source: any, keySelector?: (item: any) => any): DashboardJsArray;
            take(source: any, count: number): DashboardJsArray;
            distinct(source: any, keySelector?: (item: any) => any): DashboardJsArray;
            count(source: any, predicate?: (item: any) => boolean): number;
            sum(source: any, selector?: (item: any) => number): number;
            average(source: any, selector?: (item: any) => number): number;
            min(source: any, selector?: (item: any) => number): number;
            max(source: any, selector?: (item: any) => number): number;
            first(source: any, predicate?: (item: any) => boolean): any;
            last(source: any, predicate?: (item: any) => boolean): any;
        }

        declare const fig: DashboardFigRoot;
        declare const helpers: DashboardJsLinq;
        declare const DashboardJsLinq: DashboardJsLinq;
        """;

    public static string BuildDynamic(DashboardFigRoot? fig)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Live keys from current fig data");

        var settingKeys = CollectDictionaryKeys(fig?.clients, "settings");
        var customKeys = CollectDictionaryKeys(fig?.runSessions, "customProperties");

        if (settingKeys.Count > 0)
        {
            sb.AppendLine(
                $"type LiveSettingKey = {string.Join(" | ", settingKeys.Select(Quote))};");
            sb.AppendLine(
                "interface DashboardClient { settings: Partial<Record<LiveSettingKey, any>> & Record<string, any>; }");
        }

        if (customKeys.Count > 0)
        {
            sb.AppendLine(
                $"type LiveCustomPropertyKey = {string.Join(" | ", customKeys.Select(Quote))};");
            sb.AppendLine(
                "interface DashboardRunSession { customProperties: Partial<Record<LiveCustomPropertyKey, any>> & Record<string, any>; }");
        }

        if (sb.Length == 0 || sb.ToString().Trim() == "// Live keys from current fig data")
            return "// No live keys available\n";

        return sb.ToString();
    }

    public static string BuildExpectedResult(string componentType)
    {
        var shape = componentType?.Trim().ToLowerInvariant() switch
        {
            "kpi" =>
                """
                /** Expected inline-script return for KPI / status card components. */
                type ExpectedScriptResult =
                  | {
                      value?: any;
                      numerator?: number | string;
                      denominator?: number | string;
                      label?: string;
                      subtitle?: string;
                      trend?: string | number;
                      variant?: 'normal' | 'info' | 'success' | 'warning' | 'danger' | string;
                      icon?: string;
                    }
                  | string
                  | number
                  | boolean;
                """,
            "text" =>
                """
                /** Expected inline-script return for text components. */
                type ExpectedScriptResult =
                  | string
                  | {
                      lines: Array<{
                        text: string;
                        size?: 'xs' | 'sm' | 'md' | 'lg' | 'xl' | 'xxl' | string;
                        color?: string;
                        align?: 'left' | 'center' | 'right' | string;
                        weight?: 'normal' | 'bold' | string;
                      }>;
                    }
                  | Array<{
                      text: string;
                      size?: 'xs' | 'sm' | 'md' | 'lg' | 'xl' | 'xxl' | string;
                      color?: string;
                      align?: 'left' | 'center' | 'right' | string;
                      weight?: 'normal' | 'bold' | string;
                    }>
                  | { text: string; variant?: 'heading' | 'body' | 'muted' | string };
                """,
            "badge" =>
                """
                /** Expected inline-script return for badge components. variant: info | success | warning | danger | muted */
                type ExpectedScriptResult =
                  | string
                  | { text: string; variant?: 'info' | 'success' | 'warning' | 'danger' | 'muted' | string };
                """,
            "bar" or "donut" =>
                """
                /** Expected inline-script return for chart components. */
                type ExpectedScriptResult = Array<{ label: string; value: number | string }>;
                """,
            "table" =>
                """
                /** Expected inline-script return for table components (array of row objects). */
                type ExpectedScriptResult = Array<Record<string, any>>;
                """,
            "list" =>
                """
                /** Expected inline-script return for list components. */
                type ExpectedScriptResult =
                  | string[]
                  | Array<{ text?: string; name?: string; secondary?: string; variant?: string }>;
                """,
            "keyvalue" =>
                """
                /** Expected inline-script return for key/value components. */
                type ExpectedScriptResult =
                  | Array<{ key: string; value: any }>
                  | {
                      statusIcon?: string;
                      statusColor?: string;
                      items: Array<{ key: string; value: any }>;
                    }
                  | Record<string, any>;
                """,
            "cards" =>
                """
                /** Expected inline-script return for cards components. */
                type ExpectedScriptResult = Array<{
                  title?: string;
                  value: any;
                  variant?: 'normal' | 'info' | 'success' | 'warning' | 'danger' | string;
                  icon?: string;
                  rows?: Array<{ key: string; value: any }>;
                }>;
                """,
            _ =>
                """
                /** Expected inline-script return for this component. */
                type ExpectedScriptResult = any;
                """
        };

        return shape + """

            /**
             * Hint: scripts may `return` an ExpectedScriptResult or evaluate to one as an expression.
             * Prefer returning a value that matches ExpectedScriptResult for this component type.
             */
            declare function __figExpectedResultHint(): ExpectedScriptResult;
            """;
    }

    private static HashSet<string> CollectDictionaryKeys(DashboardJsArray? array, string propertyName)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (array is null)
            return keys;

        foreach (var item in array)
        {
            if (item is null)
                continue;

            IDictionary<string, object?>? dict = propertyName switch
            {
                "settings" when item is DashboardClientJsModel client => client.settings,
                "customProperties" when item is DashboardRunSessionJsModel session => session.customProperties,
                _ => null
            };

            if (dict is null)
                continue;

            foreach (var key in dict.Keys)
            {
                if (!string.IsNullOrWhiteSpace(key) && IsValidJsIdentifier(key))
                    keys.Add(key);
            }
        }

        return keys;
    }

    private static string Quote(string value) =>
        $"'{value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("'", "\\'", StringComparison.Ordinal)}'";

    private static bool IsValidJsIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        if (!(char.IsLetter(value[0]) || value[0] == '_' || value[0] == '$'))
            return false;
        return value.Skip(1).All(c => char.IsLetterOrDigit(c) || c == '_' || c == '$');
    }
}

public sealed record DashboardScriptExtraLib(string FilePath, string Content);
