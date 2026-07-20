using System;
using System.Text;

namespace Fig.Contracts.Diagnostics;

/// <summary>
/// Request-scoped A/B flags for Fig.Web settings-load triage.
/// Stored in browser localStorage key <c>fig.loadPerfFlags</c> (raw string) or query <c>?loadPerf=...</c>,
/// sent to the API as header <c>X-Fig-Load-Perf</c>, and recorded on load-timing OTEL as
/// <c>fig.web.load_perf_flags</c>.
/// </summary>
/// <remarks>
/// <para><b>How to A/B on a large system</b></para>
/// <list type="number">
/// <item>Deploy this branch; hard-refresh Fig.Web once.</item>
/// <item>
/// Run A (optimized): in the browser console —
/// <c>localStorage.setItem('fig.loadPerfFlags','optimized'); location.reload()</c>
/// then open Settings, wait for load+scripts, export the <c>Web.SettingsClientLoad</c> trace.
/// </item>
/// <item>
/// Run B (baseline A/B bits off): —
/// <c>localStorage.setItem('fig.loadPerfFlags','baseline'); location.reload()</c>
/// and export again.
/// </item>
/// <item>
/// Optional Run C (compact JSON off only): —
/// <c>localStorage.setItem('fig.loadPerfFlags','noCompact'); location.reload()</c>
/// </item>
/// <item>
/// Compare <c>total_duration_ms</c>, <c>httpfetch_parse_ms</c>, <c>initialize_settings_ms</c>,
/// <c>initialize_scripts_ms</c>, convert stages, and display-script counts. Prefer reverting a bit
/// when baseline−optimized &lt; 150ms (or &lt; 50ms for script batching). Keep compact JSON if
/// turning it off adds &gt; 500ms parse.
/// </item>
/// </list>
/// <para>
/// Presets: <c>optimized</c> (all on), <c>baseline</c> (all off), <c>noCompact</c> (optimized with
/// compactClientsJson off). Overrides: <c>optimized,batchDisplayScripts=0</c>.
/// </para>
/// </remarks>
public sealed class LoadPerfFlags : IEquatable<LoadPerfFlags>
{
    public const string LocalStorageKey = "fig.loadPerfFlags";
    public const string QueryParameterName = "loadPerf";

    public static LoadPerfFlags Optimized { get; } = new(
        compactClientsJson: true,
        batchDisplayScripts: true,
        skipNoopInit: true,
        deferScripts: true,
        lazyDescriptionHtml: true,
        dataGridLoadOpts: true);

    public static LoadPerfFlags Baseline { get; } = new(
        compactClientsJson: false,
        batchDisplayScripts: false,
        skipNoopInit: false,
        deferScripts: false,
        lazyDescriptionHtml: false,
        dataGridLoadOpts: false);

    /// <summary>
    /// Ambient flags for Fig.Web model constructors (set by <c>ILoadPerfFlagsService</c>).
    /// Defaults to <see cref="Optimized"/> until initialized.
    /// </summary>
    public static LoadPerfFlags Current { get; set; } = Optimized;

    public LoadPerfFlags(
        bool compactClientsJson,
        bool batchDisplayScripts,
        bool skipNoopInit,
        bool deferScripts,
        bool lazyDescriptionHtml,
        bool dataGridLoadOpts)
    {
        CompactClientsJson = compactClientsJson;
        BatchDisplayScripts = batchDisplayScripts;
        SkipNoopInit = skipNoopInit;
        DeferScripts = deferScripts;
        LazyDescriptionHtml = lazyDescriptionHtml;
        DataGridLoadOpts = dataGridLoadOpts;
    }

    /// <summary>FigWebLoad compact <c>t</c>/<c>v</c> JSON for GET /clients.</summary>
    public bool CompactClientsJson { get; }

    /// <summary>Shared JS engine via <c>RunScripts</c> for initial display-script batch.</summary>
    public bool BatchDisplayScripts { get; }

    /// <summary>Skip settings with no display script / validation regex during InitializeAsync.</summary>
    public bool SkipNoopInit { get; }

    /// <summary>Run InitializeAllClientsAsync after first paint instead of inside LoadAllClients.</summary>
    public bool DeferScripts { get; }

    /// <summary>Defer Markdig HTML until Description is first read.</summary>
    public bool LazyDescriptionHtml { get; }

    /// <summary>Data-grid clone-from-value, defer original JSON, skip empty column regex on load.</summary>
    public bool DataGridLoadOpts { get; }

    public static LoadPerfFlags Parse(string? raw)
    {
        if (raw is null)
            return Optimized;

        var trimmed = raw.Trim();
        if (trimmed.Length == 0)
            return Optimized;

        if (trimmed.Equals("optimized", StringComparison.OrdinalIgnoreCase))
            return Optimized;
        if (trimmed.Equals("baseline", StringComparison.OrdinalIgnoreCase))
            return Baseline;
        if (trimmed.Equals("noCompact", StringComparison.OrdinalIgnoreCase))
            return Optimized.With(compactClientsJson: false);

        var flags = Optimized;
        foreach (var token in trimmed.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var part = token.Trim();
            if (part.Length == 0)
                continue;

            if (part.Equals("optimized", StringComparison.OrdinalIgnoreCase))
            {
                flags = Optimized;
                continue;
            }

            if (part.Equals("baseline", StringComparison.OrdinalIgnoreCase))
            {
                flags = Baseline;
                continue;
            }

            if (part.Equals("noCompact", StringComparison.OrdinalIgnoreCase))
            {
                flags = flags.With(compactClientsJson: false);
                continue;
            }

            var eq = part.IndexOf('=');
            string name;
            bool enabled;
            if (eq < 0)
            {
                name = part;
                enabled = true;
            }
            else
            {
                name = part.Substring(0, eq).Trim();
                var value = part.Substring(eq + 1).Trim();
                enabled = !(value.Equals("0", StringComparison.OrdinalIgnoreCase)
                            || value.Equals("false", StringComparison.OrdinalIgnoreCase)
                            || value.Equals("off", StringComparison.OrdinalIgnoreCase)
                            || value.Equals("no", StringComparison.OrdinalIgnoreCase));
            }

            flags = ApplyBit(flags, name, enabled);
        }

        return flags;
    }

    public string ToHeaderValue()
    {
        // Stable, explicit form so traces are comparable.
        var sb = new StringBuilder();
        Append(sb, "compactClientsJson", CompactClientsJson);
        Append(sb, "batchDisplayScripts", BatchDisplayScripts);
        Append(sb, "skipNoopInit", SkipNoopInit);
        Append(sb, "deferScripts", DeferScripts);
        Append(sb, "lazyDescriptionHtml", LazyDescriptionHtml);
        Append(sb, "dataGridLoadOpts", DataGridLoadOpts);
        return sb.ToString();

        static void Append(StringBuilder builder, string name, bool value)
        {
            if (builder.Length > 0)
                builder.Append(',');
            builder.Append(name).Append('=').Append(value ? '1' : '0');
        }
    }

    public LoadPerfFlags With(
        bool? compactClientsJson = null,
        bool? batchDisplayScripts = null,
        bool? skipNoopInit = null,
        bool? deferScripts = null,
        bool? lazyDescriptionHtml = null,
        bool? dataGridLoadOpts = null) =>
        new(
            compactClientsJson ?? CompactClientsJson,
            batchDisplayScripts ?? BatchDisplayScripts,
            skipNoopInit ?? SkipNoopInit,
            deferScripts ?? DeferScripts,
            lazyDescriptionHtml ?? LazyDescriptionHtml,
            dataGridLoadOpts ?? DataGridLoadOpts);

    public bool Equals(LoadPerfFlags? other)
    {
        if (other is null)
            return false;
        return CompactClientsJson == other.CompactClientsJson
               && BatchDisplayScripts == other.BatchDisplayScripts
               && SkipNoopInit == other.SkipNoopInit
               && DeferScripts == other.DeferScripts
               && LazyDescriptionHtml == other.LazyDescriptionHtml
               && DataGridLoadOpts == other.DataGridLoadOpts;
    }

    public override bool Equals(object? obj) => obj is LoadPerfFlags other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + CompactClientsJson.GetHashCode();
            hash = hash * 31 + BatchDisplayScripts.GetHashCode();
            hash = hash * 31 + SkipNoopInit.GetHashCode();
            hash = hash * 31 + DeferScripts.GetHashCode();
            hash = hash * 31 + LazyDescriptionHtml.GetHashCode();
            hash = hash * 31 + DataGridLoadOpts.GetHashCode();
            return hash;
        }
    }

    public override string ToString() => ToHeaderValue();

    private static LoadPerfFlags ApplyBit(LoadPerfFlags flags, string name, bool enabled) =>
        name.ToLowerInvariant() switch
        {
            "compactclientsjson" or "compact" => flags.With(compactClientsJson: enabled),
            "batchdisplayscripts" or "batchscripts" => flags.With(batchDisplayScripts: enabled),
            "skipnoopinit" or "skipnoop" => flags.With(skipNoopInit: enabled),
            "deferscripts" or "defer" => flags.With(deferScripts: enabled),
            "lazydescriptionhtml" or "lazydescription" or "lazyhtml" => flags.With(lazyDescriptionHtml: enabled),
            "datagridloadopts" or "datagrid" => flags.With(dataGridLoadOpts: enabled),
            _ => flags
        };
}
