using Fig.Contracts.Dashboards;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fig.Web.Dashboards.Components;

public partial class DashboardComponentPropertiesForm
{
    public const string CustomSuggestionValue = "__custom__";

    private static readonly IReadOnlyList<FormDropDownOption> LegendPositionOptions =
    [
        new("right", "Right"),
        new("bottom", "Bottom"),
        new("hidden", "Hidden")
    ];

    private static readonly IReadOnlyList<FormDropDownOption> CardStyleOptions =
    [
        new("compact", "Compact"),
        new("wide", "Wide"),
        new("extraWide", "Extra wide")
    ];

    private static readonly IReadOnlyList<FormDropDownOption> ChartSizeOptions =
    [
        new("large", "Large"),
        new("small", "Small")
    ];

    [Parameter] public DashboardComponentInstanceDataContract? Component { get; set; }

    [Parameter] public DashboardLayoutCellDataContract? LayoutCell { get; set; }

    [Parameter] public string ConfigJson { get; set; } = "{}";

    [Parameter] public EventCallback<string> ConfigJsonChanged { get; set; }

    [Parameter] public bool ShowInlineScript { get; set; } = true;

    [Parameter] public bool ShowEvaluate { get; set; } = true;

    [Parameter] public bool ShowConfigJson { get; set; } = true;

    /// <summary>
    /// When true, renders a compact two-column layout used by the component edit dialog.
    /// </summary>
    [Parameter] public bool DenseLayout { get; set; }

    /// <summary>
    /// When false, continuous typing (config JSON) raises <see cref="OnMutated"/> with
    /// <c>needsReEvaluate: false</c> so the host can defer preview updates until blur/Evaluate.
    /// Discrete controls (dropdowns, title, layout) still request re-evaluate.
    /// </summary>
    [Parameter] public bool ReEvaluateOnMutate { get; set; } = true;

    [Parameter] public bool ShowExpectedShape { get; set; }

    [Parameter] public string? ExpectedScriptShape { get; set; }

    [Parameter] public string? PreviewText { get; set; }

    [Parameter] public string? PreviewError { get; set; }

    [Parameter] public EventCallback OnEvaluate { get; set; }

    /// <summary>
    /// Raised when a suggested script is applied so hosts that use Monaco can refresh the editor.
    /// </summary>
    [Parameter] public EventCallback<string> OnSuggestedScriptApplied { get; set; }

    /// <summary>
    /// Raised after any in-place mutation. <c>NeedsReEvaluate</c> is true when the canvas/preview
    /// should re-run bindings.
    /// </summary>
    [Parameter] public EventCallback<bool> OnMutated { get; set; }

    [Inject] private DashboardComponentRegistry ComponentRegistry { get; set; } = null!;

    private IReadOnlyList<DashboardComponentPreset> PresetsForType =>
        Component is null
            ? Array.Empty<DashboardComponentPreset>()
            : ComponentRegistry.PresetsFor(Component.Type).ToList();

    private IEnumerable<FormDropDownOption> SuggestedScriptOptions
    {
        get
        {
            var options = PresetsForType
                .Select(p => new FormDropDownOption(p.Id, p.DisplayName))
                .ToList();
            options.Add(new FormDropDownOption(CustomSuggestionValue, "(custom)"));
            return options;
        }
    }

    private string SelectedSuggestionId
    {
        get
        {
            if (Component is null)
                return CustomSuggestionValue;

            var script = NormalizeScript(Component.DataBinding.InlineScript);
            var match = PresetsForType.FirstOrDefault(p =>
                string.Equals(NormalizeScript(p.Script), script, StringComparison.Ordinal));
            return match?.Id ?? CustomSuggestionValue;
        }
    }

    /// <summary>
    /// Applies a registry preset script to the component binding. Used by the suggested-script dropdown
    /// and covered by unit tests.
    /// </summary>
    public static void ApplySuggestedScript(
        DashboardComponentInstanceDataContract component,
        DashboardComponentPreset preset)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(preset);

        component.DataBinding ??= new DashboardDataBindingDataContract();
        component.DataBinding.InlineScript = preset.Script;
        if (preset.DefaultConfig is not null)
            component.Config = preset.DefaultConfig.DeepClone() as JObject ?? component.Config;
    }

    private static string NormalizeScript(string? script) =>
        (script ?? string.Empty).Replace("\r\n", "\n").Trim();

    private string GetConfigString(string key) =>
        Component?.Config?[key]?.ToString() ?? string.Empty;

    private async Task SetConfigString(string key, string value)
    {
        if (Component is null)
            return;

        Component.Config ??= new JObject();
        Component.Config[key] = value;
        await SyncConfigJsonAsync();
        await NotifyMutated(needsReEvaluate: true);
    }

    private async Task OnConfigJsonChanged(string value)
    {
        ConfigJson = value;
        if (Component is null)
            return;

        try
        {
            Component.Config = JObject.Parse(string.IsNullOrWhiteSpace(value) ? "{}" : value);
            if (ReEvaluateOnMutate)
            {
                await ConfigJsonChanged.InvokeAsync(value);
                await NotifyMutated(needsReEvaluate: true);
            }
            // Dialog mode: keep local + Component.Config synced; notify parent on blur.
        }
        catch (JsonException)
        {
            // Keep typing; apply when valid.
        }
    }

    private async Task OnConfigJsonBlur()
    {
        if (ReEvaluateOnMutate)
            return;

        await ConfigJsonChanged.InvokeAsync(ConfigJson);
        await NotifyMutated(needsReEvaluate: true);
    }

    private async Task OnSuggestedScriptChanged(string suggestionId)
    {
        if (Component is null)
            return;

        if (string.IsNullOrWhiteSpace(suggestionId) ||
            string.Equals(suggestionId, CustomSuggestionValue, StringComparison.Ordinal))
            return;

        var preset = ComponentRegistry.GetPreset(suggestionId);
        if (preset is null)
            return;

        ApplySuggestedScript(Component, preset);
        await SyncConfigJsonAsync();
        await OnSuggestedScriptApplied.InvokeAsync(preset.Script);
        await NotifyMutated(needsReEvaluate: true);
    }

    private async Task OnInlineScriptChanged(string script)
    {
        if (Component is null)
            return;
        Component.DataBinding.InlineScript = script;
        await NotifyMutated(needsReEvaluate: ReEvaluateOnMutate);
    }

    private async Task OnCellXChanged(int value)
    {
        if (LayoutCell is null)
            return;
        LayoutCell.X = Math.Clamp(value, 0, 11);
        await NotifyMutated(needsReEvaluate: false);
    }

    private async Task OnCellYChanged(int value)
    {
        if (LayoutCell is null)
            return;
        LayoutCell.Y = Math.Max(0, value);
        await NotifyMutated(needsReEvaluate: false);
    }

    private async Task OnCellWidthChanged(int value)
    {
        if (LayoutCell is null)
            return;
        LayoutCell.Width = Math.Clamp(value, 1, 12);
        await NotifyMutated(needsReEvaluate: false);
    }

    private async Task OnCellHeightChanged(int value)
    {
        if (LayoutCell is null)
            return;
        LayoutCell.Height = Math.Max(1, value);
        await NotifyMutated(needsReEvaluate: false);
    }

    private string GetLegendPosition()
    {
        var value = GetConfigString("legendPosition");
        if (string.Equals(value, "bottom", StringComparison.OrdinalIgnoreCase))
            return "bottom";
        if (string.Equals(value, "hidden", StringComparison.OrdinalIgnoreCase))
            return "hidden";
        return "right";
    }

    private Task OnLegendPositionChanged(string value)
    {
        var normalized = value?.Trim().ToLowerInvariant() switch
        {
            "bottom" => "bottom",
            "hidden" => "hidden",
            _ => "right"
        };
        return SetConfigString("legendPosition", normalized);
    }

    private string GetCardStyle() =>
        DashboardComponentDataBinder.ReadCardStyle(Component?.Config);

    private Task OnCardStyleChanged(string value)
    {
        var normalized = string.Equals(value, "extraWide", StringComparison.OrdinalIgnoreCase)
            ? "extraWide"
            : string.Equals(value, "wide", StringComparison.OrdinalIgnoreCase)
                ? "wide"
                : "compact";
        return SetConfigString("cardStyle", normalized);
    }

    private string GetChartSize() =>
        DashboardComponentDataBinder.ReadChartSize(Component?.Config);

    private Task OnChartSizeChanged(string value)
    {
        var normalized = string.Equals(value, "small", StringComparison.OrdinalIgnoreCase)
            ? "small"
            : "large";
        return SetConfigString("chartSize", normalized);
    }

    private async Task SyncConfigJsonAsync()
    {
        var json = Component?.Config?.ToString(Formatting.Indented) ?? "{}";
        ConfigJson = json;
        await ConfigJsonChanged.InvokeAsync(json);
    }

    private Task NotifyMutated(bool needsReEvaluate) =>
        OnMutated.InvokeAsync(needsReEvaluate);

    public sealed record FormDropDownOption(string Value, string Text);
}
