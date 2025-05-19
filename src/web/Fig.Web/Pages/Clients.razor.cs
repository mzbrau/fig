using Fig.Contracts.Authentication;
using Fig.Web.Facades;
using Fig.Web.Models.Clients;
using Fig.Web.Notifications;
using Fig.Web.Services;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using Timer = System.Timers.Timer;

namespace Fig.Web.Pages;

public partial class Clients
{
    private bool _isRefreshInProgress;

    private DateTime _lastRefreshed;

    private Timer? _refreshTimer;

    private RadzenDataGrid<ClientRunSessionModel> _clientsGrid = default!;
    private bool _isLiveUpdateAllInProgress;
    private bool _isLiveUpdateNoneInProgress;

    [Inject]
    private IClientStatusFacade ClientStatusFacade { get; set; } = null!;

    [Inject] 
    private IAccountService AccountService { get; set; } = null!;
    
    [Inject]
    private NotificationService NotificationService { get; set; } = null!;

    [Inject]
    private INotificationFactory NotificationFactory { get; set; } = null!;

    private bool IsReadOnly => AccountService.AuthenticatedUser?.Role == Role.ReadOnly;

    private string LastRefreshedRelative => _lastRefreshed.Humanize();

    private List<ClientRunSessionModel> ClientRunSessions => ClientStatusFacade.ClientRunSessions;

    protected override async Task OnInitializedAsync()
    {
        SetupRefreshTimer();

        _isRefreshInProgress = true;
        await ClientStatusFacade.Refresh();
        await _clientsGrid.Reload();
        _lastRefreshed = DateTime.Now;
        _isRefreshInProgress = false;
        await base.OnInitializedAsync();
    }

    private async Task OnRefresh()
    {
        _isRefreshInProgress = true;
        await ClientStatusFacade.Refresh();
        await _clientsGrid.Reload();
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

    private async Task RequestRestart(ClientRunSessionModel client)
    {
        try
        {
            await ClientStatusFacade.RequestRestart(client);
            client.RestartRequested = true;
        }
        catch (Exception e)
        {
            NotificationService.Notify(NotificationFactory.Failure("Failed to request restart", e.Message));
        }
    }

    private async Task SetLiveReload(ClientRunSessionModel client)
    {
        try
        {
            await ClientStatusFacade.SetLiveReload(client);
        }
        catch (Exception e)
        {
            NotificationService.Notify(NotificationFactory.Failure("Failed to set live reload status", e.Message));
        }
    }

    private async Task LiveReloadAll()
    {
        _isLiveUpdateAllInProgress = true;
        foreach (var session in ClientRunSessions.Where(a => !a.LiveReload))
        {
            session.LiveReload = true;
            await SetLiveReload(session);
        }
            
        _isLiveUpdateAllInProgress = false;
    }

    private async Task LiveReloadNone()
    {
        _isLiveUpdateNoneInProgress = true;
        foreach (var session in ClientRunSessions.Where(a => a.LiveReload))
        {
            session.LiveReload = false;
            await SetLiveReload(session);
        }
            
        _isLiveUpdateNoneInProgress = false;
    }
    
    private void RowRender(RowRenderEventArgs<ClientRunSessionModel> args)
    {
        args.Expandable = args.Data.Health.Components.Any();
    }
}