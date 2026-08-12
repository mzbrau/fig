using Fig.Contracts.Assistant;
using Fig.Contracts.Dashboards;
using Fig.Web.Dashboards.Components;
using Fig.Web.Dashboards.Facades;
using Fig.Web.Dashboards.Runtime;
using Fig.Web.Notifications;
using Fig.Web.Services;
using Fig.Web.Services.Assistant;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Radzen;

namespace Fig.Web.Pages;

public partial class DashboardEdit : IDisposable
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IDashboardFacade DashboardFacade { get; set; } = null!;
    [Inject] private IDashboardDataProvider DataProvider { get; set; } = null!;
    [Inject] private DashboardRuntime Runtime { get; set; } = null!;
    [Inject] private DashboardComponentRegistry ComponentRegistry { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private INotificationFactory NotificationFactory { get; set; } = null!;
    [Inject] private IAssistantContextService AssistantContextService { get; set; } = null!;
    [Inject] private IDashboardAssistantActionQueue DashboardAssistantActionQueue { get; set; } = null!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;

    private ElementReference _editorRoot;
    private DashboardDataContract? _dashboard;
    private Dictionary<string, DashboardComponentResult> _results = new(StringComparer.OrdinalIgnoreCase);
    private string? _selectedComponentId;
    private string _configJson = "{}";
    private string? _previewText;
    private string? _previewError;
    private bool _loading = true;
    private bool _dirty;
    private bool _saving;
    private string _baselineJson = string.Empty;
    private bool _paletteCollapsed;
    private bool _propertiesCollapsed;

    private DashboardComponentInstanceDataContract? SelectedComponent =>
        _dashboard?.Definition.Components.FirstOrDefault(c =>
            string.Equals(c.Id, _selectedComponentId, StringComparison.OrdinalIgnoreCase));

    private DashboardLayoutCellDataContract? SelectedCell =>
        _dashboard?.Definition.Layout.FirstOrDefault(c =>
            string.Equals(c.Id, _selectedComponentId, StringComparison.OrdinalIgnoreCase));

    private IEnumerable<IGrouping<string, DashboardComponentDescriptor>> PaletteCategories =>
        ComponentRegistry.All
            .OrderBy(c => c.Category)
            .ThenBy(c => c.DisplayName)
            .GroupBy(c => c.Category);

    private string BodyCollapseClass =>
        (_paletteCollapsed, _propertiesCollapsed) switch
        {
            (true, true) => "dashboard-editor__body--both-collapsed",
            (true, false) => "dashboard-editor__body--palette-collapsed",
            (false, true) => "dashboard-editor__body--properties-collapsed",
            _ => string.Empty
        };

    protected override void OnInitialized()
    {
        DashboardAssistantActionQueue.ActionsQueued += OnDashboardAssistantActionsQueued;
        ApplyQueuedAssistantActions();
    }

    protected override async Task OnParametersSetAsync()
    {
        await LoadDashboard();
        await base.OnParametersSetAsync();
    }

    private async Task LoadDashboard()
    {
        _loading = true;
        try
        {
            _dashboard = await DashboardFacade.Get(Id);
            if (_dashboard is null)
                return;

            _dashboard.Definition ??= new DashboardDefinitionDataContract();
            _dashboard.Definition.Refresh ??= new DashboardRefreshDataContract();
            _dashboard.Definition.Transforms ??= new List<DashboardTransformDataContract>();
            _dashboard.Definition.Components ??= new List<DashboardComponentInstanceDataContract>();
            _dashboard.Definition.Layout ??= new List<DashboardLayoutCellDataContract>();

            await DataProvider.EnsureLoadedAsync();
            ReEvaluate();
            CaptureBaseline();
            SyncConfigEditor();
            ApplyQueuedAssistantActions();
            PublishAssistantContext();
        }
        finally
        {
            _loading = false;
        }
    }

    private void CaptureBaseline()
    {
        _baselineJson = SerializeDashboard(_dashboard);
        _dirty = false;
    }

    private void MarkDirty()
    {
        _dirty = !string.Equals(SerializeDashboard(_dashboard), _baselineJson, StringComparison.Ordinal);
    }

    private static string SerializeDashboard(DashboardDataContract? dashboard) =>
        JsonConvert.SerializeObject(dashboard);

    private void ReEvaluate()
    {
        if (_dashboard is null)
            return;

        Runtime.SetDefinition(_dashboard.Definition);
        _results = Runtime.Evaluate();
    }

    private Task OnSelectedChanged(string? id)
    {
        _selectedComponentId = id;
        _previewText = null;
        _previewError = null;
        SyncConfigEditor();
        PublishAssistantContext();
        _ = FocusCanvasAsync();
        return Task.CompletedTask;
    }

    private async Task FocusCanvasAsync()
    {
        try
        {
            await _editorRoot.FocusAsync();
        }
        catch
        {
            // Element may not be ready yet.
        }
    }

    private void SyncConfigEditor()
    {
        _configJson = SelectedComponent?.Config?.ToString(Formatting.Indented) ?? "{}";
    }

    private void TogglePaletteCollapsed()
    {
        _paletteCollapsed = !_paletteCollapsed;
    }

    private void TogglePropertiesCollapsed()
    {
        _propertiesCollapsed = !_propertiesCollapsed;
    }

    private async Task OnEditorKeyDown(KeyboardEventArgs args)
    {
        if (SelectedCell is null)
            return;

        if (args.Key is not ("ArrowLeft" or "ArrowRight" or "ArrowUp" or "ArrowDown"))
            return;

        try
        {
            if (await JsRuntime.InvokeAsync<bool>("figIsEditableFocus"))
                return;
        }
        catch (JSException)
        {
            // If interop is unavailable, still allow nudge when not typing.
        }

        switch (args.Key)
        {
            case "ArrowLeft":
                Nudge(-1, 0);
                break;
            case "ArrowRight":
                Nudge(1, 0);
                break;
            case "ArrowUp":
                Nudge(0, -1);
                break;
            case "ArrowDown":
                Nudge(0, 1);
                break;
        }
    }

    private void AddComponent(string type)
    {
        if (_dashboard is null)
            return;

        var descriptor = ComponentRegistry.Get(type);
        if (descriptor is null)
            return;

        var id = $"{type}-{Guid.NewGuid():N}"[..18];
        var y = _dashboard.Definition.Layout.Count == 0
            ? 0
            : _dashboard.Definition.Layout.Max(c => c.Y + Math.Max(c.Height, 1));

        var preset = descriptor.Presets.FirstOrDefault();
        var component = new DashboardComponentInstanceDataContract
        {
            Id = id,
            Type = type,
            Config = preset?.DefaultConfig.DeepClone() as JObject ?? new JObject { ["title"] = descriptor.DisplayName },
            DataBinding = preset is null
                ? new DashboardDataBindingDataContract
                {
                    Mode = "inline",
                    InlineScript = DefaultInlineScript(type)
                }
                : new DashboardDataBindingDataContract
                {
                    Mode = "preset",
                    PresetId = preset.Id
                }
        };

        _dashboard.Definition.Components.Add(component);
        _dashboard.Definition.Layout.Add(new DashboardLayoutCellDataContract
        {
            Id = id,
            X = 0,
            Y = y,
            Width = 4,
            Height = 2
        });

        _selectedComponentId = id;
        SyncConfigEditor();
        ReEvaluate();
        MarkDirty();
        PublishAssistantContext();
    }

    private static string DefaultInlineScript(string type) => type.ToLowerInvariant() switch
    {
        "kpi" => "return { value: fig.runSessions.length, label: 'Run sessions' };",
        "text" => "return { text: 'Hello from Fig', variant: 'heading' };",
        "badge" => "return { text: 'OK', variant: 'success' };",
        "list" => "return fig.runSessions.map(s => ({ text: s.name, secondary: s.hostname }));",
        "keyvalue" => """
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
            """,
        "table" => "return fig.runSessions.map(s => ({ name: s.name, hostname: s.hostname }));",
        "bar" or "donut" =>
            "return fig.runSessions.groupBy(s => s.name).map(g => ({ label: g.key, value: g.items.length }));",
        _ => "return null;"
    };

    private void Nudge(int dx, int dy)
    {
        if (SelectedCell is null)
            return;

        SelectedCell.X = Math.Clamp(SelectedCell.X + dx, 0, 11);
        SelectedCell.Y = Math.Max(0, SelectedCell.Y + dy);
        MarkDirty();
    }

    private void DuplicateSelected()
    {
        if (_dashboard is null || SelectedComponent is null || SelectedCell is null)
            return;

        var source = SelectedComponent;
        var sourceCell = SelectedCell;
        var id = $"{source.Type}-{Guid.NewGuid():N}"[..18];

        var cloneJson = JsonConvert.SerializeObject(source);
        var clone = JsonConvert.DeserializeObject<DashboardComponentInstanceDataContract>(cloneJson)!;
        clone.Id = id;

        _dashboard.Definition.Components.Add(clone);
        _dashboard.Definition.Layout.Add(new DashboardLayoutCellDataContract
        {
            Id = id,
            X = Math.Clamp(sourceCell.X + 1, 0, 11),
            Y = sourceCell.Y + 1,
            Width = sourceCell.Width,
            Height = sourceCell.Height
        });

        _selectedComponentId = id;
        SyncConfigEditor();
        ReEvaluate();
        MarkDirty();
        PublishAssistantContext();
    }

    private void DeleteSelected()
    {
        if (_dashboard is null || _selectedComponentId is null)
            return;

        _dashboard.Definition.Components.RemoveAll(c =>
            string.Equals(c.Id, _selectedComponentId, StringComparison.OrdinalIgnoreCase));
        _dashboard.Definition.Layout.RemoveAll(c =>
            string.Equals(c.Id, _selectedComponentId, StringComparison.OrdinalIgnoreCase));
        _selectedComponentId = null;
        _previewText = null;
        _previewError = null;
        ReEvaluate();
        MarkDirty();
        PublishAssistantContext();
    }

    private void OnConfigJsonFromForm(string value)
    {
        _configJson = value;
    }

    private void OnComponentFormMutated(bool needsReEvaluate)
    {
        if (needsReEvaluate)
            ReEvaluate();
        MarkDirty();
        PublishAssistantContext();
    }

    private void AddTransform()
    {
        if (_dashboard is null) return;
        var id = $"transform-{_dashboard.Definition.Transforms.Count + 1}";
        _dashboard.Definition.Transforms.Add(new DashboardTransformDataContract
        {
            Id = id,
            Name = id,
            Script = "return fig.runSessions;",
            DependsOn = new List<string>()
        });
        MarkDirty();
        PublishAssistantContext();
    }

    private void RemoveTransform(DashboardTransformDataContract transform)
    {
        _dashboard?.Definition.Transforms.Remove(transform);
        MarkDirty();
        PublishAssistantContext();
    }

    private void OnTransformField(DashboardTransformDataContract transform, string field, string value)
    {
        switch (field)
        {
            case nameof(DashboardTransformDataContract.Id):
                transform.Id = value;
                break;
            case nameof(DashboardTransformDataContract.Name):
                transform.Name = value;
                break;
            case nameof(DashboardTransformDataContract.Script):
                transform.Script = value;
                break;
        }

        MarkDirty();
        PublishAssistantContext();
    }

    private void OnTransformDependsOn(DashboardTransformDataContract transform, string value)
    {
        transform.DependsOn = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        MarkDirty();
    }

    private async Task EvaluateSelected()
    {
        if (_dashboard is null || SelectedComponent is null)
            return;

        await DataProvider.EnsureLoadedAsync();
        ReEvaluate();
        if (_results.TryGetValue(SelectedComponent.Id, out var result))
        {
            if (result.Success)
            {
                _previewError = null;
                _previewText = JsonConvert.SerializeObject(result.Data, Formatting.Indented);
            }
            else
            {
                _previewText = null;
                _previewError = result.Error;
            }
        }
    }

    private async Task OpenDataExplorer()
    {
        await DataProvider.EnsureLoadedAsync();
        ReEvaluate();

        await DialogService.OpenAsync<Dialogs.DashboardDataExplorerDialog>(
            "Data explorer",
            new Dictionary<string, object?>
            {
                { nameof(Dialogs.DashboardDataExplorerDialog.Fig), DataProvider.Current },
                { nameof(Dialogs.DashboardDataExplorerDialog.NamedTransforms), Runtime.NamedTransformResults }
            },
            new DialogOptions
            {
                Width = "90vw",
                Height = "85vh",
                Resizable = true,
                Draggable = true
            });
    }

    private async Task OpenComponentEditor()
    {
        if (_dashboard is null || SelectedComponent is null)
            return;

        await DataProvider.EnsureLoadedAsync();
        ReEvaluate();

        await DialogService.OpenAsync<Dialogs.DashboardComponentEditDialog>(
            $"Edit {SelectedComponent.Type}",
            new Dictionary<string, object?>
            {
                { nameof(Dialogs.DashboardComponentEditDialog.Definition), _dashboard.Definition },
                { nameof(Dialogs.DashboardComponentEditDialog.Component), SelectedComponent },
                { nameof(Dialogs.DashboardComponentEditDialog.LayoutCell), SelectedCell },
                { nameof(Dialogs.DashboardComponentEditDialog.OnMutated),
                    EventCallback.Factory.Create(this, OnDialogMutated) }
            },
            new DialogOptions
            {
                Width = "92vw",
                Height = "90vh",
                Resizable = true,
                Draggable = true,
                CloseDialogOnOverlayClick = false
            });

        SyncConfigEditor();
        ReEvaluate();
        PublishAssistantContext();
        StateHasChanged();
    }

    private void OnDialogMutated()
    {
        MarkDirty();
        PublishAssistantContext();
    }

    private async Task Save()
    {
        if (_dashboard?.Id is not Guid)
            return;

        _saving = true;
        try
        {
            await SaveInternal(forceOverwrite: false);
        }
        finally
        {
            _saving = false;
        }
    }

    private async Task SaveInternal(bool forceOverwrite)
    {
        if (_dashboard?.Id is not Guid id)
            return;

        try
        {
            var updated = await DashboardFacade.Update(id, _dashboard, forceOverwrite);
            if (updated is null)
            {
                NotificationService.Notify(NotificationFactory.Failure("Save Failed", "Could not save dashboard."));
                return;
            }

            _dashboard = updated;
            CaptureBaseline();
            PublishAssistantContext();
            NotificationService.Notify(NotificationFactory.Success("Saved", $"Dashboard '{updated.Name}' saved."));
        }
        catch (DashboardConcurrencyConflictException ex)
        {
            var choice = await DialogService.Confirm(
                "This dashboard was modified by another user. Reload their version, or force overwrite with your changes?",
                "Concurrency Conflict",
                new ConfirmOptions
                {
                    OkButtonText = "Force overwrite",
                    CancelButtonText = "Reload"
                });

            if (choice == true)
            {
                await SaveInternal(forceOverwrite: true);
            }
            else
            {
                _dashboard = ex.Current;
                _selectedComponentId = null;
                ReEvaluate();
                CaptureBaseline();
                SyncConfigEditor();
                PublishAssistantContext();
                NotificationService.Notify(NotificationFactory.Info("Reloaded", "Loaded the latest saved version."));
            }
        }
    }

    private void PublishAssistantContext()
    {
        if (_dashboard is null)
            return;

        var selected = SelectedComponent;
        var descriptor = selected is null ? null : ComponentRegistry.Get(selected.Type);
        AssistantContextService.Publish(new AssistantUiContextDataContract
        {
            CurrentPage = "Dashboard Edit",
            Dashboard = new AssistantDashboardContextDataContract
            {
                DashboardId = _dashboard.Id,
                DashboardName = _dashboard.Name,
                SelectedComponentId = selected?.Id,
                SelectedComponentType = selected?.Type,
                BindingMode = selected?.DataBinding.Mode,
                InlineScript = selected?.DataBinding.InlineScript,
                ExpectedResponseShape = descriptor?.ExpectedScriptShape,
                JsModelSummary = DashboardComponentRegistry.JsModelSummary,
                NamedTransformIds = _dashboard.Definition.Transforms.Select(t => t.Id).ToList()
            }
        });
    }

    private void OnDashboardAssistantActionsQueued()
    {
        _ = InvokeAsync(ApplyQueuedAssistantActions);
    }

    private void ApplyQueuedAssistantActions()
    {
        var actions = DashboardAssistantActionQueue.DequeueAll();
        if (actions.Count == 0 || _dashboard is null)
            return;

        var applied = 0;
        foreach (var action in actions)
        {
            var component = _dashboard.Definition.Components.FirstOrDefault(c =>
                string.Equals(c.Id, action.ComponentId, StringComparison.OrdinalIgnoreCase));
            if (component is null)
                continue;

            component.DataBinding.Mode = "inline";
            component.DataBinding.InlineScript = action.Script;
            _selectedComponentId = component.Id;
            applied++;
        }

        if (applied == 0)
            return;

        SyncConfigEditor();
        ReEvaluate();
        MarkDirty();
        PublishAssistantContext();
        NotificationService.Notify(NotificationFactory.Success(
            "Assistant script ready",
            $"{applied} inline script draft(s) applied. Review and Save the dashboard."));
        StateHasChanged();
    }

    private void GoToView() => NavigationManager.NavigateTo($"/dashboards/{Id}");
    private void GoToList() => NavigationManager.NavigateTo("/dashboards");

    public void Dispose()
    {
        DashboardAssistantActionQueue.ActionsQueued -= OnDashboardAssistantActionsQueued;
        AssistantContextService.Publish(new AssistantUiContextDataContract());
    }
}
