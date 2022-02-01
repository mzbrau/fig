using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
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
        private readonly Action<string> _logger;
        private readonly IFigOptions _options;

        public FigConfigurationProvider(IFigOptions options, Action<string> logger)
        {
            if (options?.ApiUri?.OriginalString == null) throw new ArgumentException("Invalid API Address");

            _options = options;
            _logger = logger;
        }

        public async Task<T> Initialize<T>() where T : SettingsBase
        {
            var settings = await RegisterSettings<T>();
            return await ReadSettings(settings);
        }

        private async Task<T> RegisterSettings<T>() where T : SettingsBase
        {
            var settings = (T)Activator.CreateInstance(typeof(T));
            var settingsDataContract = settings.CreateDataContract();

            await RegisterWithService(settings.ClientSecret, settingsDataContract);
            return settings;
        }

        private async Task RegisterWithService(string clientSecret, SettingsClientDefinitionDataContract settings)
        {
            _logger($"Fig: Registering settings with API at address {_options.ApiUri}...");
            using var client = new HttpClient();
            client.BaseAddress = _options.ApiUri;
            var json = JsonConvert.SerializeObject(settings);
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("clientSecret", clientSecret);
            client.DefaultRequestHeaders.Add("Fig_IpAddress", GetLocalIpAddress());
            client.DefaultRequestHeaders.Add("Fig_Hostname", Environment.MachineName);
            var result = await client.PostAsync("/clients", data);

            _logger(result.IsSuccessStatusCode
                ? "Fig: Setting registration complete."
                : $"Unable to successfully register settings. Code:{result.StatusCode}");
        }

        private async Task<T> ReadSettings<T>(T settings) where T : SettingsBase
        {
            _logger($"Fig: Reading settings from API at address {_options.ApiUri}...");
            using var client = new HttpClient();
            client.BaseAddress = _options.ApiUri;


            client.DefaultRequestHeaders.Add("clientSecret", settings.ClientSecret);
            var result = await client.GetStringAsync($"/clients/{settings.ClientName}/settings");

            var settingValues = JsonConvert.DeserializeObject<IEnumerable<SettingDataContract>>(result);

            settings.Initialize(settingValues);
            _logger("Fig: Settings successfully populated.");
            return settings;
        }

        public static string GetLocalIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }

            return null;
        }
    }
}