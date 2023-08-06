using Fig.Web.Facades;
using Fig.Web.Models.Api;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor;
using Timer = System.Timers.Timer;

namespace Fig.Web.Pages;

public partial class ApiStatus
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
        _refreshTimer.Elapsed += (_, _) => StateHasChanged();
        _refreshTimer.Start();
    }
}