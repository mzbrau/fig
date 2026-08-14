using System.Text.RegularExpressions;
using Fig.Common.Events;
using Fig.Web.Events;
using Fig.Web.Facades;
using Fig.Web.Models.Clients;
using Fig.Web.Models.Setting;
using Fig.Web.Services;

namespace Fig.Web.Dashboards.Runtime;

public class DashboardDataProvider : IDashboardDataProvider
{
    private readonly IClientStatusFacade _clientStatusFacade;
    private readonly ISettingClientFacade _settingClientFacade;
    private readonly IAccountService _accountService;

    private List<DashboardClientJsModel> _settingsCache = new();
    private List<DashboardRunSessionJsModel> _statusCache = new();

    public DashboardDataProvider(
        IClientStatusFacade clientStatusFacade,
        ISettingClientFacade settingClientFacade,
        IAccountService accountService,
        IEventDistributor eventDistributor)
    {
        _clientStatusFacade = clientStatusFacade;
        _settingClientFacade = settingClientFacade;
        _accountService = accountService;
        eventDistributor.Subscribe(EventConstants.LogoutEvent, ClearCaches);
    }

    public DashboardFigRoot Current { get; private set; } = new();

    public DateTime? SettingsLastRefreshUtc { get; private set; }

    public DateTime? StatusLastRefreshUtc { get; private set; }

    public async Task RefreshStatusAsync()
    {
        await _clientStatusFacade.Refresh();
        _statusCache = ProjectRunSessions(_clientStatusFacade.ClientRunSessions).ToList();
        StatusLastRefreshUtc = DateTime.UtcNow;
        RebuildCurrent();
    }

    public async Task RefreshSettingsAsync()
    {
        await _settingClientFacade.LoadAllClients(initializeScripts: false);
        _settingsCache = ProjectClients(_settingClientFacade.SettingClients).ToList();
        SettingsLastRefreshUtc = DateTime.UtcNow;
        RebuildCurrent();
    }

    public async Task RefreshAllAsync()
    {
        await RefreshStatusAsync();
        await RefreshSettingsAsync();
    }

    public async Task EnsureLoadedAsync()
    {
        if (StatusLastRefreshUtc is null)
            await RefreshStatusAsync();

        if (SettingsLastRefreshUtc is null)
            await RefreshSettingsAsync();
    }

    private void ClearCaches()
    {
        _settingsCache = new();
        _statusCache = new();
        SettingsLastRefreshUtc = null;
        StatusLastRefreshUtc = null;
        Current = new DashboardFigRoot();
    }

    private void RebuildCurrent()
    {
        Current = new DashboardFigRoot
        {
            clients = new DashboardJsArray(_settingsCache.Cast<object?>()),
            runSessions = new DashboardJsArray(_statusCache.Cast<object?>())
        };
    }

    private IEnumerable<DashboardRunSessionJsModel> ProjectRunSessions(IEnumerable<ClientRunSessionModel> sessions)
    {
        var filter = GetClientFilterRegex();
        foreach (var session in sessions)
        {
            if (!MatchesClientFilter(session.Name, filter))
                continue;
            yield return DashboardFigApiMapper.ToJsModel(session);
        }
    }

    private IEnumerable<DashboardClientJsModel> ProjectClients(IEnumerable<SettingClientConfigurationModel> clients)
    {
        var filter = GetClientFilterRegex();
        var allowed = _accountService.AuthenticatedUser?.AllowedClassifications;

        foreach (var client in clients)
        {
            if (client.IsGroup)
                continue;
            if (!MatchesClientFilter(client.Name, filter))
                continue;
            yield return DashboardFigApiMapper.ToJsModel(client, allowed);
        }
    }

    private Regex GetClientFilterRegex()
    {
        var pattern = _accountService.AuthenticatedUser?.ClientFilter;
        if (string.IsNullOrWhiteSpace(pattern))
            pattern = ".*";

        try
        {
            return new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        }
        catch (ArgumentException)
        {
            return new Regex(".*", RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        }
    }

    private static bool MatchesClientFilter(string clientName, Regex filter)
    {
        try
        {
            return filter.IsMatch(clientName);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}
