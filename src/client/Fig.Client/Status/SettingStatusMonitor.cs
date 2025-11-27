using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Fig.Client.Configuration;
using Fig.Client.ConfigurationProvider;
using Fig.Client.Contracts;
using Fig.Client.Events;
using Fig.Client.Health;
using Fig.Client.Versions;
using Fig.Common.NetStandard.Constants;
using Fig.Common.NetStandard.Diag;
using Fig.Common.NetStandard.IpAddress;
using Fig.Contracts.Health;
using Fig.Contracts.Status;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fig.Client.Status;

internal class SettingStatusMonitor : ISettingStatusMonitor
{
    private readonly IDiagnostics _diagnostics;
    private readonly IFigConfigurationSource _config;
    private readonly IIpAddressResolver _ipAddressResolver;
    private readonly DateTime _startTime;
    private readonly Timer _statusTimer;
    private readonly IVersionProvider _versionProvider;
    private readonly HttpClient _httpClient;
    private readonly IClientSecretProvider _clientSecretProvider;
    private readonly ILogger<SettingStatusMonitor> _logger;
    private bool _isOffline;
    private DateTime _lastSettingUpdate;
    private bool _disposed = false;
    private string? _failedRegistrationMessage = null;
    private bool _hasReportedExternallyManagedSettings = false;

    public SettingStatusMonitor(
        IIpAddressResolver ipAddressResolver, 
        IVersionProvider versionProvider,
        IDiagnostics diagnostics,
        IHttpClientFactory httpClientFactory,
        IFigConfigurationSource config,
        IClientSecretProvider clientSecretProvider,
        ILogger<SettingStatusMonitor> logger)
    {
        _ipAddressResolver = ipAddressResolver;
        _versionProvider = versionProvider;
        _diagnostics = diagnostics;
        _config = config;
        _clientSecretProvider = clientSecretProvider;
        _logger = logger;
        _startTime = DateTime.UtcNow;
        _lastSettingUpdate = DateTime.UtcNow;
        _statusTimer = new Timer();
        _statusTimer.Interval = _config.PollIntervalMs;
        _httpClient = httpClientFactory.CreateClient(HttpClientNames.FigApi);
        _statusTimer.Elapsed += OnStatusTimerElapsed;
    }

    public event EventHandler<ChangedSettingsEventArgs>? SettingsChanged;
    public event EventHandler? ReconnectedToApi;
    public event EventHandler? OfflineSettingsDisabled;
    public event EventHandler? RestartRequested;

    public bool AllowOfflineSettings { get; private set; } = true;

    public void Initialize()
    {
        try
        {
            GetStatus().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            if (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogError("Failed to sync status with the Fig API {ExceptionMessage}", ex.Message);
            }
            else
            {
                _logger.LogError(ex, "Failed to sync status with the Fig API");
            }
            
            _isOffline = true;
        }
        finally
        {
            if (!_disposed)
                _statusTimer.Start();
        }
    }

    public void SettingsUpdated()
    {
        _lastSettingUpdate = DateTime.UtcNow;
    }

    public async Task SyncStatus()
    {
        _statusTimer.Stop();
        try
        {
            await GetStatus();
        }
        finally
        {
            if (!_disposed)
                _statusTimer.Start();
        }
    }

    public void SetFailedRegistration(string message)
    {
        _failedRegistrationMessage = message;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _statusTimer.Stop();
            _statusTimer.Dispose();
            _httpClient.Dispose();
        }
    }

    private async void OnStatusTimerElapsed(object sender, ElapsedEventArgs e)
    {
        _statusTimer.Stop();
        try
        {
            await GetStatus();

            if (_isOffline)
            {
                _logger.LogInformation("Reconnected to Fig API");
                ReconnectedToApi?.Invoke(this, EventArgs.Empty);
            }

            _isOffline = false;
        }
        catch (Exception ex)
        {
            if (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogError("Failed to sync status with the Fig API {ExceptionMessage}", ex.Message);
            }
            else
            {
                _logger.LogError(ex, "Failed to sync status with the Fig API");
            }
            
            _isOffline = true;
            _statusTimer.Interval = _config.PollIntervalMs * 5; // try less often if we are offline
        }
        finally
        {
            if (!_disposed)
                _statusTimer.Start();
        }
    }

    private async Task GetStatus()
    {
        HealthDataContract? healthReport = null;
        if (HealthCheckBridge.GetHealthReportAsync != null)
        {
            healthReport = await HealthCheckBridge.GetHealthReportAsync();
        }

        if (!string.IsNullOrEmpty(_failedRegistrationMessage) && healthReport is not null)
        {
            healthReport.Components.Add(new ComponentHealthDataContract("Registration",
                FigHealthStatus.Unhealthy,
                _failedRegistrationMessage));
            healthReport.Status = FigHealthStatus.Unhealthy;
        }

        var offlineSettingsEnabled = _config.AllowOfflineSettings && AllowOfflineSettings;
        
        // Get externally managed settings only once per session
        List<ExternallyManagedSettingDataContract>? externallyManagedSettings = null;
        if (!_hasReportedExternallyManagedSettings && ExternallyManagedSettingsBridge.ExternallyManagedSettings != null)
        {
            externallyManagedSettings = ExternallyManagedSettingsBridge.ExternallyManagedSettings;
            _hasReportedExternallyManagedSettings = true;
            _logger.LogInformation("Reporting {Count} externally managed setting(s) to Fig API", 
                externallyManagedSettings.Count);
        }
        
        var request = new StatusRequestDataContract(RunSession.GetId(_config.ClientName),
            _startTime,
            _lastSettingUpdate,
            _statusTimer.Interval,
            _versionProvider.GetFigVersion(),
            _versionProvider.GetHostVersion(),
            offlineSettingsEnabled,
            RestartStore.SupportsRestart,
            _diagnostics.GetRunningUser(),
            _diagnostics.GetMemoryUsageBytes(),
            healthReport,
            externallyManagedSettings);
        
        var json = JsonConvert.SerializeObject(request);
        var data = new StringContent(json, Encoding.UTF8, "application/json");
        var secret = await _clientSecretProvider.GetSecret(_config.ClientName);
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Fig_IpAddress", _ipAddressResolver.Resolve());
        _httpClient.DefaultRequestHeaders.Add("Fig_Hostname", Environment.MachineName);
        _httpClient.DefaultRequestHeaders.Add("clientSecret", secret);
        
        var  uri = $"/statuses/{Uri.EscapeDataString(_config.ClientName)}";
        if (_config.Instance != null)
            uri += $"?instance={Uri.EscapeDataString(_config.Instance)}";

        if (_disposed)
            return;

        try
        {
            var response = await _httpClient.PutAsync(uri, data);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get status from Fig API. {StatusCode}", response.StatusCode);
                return;
            }

            var result = await response.Content.ReadAsStringAsync();
            var statusResponse = JsonConvert.DeserializeObject<StatusResponseDataContract>(result);
            ProcessResponse(statusResponse);
        }
        catch (TaskCanceledException) when (_disposed)
        {
            // Suppress expected cancellation if we are shutting down
        }
    }

    private void ProcessResponse(StatusResponseDataContract? statusResponse)
    {
        if (statusResponse is null)
            return;

        _statusTimer.Interval = statusResponse.PollIntervalMs ?? 30000;

        if (statusResponse.SettingUpdateAvailable)
        {
            SettingsChanged?.Invoke(this, new ChangedSettingsEventArgs(statusResponse.ChangedSettings ?? Array.Empty<string>().ToList()));
        }
        
        AllowOfflineSettings = statusResponse.AllowOfflineSettings;
        if (!AllowOfflineSettings)
            OfflineSettingsDisabled?.Invoke(this, EventArgs.Empty);

        if (statusResponse.RestartRequested)
            RestartRequested?.Invoke(this, EventArgs.Empty);
    }
}