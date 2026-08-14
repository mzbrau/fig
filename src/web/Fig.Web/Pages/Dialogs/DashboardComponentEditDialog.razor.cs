using Fig.Common.NetStandard.Scripting;
using Fig.Contracts.Dashboards;
using Fig.Web.Dashboards.Components;
using Fig.Web.Dashboards.Runtime;
using Fig.Web.Dashboards.Scripting;
using Fig.Web.Notifications;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Radzen;

namespace Fig.Web.Pages.Dialogs;

public partial class DashboardComponentEditDialog : IAsyncDisposable
{
    private readonly string _editorId = $"dashboard-script-editor-{Guid.NewGuid():N}";
    private DotNetObjectReference<DashboardComponentEditDialog>? _dotNetRef;
    private bool _monacoInitialized;
    private bool _disposed;
    private string _configJson = "{}";
    private string _expectedShape = string.Empty;
    private string? _previewText;
    private string? _previewError;
    private DashboardComponentResult? _componentResult;
    private Dictionary<string, DashboardComponentResult> _results = new(StringComparer.OrdinalIgnoreCase);

    [Parameter] public DashboardDefinitionDataContract Definition { get; set; } = new();

    [Parameter] public DashboardComponentInstanceDataContract Component { get; set; } = new();

    [Parameter] public DashboardLayoutCellDataContract? LayoutCell { get; set; }

    [Parameter] public EventCallback OnMutated { get; set; }

    [Inject] private IDashboardDataProvider DataProvider { get; set; } = null!;
    [Inject] private DashboardRuntime Runtime { get; set; } = null!;
    [Inject] private DashboardComponentRegistry ComponentRegistry { get; set; } = null!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] private IScriptBeautifier ScriptBeautifier { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private INotificationFactory NotificationFactory { get; set; } = null!;

    protected override void OnParametersSet()
    {
        _configJson = Component.Config?.ToString(Formatting.Indented) ?? "{}";
        _expectedShape = ComponentRegistry.Get(Component.Type)?.ExpectedScriptShape
                         ?? "See component documentation for the expected return shape.";
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await DataProvider.EnsureLoadedAsync();
            await EvaluateNow();
            await Task.Delay(200);
            await InitializeMonacoAsync();
        }
    }

    private async Task InitializeMonacoAsync()
    {
        if (_monacoInitialized || _disposed)
            return;

        try
        {
            var libs = DashboardScriptTypings.Build(Component.Type, DataProvider.Current);
            var libPayload = libs.Select(l => new { content = l.Content, filePath = l.FilePath }).ToArray();
            await JsRuntime.InvokeVoidAsync("MonacoIntegration.setJavascriptExtraLibs", libPayload);

            var options = new
            {
                value = Component.DataBinding.InlineScript ?? string.Empty,
                language = "javascript",
                theme = "vs-dark",
                readOnly = false,
                automaticLayout = true,
                isDialog = true
            };

            await JsRuntime.InvokeVoidAsync("MonacoIntegration.initialize", _editorId, options);

            _dotNetRef = DotNetObjectReference.Create(this);
            // Do not sync on every keystroke — that interrupts IntelliSense when typing '.'.
            // Sync + evaluate on blur, Evaluate, and Close instead.
            await JsRuntime.InvokeVoidAsync(
                "MonacoIntegration.onDidBlurEditorText",
                _editorId,
                _dotNetRef,
                nameof(OnMonacoBlurred));

            _monacoInitialized = true;

            await Task.Delay(100);
            await JsRuntime.InvokeVoidAsync("MonacoIntegration.resize", _editorId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize dashboard script Monaco editor: {ex.Message}");
        }
    }

    [JSInvokable]
    public async Task OnMonacoBlurred()
    {
        if (!_monacoInitialized || _disposed)
            return;

        await SyncScriptFromMonacoAsync(evaluate: true, notifyParent: true);
    }

    private async Task SyncScriptFromMonacoAsync(bool evaluate, bool notifyParent)
    {
        try
        {
            var value = await JsRuntime.InvokeAsync<string>("MonacoIntegration.getValue", _editorId);
            Component.DataBinding.InlineScript = value;

            if (notifyParent)
                await OnMutated.InvokeAsync();

            if (evaluate)
                await EvaluateNow();
        }
        catch (JSException)
        {
            // Dialog may be closing.
        }
    }

    private async Task OnSuggestedScriptApplied(string script)
    {
        if (_monacoInitialized)
        {
            try
            {
                await JsRuntime.InvokeVoidAsync("MonacoIntegration.setValue", _editorId, script ?? string.Empty);
            }
            catch (JSException)
            {
                // Editor may not be ready.
            }
        }

        await EvaluateNow();
    }

    private async Task OnFormMutated(bool needsReEvaluate)
    {
        await OnMutated.InvokeAsync();

        // Defer preview re-render for continuous typing (config JSON); discrete controls pass true.
        if (needsReEvaluate)
            await EvaluateNow();
    }

    private async Task DisposeMonacoAsync()
    {
        if (!_monacoInitialized)
            return;

        try
        {
            await JsRuntime.InvokeVoidAsync("MonacoIntegration.dispose", _editorId);
        }
        catch
        {
            // Ignore dispose failures.
        }

        _monacoInitialized = false;
        _dotNetRef?.Dispose();
        _dotNetRef = null;
    }

    private Task OnConfigJsonChanged(string value)
    {
        _configJson = value;
        return Task.CompletedTask;
    }

    private async Task EvaluateAndNotify()
    {
        if (_monacoInitialized)
            await SyncScriptFromMonacoAsync(evaluate: false, notifyParent: false);

        await EvaluateNow();
        await OnMutated.InvokeAsync();
    }

    private async Task FormatScriptAsync()
    {
        if (_monacoInitialized)
            await SyncScriptFromMonacoAsync(evaluate: false, notifyParent: false);

        var script = Component.DataBinding.InlineScript ?? string.Empty;
        if (string.IsNullOrWhiteSpace(script))
            return;

        var formatted = ScriptBeautifier.FormatScript(script);
        Component.DataBinding.InlineScript = formatted;

        if (_monacoInitialized)
        {
            try
            {
                await JsRuntime.InvokeVoidAsync("MonacoIntegration.setValue", _editorId, formatted);
            }
            catch (JSException)
            {
                // Editor may not be ready.
            }
        }

        await OnMutated.InvokeAsync();
    }

    private async Task CopyAiPromptAsync()
    {
        if (_monacoInitialized)
            await SyncScriptFromMonacoAsync(evaluate: false, notifyParent: false);

        await DataProvider.EnsureLoadedAsync();

        var descriptor = ComponentRegistry.Get(Component.Type);
        var prompt = DashboardScriptAiPromptBuilder.Build(
            Component.Type,
            descriptor?.DisplayName,
            descriptor?.ExpectedScriptShape ?? _expectedShape,
            Component.DataBinding.InlineScript,
            DataProvider.Current);

        try
        {
            await JsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", prompt);
            NotificationService.Notify(NotificationFactory.Success(
                "Copied",
                "AI prompt copied. Add what you want shown, then paste into your AI tool."));
        }
        catch (Exception)
        {
            NotificationService.Notify(NotificationFactory.Warning(
                "Copy failed",
                "Could not write to the clipboard. Check browser permissions and try again."));
        }
    }

    private async Task EvaluateNow()
    {
        await DataProvider.EnsureLoadedAsync();
        Runtime.SetDefinition(Definition);
        _results = Runtime.Evaluate();

        if (_results.TryGetValue(Component.Id, out var result))
        {
            _componentResult = result;
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
        else
        {
            _componentResult = null;
            _previewText = null;
            _previewError = "No evaluation result for this component.";
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task Close()
    {
        if (_monacoInitialized)
            await SyncScriptFromMonacoAsync(evaluate: false, notifyParent: true);
        else
            await OnMutated.InvokeAsync();

        DialogService.Close();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (_monacoInitialized)
            await DisposeMonacoAsync();

        _dotNetRef?.Dispose();
        _dotNetRef = null;
    }
}
