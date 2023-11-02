using Microsoft.Extensions.Logging;
using System.Net.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Client.Configuration;
using Fig.Client.Events;
using Fig.Client.OfflineSettings;
using Fig.Client.Status;
using Fig.Common.NetStandard.IpAddress;
using Fig.Client.ExtensionMethods;

namespace Fig.Client.ConfigurationProvider;

public class FigConfigurationProvider : Microsoft.Extensions.Configuration.ConfigurationProvider, IDisposable
{
    private readonly ILogger _logger;

    private readonly IFigConfigurationSource _source;
    private readonly IApiCommunicationHandler _apiCommunicationHandler;
    private readonly IIpAddressResolver _ipAddressResolver;
    private readonly IOfflineSettingsManager _offlineSettingsManager;
    private readonly ISettingStatusMonitor _statusMonitor;
    private readonly SettingsBase _settings;
    private readonly Dictionary<string, CustomConfigurationSection> _configurationSections;
    private bool _disposed;

    internal FigConfigurationProvider(IFigConfigurationSource source,
        ILogger<FigConfigurationProvider> logger,
        IIpAddressResolver ipAddressResolver,
        IOfflineSettingsManager offlineSettingsManager,
        ISettingStatusMonitor statusMonitor,
        SettingsBase settings,
        IApiCommunicationHandler apiCommunicationHandler)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _logger = logger;

        _settings = settings;
        _ipAddressResolver = ipAddressResolver;
        _offlineSettingsManager = offlineSettingsManager;
        _apiCommunicationHandler = apiCommunicationHandler;
        _statusMonitor = statusMonitor;

        _configurationSections = settings.GetConfigurationSections();
        RegisterSettings();
        _statusMonitor.Initialize();

        _statusMonitor.SettingsChanged += OnSettingsChanged;
        _statusMonitor.ReconnectedToApi += OnReconnectedToApi;
        _statusMonitor.OfflineSettingsDisabled += OnOfflineSettingsDisabled;
        _statusMonitor.RestartRequested += OnRestartRequested;
    }

    public override void Load() => LoadAsync().ContinueWith(task =>
    {
        if (task.IsFaulted && task.Exception != null)
        {
            var ex = task.Exception.Flatten();
            _logger.LogError(ex, "Failed to load remote configuration provider");
            throw ex;
        }

        _logger.LogInformation("Fig: Settings successfully populated.");
    }).GetAwaiter().GetResult();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _statusMonitor.SettingsChanged -= OnSettingsChanged;
            _statusMonitor.ReconnectedToApi -= OnReconnectedToApi;
            _statusMonitor.OfflineSettingsDisabled -= OnOfflineSettingsDisabled;
            _statusMonitor.RestartRequested -= OnRestartRequested;
        }

        _disposed = true;
    }

    private void OnRestartRequested(object sender, EventArgs e)
    {
        Data[nameof(SettingsBase.RestartRequested)] = "true";
        OnReload();
    }

    private void RegisterSettings()
    {
        _logger.LogInformation("Registering configuration with the Fig API at address {FigUri}", _source.ApiUri);
        var settingsDataContract = _settings.CreateDataContract(_source.LiveReload, _source.ClientName);

        if (settingsDataContract.ClientSettingOverrides.Any())
            _logger.LogInformation("Requesting value overrides for the following settings {SettingNames}",
                string.Join(", ",
                    settingsDataContract.ClientSettingOverrides.Select(a => a.Name)));

        _apiCommunicationHandler.RegisterWithFigApi(_source.ClientName, settingsDataContract).GetAwaiter().GetResult();
        _logger.LogInformation("Successfully registered settings with Fig API");
    }


    private async Task LoadAsync()
    {
        try
        {
            _logger.LogInformation("Requesting configuration from Fig API for client name '{ClientName}' and instance '{Instance}'", _source.ClientName, _source.Instance);
            var settingValues = await _apiCommunicationHandler.RequestConfiguration(_source.ApiUri!, _source.ClientName, _source.Instance);

            if (_source.AllowOfflineSettings)
                _offlineSettingsManager.Save(_source.ClientName, settingValues);
            _statusMonitor.SettingsUpdated();

            foreach (var setting in settingValues.ToDataProviderFormat(_ipAddressResolver, _configurationSections))
                Data[setting.Key] = setting.Value;
            
            _logger.LogInformation("Successfully applied {SettingCount} settings from Fig API", settingValues.Count);
        }
        catch (HttpRequestException ex)
        {
            if (_source.AllowOfflineSettings && _statusMonitor.AllowOfflineSettings)
            {
                _logger.LogWarning("Failed to get settings from Fig API. {Message}.", ex.Message);
                var offlineSettings = _offlineSettingsManager.Get(_source.ClientName)?.ToList();
                if (offlineSettings is not null)
                {
                    foreach (var setting in offlineSettings.ToDataProviderFormat(_ipAddressResolver, _configurationSections))
                        Data[setting.Key] = setting.Value;
                }
            }
            else
            {
                _logger.LogError("Failed to get settings from Fig API. {Message}", ex.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while trying to request configuration from Fig API");
        }
    }

    private void ReloadSettings()
    {
        Load();
        OnReload();
    }

    private void OnOfflineSettingsDisabled(object sender, EventArgs e)
    {
        _logger.LogInformation("Offline settings disabled, deleting offline configuration file.");
        _offlineSettingsManager.Delete(_source.ClientName);
    }

    private void OnReconnectedToApi(object sender, EventArgs e)
    {
        _logger.LogInformation("Reconnected to Fig API, reloading settings.");
        ReloadSettings();
    }

    private void OnSettingsChanged(object sender, ChangedSettingsEventArgs e)
    {
        _logger.LogInformation("The following Settings changed on Fig API {ChangedSettings}, reloading settings.", string.Join(",", e.SettingNames));
        ReloadSettings();
    }
}