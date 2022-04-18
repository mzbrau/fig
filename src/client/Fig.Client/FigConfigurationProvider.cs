using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fig.Client.Configuration;
using Fig.Client.IPAddress;
using Fig.Client.Status;
using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Newtonsoft.Json;

namespace Fig.Client
{
    public class FigConfigurationProvider
    {
        private readonly IIpAddressResolver _ipAddressResolver;
        private readonly Action<string> _logger;
        private readonly IFigOptions _options;
        private readonly ISettingStatusMonitor _statusMonitor;
        private SettingsBase _settings;

        public FigConfigurationProvider(IFigOptions options, Action<string> logger)
            : this(options, new SettingStatusMonitor(new IpAddressResolver()), new IpAddressResolver(), logger)
        {
        }

        internal FigConfigurationProvider(
            IFigOptions options,
            ISettingStatusMonitor statusMonitor,
            IIpAddressResolver ipAddressResolver,
            Action<string> logger)
        {
            if (options?.ApiUri?.OriginalString == null) throw new ArgumentException("Invalid API Address");

            _options = options;
            _statusMonitor = statusMonitor ?? throw new ArgumentNullException(nameof(statusMonitor));
            _ipAddressResolver = ipAddressResolver;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _statusMonitor.SettingsChanged += OnSettingsChanged;
        }

        public async Task<T> Initialize<T>() where T : SettingsBase
        {
            _settings = await RegisterSettings<T>();
            _statusMonitor.Initialize(_settings, _options, _logger);
            return (T) await ReadSettings(_settings, false);
        }

        private async Task<T> RegisterSettings<T>() where T : SettingsBase
        {
            var settings = (T) Activator.CreateInstance(typeof(T));
            _logger($"Fig: Registering settings for {settings.ClientName} with API at address {_options.ApiUri}...");
            var settingsDataContract = settings.CreateDataContract();

            await RegisterWithService(settings.ClientSecret, settingsDataContract);
            return settings;
        }

        private async Task RegisterWithService(string clientSecret, SettingsClientDefinitionDataContract settings)
        {
            using var client = new HttpClient();
            client.BaseAddress = _options.ApiUri;
            var json = JsonConvert.SerializeObject(settings);
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("clientSecret", clientSecret);
            var result = await client.PostAsync("/clients", data);

            _logger(result.IsSuccessStatusCode
                ? "Fig: Setting registration complete."
                : $"Unable to successfully register settings. Code:{result.StatusCode}");

            if (result.IsSuccessStatusCode)
            {
                _logger("Fig: Setting registration complete.");
            }
            else
            {
                var error = await GetErrorResult(result);
                _logger(
                    $"Unable to successfully register settings. Code:{result.StatusCode}{Environment.NewLine}{error}");
            }
        }

        private async Task<T> ReadSettings<T>(T settings, bool isUpdate) where T : SettingsBase
        {
            _logger($"Fig: Reading settings from API at address {_options.ApiUri}...");
            using var client = new HttpClient();
            client.BaseAddress = _options.ApiUri;

            client.DefaultRequestHeaders.Add("Fig_IpAddress", _ipAddressResolver.Resolve());
            client.DefaultRequestHeaders.Add("Fig_Hostname", Environment.MachineName);
            client.DefaultRequestHeaders.Add("clientSecret", settings.ClientSecret);
            var result = await client.GetStringAsync($"/clients/{settings.ClientName}/settings");

            var settingValues = JsonConvert.DeserializeObject<IEnumerable<SettingDataContract>>(result);

            if (isUpdate)
                settings.Update(settingValues);
            else
                settings.Initialize(settingValues);

            _logger("Fig: Settings successfully populated.");
            _statusMonitor.SettingsUpdated();

            return settings;
        }

        private async void OnSettingsChanged(object sender, EventArgs e)
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
                    errorContract = new ErrorResultDataContract
                    {
                        Message = response.StatusCode.ToString(),
                        Detail = resultString
                    };
            }

            return errorContract;
        }
    }
}