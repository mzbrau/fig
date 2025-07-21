using Microsoft.Extensions.Logging;
using System.Net.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fig.Client.Configuration;
using Fig.Client.Enums;
using Fig.Client.Events;
using Fig.Client.OfflineSettings;
using Fig.Client.Status;
using Fig.Common.NetStandard.IpAddress;
using Fig.Client.ExtensionMethods;
using Fig.Client.Parsers;
using Newtonsoft.Json;

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
    private readonly Dictionary<string, List<CustomConfigurationSection>> _configurationSections;
    private bool _disposed;
    private List<string> _secretSettings = [];

    internal FigConfigurationProvider(IFigConfigurationSource source,
        ILogger<FigConfigurationProvider> logger,
        IIpAddressResolver ipAddressResolver,
        IOfflineSettingsManager offlineSettingsManager,
        ISettingStatusMonitor statusMonitor,
        SettingsBase settings,
        IApiCommunicationHandler apiCommunicationHandler)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        RegisteredProviders.Register(this);
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
            _logger.LogError(ex, "Failed to load settings from Fig API");
            throw ex;
        }

        _logger.LogInformation("Fig: Settings successfully populated.");
    }).GetAwaiter().GetResult();

    public string Name => _source.ClientName;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            RegisteredProviders.Unregister(this);
            _statusMonitor.SettingsChanged -= OnSettingsChanged;
            _statusMonitor.ReconnectedToApi -= OnReconnectedToApi;
            _statusMonitor.OfflineSettingsDisabled -= OnOfflineSettingsDisabled;
            _statusMonitor.RestartRequested -= OnRestartRequested;
            _statusMonitor.Dispose();
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
        var settingsDataContract = _settings.CreateDataContract(_source.ClientName, _source.AutomaticallyGenerateHeadings);
        _secretSettings = settingsDataContract.Settings
            .Where(a => a.IsSecret)
            .Select(a => a.Name)
            .ToList();

        if (settingsDataContract.ClientSettingOverrides.Any())
            _logger.LogInformation("Requesting value overrides for the following settings {SettingNames}",
                string.Join(", ",
                    settingsDataContract.ClientSettingOverrides.Select(a => a.Name)));

        if (settingsDataContract.Description.Length > 2500000)
        {
            var sizeInMb = Math.Round(settingsDataContract.Description.Length * 2 / 1024.0 / 1024.0, 2);
            _logger.LogWarning(
                "Client description exceeds 5MB (approx size is {Size}MB), which may cause issues with the Fig API. " +
                "Consider reducing the size of the description by removing or resizing images", sizeInMb);
        }

        try
        {
            _apiCommunicationHandler.RegisterWithFigApi(settingsDataContract).GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            if (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogError("Failed to register settings with Fig API. {ExceptionMessage}", ex.Message);
            }
            else
            {
                _logger.LogError(ex, "Failed to register settings with Fig API");
            }
        }
    }

    private async Task LoadAsync()
    {
        try
        {
            _logger.LogInformation(
                "Requesting configuration from Fig API for client name '{ClientName}' and instance '{Instance}'",
                _source.ClientName, _source.Instance);
            var settingValues = await _apiCommunicationHandler.RequestConfiguration();

            if (_source.AllowOfflineSettings)
                await _offlineSettingsManager.Save(_source.ClientName, _source.Instance, settingValues);
            _statusMonitor.SettingsUpdated();

            Data.Clear();

            _logger.LogDebug("Applied values from Fig:");

            foreach (var setting in settingValues.ToDataProviderFormat(_ipAddressResolver, _configurationSections))
            {
                if (_secretSettings.Any(secretName => setting.Key.Split(':').Any(s => s.Equals(secretName, StringComparison.OrdinalIgnoreCase))))
                {
                    _logger.LogDebug("{SettingKey} -> ******", setting.Key);
                }
                else
                {
                    _logger.LogDebug("{SettingKey} -> {SettingValue}", setting.Key, setting.Value);
                }

                Data[setting.Key] = setting.Value;
            }

            SetMetadataProperties(LoadType.Server);

            LogAppConfigDetails();

            _logger.LogInformation("Successfully applied {SettingCount} settings from Fig API", settingValues.Count);
        }
        catch (Exception ex)
        {
            if (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogError("Error while trying to request configuration from Fig API. {ExceptionMessage}",
                    ex.Message);
                if (_source.AllowOfflineSettings && _statusMonitor.AllowOfflineSettings)
                {
                    await LoadOfflineSettings();
                }
            }
            else
            {
                _logger.LogError(ex, "Error while trying to request configuration from Fig API");
            }
        }

        async Task LoadOfflineSettings()
        {
            Data.Clear();

            var offlineSettings = (await _offlineSettingsManager.Get(_source.ClientName, _source.Instance))?.ToList();
            if (offlineSettings is not null)
            {
                foreach (var setting in
                         offlineSettings.ToDataProviderFormat(_ipAddressResolver, _configurationSections))
                    Data[setting.Key] = setting.Value;
            }
            else
            {
                foreach (var setting in GetDefaultValuesInDataProviderFormat())
                    Data[setting.Key] = setting.Value;
            }

            SetMetadataProperties(LoadType.Offline);
        }
    }    private void SetMetadataProperties(LoadType loadType)
    {
        Data["LastFigUpdateUtcTicks"] = DateTime.UtcNow.Ticks.ToString();
        Data[nameof(SettingsBase.FigSettingLoadType)] = loadType.ToString();
    }

    private void LogAppConfigDetails()
    {
        if (!_source.LogAppConfigConfiguration)
            return;

        var builder = new StringBuilder();
        builder.AppendLine("---- App.Config Configuration ----");
        builder.AppendLine("<appSettings>");
        foreach (var kvp in Data)
        {
            builder.AppendLine($"<add key=\"{kvp.Key}\" value=\"{kvp.Value}\" />");
        }

        builder.AppendLine("</appSettings>");
        _logger.LogInformation(builder.ToString());
    }

    private async Task ReloadSettings()
    {
        Load();
        OnReload();
        await _statusMonitor.SyncStatus();
    }

    private void OnOfflineSettingsDisabled(object sender, EventArgs e)
    {
        _logger.LogInformation("Offline settings disabled, deleting offline configuration file.");
        _offlineSettingsManager.Delete(_source.ClientName, _source.Instance);
    }

    private async void OnReconnectedToApi(object sender, EventArgs e)
    {
        _logger.LogInformation("Reconnected to Fig API, reloading settings.");
        await ReloadSettings();
    }

    private async void OnSettingsChanged(object sender, ChangedSettingsEventArgs e)
    {
        _logger.LogInformation("The following Settings changed on Fig API {ChangedSettings}, reloading settings.",
            string.Join(",", e.SettingNames));
        await ReloadSettings();
    }

    private Dictionary<string, string?> GetDefaultValuesInDataProviderFormat()
    {
        var result = new Dictionary<string, string?>();
        _settings.OverrideCollectionDefaultValues();

        var value = JsonConvert.SerializeObject(_settings);
        var parser = new JsonValueParser();
        foreach (var kvp in parser.ParseJsonValue(value))
        {
            result[kvp.Key] = kvp.Value;

            if (_configurationSections.TryGetValue(kvp.Key, out var sections) && sections != null)
            {
                foreach (var section in sections)
                {
                    if (!string.IsNullOrEmpty(section.SectionName))
                    {
                        // If the configuration setting value is set, we set it in both places.
                        result[$"{section.SectionName}:{section.SettingNameOverride ?? kvp.Key}"] = kvp.Value;
                    }
                }
            }
        }

        return result;
    }
}