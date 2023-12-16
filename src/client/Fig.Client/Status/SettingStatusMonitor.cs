using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Fig.Client.ClientSecret;
using Fig.Client.Configuration;
using Fig.Client.ConfigurationProvider;
using Fig.Client.Events;
using Fig.Client.Versions;
using Fig.Common.NetStandard.Constants;
using Fig.Common.NetStandard.Diag;
using Fig.Common.NetStandard.IpAddress;
using Fig.Contracts.Status;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fig.Client.Status;

internal class SettingStatusMonitor : ISettingStatusMonitor
{
    private readonly IDiagnostics _diagnostics;
    private readonly IFigConfigurationSource _config;
    private readonly IIpAddressResolver _ipAddressResolver;
    private readonly Guid _runSessionId;
    private readonly DateTime _startTime;
    private readonly Timer _statusTimer;
    private readonly IVersionProvider _versionProvider;
    private readonly HttpClient _httpClient;
    private readonly IClientSecretProvider _clientSecretProvider;
    private readonly ILogger<SettingStatusMonitor> _logger;
    private readonly bool _supportsRestart;
    private bool _isOffline;
    private DateTime _lastSettingUpdate;

    public SettingStatusMonitor(
        IIpAddressResolver ipAddressResolver, 
        IVersionProvider versionProvider,
        IDiagnostics diagnostics,
        IHttpClientFactory httpClientFactory,
        IFigConfigurationSource config,
        IClientSecretProvider clientSecretProvider,
        ILogger<SettingStatusMonitor> logger,
        bool supportsRestart)
    {
        _ipAddressResolver = ipAddressResolver;
        _versionProvider = versionProvider;
        _diagnostics = diagnostics;
        _config = config;
        _clientSecretProvider = clientSecretProvider;
        _logger = logger;
        _startTime = DateTime.UtcNow;
        _lastSettingUpdate = DateTime.UtcNow;
        _runSessionId = Guid.NewGuid();
        _statusTimer = new Timer();
        _statusTimer.Interval = _config.PollIntervalMs;
        _httpClient = httpClientFactory.CreateClient(HttpClientNames.FigApi);
        _statusTimer.Elapsed += OnStatusTimerElapsed;
        _supportsRestart = supportsRestart;
    }

    public event EventHandler<ChangedSettingsEventArgs>? SettingsChanged;
    public event EventHandler? ReconnectedToApi;
    public event EventHandler? OfflineSettingsDisabled;
    public event EventHandler? RestartRequested;

    public bool AllowOfflineSettings { get; private set; } = true;

    public Guid RunSessionId => _runSessionId;

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
            _statusTimer.Start();
        }
    }

    public void SettingsUpdated()
    {
        _lastSettingUpdate = DateTime.UtcNow;
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

            _statusTimer.Interval = _config.PollIntervalMs;
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
            _statusTimer.Start();
        }
    }

    private async Task GetStatus()
    {
        var offlineSettingsEnabled = _config.AllowOfflineSettings && AllowOfflineSettings;
        var request = new StatusRequestDataContract(_runSessionId,
            _startTime,
            _lastSettingUpdate,
            _statusTimer.Interval,
            _versionProvider.GetFigVersion(),
            _versionProvider.GetHostVersion(),
            offlineSettingsEnabled,
            _supportsRestart,
            _diagnostics.GetRunningUser(),
            _diagnostics.GetMemoryUsageBytes(),
            OptionsSingleton.Options?.CurrentValue.HasConfigurationError ?? false,
            OptionsSingleton.Options?.CurrentValue.GetConfigurationErrors() ?? new List<string>());
        
        var json = JsonConvert.SerializeObject(request);
        var data = new StringContent(json, Encoding.UTF8, "application/json");
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Fig_IpAddress", _ipAddressResolver.Resolve());
        _httpClient.DefaultRequestHeaders.Add("Fig_Hostname", Environment.MachineName);
        _httpClient.DefaultRequestHeaders.Add("clientSecret", _clientSecretProvider.GetSecret(_config.ClientName));
        
        var  uri = $"/statuses/{Uri.EscapeDataString(_config.ClientName)}";
        if (_config.Instance != null)
            uri += $"?instance={Uri.EscapeDataString(_config.Instance)}";

        var response = await _httpClient.PutAsync(uri, data);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Failed to get status from Fig API. {response.StatusCode}");
            return;
        }

        var result = await response.Content.ReadAsStringAsync();
        var statusResponse = JsonConvert.DeserializeObject<StatusResponseDataContract>(result);
        ProcessResponse(statusResponse);
    }

    private void ProcessResponse(StatusResponseDataContract? statusResponse)
    {
        if (statusResponse is null)
            return;

        _statusTimer.Interval = statusResponse.PollIntervalMs ?? 30000;

        if (statusResponse.SettingUpdateAvailable)
            SettingsChanged?.Invoke(this, new ChangedSettingsEventArgs(statusResponse.ChangedSettings ?? Array.Empty<string>().ToList()));

        AllowOfflineSettings = statusResponse.AllowOfflineSettings;
        if (!AllowOfflineSettings)
            OfflineSettingsDisabled?.Invoke(this, EventArgs.Empty);

        if (statusResponse.RestartRequested)
            RestartRequested?.Invoke(this, EventArgs.Empty);
    }
}