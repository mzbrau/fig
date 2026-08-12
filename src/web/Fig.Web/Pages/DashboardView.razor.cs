using System.Text;
using Fig.Contracts.Authentication;
using Fig.Contracts.Dashboards;
using Fig.Web.Dashboards.Export;
using Fig.Web.Dashboards.Facades;
using Fig.Web.Dashboards.Runtime;
using Fig.Web.Notifications;
using Fig.Web.Services;
using Fig.Web.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;

namespace Fig.Web.Pages;

public partial class DashboardView : IDisposable
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IDashboardFacade DashboardFacade { get; set; } = null!;
    [Inject] private IDashboardDataProvider DataProvider { get; set; } = null!;
    [Inject] private DashboardRuntime Runtime { get; set; } = null!;
    [Inject] private DashboardHtmlExporter HtmlExporter { get; set; } = null!;
    [Inject] private IAccountService AccountService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private INotificationFactory NotificationFactory { get; set; } = null!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;

    private ElementReference _viewRoot;
    private DashboardRefreshManager? _refreshManager;
    private DashboardDataContract? _dashboard;
    private Dictionary<string, DashboardComponentResult> _results = new(StringComparer.OrdinalIgnoreCase);
    private bool _loading = true;
    private bool _wallboard;
    private bool _refreshingStatus;
    private bool _refreshingSettings;
    private bool _exporting;
    private DotNetObjectReference<DashboardView>? _dotNetRef;
    private bool _escapeBound;

    private bool IsAdmin => AccountService.AuthenticatedUser?.Role == Role.Administrator;

    protected override async Task OnParametersSetAsync()
    {
        ReadWallboardFromQuery();
        await LoadAndEvaluate();
        await base.OnParametersSetAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_wallboard && !_escapeBound)
        {
            _dotNetRef ??= DotNetObjectReference.Create(this);
            try
            {
                await JsRuntime.InvokeVoidAsync("figBindDocumentKey", "Escape", _dotNetRef, nameof(ExitWallboardFromJs));
                _escapeBound = true;
            }
            catch (JSException)
            {
                // Ignore if helpers are unavailable.
            }
        }
        else if (!_wallboard && _escapeBound)
        {
            await UnbindEscape();
        }
    }

    private void ReadWallboardFromQuery()
    {
        var uri = new Uri(NavigationManager.Uri);
        var query = ParseQuery(uri.Query);
        if (query.TryGetValue("wallboard", out var value))
        {
            _wallboard = value == "1" ||
                         string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            _wallboard = false;
        }
    }

    private async Task LoadAndEvaluate()
    {
        _loading = true;
        _refreshManager?.Dispose();
        _refreshManager = null;
        try
        {
            _dashboard = await DashboardFacade.Get(Id);
            if (_dashboard is null)
                return;

            await DataProvider.EnsureLoadedAsync();
            Runtime.SetDefinition(_dashboard.Definition);
            _results = Runtime.Evaluate();

            _refreshManager = new DashboardRefreshManager(DataProvider);
            _refreshManager.Start(_dashboard.Definition.Refresh, OnAutoRefreshAsync);
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task OnAutoRefreshAsync()
    {
        if (_dashboard is null)
            return;

        await InvokeAsync(() =>
        {
            Runtime.SetDefinition(_dashboard.Definition);
            _results = Runtime.Evaluate();
            StateHasChanged();
            return Task.CompletedTask;
        });
    }

    private async Task RefreshStatus()
    {
        _refreshingStatus = true;
        try
        {
            await DataProvider.RefreshStatusAsync();
            ReEvaluate();
            NotificationService.Notify(NotificationFactory.Info("Status refreshed", "Run session data updated."));
        }
        finally
        {
            _refreshingStatus = false;
        }
    }

    private async Task RefreshSettings()
    {
        _refreshingSettings = true;
        try
        {
            await DataProvider.RefreshSettingsAsync();
            ReEvaluate();
            NotificationService.Notify(NotificationFactory.Info("Settings refreshed", "Client settings data updated."));
        }
        finally
        {
            _refreshingSettings = false;
        }
    }

    private void ReEvaluate()
    {
        if (_dashboard is null)
            return;
        Runtime.SetDefinition(_dashboard.Definition);
        _results = Runtime.Evaluate();
    }

    private void ToggleWallboard()
    {
        var uri = new Uri(NavigationManager.Uri);
        var query = ParseQuery(uri.Query);

        if (_wallboard)
            query.Remove("wallboard");
        else
            query["wallboard"] = "1";

        var next = uri.AbsolutePath;
        if (query.Count > 0)
        {
            next += "?" + string.Join("&", query.Select(kvp =>
                $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
        }

        NavigationManager.NavigateTo(next);
    }

    private void OnViewKeyDown(KeyboardEventArgs args)
    {
        if (_wallboard && args.Key == "Escape")
            ToggleWallboard();
    }

    [JSInvokable]
    public Task ExitWallboardFromJs()
    {
        if (_wallboard)
            ToggleWallboard();
        return Task.CompletedTask;
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(query))
            return result;

        var trimmed = query.StartsWith('?') ? query[1..] : query;
        foreach (var part in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = part.IndexOf('=');
            if (idx < 0)
            {
                result[Uri.UnescapeDataString(part)] = string.Empty;
                continue;
            }

            var key = Uri.UnescapeDataString(part[..idx]);
            var value = Uri.UnescapeDataString(part[(idx + 1)..]);
            result[key] = value;
        }

        return result;
    }

    private async Task ExportHtml()
    {
        if (_dashboard is null)
            return;

        _exporting = true;
        try
        {
            var html = HtmlExporter.Export(_dashboard, _results);
            var bytes = Encoding.UTF8.GetBytes(html);
            var safeName = string.Join("_", (_dashboard.Name ?? "dashboard").Split(Path.GetInvalidFileNameChars()));
            await FileUtil.SaveAs(JsRuntime, $"FigDashboard-{safeName}-{DateTime.Now:yyyy-MM-ddTHH-mm-ss}.html", bytes);
        }
        finally
        {
            _exporting = false;
        }
    }

    private void GoToEdit() => NavigationManager.NavigateTo($"/dashboards/{Id}/edit");

    private void GoToList() => NavigationManager.NavigateTo("/dashboards");

    private static string FormatCache(DateTime? utc) =>
        utc is null ? "never" : utc.Value.ToLocalTime().ToString("g");

    private async Task UnbindEscape()
    {
        try
        {
            await JsRuntime.InvokeVoidAsync("figUnbindDocumentKey", "Escape");
        }
        catch (JSException)
        {
        }

        _escapeBound = false;
    }

    public void Dispose()
    {
        _refreshManager?.Dispose();
        _refreshManager = null;
        if (_escapeBound)
        {
            try
            {
                _ = JsRuntime.InvokeVoidAsync("figUnbindDocumentKey", "Escape");
            }
            catch
            {
                // Dispose path; ignore.
            }

            _escapeBound = false;
        }

        _dotNetRef?.Dispose();
        _dotNetRef = null;
    }
}
