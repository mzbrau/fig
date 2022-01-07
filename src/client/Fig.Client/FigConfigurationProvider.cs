using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fig.Client.Configuration;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Newtonsoft.Json;

namespace Fig.Client
{
    public class FigConfigurationProvider
    {
        private readonly IFigOptions _options;
        private readonly Action<string> _logger;

        public FigConfigurationProvider(IFigOptions options, Action<string> logger)
        {
            if (options?.ApiUri?.OriginalString == null)
            {
                throw new ArgumentException("Invalid API Address");
            }

            _options = options;
            _logger = logger;
        }

        public async Task<T> Initialize<T>() where T : SettingsBase
        {
            var clientSecret = await RegisterSettings<T>();
            return await ReadSettings<T>(clientSecret);
        }

        private async Task<string> RegisterSettings<T>() where T : SettingsBase
        {
            var settings = (T)Activator.CreateInstance(typeof(T));
            var settingsDataContract = settings.CreateDataContract();

            await RegisterWithService(settings.ClientSecret, settingsDataContract);
            return settings.ClientSecret;
        }

        private async Task RegisterWithService(string clientSecret, SettingsClientDefinitionDataContract settings)
        {
            _logger($"Fig: Registering settings with API at address {_options.ApiUri}...");
            using var client = new HttpClient();
            client.BaseAddress = _options.ApiUri;
            var json = JsonConvert.SerializeObject(settings);
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Add("clientSecret", clientSecret);
            var result = await client.PostAsync("/api/clients", data);

            _logger(result.IsSuccessStatusCode
                ? $"Fig: Setting registration complete."
                : $"Unable to successfully register settings. Code:{result.StatusCode}");
        }

        private async Task<T> ReadSettings<T>(string clientSecret) where T : SettingsBase
        {
            _logger($"Fig: Reading settings from API at address {_options.ApiUri}...");
            using HttpClient client = new HttpClient();
            client.BaseAddress = _options.ApiUri;


            client.DefaultRequestHeaders.Add("clientSecret", clientSecret);
            var result = await client.GetStringAsync("/api/clients");

            var settingValues = JsonConvert.DeserializeObject<IEnumerable<SettingDataContract>>(result);

            var settings = (T)Activator.CreateInstance(typeof(T));
            settings.Initialize(settingValues);
            _logger($"Fig: Settings successfully populated.");
            return settings;
        }
    }
}