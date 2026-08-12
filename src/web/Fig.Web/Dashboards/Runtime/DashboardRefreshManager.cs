using Fig.Contracts.Dashboards;

namespace Fig.Web.Dashboards.Runtime;

/// <summary>
/// Schedules status/settings refreshes according to a dashboard refresh policy.
/// Status interval is clamped to ≥ 60s; settings to ≥ 600s.
/// </summary>
public sealed class DashboardRefreshManager : IDisposable
{
    public const int MinStatusSeconds = 60;
    public const int MinSettingsSeconds = 600;

    private readonly IDashboardDataProvider _dataProvider;
    private System.Threading.Timer? _statusTimer;
    private System.Threading.Timer? _settingsTimer;
    private Func<Task>? _onRefreshed;
    private int _statusBusy;
    private int _settingsBusy;
    private bool _disposed;

    public DashboardRefreshManager(IDashboardDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public void Start(DashboardRefreshDataContract? policy, Func<Task> onRefreshed)
    {
        Stop();
        _onRefreshed = onRefreshed ?? throw new ArgumentNullException(nameof(onRefreshed));

        var statusSeconds = Math.Max(MinStatusSeconds, policy?.StatusSeconds ?? MinStatusSeconds);
        var settingsSeconds = Math.Max(MinSettingsSeconds, policy?.SettingsSeconds ?? MinSettingsSeconds);

        var statusPeriod = TimeSpan.FromSeconds(statusSeconds);
        var settingsPeriod = TimeSpan.FromSeconds(settingsSeconds);

        _statusTimer = new System.Threading.Timer(
            async _ => await TickStatusAsync(),
            null,
            statusPeriod,
            statusPeriod);

        _settingsTimer = new System.Threading.Timer(
            async _ => await TickSettingsAsync(),
            null,
            settingsPeriod,
            settingsPeriod);
    }

    public void Stop()
    {
        _statusTimer?.Dispose();
        _settingsTimer?.Dispose();
        _statusTimer = null;
        _settingsTimer = null;
        _onRefreshed = null;
    }

    private async Task TickStatusAsync()
    {
        if (_disposed || Interlocked.Exchange(ref _statusBusy, 1) == 1)
            return;

        try
        {
            await _dataProvider.RefreshStatusAsync();
            var callback = _onRefreshed;
            if (callback is not null)
                await callback();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Dashboard status refresh failed: {ex.Message}");
        }
        finally
        {
            Interlocked.Exchange(ref _statusBusy, 0);
        }
    }

    private async Task TickSettingsAsync()
    {
        if (_disposed || Interlocked.Exchange(ref _settingsBusy, 1) == 1)
            return;

        try
        {
            await _dataProvider.RefreshSettingsAsync();
            var callback = _onRefreshed;
            if (callback is not null)
                await callback();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Dashboard settings refresh failed: {ex.Message}");
        }
        finally
        {
            Interlocked.Exchange(ref _settingsBusy, 0);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Stop();
    }
}
