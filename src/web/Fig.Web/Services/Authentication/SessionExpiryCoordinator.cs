using Fig.Common.Timer;
using Fig.Web.Models.Authentication;
using Microsoft.Extensions.Options;

namespace Fig.Web.Services.Authentication;

public sealed class SessionExpiryCoordinator : ISessionExpiryCoordinator
{
    private static readonly TimeSpan MonitorInterval = TimeSpan.FromSeconds(15);

    private readonly WebAuthMode _authenticationMode;
    private readonly ILocalStorageService _localStorageService;
    private readonly ITimerFactory _timerFactory;
    private readonly object _sync = new();

    private Fig.Common.Timer.ITimer? _timer;
    private bool _reauthenticationPending;
    private bool _logoutOnAbandon;
    private bool _warningRaisedForCurrentToken;
    private string? _monitoredToken;
    private TimeSpan _warningLead = TimeSpan.FromMinutes(2);
    private bool _disposed;

    public SessionExpiryCoordinator(
        IOptions<WebSettings> webSettings,
        ILocalStorageService localStorageService,
        ITimerFactory timerFactory)
    {
        _authenticationMode = webSettings.Value.Authentication.Mode;
        _localStorageService = localStorageService;
        _timerFactory = timerFactory;
    }

    public bool IsReauthenticationPending
    {
        get
        {
            lock (_sync)
                return _reauthenticationPending;
        }
    }

    public bool LogoutOnAbandon
    {
        get
        {
            lock (_sync)
                return _logoutOnAbandon;
        }
    }

    public event Action? ReauthenticationRequired;

    public event Action? SessionExpiringSoon;

    public void NotifySessionExpired() => BeginReauthentication(logoutOnAbandon: true);

    public void RequestReauthentication() => BeginReauthentication(logoutOnAbandon: false);

    public void MarkReauthenticationCompleted()
    {
        lock (_sync)
        {
            _reauthenticationPending = false;
            _logoutOnAbandon = false;
            _warningRaisedForCurrentToken = false;
            _monitoredToken = null;
        }

        StartMonitoring();
    }

    public void MarkReauthenticationAbandoned()
    {
        var shouldStopMonitoring = LogoutOnAbandon;
        lock (_sync)
        {
            _reauthenticationPending = false;
            _logoutOnAbandon = false;
        }

        if (shouldStopMonitoring)
            StopMonitoring();
    }

    public void StartMonitoring()
    {
        if (_authenticationMode != WebAuthMode.FigManaged || _disposed)
            return;

        _ = StartMonitoringAsync();
    }

    public void StopMonitoring()
    {
        lock (_sync)
        {
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
            _monitoredToken = null;
            _warningRaisedForCurrentToken = false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        StopMonitoring();
    }

    internal async Task CheckExpiryAsync()
    {
        if (_authenticationMode != WebAuthMode.FigManaged || _disposed)
            return;

        bool pending;
        lock (_sync)
            pending = _reauthenticationPending;

        if (pending)
            return;

        var user = await _localStorageService.GetItem<AuthenticatedUserModel>(
            WebAuthenticationConstants.AuthenticatedUserStorageKey);
        var token = user?.Token;
        if (string.IsNullOrWhiteSpace(token) || !JwtTokenHelper.TryGetExpiry(token, out var expiry))
            return;

        EnsureWarningLeadConfigured(token, expiry);

        var remaining = expiry - DateTimeOffset.UtcNow;
        if (remaining <= TimeSpan.Zero)
            return;

        bool shouldWarn;
        lock (_sync)
        {
            shouldWarn = !_warningRaisedForCurrentToken && remaining <= _warningLead;
            if (shouldWarn)
                _warningRaisedForCurrentToken = true;
        }

        if (shouldWarn)
            SessionExpiringSoon?.Invoke();
    }

    private async Task StartMonitoringAsync()
    {
        var user = await _localStorageService.GetItem<AuthenticatedUserModel>(
            WebAuthenticationConstants.AuthenticatedUserStorageKey);
        var token = user?.Token;
        if (string.IsNullOrWhiteSpace(token) || !JwtTokenHelper.TryGetExpiry(token, out var expiry))
        {
            StopMonitoring();
            return;
        }

        lock (_sync)
        {
            if (_disposed)
                return;

            if (_monitoredToken == token && _timer is not null)
                return;

            _timer?.Stop();
            _timer?.Dispose();

            _monitoredToken = token;
            _warningRaisedForCurrentToken = false;
            ConfigureWarningLead(token, expiry);

            _timer = _timerFactory.Create(CheckExpiryAsync, MonitorInterval);
            _timer.Start();
        }

        await CheckExpiryAsync();
    }

    private void EnsureWarningLeadConfigured(string token, DateTimeOffset expiry)
    {
        lock (_sync)
        {
            if (_monitoredToken == token)
                return;

            _monitoredToken = token;
            _warningRaisedForCurrentToken = false;
            ConfigureWarningLead(token, expiry);
        }
    }

    private void ConfigureWarningLead(string token, DateTimeOffset expiry)
    {
        var now = DateTimeOffset.UtcNow;
        TimeSpan lifetime;
        if (JwtTokenHelper.TryGetIssuedAt(token, out var issuedAt) && issuedAt < expiry)
            lifetime = expiry - issuedAt;
        else
            lifetime = expiry > now ? expiry - now : TimeSpan.Zero;

        _warningLead = JwtTokenHelper.GetWarningLead(lifetime);
    }

    private void BeginReauthentication(bool logoutOnAbandon)
    {
        if (_authenticationMode != WebAuthMode.FigManaged)
            return;

        lock (_sync)
        {
            if (_reauthenticationPending)
                return;

            _reauthenticationPending = true;
            _logoutOnAbandon = logoutOnAbandon ||
                               (!string.IsNullOrWhiteSpace(_monitoredToken) &&
                                JwtTokenHelper.IsExpired(_monitoredToken));
        }

        ReauthenticationRequired?.Invoke();
    }
}
