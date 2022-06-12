using Fig.Web.Facades;
using Fig.Web.Models.Clients;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor;
using Timer = System.Timers.Timer;

namespace Fig.Web.Pages;

public partial class Clients
{
    private bool _isRefreshInProgress;

    private DateTime _lastRefreshed;

    private Timer _refreshTimer;

    private RadzenDataGrid<ClientRunSessionModel> clientsGrid;

    [Inject]
    private IClientStatusFacade ClientStatusFacade { get; set; } = null!;

    private string _lastRefreshedRelative => _lastRefreshed.Humanize();

    private List<ClientRunSessionModel> ClientRunSessions => ClientStatusFacade.ClientRunSessions;

    protected override async Task OnInitializedAsync()
    {
        SetupRefreshTimer();

        _isRefreshInProgress = true;
        await ClientStatusFacade.Refresh();
        await clientsGrid.Reload();
        _lastRefreshed = DateTime.Now;
        _isRefreshInProgress = false;
        await base.OnInitializedAsync();
    }

    private async Task OnRefresh()
    {
        _isRefreshInProgress = true;
        await ClientStatusFacade.Refresh();
        await clientsGrid.Reload();
        _lastRefreshed = DateTime.Now;
        _isRefreshInProgress = false;
    }

    private void SetupRefreshTimer()
    {
        _refreshTimer = new Timer();
        _refreshTimer.Interval = 10000;
        _refreshTimer.Elapsed += (sender, args) => StateHasChanged();
        _refreshTimer.Start();
    }

    private async Task RequestRestart(ClientRunSessionModel client)
    {
        await ClientStatusFacade.RequestRestart(client);
        client.RestartRequested = true;
    }
}