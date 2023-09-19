using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Fig.Client.ClientSecret;
using Fig.Client.Configuration;
using Fig.Client.Events;
using Fig.Client.OfflineSettings;
using Fig.Client.Status;
using Fig.Client.Versions;
using Fig.Common.NetStandard.Constants;
using Fig.Common.NetStandard.Cryptography;
using Fig.Common.NetStandard.Diag;
using Fig.Common.NetStandard.IpAddress;
using Fig.Common.NetStandard.Json;
using Fig.Common.NetStandard.Validation;
using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fig.Client;

public class FigConfigurationProvider : IFigConfigurationProvider, IDisposable
{
    private readonly IClientSecretProvider _clientSecretProvider;
    private readonly IIpAddressResolver _ipAddressResolver;
    private readonly ILogger<FigConfigurationProvider> _logger;
    private readonly IOfflineSettingsManager _offlineSettingsManager;
    private readonly IClientNameValidator _clientNameValidator;
    private readonly HttpClient _httpClient;
    private readonly IFigOptions _options;
    private readonly ISettingStatusMonitor _statusMonitor;
    private bool _isInitialized;
    private SettingsBase? _settings;

    public static FigConfigurationProvider Create(ILoggerFactory loggerFactory, IFigOptions options,
        IHttpClientFactory httpClientFactory)
    {
        return new FigConfigurationProvider(loggerFactory, options, httpClientFactory);
    }
    
    [ActivatorUtilitiesConstructor]
    public FigConfigurationProvider(
        IFigOptions options,
        ISettingStatusMonitor statusMonitor,
        IIpAddressResolver ipAddressResolver,
        IClientSecretProvider clientSecretProvider,
        IOfflineSettingsManager offlineSettingsManager,
        IHttpClientFactory httpClientFactory,
        IClientNameValidator clientNameValidator,
        ILogger<FigConfigurationProvider> logger)
    {
        if (options.ApiUri?.OriginalString == null) throw new ArgumentException("Invalid API Address");

        _options = options;
        _statusMonitor = statusMonitor ?? throw new ArgumentNullException(nameof(statusMonitor));
        _ipAddressResolver = ipAddressResolver;
        _clientSecretProvider = clientSecretProvider;
        _offlineSettingsManager = offlineSettingsManager;
        _clientNameValidator = clientNameValidator;
        _httpClient = httpClientFactory.CreateClient(HttpClientNames.FigApi);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _statusMonitor.SettingsChanged += OnSettingsChanged;
        _statusMonitor.ReconnectedToApi += OnReconnectedToApi;
        _statusMonitor.OfflineSettingsDisabled += OnOfflineSettingsDisabled;
    }
    
    internal FigConfigurationProvider(ILoggerFactory loggerFactory, IFigOptions options, IHttpClientFactory httpClientFactory)
        : this(options,
            new SettingStatusMonitor(new IpAddressResolver(),
                new VersionProvider(options), new Diagnostics(), httpClientFactory),
            new IpAddressResolver(),
            new ClientSecretProvider(options,
                loggerFactory.CreateLogger<ClientSecretProvider>()),
            httpClientFactory,
            loggerFactory)
    {
    }

    private FigConfigurationProvider(IFigOptions options,
        ISettingStatusMonitor statusMonitor,
        IIpAddressResolver ipAddressResolver,
        IClientSecretProvider clientSecretProvider,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
        : this(options,
            statusMonitor,
            ipAddressResolver,
            clientSecretProvider,
            new OfflineSettingsManager(new Cryptography(),
                new BinaryFile(),
                clientSecretProvider,
                loggerFactory.CreateLogger<OfflineSettingsManager>()),
            httpClientFactory,
            new ClientNameValidator(),
            loggerFactory.CreateLogger<FigConfigurationProvider>())
    {
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
        
        T? result;
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
                _logger.LogWarning($"Failed to get settings from Fig API. {e.Message}");
                _settings = (T) Activator.CreateInstance(typeof(T));
                result = (T) ReadOfflineSettings(_settings);
            }
            else
            {
                _logger.LogError($"Failed to get settings from Fig API. {e.Message}");
                throw new ApplicationException("Settings failed to load");
            }
        }

        if (!_options.AllowOfflineSettings || !_statusMonitor.AllowOfflineSettings)
        {
            _settings ??= (T) Activator.CreateInstance(typeof(T));
            _offlineSettingsManager.Delete(_settings.ClientName);
        }

        _isInitialized = true;

        return result;
    }

    private async Task<T> RegisterSettings<T>() where T : SettingsBase
    {
        var settings = (T) Activator.CreateInstance(typeof(T));
        _clientNameValidator.Validate(settings.ClientName);
        _logger.LogInformation(
            $"Fig: Registering settings for {settings.ClientName} with API at address {_options.ApiUri}...");
        var settingsDataContract = settings.CreateDataContract(_options.LiveReload);

        await RegisterWithService(_clientSecretProvider.GetSecret(settings.ClientName), settingsDataContract);
        return settings;
    }

    private async Task RegisterWithService(SecureString clientSecret, SettingsClientDefinitionDataContract settings)
    {

        var json = JsonConvert.SerializeObject(settings, JsonSettings.FigDefault);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        AddHeaderToHttpClient("clientSecret", clientSecret.Read);
        var result = await _httpClient.PostAsync("/clients", data);

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

    private async Task<T> ReadSettings<T>(T settings, bool isUpdate, List<string>? changeSettingNames = null) where T : SettingsBase
    {
        _logger.LogDebug("Fig: Reading settings from API at address {OptionsApiUri}...", _options.ApiUri);
        AddHeaderToHttpClient("Fig_IpAddress", () => _ipAddressResolver.Resolve());
        AddHeaderToHttpClient("Fig_Hostname", () => Environment.MachineName);
        AddHeaderToHttpClient("clientSecret", () => _clientSecretProvider.GetSecret(settings.ClientName).Read());

        var  uri = $"/clients/{Uri.EscapeDataString(settings.ClientName)}/settings";
        if (_options.Instance != null)
            uri += $"?instance={Uri.EscapeDataString(_options.Instance)}";
        
        var result = await _httpClient.GetStringAsync(uri);

        var settingValues = (JsonConvert.DeserializeObject<IEnumerable<SettingDataContract>>(result, JsonSettings.FigDefault) ??
                             Array.Empty<SettingDataContract>()).ToList();

        if (isUpdate)
            settings.Update(settingValues, changeSettingNames);
        else
            settings.Initialize(settingValues);

        if (_options.AllowOfflineSettings)
            _offlineSettingsManager.Save(settings.ClientName, settingValues);

        _logger.LogDebug("Fig: Settings successfully populated.");
        _statusMonitor.SettingsUpdated();

        return settings;
    }

    private void AddHeaderToHttpClient(string key, Func<string> getValue)
    {
        if (!_httpClient.DefaultRequestHeaders.Contains(key))
            _httpClient.DefaultRequestHeaders.Add(key, getValue());
    }

    private T ReadOfflineSettings<T>(T settings) where T : SettingsBase
    {
        var settingValues = _offlineSettingsManager.Get(settings.ClientName);
        settings.Initialize(settingValues);
        return settings;
    }

    private async void OnSettingsChanged(object sender, ChangedSettingsEventArgs e)
    {
        await ReadSettingsIfNotNull(e.SettingNames);
    }

    private async void OnReconnectedToApi(object sender, EventArgs e)
    {
        await ReadSettingsIfNotNull();
    }

    private void OnOfflineSettingsDisabled(object sender, EventArgs e)
    {
        if (_settings?.ClientName != null)
            _offlineSettingsManager.Delete(_settings.ClientName);
    }

    private async Task ReadSettingsIfNotNull(List<string>? changedSettingNames = null)
    {
        if (_settings == null)
            return;

        await ReadSettings(_settings, true, changedSettingNames);
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