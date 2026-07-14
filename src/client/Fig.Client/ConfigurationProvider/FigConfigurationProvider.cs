using Microsoft.Extensions.Logging;
using System.Net.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fig.Client.Configuration;
using Fig.Client.Enums;
using Fig.Client.Events;
using Fig.Client.Exceptions;
using Fig.Client.OfflineSettings;
using Fig.Client.RegistrationChecksum;
using Fig.Client.Status;
using Fig.Client.Migration;
using Fig.Contracts.CustomActions;
using Fig.Contracts.LookupTable;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
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
    private readonly IRegistrationChecksumStore _registrationChecksumStore;
    private readonly ISettingStatusMonitor _statusMonitor;
    private readonly SettingsBase _settings;
    private readonly SettingsClientDefinitionDataContract _settingsDefinition;
    private readonly Dictionary<string, List<CustomConfigurationSection>> _configurationSections;
    private readonly IFigClientBridge? _clientBridge;
    private readonly object _dataLock = new();
    private readonly SemaphoreSlim _reloadGate = new(1, 1);
    private bool _disposed;
    private List<string> _secretSettings = [];

    internal FigConfigurationProvider(IFigConfigurationSource source,
        ILogger<FigConfigurationProvider> logger,
        IIpAddressResolver ipAddressResolver,
        IOfflineSettingsManager offlineSettingsManager,
        IRegistrationChecksumStore registrationChecksumStore,
        ISettingStatusMonitor statusMonitor,
        SettingsBase settings,
        IApiCommunicationHandler apiCommunicationHandler,
        FigClientBridgeOptions bridgeOptions)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        RegisteredProviders.Register(this);
        _logger = logger;

        _settings = settings;
        _ipAddressResolver = ipAddressResolver;
        _offlineSettingsManager = offlineSettingsManager;
        _registrationChecksumStore = registrationChecksumStore;
        _apiCommunicationHandler = apiCommunicationHandler;
        _clientBridge = apiCommunicationHandler is IFigClientBridge bridge
            ? new ProviderBackedClientBridge(this, bridge)
            : null;
        _statusMonitor = statusMonitor;
        RunSession.Acquire(_source.ClientName, _source.Instance);
        if (_clientBridge is not null)
            FigClientBridgeRegistry.Register(_source.SettingsType, _clientBridge, bridgeOptions);

        _configurationSections = settings.GetConfigurationSections();
        _settingsDefinition = BuildSettingsDefinition();
        if (ShouldSkipRegistration(_settingsDefinition))
            _logger.LogInformation("Registration checksum matches stored value; skipping settings registration");
        else
            RegisterSettings(_settingsDefinition);
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
    }).GetAwaiter().GetResult();

    public string Name => _source.ClientName;

    internal ProviderRegistrationKey RegistrationKey => new(_source.ClientName, _source.Instance, _source.SettingsType);

    internal bool IsDisposed => _disposed;

    internal Action? BeforeDataReadLockEnterForTesting { get; set; }

    public override bool TryGet(string key, out string? value)
    {
        string? localValue = null;
        var found = ExecuteReadWithDataLock(() => base.TryGet(key, out localValue));
        value = localValue;
        return found;
    }

    public override void Set(string key, string? value)
    {
        lock (_dataLock)
        {
            base.Set(key, value);
        }
    }

    public override IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string? parentPath)
    {
        return ExecuteReadWithDataLock(() => base.GetChildKeys(earlierKeys, parentPath).ToList());
    }

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
            RunSession.Release(_source.ClientName, _source.Instance);
            if (_clientBridge is not null)
                FigClientBridgeRegistry.Unregister(_source.SettingsType, _clientBridge);
            _reloadGate.Dispose();
        }

        _disposed = true;
    }

    private void OnRestartRequested(object sender, EventArgs e)
    {
        _reloadGate.Wait();
        try
        {
            lock (_dataLock)
            {
                Data[nameof(SettingsBase.RestartRequested)] = "true";
            }
        }
        finally
        {
            _reloadGate.Release();
        }
        OnReload();
    }

    private SettingsClientDefinitionDataContract BuildSettingsDefinition()
    {
        var settingsDataContract = _settings.CreateDataContract(_source.ClientName, _source.AutomaticallyGenerateHeadings);

        foreach (var warning in SettingDefinitionWarnings.GetHiddenCategoryHeadingWarnings(settingsDataContract.Settings))
        {
            _logger.LogWarning(
                "Category '{CategoryName}' for client {ClientName}: the category heading is on advanced setting '{FirstSettingName}', " +
                "but non-advanced settings follow in the same category ({NonAdvancedSettingNames}). " +
                "Move the non-advanced setting(s) above '{FirstSettingName}' in your settings class so the category heading remains visible when advanced settings are hidden.",
                warning.CategoryName,
                _source.ClientName,
                warning.FirstSettingName,
                string.Join(", ", warning.NonAdvancedSettingNames),
                warning.FirstSettingName);
        }

        _secretSettings = settingsDataContract.Settings
            .Where(a => a.IsSecret)
            .Select(a => a.Name)
            .ToList();

        if (settingsDataContract.ClientSettingOverrides.Any())
            _logger.LogInformation("Requesting value overrides for the following settings {SettingNames}",
                string.Join(", ",
                    settingsDataContract.ClientSettingOverrides.Select(a => a.Name)));

        if ((settingsDataContract.Description?.Length ?? 0) > 2500000)
        {
            var sizeInMb = Math.Round(settingsDataContract.Description!.Length * 2 / 1024.0 / 1024.0, 2);
            _logger.LogWarning(
                "Client description exceeds 5MB (approx size is {Size}MB), which may cause issues with the Fig API. " +
                "Consider reducing the size of the description by removing or resizing images", sizeInMb);
        }

        if (IsMigrateFromDisabled())
        {
            _logger.LogInformation("MigrateFrom is disabled via FIG_DISABLE_MIGRATE_FROM environment variable. Clearing migration metadata before registration.");
            foreach (var setting in settingsDataContract.Settings)
            {
                setting.MigrateFrom = null;
                setting.MigrateFromMigrationMethod = null;
                setting.MigrateFromMigrationMethodInfo = null;
            }
        }

        return settingsDataContract;
    }

    private bool ShouldSkipRegistration(SettingsClientDefinitionDataContract definition)
    {
        if (IsRegistrationChecksumDisabled())
            return false;

        try
        {
            var computedChecksum = RegistrationChecksumCalculator.Compute(definition);
            var storedChecksum = _registrationChecksumStore.Get(_source.ClientName, _source.Instance);
            return storedChecksum is not null &&
                   storedChecksum.Equals(computedChecksum, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to evaluate registration checksum for client {ClientName} instance {Instance}; proceeding with full registration",
                _source.ClientName,
                _source.Instance);
            return false;
        }
    }

    private static bool IsRegistrationChecksumDisabled()
    {
        var value = Environment.GetEnvironmentVariable("FIG_DISABLE_REGISTRATION_CHECKSUM");
        return !string.IsNullOrWhiteSpace(value) &&
               (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1");
    }

    private static bool IsOfflineFallbackException(Exception ex) =>
        ex is HttpRequestException or TaskCanceledException or FigClientNotFoundException;

    private void RegisterSettings(SettingsClientDefinitionDataContract settingsDataContract)
    {
        try
        {
            if (!IsMigrateFromDisabled())
                TryApplyMigrateFromMigrations(settingsDataContract);
            var registered = _apiCommunicationHandler.RegisterWithFigApi(settingsDataContract).GetAwaiter()
                .GetResult();
            if (registered)
                SaveRegistrationChecksum(settingsDataContract);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Failed to register settings with Fig API. {ExceptionMessage}", ex.Message);
        }
        catch (FigRegistrationException registrationException)
        {
            var message = $"Failed to register settings with Fig API: {registrationException.Result}";
            _statusMonitor.SetFailedRegistration(message);
            _logger.LogError(registrationException, "{Message}", message);
        }
    }

    private void SaveRegistrationChecksum(SettingsClientDefinitionDataContract settingsDataContract)
    {
        if (IsRegistrationChecksumDisabled())
            return;

        try
        {
            var checksum = RegistrationChecksumCalculator.Compute(settingsDataContract);
            _registrationChecksumStore.Save(_source.ClientName, _source.Instance, checksum);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to persist registration checksum for client {ClientName} instance {Instance}",
                _source.ClientName,
                _source.Instance);
        }
    }

    private static bool IsMigrateFromDisabled()
    {
        var value = Environment.GetEnvironmentVariable("FIG_DISABLE_MIGRATE_FROM");
        return !string.IsNullOrWhiteSpace(value) &&
               (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1");
    }

    private void ApplyMigrateFromMigrations(SettingsClientDefinitionDataContract settingsDataContract)
    {
        if (!settingsDataContract.Settings.Any(setting => setting.MigrateFromMigrationMethodInfo is not null))
            return;

        var migrationRequests = _apiCommunicationHandler.GetMigrateFromMigrationRequests(settingsDataContract)
            .GetAwaiter()
            .GetResult();
        if (migrationRequests == null || !migrationRequests.Any())
            return;

        settingsDataContract.SettingMigrationResults =
            new MigrateFromMigrationConverter().Convert(settingsDataContract, migrationRequests);
    }

    private void TryApplyMigrateFromMigrations(SettingsClientDefinitionDataContract settingsDataContract)
    {
        try
        {
            ApplyMigrateFromMigrations(settingsDataContract);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or FigRegistrationException)
        {
            _logger.LogWarning(
                ex,
                "Skipping MigrateFrom preview for client {ClientName}; registration will continue without previewed migration results",
                _source.ClientName);
        }
    }

    private async Task LoadAsync()
    {
        await _reloadGate.WaitAsync();
        try
        {
            try
            {
                var snapshot = await LoadServerSnapshot();
                await ApplySnapshot(snapshot);
            }
            catch (Exception ex)
            {
                if (IsOfflineFallbackException(ex))
                {
                    _logger.LogError("Error while trying to request configuration from Fig API. {ExceptionMessage}",
                        ex.Message);
                    if (_source.AllowOfflineSettings && _statusMonitor.AllowOfflineSettings)
                    {
                        var fallback = await LoadOfflineSnapshot();
                        await ApplySnapshot(fallback);
                    }
                }
                else
                {
                    _logger.LogError(ex, "Error while trying to request configuration from Fig API");
                }
            }
        }
        finally
        {
            _reloadGate.Release();
        }

        await _statusMonitor.SyncStatus();
    }

    private static void SetMetadataProperties(IDictionary<string, string?> data, LoadType loadType)
    {
        data["LastFigUpdateUtcTicks"] = DateTime.UtcNow.Ticks.ToString();
        data[nameof(SettingsBase.FigSettingLoadType)] = loadType.ToString();
    }

    private async Task ReloadSettings()
    {
        await _reloadGate.WaitAsync();
        try
        {
            try
            {
                var snapshot = await LoadServerSnapshot();
                await ApplySnapshot(snapshot);
            }
            catch (Exception ex)
            {
                if (IsOfflineFallbackException(ex))
                {
                    _logger.LogError("Error while trying to request configuration from Fig API. {ExceptionMessage}",
                        ex.Message);
                    if (_source.AllowOfflineSettings && _statusMonitor.AllowOfflineSettings)
                    {
                        var fallback = await LoadOfflineSnapshot();
                        await ApplySnapshot(fallback);
                    }
                }
                else
                {
                    _logger.LogError(ex, "Error while trying to request configuration from Fig API");
                }
            }
        }
        finally
        {
            _reloadGate.Release();
        }

        OnReload();
        await _statusMonitor.SyncStatus();
    }

    private async Task ReloadAfterClientSelfUpdate()
    {
        await _reloadGate.WaitAsync();
        try
        {
            var snapshot = await LoadServerSnapshot();
            await ApplySnapshot(snapshot);
        }
        catch (Exception ex)
        {
            throw new FigSettingRefreshException(
                "The setting update succeeded, but refreshing the local Fig configuration failed.",
                ex);
        }
        finally
        {
            _reloadGate.Release();
        }

        OnReload();
    }

    private void OnOfflineSettingsDisabled(object sender, EventArgs e)
    {
        _logger.LogInformation("Offline settings disabled, deleting offline configuration file.");
        _offlineSettingsManager.Delete(_source.ClientName, _source.Instance);
    }

    private async void OnReconnectedToApi(object sender, EventArgs e)
    {
        try
        {
            _logger.LogInformation("Reconnected to Fig API, reloading settings.");
            await ReloadSettings();
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown - ignore silently
        }
        catch (ObjectDisposedException)
        {
            // Expected during shutdown - ignore silently
        }
    }

    private async void OnSettingsChanged(object sender, ChangedSettingsEventArgs e)
    {
        try
        {
            _logger.LogInformation("The following Settings changed on Fig API {ChangedSettings}, reloading settings.",
                string.Join(",", e.SettingNames));
            await ReloadSettings();
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown - ignore silently
        }
        catch (ObjectDisposedException)
        {
            // Expected during shutdown - ignore silently
        }
    }

    private Dictionary<string, string?> GetDefaultValuesInDataProviderFormat()
    {
        var result = new Dictionary<string, string?>();
        _settings.OverrideCollectionDefaultValues();

        // Normalize keys from "->" format to ":" format to match JSON parser output
        var normalizedSections = ConfigurationSectionKeyNormalizer.Normalize(_configurationSections);

        var value = JsonConvert.SerializeObject(_settings);
        var parser = new JsonValueParser();
        foreach (var kvp in parser.ParseJsonValue(value))
        {
            result[kvp.Key] = kvp.Value;

            foreach (var sectionValue in ConfigurationSectionOverrideKeyBuilder.BuildEntriesForFlattenedValue(kvp.Key, kvp.Value, normalizedSections))
            {
                result[sectionValue.Key] = sectionValue.Value;
            }
        }

        return result;
    }

    private async Task<ProviderSnapshot> LoadServerSnapshot()
    {
        _logger.LogInformation(
            "Requesting configuration from Fig API for client name '{ClientName}' and instance '{Instance}'",
            _source.ClientName, _source.Instance);

        List<SettingDataContract> settingValues;
        try
        {
            settingValues = await _apiCommunicationHandler.RequestConfiguration();
        }
        catch (FigClientNotFoundException)
        {
            _logger.LogWarning(
                "Client {ClientName} was not found on Fig API (404). Re-registering settings and retrying.",
                _source.ClientName);
            RegisterSettings(_settingsDefinition);
            settingValues = await _apiCommunicationHandler.RequestConfiguration();
        }

        if (_source.AllowOfflineSettings)
        {
            try
            {
                await _offlineSettingsManager.Save(_source.ClientName, _source.Instance, settingValues);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to persist offline settings for client {ClientName} instance {Instance}; continuing with in-memory snapshot",
                    _source.ClientName,
                    _source.Instance);
            }
        }
        _statusMonitor.SettingsUpdated();

        var data = new Dictionary<string, string?>();
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

            data[setting.Key] = setting.Value;
        }

        SetMetadataProperties(data, LoadType.Server);
        LogAppConfigDetails(data);
        _logger.LogInformation("Successfully applied {SettingCount} settings from Fig API", settingValues.Count);
        return new ProviderSnapshot(data, LoadType.Server);
    }

    private T ExecuteReadWithDataLock<T>(Func<T> action)
    {
        BeforeDataReadLockEnterForTesting?.Invoke();
        lock (_dataLock)
        {
            return action();
        }
    }

    private async Task<ProviderSnapshot> LoadOfflineSnapshot()
    {
        var data = new Dictionary<string, string?>();
        var offlineSettings = (await _offlineSettingsManager.Get(_source.ClientName, _source.Instance))?.ToList();
        if (offlineSettings is not null)
        {
            foreach (var setting in offlineSettings.ToDataProviderFormat(_ipAddressResolver, _configurationSections))
                data[setting.Key] = setting.Value;

            _logger.LogInformation("Successfully applied {SettingCount} settings from Offline Settings", offlineSettings.Count);
        }
        else
        {
            var defaultValueCount = 0;
            var envVarOverrideCount = 0;

            var envVars = Environment.GetEnvironmentVariables()
                .Cast<System.Collections.DictionaryEntry>()
                .GroupBy(e => (string)e.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToDictionary(e => (string)e.Key, e => (string?)e.Value, StringComparer.OrdinalIgnoreCase);

            foreach (var setting in GetDefaultValuesInDataProviderFormat())
            {
                var envVarKey = setting.Key.Replace(":", "__");
                envVars.TryGetValue(envVarKey, out var envVarValue);

                if (envVarValue is null)
                {
                    data[setting.Key] = setting.Value;
                    defaultValueCount++;
                }
                else
                {
                    data[setting.Key] = envVarValue;
                    envVarOverrideCount++;
                }
            }

            _logger.LogWarning("Using default setting values after failed to get settings from API or offline settings. " +
                               "Applied {DefaultCount} default values and {EnvVarCount} environment variable value",
                defaultValueCount, envVarOverrideCount);
        }

        SetMetadataProperties(data, LoadType.Offline);
        return new ProviderSnapshot(data, LoadType.Offline);
    }

    private Task ApplySnapshot(ProviderSnapshot snapshot)
    {
        lock (_dataLock)
        {
            Data.Clear();
            foreach (var setting in snapshot.Data)
                Data[setting.Key] = setting.Value;
        }

        return Task.CompletedTask;
    }

    private void LogAppConfigDetails(IReadOnlyDictionary<string, string?> data)
    {
        if (!_source.LogAppConfigConfiguration)
            return;

        var builder = new StringBuilder();
        builder.AppendLine("---- App.Config Configuration ----");
        builder.AppendLine("<appSettings>");
        foreach (var kvp in data)
        {
            builder.AppendLine($"<add key=\"{kvp.Key}\" value=\"{kvp.Value}\" />");
        }

        builder.AppendLine("</appSettings>");
        _logger.LogInformation(builder.ToString());
    }

    private sealed record ProviderSnapshot(Dictionary<string, string?> Data, LoadType LoadType);

    private sealed class ProviderBackedClientBridge : IFigClientBridge
    {
        private readonly FigConfigurationProvider _provider;
        private readonly IFigClientBridge _innerBridge;

        public ProviderBackedClientBridge(FigConfigurationProvider provider, IFigClientBridge innerBridge)
        {
            _provider = provider;
            _innerBridge = innerBridge;
        }

        public Task<IEnumerable<CustomActionPollResponseDataContract>?> PollForCustomActionRequests() =>
            _innerBridge.PollForCustomActionRequests();

        public Task SendCustomActionResults(CustomActionExecutionResultsDataContract results) =>
            _innerBridge.SendCustomActionResults(results);

        public Task RegisterCustomActions(List<CustomActionDefinitionDataContract> customActions) =>
            _innerBridge.RegisterCustomActions(customActions);

        public Task RegisterLookupTable(LookupTableDataContract lookupTable) =>
            _innerBridge.RegisterLookupTable(lookupTable);

        public async Task UpdateSettings(SettingValueUpdatesDataContract updates)
        {
            await _innerBridge.UpdateSettings(updates).ConfigureAwait(false);
            await _provider.ReloadAfterClientSelfUpdate().ConfigureAwait(false);
        }
    }
}
