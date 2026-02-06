using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Fig.Client;
using Fig.Common.NetStandard.IpAddress;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.Status;
using Newtonsoft.Json;

namespace Fig.LoadTest;

public sealed class LoadTestClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly LoadTestClientDefinition _definition;
    private readonly LoadTestOptions _options;
    private readonly DateTime _startTimeUtc;
    private readonly IpAddressResolver _ipAddressResolver = new();
    private readonly Guid _runSessionId = Guid.NewGuid();
    private DateTime _lastSettingUpdateUtc;

    public LoadTestClient(LoadTestClientDefinition definition, LoadTestOptions options)
    {
        _definition = definition;
        _options = options;
        _startTimeUtc = DateTime.UtcNow;
        _lastSettingUpdateUtc = _startTimeUtc;
        _httpClient = new HttpClient
        {
            BaseAddress = options.ApiUri,
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public string ClientName => _definition.ClientName;
    public string InstanceName => _definition.InstanceName;

    public async Task RegisterAsync(SettingsBase settings)
    {
        var contract = settings.CreateDataContract(ClientName, automaticallyGenerateHeadings: true, instance: null);
        var json = JsonConvert.SerializeObject(contract, JsonSettings.FigDefault);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("ClientSecret", _options.ClientSecret);

        var response = await _httpClient.PostAsync("/clients", data);
        response.EnsureSuccessStatusCode();
    }

    public async Task<TimeSpan> SyncStatusAsync()
    {
        var request = BuildStatusRequest();
        var json = JsonConvert.SerializeObject(request, JsonSettings.FigDefault);
        var data = new StringContent(json, Encoding.UTF8, "application/json");
        var uri = $"/statuses/{Uri.EscapeDataString(ClientName)}?instance={Uri.EscapeDataString(InstanceName)}";

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Fig_IpAddress", _ipAddressResolver.Resolve());
        _httpClient.DefaultRequestHeaders.Add("Fig_Hostname", Environment.MachineName);
        _httpClient.DefaultRequestHeaders.Add("clientSecret", _options.ClientSecret);

        var sw = Stopwatch.StartNew();
        var response = await _httpClient.PutAsync(uri, data);
        sw.Stop();

        response.EnsureSuccessStatusCode();
        return sw.Elapsed;
    }

    private StatusRequestDataContract BuildStatusRequest()
    {
        return new StatusRequestDataContract(
            _runSessionId,
            _startTimeUtc,
            _lastSettingUpdateUtc,
            _options.SyncInterval.TotalMilliseconds,
            "LoadTest",
            GetApplicationVersion(),
            offlineSettingsEnabled: false,
            supportsRestart: false,
            runningUser: Environment.UserName,
            memoryUsageBytes: Process.GetCurrentProcess().WorkingSet64);
    }

    private static string GetApplicationVersion()
    {
        return typeof(LoadTestClient).Assembly.GetName().Version?.ToString() ?? "1.0.0";
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
