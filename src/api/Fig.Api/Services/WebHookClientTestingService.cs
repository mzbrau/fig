using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using Fig.Api.ExtensionMethods;
using Fig.Contracts.WebHook;
using Fig.Datalayer.BusinessEntities;
using Fig.WebHooks.Contracts;
using Newtonsoft.Json;

namespace Fig.Api.Services;

public class WebHookClientTestingService : IWebHookClientTestingService
{
    private readonly HttpClient _httpClient;

    public WebHookClientTestingService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
        if (!_httpClient.Timeout.Equals(TimeSpan.FromSeconds(2)))
            _httpClient.Timeout = TimeSpan.FromSeconds(2);
    }
    
    public async Task<WebHookClientTestResultsDataContract> PerformTest(WebHookClientBusinessEntity client)
    {
        var results = new List<TestResultDataContract>();
        foreach (var webHookType in Enum.GetValues(typeof(WebHookType)).Cast<WebHookType>())
        {
            results.Add(await PerformTestOfWebhookType(client, webHookType));
        }

        return new WebHookClientTestResultsDataContract(client.Id, client.Name, results);
    }

    private async Task<TestResultDataContract> PerformTestOfWebhookType(WebHookClientBusinessEntity client, WebHookType webHookType)
    {
        var testContract = CreateContract(webHookType);
        var request = CreateRequest(client, webHookType, testContract);
        var watch = Stopwatch.StartNew();
        
        try
        {
            var result = await _httpClient.SendAsync(request);
            var resultText = result.IsSuccessStatusCode ? "Succeeded" : "Failed";
            return new TestResultDataContract(webHookType, resultText, result.StatusCode, null, watch.Elapsed);
        }
        catch (Exception e)
        {
            return new TestResultDataContract(webHookType, "Failed", null, e.Message, watch.Elapsed);
        }
    }

    private object CreateContract(WebHookType webHookType)
    {
        var link = new Uri("https://localhost:7148/");
        
        return webHookType switch
        {
            WebHookType.ClientStatusChanged => new ClientStatusChangedDataContract("Test", null,
                ConnectionEvent.Connected, DateTime.UtcNow, "192.168.1.1", "localhost", "X", "X", link),
            WebHookType.SettingValueChanged => new SettingValueChangedDataContract("Test", null,
                ["TestSetting"], "FigTester", "TestOnly", link),
            WebHookType.NewClientRegistration => new ClientRegistrationDataContract("Test", null,
                ["TestSetting"], RegistrationType.New, link),
            WebHookType.UpdatedClientRegistration => new ClientRegistrationDataContract("Test", null,
                ["TestSetting"], RegistrationType.Updated, link),
            WebHookType.MinRunSessions =>
                new MinRunSessionsDataContract("Test", null, 1, RunSessionsEvent.BelowMinimum, link),
            WebHookType.HealthStatusChanged =>
                new ClientHealthChangedDataContract("Test", null, "server1", "1.2.3.4", HealthStatus.Healthy, "v1", "v2",
                    new HealthDetails(), link),
            WebHookType.SecurityEvent =>
                new SecurityEventDataContract("Login", DateTime.UtcNow, "testuser", true, "192.168.1.1", "testhost", null, link),
            _ => throw new ArgumentOutOfRangeException(nameof(webHookType), webHookType, null)
        };
    }

    private HttpRequestMessage CreateRequest(WebHookClientBusinessEntity client, WebHookType webHookType, object value)
    {
        var route = webHookType.GetRoute();
        var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(client.BaseUri), route));
        request.Content = new StringContent(JsonConvert.SerializeObject(value), Encoding.UTF8, "application/json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Secret", client.Secret);

        return request;
    }
}