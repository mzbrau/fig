using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Fig.Client;
using Fig.Contracts;
using Fig.Contracts.Authentication;
using Fig.Contracts.EventHistory;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Contracts.SettingVerification;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

public abstract class IntegrationTestBase
{
    private WebApplicationFactory<Program>? _app;
    protected string? BearerToken;

    [OneTimeSetUp]
    public async Task FixtureSetup()
    {
        _app = new WebApplicationFactory<Program>();

        await Authenticate();
    }

    [OneTimeTearDown]
    public void FixtureTearDown()
    {
        _app?.Dispose();
    }

    protected async Task<List<SettingDataContract>> GetSettingsForClient(string clientName,
        string clientSecret, string? instance = null)
    {
        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("clientSecret", clientSecret);
        var requestUri = $"/clients/{HttpUtility.UrlEncode(clientName)}/settings";
        if (instance != null) requestUri += $"?instance={HttpUtility.UrlEncode(instance)}";

        var result = await httpClient.GetStringAsync(requestUri);

        if (!string.IsNullOrEmpty(result))
            return JsonConvert.DeserializeObject<IEnumerable<SettingDataContract>>(result).ToList();

        return Array.Empty<SettingDataContract>().ToList();
    }

    protected async Task<IEnumerable<SettingsClientDefinitionDataContract>> GetAllClients(bool authenticate = true)
    {
        using var httpClient = GetHttpClient();

        if (authenticate)
            httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);

        var result = await httpClient.GetStringAsync("/clients");

        Assert.That(result, Is.Not.Null, "Get all clients should succeed.");

        return JsonConvert.DeserializeObject<IEnumerable<SettingsClientDefinitionDataContract>>(result);
    }

    protected async Task SetSettings(string clientName, IEnumerable<SettingDataContract> settings,
        string? instance = null, bool authenticate = true)
    {
        var json = JsonConvert.SerializeObject(settings);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        var requestUri = $"/clients/{HttpUtility.UrlEncode(clientName)}/settings";
        if (instance != null) requestUri += $"?instance={HttpUtility.UrlEncode(instance)}";

        using var httpClient = GetHttpClient();

        if (authenticate)
            httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);

        var result = await httpClient.PutAsync(requestUri, data);

        var error = await GetErrorResult(result);
        Assert.That(result.IsSuccessStatusCode, Is.True, $"Set of settings should succeed. {error}");
    }

    protected async Task DeleteClient(string clientName, string? instance = null, bool authenticate = true)
    {
        var requestUri = $"/clients/{HttpUtility.UrlEncode(clientName)}";
        if (instance != null) requestUri += $"?instance={HttpUtility.UrlEncode(instance)}";

        using var httpClient = GetHttpClient();

        if (authenticate)
            httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);

        var result = await httpClient.DeleteAsync(requestUri);
        var error = await GetErrorResult(result);
        Assert.That(result.IsSuccessStatusCode, Is.True, $"Delete of clients should succeed. {error}");
    }

    protected async Task<T> RegisterSettings<T>(string? clientSecret = null) where T : SettingsBase
    {
        var settings = Activator.CreateInstance<T>();
        var dataContract = settings.CreateDataContract();
        var json = JsonConvert.SerializeObject(dataContract);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("clientSecret", clientSecret ?? settings.ClientSecret);
        var result = await httpClient.PostAsync("/clients", data);

        var error = await GetErrorResult(result);
        Assert.That(result.IsSuccessStatusCode, Is.True,
            $"Registration of settings should succeed. {error}");

        return settings;
    }

    protected async Task<VerificationResultDataContract> RunVerification(string clientName, string verificationName,
        bool authenticate = true)
    {
        var uri = $"/clients/{HttpUtility.UrlEncode(clientName)}/verifications/{verificationName}";

        using var httpClient = GetHttpClient();

        if (authenticate)
            httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);

        var response = await httpClient.PutAsync(uri, null);

        var error = await GetErrorResult(response);
        Assert.That(response.IsSuccessStatusCode, Is.True,
            $"Verification should not throw an error result. {error}");

        var result = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<VerificationResultDataContract>(result);
    }

    protected async Task DeleteAllClients()
    {
        var clients = await GetAllClients();
        foreach (var client in clients)
            await DeleteClient(client.Name, client.Instance);
    }

    protected async Task Authenticate()
    {
        var auth = new AuthenticateRequestDataContract
        {
            Username = "admin",
            Password = "admin"
        };

        var json = JsonConvert.SerializeObject(auth);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpClient = GetHttpClient();
        var response = await httpClient.PostAsync("/users/authenticate", data);

        var error = await GetErrorResult(response);
        Assert.That(response.IsSuccessStatusCode, Is.True, $"Authentication should succeed. {error}");

        var responseString = await response.Content.ReadAsStringAsync();
        var responseObject = JsonConvert.DeserializeObject<AuthenticateResponseDataContract>(responseString);

        BearerToken = responseObject.Token;

        Assert.That(BearerToken, Is.Not.Null, "A bearer token should be set after authentication");
    }

    protected HttpClient GetHttpClient()
    {
        return _app.CreateClient();
    }

    protected async Task<ErrorResultDataContract?> GetErrorResult(HttpResponseMessage response)
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

    protected async Task<EventLogCollectionDataContract> GetEvents(DateTime startTime, DateTime endTime)
    {
        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);

        var uri = $"/events" +
                  $"?startTime={HttpUtility.UrlEncode(startTime.ToString("o"))}" +
                  $"&endTime={HttpUtility.UrlEncode(endTime.ToString("o"))}";
        var result = await httpClient.GetStringAsync(uri);

        Assert.That(result, Is.Not.Null, "Get events should succeed.");

        return JsonConvert.DeserializeObject<EventLogCollectionDataContract>(result);
    }
}