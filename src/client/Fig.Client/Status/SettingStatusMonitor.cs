using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Fig.Client.ClientSecret;
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
    private readonly SettingsBase _settings;
    private readonly bool _supportsRestart;
    private bool _isOffline;
    private DateTime _lastSettingUpdate;
    private bool _liveReload;

    public SettingStatusMonitor(
        IIpAddressResolver ipAddressResolver, 
        IVersionProvider versionProvider,
        IDiagnostics diagnostics,
        IHttpClientFactory httpClientFactory,
        IFigConfigurationSource config,
        IClientSecretProvider clientSecretProvider,
        ILogger<SettingStatusMonitor> logger,
        SettingsBase settings,
        bool supportsRestart)
    {
        _ipAddressResolver = ipAddressResolver;
        _versionProvider = versionProvider;
        _diagnostics = diagnostics;
        _config = config;
        _clientSecretProvider = clientSecretProvider;
        _logger = logger;
        _startTime = DateTime.UtcNow;
        _liveReload = _config.LiveReload;
        _lastSettingUpdate = DateTime.UtcNow;
        _runSessionId = Guid.NewGuid();
        _statusTimer = new Timer();
        _statusTimer.Interval = _config.PollIntervalMs;
        _httpClient = httpClientFactory.CreateClient(HttpClientNames.FigApi);
        _statusTimer.Elapsed += OnStatusTimerElapsed;
        _settings = settings;
        _supportsRestart = supportsRestart;
    }

    public event EventHandler<ChangedSettingsEventArgs>? SettingsChanged;
    public event EventHandler? ReconnectedToApi;
    public event EventHandler? OfflineSettingsDisabled;
    public event EventHandler? RestartRequested;

    public bool AllowOfflineSettings { get; private set; } = true;

    public void Initialize()
    {
        _statusTimer.Start();
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

            _isOffline = false;
        }
        catch (HttpRequestException exception)
        {
            _isOffline = true;
            _logger.LogError($"Unable to contact Fig API {exception.Message}");
        }
        catch (Exception exception)
        {
            _logger.LogError($"Error getting status: {exception}");
        }
        finally
        {
            _statusTimer.Start();
        }
    }

    private async Task GetStatus()
    {
        var upTimeSeconds = (DateTime.UtcNow - _startTime).TotalSeconds;
        var offlineSettingsEnabled = _config.AllowOfflineSettings && AllowOfflineSettings;
        var request = new StatusRequestDataContract(_runSessionId,
            upTimeSeconds,
            _lastSettingUpdate,
            _statusTimer.Interval,
            _liveReload,
            _versionProvider.GetFigVersion(),
            _versionProvider.GetHostVersion(),
            offlineSettingsEnabled,
            _supportsRestart,
            _diagnostics.GetRunningUser(),
            _diagnostics.GetMemoryUsageBytes(),
            _settings.HasConfigurationError,
            _settings.GetConfigurationErrors());
        
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
        _liveReload = statusResponse.LiveReload;

        if (statusResponse is { LiveReload: true, SettingUpdateAvailable: true })
            SettingsChanged?.Invoke(this, new ChangedSettingsEventArgs(statusResponse.ChangedSettings ?? Array.Empty<string>().ToList()));

        AllowOfflineSettings = statusResponse.AllowOfflineSettings;
        if (!AllowOfflineSettings)
            OfflineSettingsDisabled?.Invoke(this, EventArgs.Empty);

        if (statusResponse.RestartRequested)
            RestartRequested?.Invoke(this, EventArgs.Empty);
    }
}