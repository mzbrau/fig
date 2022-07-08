using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Fig.Client.ClientSecret;
using Fig.Client.Configuration;
using Fig.Client.OfflineSettings;
using Fig.Client.Status;
using Fig.Client.Versions;
using Fig.Common.Cryptography;
using Fig.Common.Diag;
using Fig.Common.IpAddress;
using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fig.Client;

public class FigConfigurationProvider : IDisposable
{
    private readonly IClientSecretProvider _clientSecretProvider;
    private readonly IIpAddressResolver _ipAddressResolver;
    private readonly ILogger _logger;
    private readonly IOfflineSettingsManager _offlineSettingsManager;
    private readonly IFigOptions _options;
    private readonly ISettingStatusMonitor _statusMonitor;
    private bool _isInitialized;
    private SettingsBase _settings;

    public FigConfigurationProvider(ILogger logger, IFigOptions options)
        : this(options,
            new SettingStatusMonitor(new IpAddressResolver(),
                new VersionProvider(options), new Diagnostics()),
            new IpAddressResolver(),
            new ClientSecretProvider(options,
                logger),
            logger)
    {
    }

    internal FigConfigurationProvider(IFigOptions options,
        ISettingStatusMonitor statusMonitor,
        IIpAddressResolver ipAddressResolver,
        IClientSecretProvider clientSecretProvider,
        ILogger logger)
        : this(options,
            statusMonitor,
            ipAddressResolver,
            clientSecretProvider,
            new OfflineSettingsManager(new Cryptography(),
                new BinaryFile(),
                clientSecretProvider,
                logger),
            logger)
    {
    }

    internal FigConfigurationProvider(
        IFigOptions options,
        ISettingStatusMonitor statusMonitor,
        IIpAddressResolver ipAddressResolver,
        IClientSecretProvider clientSecretProvider,
        IOfflineSettingsManager offlineSettingsManager,
        ILogger logger)
    {
        if (options.ApiUri.OriginalString == null) throw new ArgumentException("Invalid API Address");

        _options = options;
        _statusMonitor = statusMonitor ?? throw new ArgumentNullException(nameof(statusMonitor));
        _ipAddressResolver = ipAddressResolver;
        _clientSecretProvider = clientSecretProvider;
        _offlineSettingsManager = offlineSettingsManager;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _statusMonitor.SettingsChanged += OnSettingsChanged;
        _statusMonitor.ReconnectedToApi += OnReconnectedToApi;
        _statusMonitor.OfflineSettingsDisabled += OnOfflineSettingsDisabled;
    }

    public void Dispose()
    {
        _statusMonitor.SettingsChanged -= OnSettingsChanged;
        _statusMonitor.ReconnectedToApi -= OnReconnectedToApi;
        _statusMonitor.OfflineSettingsDisabled -= OnOfflineSettingsDisabled;
    }

    public async Task<T> Initialize<T>() where T : SettingsBase
    {
        if (_isInitialized)
            throw new ApplicationException("Provider is already initialized");

        T? result = null;
        try
        {
            _settings = await RegisterSettings<T>();
            _statusMonitor.Initialize(_settings, _options, _clientSecretProvider, _logger);
            result = (T) await ReadSettings(_settings, false);
        }
        catch (HttpRequestException e)
        {
            if (_options.AllowOfflineSettings && _statusMonitor.AllowOfflineSettings)
            {
                _logger.LogWarning("Failed to get settings from Fig API. ", e.Message);
                _settings = (T) Activator.CreateInstance(typeof(T));
                result = (T) ReadOfflineSettings(_settings);
            }
            else
            {
                _logger.LogError("Failed to get settings from Fig API. ", e.Message);
            }
        }

        if (!_options.AllowOfflineSettings || !_statusMonitor.AllowOfflineSettings)
        {
            _settings ??= (T) Activator.CreateInstance(typeof(T));
            _offlineSettingsManager.Delete(_settings.ClientName);
        }

        _isInitialized = true;

        if (result == null)
            throw new ApplicationException("Setting initialization failed");
        
        return result;
    }

    private async Task<T> RegisterSettings<T>() where T : SettingsBase
    {
        var settings = (T) Activator.CreateInstance(typeof(T));
        _logger.LogInformation(
            $"Fig: Registering settings for {settings.ClientName} with API at address {_options.ApiUri}...");
        var settingsDataContract = settings.CreateDataContract();

        await RegisterWithService(_clientSecretProvider.GetSecret(settings.ClientName), settingsDataContract);
        return settings;
    }

    private async Task RegisterWithService(SecureString clientSecret, SettingsClientDefinitionDataContract settings)
    {
        using var client = new HttpClient();
        client.BaseAddress = _options.ApiUri;
        var json = JsonConvert.SerializeObject(settings);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("clientSecret", clientSecret.Read());
        var result = await client.PostAsync("/clients", data);

        if (result.IsSuccessStatusCode)
        {
            _logger.LogInformation("Fig: Setting registration complete.");
        }
        else
        {
            var error = await GetErrorResult(result);
            _logger.LogError(
                $"Unable to successfully register settings. Code:{result.StatusCode}{Environment.NewLine}{error}");
        }
    }

    private async Task<T> ReadSettings<T>(T settings, bool isUpdate) where T : SettingsBase
    {
        _logger.LogDebug($"Fig: Reading settings from API at address {_options.ApiUri}...");
        using var client = new HttpClient();
        client.BaseAddress = _options.ApiUri;

        client.DefaultRequestHeaders.Add("Fig_IpAddress", _ipAddressResolver.Resolve());
        client.DefaultRequestHeaders.Add("Fig_Hostname", Environment.MachineName);
        client.DefaultRequestHeaders.Add("clientSecret", _clientSecretProvider.GetSecret(settings.ClientName).Read());
        var result = await client.GetStringAsync($"/clients/{settings.ClientName}/settings");

        var settingValues = JsonConvert.DeserializeObject<IEnumerable<SettingDataContract>>(result).ToList();

        if (isUpdate)
            settings.Update(settingValues);
        else
            settings.Initialize(settingValues);

        if (_options.AllowOfflineSettings)
            _offlineSettingsManager.Save(settings.ClientName, settingValues);

        _logger.LogDebug("Fig: Settings successfully populated.");
        _statusMonitor.SettingsUpdated();

        return settings;
    }

    private T ReadOfflineSettings<T>(T settings) where T : SettingsBase
    {
        var settingValues = _offlineSettingsManager.Get(settings.ClientName);
        settings.Initialize(settingValues);
        return settings;
    }

    private async void OnSettingsChanged(object sender, EventArgs e)
    {
        await ReadSettingsIfNotNull();
    }

    private async void OnReconnectedToApi(object sender, EventArgs e)
    {
        await ReadSettingsIfNotNull();
    }

    private void OnOfflineSettingsDisabled(object sender, EventArgs e)
    {
        _offlineSettingsManager.Delete(_settings.ClientName);
    }

    private async Task ReadSettingsIfNotNull()
    {
        if (_settings == null)
            return;

        await ReadSettings(_settings, true);
    }

    private async Task<ErrorResultDataContract?> GetErrorResult(HttpResponseMessage response)
    {
        ErrorResultDataContract? errorContract = null;
        if (!response.IsSuccessStatusCode)
        {
            var resultString = await response.Content.ReadAsStringAsync();

            if (resultString.Contains("Reference"))
                errorContract = JsonConvert.DeserializeObject<ErrorResultDataContract>(resultString);
            else
                errorContract =
                    new ErrorResultDataContract("Unknown", response.StatusCode.ToString(), resultString, null);
        }

        return errorContract;
    }
}