using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Fig.Client.ClientSecret;
using Fig.Client.Configuration;
using Fig.Client.Events;
using Fig.Client.Exceptions;
using Fig.Client.Versions;
using Fig.Common.NetStandard.Cryptography;
using Fig.Common.NetStandard.Diag;
using Fig.Common.NetStandard.IpAddress;
using Fig.Contracts.Status;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fig.Client.Status;

public class SettingStatusMonitor : ISettingStatusMonitor
{
    private readonly IDiagnostics _diagnostics;
    private readonly IIpAddressResolver _ipAddressResolver;
    private readonly Guid _runSessionId;
    private readonly DateTime _startTime;
    private readonly Timer _statusTimer;
    private readonly IVersionProvider _versionProvider;
    private IClientSecretProvider? _clientSecretProvider;
    private bool _isOffline;
    private DateTime _lastSettingUpdate;
    private bool _liveReload;
    private ILogger? _logger;
    private IFigOptions? _options;
    private SettingsBase? _settings;

    public SettingStatusMonitor(IIpAddressResolver ipAddressResolver, IVersionProvider versionProvider,
        IDiagnostics diagnostics)
    {
        _ipAddressResolver = ipAddressResolver;
        _versionProvider = versionProvider;
        _diagnostics = diagnostics;
        _startTime = DateTime.UtcNow;
        _runSessionId = Guid.NewGuid();
        _statusTimer = new Timer();
        _statusTimer.Elapsed += OnStatusTimerElapsed;
    }

    public event EventHandler<ChangedSettingsEventArgs>? SettingsChanged;
    public event EventHandler? ReconnectedToApi;
    public event EventHandler? OfflineSettingsDisabled;

    public bool AllowOfflineSettings { get; private set; } = true;

    public void Initialize<T>(T settings, IFigOptions figOptions, IClientSecretProvider clientSecretProvider,
        ILogger logger) where T : SettingsBase
    {
        _logger = logger;
        _options = figOptions;
        _clientSecretProvider = clientSecretProvider;
        _settings = settings;
        _liveReload = figOptions.LiveReload;
        _lastSettingUpdate = DateTime.UtcNow;
        _statusTimer.Interval = figOptions.PollIntervalMs;
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
                _logger?.LogInformation("Reconnected to Fig API");
                ReconnectedToApi?.Invoke(this, EventArgs.Empty);
            }

            _isOffline = false;
        }
        catch (HttpRequestException exception)
        {
            _isOffline = true;
            _logger?.LogError($"Unable to contact Fig API {exception.Message}");
        }
        catch (Exception exception)
        {
            _logger?.LogError($"Error getting status: {exception}");
        }
        finally
        {
            _statusTimer.Start();
        }
    }

    private async Task GetStatus()
    {
        if (_options is null || _settings is null || _clientSecretProvider is null)
            throw new NotInitializedException();

        using var client = new HttpClient();
        client.BaseAddress = _options.ApiUri;

        var uptimeSeconds = (DateTime.UtcNow - _startTime).TotalSeconds;
        var offlineSettingsEnabled = _options.AllowOfflineSettings && AllowOfflineSettings;
        var request = new StatusRequestDataContract(_runSessionId,
            uptimeSeconds,
            _lastSettingUpdate,
            _statusTimer.Interval,
            _liveReload,
            _versionProvider.GetFigVersion(),
            _versionProvider.GetHostVersion(),
            offlineSettingsEnabled,
            _settings.SupportsRestart,
            _diagnostics.GetRunningUser(),
            _diagnostics.GetMemoryUsageBytes(),
            _settings.HasConfigurationError,
            _settings.GetConfigurationErrors());
        
        var json = JsonConvert.SerializeObject(request);
        var data = new StringContent(json, Encoding.UTF8, "application/json");
        client.DefaultRequestHeaders.Add("Fig_IpAddress", _ipAddressResolver.Resolve());
        client.DefaultRequestHeaders.Add("Fig_Hostname", Environment.MachineName);
        client.DefaultRequestHeaders.Add("clientSecret", _clientSecretProvider.GetSecret(_settings.ClientName).Read());
        
        var  uri = $"/statuses/{Uri.EscapeDataString(_settings.ClientName)}";
        if (_options.Instance != null)
            uri += $"?instance={Uri.EscapeDataString(_options.Instance)}";

        var response = await client.PutAsync(uri, data);

        if (!response.IsSuccessStatusCode)
        {
            _logger?.LogError($"Failed to get status from Fig API. {response.StatusCode}");
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

        if (statusResponse.LiveReload && statusResponse.SettingUpdateAvailable)
            SettingsChanged?.Invoke(this, new ChangedSettingsEventArgs(statusResponse.ChangedSettings ?? Array.Empty<string>().ToList()));

        AllowOfflineSettings = statusResponse.AllowOfflineSettings;
        if (!AllowOfflineSettings)
            OfflineSettingsDisabled?.Invoke(this, EventArgs.Empty);

        if (statusResponse.RestartRequested)
            _settings?.RequestRestart();
    }
}