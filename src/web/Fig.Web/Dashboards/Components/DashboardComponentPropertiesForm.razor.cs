using Fig.Contracts.Dashboards;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fig.Web.Dashboards.Components;

public partial class DashboardComponentPropertiesForm
{
    private static readonly string[] BindingModes = ["inline", "preset", "namedTransform"];

    private static readonly IReadOnlyList<FormDropDownOption> LegendPositionOptions =
    [
        new("right", "Right"),
        new("bottom", "Bottom"),
        new("hidden", "Hidden")
    ];

    [Parameter] public DashboardComponentInstanceDataContract? Component { get; set; }

    [Parameter] public DashboardLayoutCellDataContract? LayoutCell { get; set; }

    [Parameter] public string ConfigJson { get; set; } = "{}";

    [Parameter] public EventCallback<string> ConfigJsonChanged { get; set; }

    [Parameter] public bool ShowInlineScript { get; set; } = true;

    [Parameter] public bool ShowEvaluate { get; set; } = true;

    [Parameter] public bool ShowConfigJson { get; set; } = true;

    [Parameter] public string? PreviewText { get; set; }

    [Parameter] public string? PreviewError { get; set; }

    [Parameter] public EventCallback OnEvaluate { get; set; }

    [Parameter] public IReadOnlyList<DashboardTransformDataContract> Transforms { get; set; } =
        Array.Empty<DashboardTransformDataContract>();

    /// <summary>
    /// Raised after any in-place mutation. <c>NeedsReEvaluate</c> is true when the canvas/preview
    /// should re-run bindings.
    /// </summary>
    [Parameter] public EventCallback<bool> OnMutated { get; set; }

    [Inject] private DashboardComponentRegistry ComponentRegistry { get; set; } = null!;

    private IEnumerable<FormDropDownOption> PresetOptions
    {
        get
        {
            if (Component is null)
                return Array.Empty<FormDropDownOption>();

            return ComponentRegistry.PresetsFor(Component.Type)
                .Select(p => new FormDropDownOption(p.Id, p.DisplayName))
                .Prepend(new FormDropDownOption(string.Empty, "(none)"))
                .ToList();
        }
    }

    private IEnumerable<FormDropDownOption> TransformOptions =>
        Transforms
            .Select(t => new FormDropDownOption(
                t.Id,
                string.IsNullOrWhiteSpace(t.Name) ? t.Id : $"{t.Name} ({t.Id})"))
            .Prepend(new FormDropDownOption(string.Empty, "(none)"))
            .ToList();

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
        await ConfigJsonChanged.InvokeAsync(value);
        if (Component is null)
            return;

        try
        {
            Component.Config = JObject.Parse(string.IsNullOrWhiteSpace(value) ? "{}" : value);
            await NotifyMutated(needsReEvaluate: true);
        }
        catch (JsonException)
        {
            // Keep typing; apply when valid.
        }
    }

    private async Task OnBindingModeChanged(string mode)
    {
        if (Component is null)
            return;
        Component.DataBinding.Mode = mode;
        await NotifyMutated(needsReEvaluate: true);
    }

    private async Task OnPresetChanged(string presetId)
    {
        if (Component is null)
            return;
        Component.DataBinding.PresetId = string.IsNullOrWhiteSpace(presetId) ? null : presetId;
        var preset = ComponentRegistry.GetPreset(presetId);
        if (preset?.DefaultConfig is not null)
        {
            Component.Config = preset.DefaultConfig.DeepClone() as JObject ?? Component.Config;
            await SyncConfigJsonAsync();
        }

        await NotifyMutated(needsReEvaluate: true);
    }

    private async Task OnTransformIdChanged(string transformId)
    {
        if (Component is null)
            return;
        Component.DataBinding.TransformId = string.IsNullOrWhiteSpace(transformId) ? null : transformId;
        await NotifyMutated(needsReEvaluate: true);
    }

    private async Task OnInlineScriptChanged(string script)
    {
        if (Component is null)
            return;
        Component.DataBinding.InlineScript = script;
        await NotifyMutated(needsReEvaluate: true);
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
