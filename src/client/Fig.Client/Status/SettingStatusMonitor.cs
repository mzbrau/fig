using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Fig.Client.Configuration;
using Fig.Client.IPAddress;
using Fig.Contracts.Status;
using Newtonsoft.Json;

namespace Fig.Client.Status
{
    public class SettingStatusMonitor : ISettingStatusMonitor
    {
        private readonly IIpAddressResolver _ipAddressResolver;
        private readonly Timer _statusTimer;
        private readonly Guid _runSessionId;
        private readonly DateTime _startTime;
        private DateTime _lastSettingUpdate;
        private Action<string> _logger;
        private IFigOptions _options;
        private SettingsBase _settings;
        private bool _liveReload;
        public event EventHandler SettingsChanged;

        public SettingStatusMonitor(IIpAddressResolver ipAddressResolver)
        {
            _ipAddressResolver = ipAddressResolver;
            _startTime = DateTime.UtcNow;
            _runSessionId = Guid.NewGuid();
            _statusTimer = new Timer();
            _statusTimer.Elapsed += OnStatusTimerElapsed;
        }

        public void Initialize<T>(T settings, IFigOptions figOptions, Action<string> logger) where T : SettingsBase
        {
            _logger = logger;
            _options = figOptions;
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
            }
            catch (Exception exception)
            {
                _logger($"Error getting status: {exception}");
            }
            finally
            {
                _statusTimer.Start();
            }
        }

        private async Task GetStatus()
        {
            using var client = new HttpClient();
            client.BaseAddress = _options.ApiUri;

            var request = new StatusRequestDataContract
            {
                RunSessionId = _runSessionId,
                UptimeSeconds = (DateTime.UtcNow - _startTime).TotalSeconds,
                LastSettingUpdate = _lastSettingUpdate,
                PollIntervalMs = _statusTimer.Interval,
                LiveReload = _liveReload
            };

            var json = JsonConvert.SerializeObject(request);
            var data = new StringContent(json, Encoding.UTF8, "application/json");
            client.DefaultRequestHeaders.Add("Fig_IpAddress", _ipAddressResolver.Resolve());
            client.DefaultRequestHeaders.Add("Fig_Hostname", Environment.MachineName);
            
            client.DefaultRequestHeaders.Add("clientSecret", _settings.ClientSecret);
            var response = await client.PutAsync($"/statuses/{_settings.ClientName}", data);

            if (!response.IsSuccessStatusCode)
            {
                _logger($"Failed to get status from Fig API. {response.StatusCode}");
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

            _statusTimer.Interval = statusResponse.PollIntervalMs;
            _liveReload = statusResponse.LiveReload;
            
            if (statusResponse.LiveReload && statusResponse.SettingUpdateAvailable)
                SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}