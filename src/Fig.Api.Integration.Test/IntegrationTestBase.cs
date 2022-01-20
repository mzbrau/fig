using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Fig.Api.Integration.Test.TestSettings;
using Fig.Client;
using Fig.Contracts.Authentication;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Contracts.SettingVerification;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Api.Integration.Test;

public abstract class IntegrationTestBase
{
    protected string? BearerToken;
    private WebApplicationFactory<Program>? _app;

    [OneTimeSetUp]
    public async Task FixtureSetup()
    {
        _app = new WebApplicationFactory<Program>();

        await Authenticate();
    }

    [OneTimeTearDown]
    public void FixtureTearDown()
    {
        _app.Dispose();
    }

    protected async Task RegisterSettings(SettingsBase settings, string? clientSecret = null)
    {
        var dataContract = settings.CreateDataContract();
        var json = JsonConvert.SerializeObject(dataContract);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("clientSecret", clientSecret ?? settings.ClientSecret);
        var result = await httpClient.PostAsync("/api/clients", data);

        Assert.That(result.IsSuccessStatusCode, Is.True,
            $"Registration of settings should succeed. {result.StatusCode}:{result.ReasonPhrase}");
    }

    protected async Task<List<SettingDataContract>> GetSettingsForClient(string clientName,
        string clientSecret, string? instance = null)
    {
        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("clientSecret", clientSecret);
        var requestUri = $"/api/clients/{HttpUtility.UrlEncode(clientName)}/settings";
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
        
        var result = await httpClient.GetStringAsync("/api/clients");

        Assert.That(result, Is.Not.Null, "Get all clients should succeed.");

        return JsonConvert.DeserializeObject<IEnumerable<SettingsClientDefinitionDataContract>>(result);
    }

    protected async Task SetSettings(string clientName, IEnumerable<SettingDataContract> settings,
        string? instance = null, bool authenticate = true)
    {
        var json = JsonConvert.SerializeObject(settings);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        var requestUri = $"/api/clients/{HttpUtility.UrlEncode(clientName)}/settings";
        if (instance != null) requestUri += $"?instance={HttpUtility.UrlEncode(instance)}";

        using var httpClient = GetHttpClient();
        
        if (authenticate)
            httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);
        
        var result = await httpClient.PutAsync(requestUri, data);

        Assert.That(result.IsSuccessStatusCode, Is.True, "Set of settings should succeed.");
    }

    protected async Task DeleteClient(string clientName, string? instance = null, bool authenticate = true)
    {
        var requestUri = $"/api/clients/{HttpUtility.UrlEncode(clientName)}";
        if (instance != null) requestUri += $"?instance={HttpUtility.UrlEncode(instance)}";

        using var httpClient = GetHttpClient();
        
        if (authenticate)
            httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);
        
        var result = await httpClient.DeleteAsync(requestUri);

        Assert.That(result.IsSuccessStatusCode, Is.True, "Delete of clients should succeed.");
    }

    protected async Task<ClientXWithTwoSettings> RegisterClientXWithTwoSettings()
    {
        var settings = new ClientXWithTwoSettings();
        await RegisterSettings(settings);
        return settings;
    }

    protected async Task<ClientXWithThreeSettings> RegisterClientXWithThreeSettings()
    {
        var settings = new ClientXWithThreeSettings();
        await RegisterSettings(settings);
        return settings;
    }

    protected async Task<NoSettings> RegisterNoSettings()
    {
        var settings = new NoSettings();
        await RegisterSettings(settings);
        return settings;
    }

    protected async Task<ThreeSettings> RegisterThreeSettings(string? clientSecret = null)
    {
        var settings = new ThreeSettings();
        await RegisterSettings(settings, clientSecret);
        return settings;
    }

    protected async Task<AllSettingsAndTypes> RegisterAllSettingsAndTypes()
    {
        var settings = new AllSettingsAndTypes();
        await RegisterSettings(settings);
        return settings;
    }

    protected async Task<SettingsWithVerifications> RegisterSettingsWithVerification()
    {
        var settings = new SettingsWithVerifications();
        await RegisterSettings(settings);
        return settings;
    }

    protected async Task<VerificationResultDataContract> RunVerification(string clientName, string verificationName, bool authenticate = true)
    {
        var uri = $"/api/clients/{HttpUtility.UrlEncode(clientName)}/{verificationName}";

        using var httpClient = GetHttpClient();
        
        if (authenticate)
            httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);
        
        var response = await httpClient.PutAsync(uri, null);

        Assert.That(response.IsSuccessStatusCode, Is.True,
            $"Verification should not throw an error result. {response.StatusCode}:{response.ReasonPhrase}");

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

        try
        {
            using var httpClient = GetHttpClient();
            var response = await httpClient.PostAsync("/api/users/authenticate", data);

            Assert.That(response.IsSuccessStatusCode, Is.True);

            var responseString = await response.Content.ReadAsStringAsync();

            var responseObject = JsonConvert.DeserializeObject<AuthenticateResponseDataContract>(responseString);

            BearerToken = responseObject.Token;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        
        
        Assert.That(BearerToken, Is.Not.Null);
    }

    protected HttpClient GetHttpClient()
    {
        return _app.CreateClient();
    }
}