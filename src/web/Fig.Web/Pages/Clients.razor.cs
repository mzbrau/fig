using System.Text;
using Fig.Contracts.Authentication;
using Fig.Web.ExtensionMethods;
using Fig.Web.Facades;
using Fig.Web.Models.Clients;
using Fig.Web.Notifications;
using Fig.Web.Services;
using Fig.Web.Utils;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using Timer = System.Timers.Timer;

namespace Fig.Web.Pages;

public partial class Clients : IDisposable
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

    [Inject]
    public IJSRuntime JavascriptRuntime { get; set; } = null!;

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

    private async Task ExportClients()
    {
        var builder = new StringBuilder();

        builder.AppendLine(string.Join(',', new[]
        {
            "Name",
            "Instance",
            "Running Latest",
            "Health",
            "Last Seen",
            "Last Registration Date",
            "Last Registration",
            "Last Setting Change Date",
            "Last Setting Change",
            "RunSessionId",
            "Last Seen Date",
            "Start Time",
            "Poll Interval",
            "Uptime",
            "IpAddress",
            "Hostname",
            "Fig Client Version",
            "Application Version",
            "Offline Settings Enabled",
            "Running User",
            "Memory Usage",
            "Last Setting Reload",
            "Live Reload",
            "Restart Supported",
            "Restart Requested",
            "Restart Required"
        }.Select(header => header.EscapeAndQuote())));

        foreach (var session in ClientRunSessions)
        {
            builder.AppendLine(string.Join(',',
                session.Name.EscapeAndQuote(),
                session.Instance.EscapeAndQuote(),
                session.RunningLatestSettings.ToString().EscapeAndQuote(),
                session.Health.Status.ToString().EscapeAndQuote(),
                session.LastSeenRelative.EscapeAndQuote(),
                FormatDateTime(session.LastRegistration).EscapeAndQuote(),
                session.LastRegistrationRelative.EscapeAndQuote(),
                FormatDateTime(session.LastSettingValueUpdate).EscapeAndQuote(),
                session.LastSettingValueUpdateRelative.EscapeAndQuote(),
                session.RunSessionId.ToString().EscapeAndQuote(),
                FormatDateTime(session.LastSeen).EscapeAndQuote(),
                FormatDateTime(session.StartTimeLocal).EscapeAndQuote(),
                session.PollIntervalHuman.EscapeAndQuote(),
                session.UptimeHuman.EscapeAndQuote(),
                session.IpAddress.EscapeAndQuote(),
                session.Hostname.EscapeAndQuote(),
                session.FigVersion.EscapeAndQuote(),
                session.ApplicationVersion.EscapeAndQuote(),
                session.OfflineSettingsEnabled.ToString().EscapeAndQuote(),
                session.RunningUser.EscapeAndQuote(),
                session.MemoryUsage.EscapeAndQuote(),
                FormatDateTime(session.LastSettingLoadLocal).EscapeAndQuote(),
                session.LiveReload.ToString().EscapeAndQuote(),
                session.SupportsRestart.ToString().EscapeAndQuote(),
                session.RestartRequested.ToString().EscapeAndQuote(),
                session.RestartRequiredToApplySettings.ToString().EscapeAndQuote()));
        }

        var bytes = Encoding.UTF8.GetBytes(builder.ToString());
        await FileUtil.SaveAs(JavascriptRuntime, $"FigClients-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv", bytes);
    }

    private static string? FormatDateTime(DateTime? dateTime)
    {
        return dateTime?.ToString("yyyy-MM-dd HH:mm:ss");
    }
     
    private void RowRender(RowRenderEventArgs<ClientRunSessionModel> args)
    {
        args.Expandable = args.Data.Health.Components.Any();
    }
}
