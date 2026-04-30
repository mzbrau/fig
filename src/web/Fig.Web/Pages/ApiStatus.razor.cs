using Fig.Web.Facades;
using Fig.Web.Models.Api;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor;
using Timer = System.Timers.Timer;

namespace Fig.Web.Pages;

public partial class ApiStatus : IDisposable
{
    private bool _isRefreshInProgress;

    private DateTime _lastRefreshed;

    private Timer? _refreshTimer;

    private RadzenDataGrid<ApiStatusModel> _apiStatusGrid = null!;

    [Inject]
    private IApiStatusFacade ApiStatusFacade { get; set; } = null!;

    private string LastRefreshedRelative => _lastRefreshed.Humanize();

    private List<ApiStatusModel> ApiStatusModels => ApiStatusFacade.ApiStatusModels;

    protected override async Task OnInitializedAsync()
    {
        SetupRefreshTimer();

        _isRefreshInProgress = true;
        await ApiStatusFacade.Refresh();
        await _apiStatusGrid.Reload();
        _lastRefreshed = DateTime.Now;
        _isRefreshInProgress = false;
        await base.OnInitializedAsync();
    }

    private async Task OnRefresh()
    {
        _isRefreshInProgress = true;
        await ApiStatusFacade.Refresh();
        await _apiStatusGrid.Reload();
        _lastRefreshed = DateTime.Now;
        _isRefreshInProgress = false;
    }

    private void SetupRefreshTimer()
    {
        _refreshTimer = new Timer();
        _refreshTimer.Interval = 10000;
        _refreshTimer.Elapsed += OnRefreshTimerElapsed;
        _refreshTimer.Start();
    }

    private void OnRefreshTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        _ = InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        if (_refreshTimer is null)
            return;

        _refreshTimer.Stop();
        _refreshTimer.Elapsed -= OnRefreshTimerElapsed;
        _refreshTimer.Dispose();
        _refreshTimer = null;
    }
}
