using Fig.Contracts.Dashboards;
using Fig.Web.Dashboards.Components;
using Fig.Web.Dashboards.Runtime;
using Fig.Web.Dashboards.Scripting;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Radzen;

namespace Fig.Web.Pages.Dialogs;

public partial class DashboardComponentEditDialog : IAsyncDisposable
{
    private readonly string _editorId = $"dashboard-script-editor-{Guid.NewGuid():N}";
    private DotNetObjectReference<DashboardComponentEditDialog>? _dotNetRef;
    private CancellationTokenSource? _debounceCts;
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

    private bool IsInlineMode =>
        string.Equals(Component.DataBinding.Mode, "inline", StringComparison.OrdinalIgnoreCase)
        || string.IsNullOrWhiteSpace(Component.DataBinding.Mode);

    private IReadOnlyList<DashboardTransformDataContract> TransformList =>
        Definition.Transforms ?? (IReadOnlyList<DashboardTransformDataContract>)Array.Empty<DashboardTransformDataContract>();

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
            if (IsInlineMode)
            {
                await Task.Delay(200);
                await InitializeMonacoAsync();
            }
        }
    }

    private async Task InitializeMonacoAsync()
    {
        if (_monacoInitialized || _disposed)
            return;

        try
        {
            var libs = DashboardScriptTypings.Build(
                Component.Type,
                DataProvider.Current,
                Runtime.NamedTransformResults);

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
            await JsRuntime.InvokeVoidAsync(
                "MonacoIntegration.onDidChangeModelContent",
                _editorId,
                _dotNetRef,
                nameof(OnMonacoContentChanged));

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
    public async Task OnMonacoContentChanged()
    {
        if (!_monacoInitialized || _disposed)
            return;

        try
        {
            var value = await JsRuntime.InvokeAsync<string>("MonacoIntegration.getValue", _editorId);
            Component.DataBinding.InlineScript = value;
            await OnMutated.InvokeAsync();
            await DebouncedEvaluateAsync();
            await InvokeAsync(StateHasChanged);
        }
        catch (JSException)
        {
            // Dialog may be closing.
        }
    }

    private async Task DebouncedEvaluateAsync()
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        try
        {
            await Task.Delay(400, token);
            if (!token.IsCancellationRequested)
                await EvaluateNow();
        }
        catch (TaskCanceledException)
        {
            // Newer keystroke superseded this evaluate.
        }
    }

    private async Task OnFormMutated(bool needsReEvaluate)
    {
        await OnMutated.InvokeAsync();
        if (needsReEvaluate)
            await EvaluateNow();

        if (IsInlineMode)
        {
            if (!_monacoInitialized)
            {
                await InvokeAsync(async () =>
                {
                    StateHasChanged();
                    await Task.Delay(50);
                    await InitializeMonacoAsync();
                });
                return;
            }

            // Keep IntelliSense libs aligned with current component type / live data.
            try
            {
                var libs = DashboardScriptTypings.Build(
                    Component.Type,
                    DataProvider.Current,
                    Runtime.NamedTransformResults);
                var libPayload = libs.Select(l => new { content = l.Content, filePath = l.FilePath }).ToArray();
                await JsRuntime.InvokeVoidAsync("MonacoIntegration.setJavascriptExtraLibs", libPayload);
            }
            catch
            {
                // Non-fatal if typings refresh fails.
            }
        }
        else if (_monacoInitialized)
        {
            await DisposeMonacoAsync();
        }

        await InvokeAsync(StateHasChanged);
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
            // Ignore dispose failures during mode switches.
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

    private void Close() => DialogService.Close();

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;

        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = null;

        if (_monacoInitialized)
            await DisposeMonacoAsync();

        _dotNetRef?.Dispose();
        _dotNetRef = null;
    }
}
