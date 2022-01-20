using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Fig.Api.Integration.Test.TestSettings;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Contracts.SettingVerification;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Api.Integration.Test;

public class SettingsVerificationTests : IntegrationTestBase
{
    [SetUp]
    public async Task Setup()
    {
        await DeleteAllClients();
    }

    [TearDown]
    public async Task TearDown()
    {
        await DeleteAllClients();
    }

    [Test]
    public async Task ShallRegisterVerifications()
    {
        var settings = await RegisterSettingsWithVerification();
        var client = await GetClient(settings);
        
        Assert.That(client, Is.Not.Null);
        Assert.That(client.DynamicVerifications.Count, Is.EqualTo(1));
        Assert.That(client.PluginVerifications.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task ShallVerifySuccessWithDynamicVerifier()
    {
        var settings = await RegisterSettingsWithVerification();
        var client = await GetClient(settings);

        var verification = client.DynamicVerifications.Single();
        var result = await RunVerification(settings.ClientName, verification.Name);
        
        Assert.That(result.Success, Is.True);
        Assert.That(result.Message, Is.EqualTo("Succeeded"));
        Assert.That(result.Logs.Count, Is.EqualTo(1));
    }
    
    [Test]
    public async Task ShallVerifyFailureWithDynamicVerifier()
    {
        var settings = await RegisterSettingsWithVerification();
        
        await UpdateWebsiteToInvalidValue(settings);
        
        var client = await GetClient(settings);

        var verification = client.DynamicVerifications.Single();
        var result = await RunVerification(settings.ClientName, verification.Name);
        
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message.StartsWith("Exception during code execution"));
    }
    
    [Test]
    public async Task ShallVerifySuccessWithPluginVerifier()
    {
        var settings = await RegisterSettingsWithVerification();
        var client = await GetClient(settings);

        var verification = client.PluginVerifications.Single();
        var result = await RunVerification(settings.ClientName, verification.Name);
        
        Assert.That(result.Success, Is.True);
        Assert.That(result.Message, Is.EqualTo("Succeeded"));
        Assert.That(result.Logs.Count, Is.EqualTo(1));
    }
    
    [Test]
    public async Task ShallVerifyFailureWithPluginVerifier()
    {
        var settings = await RegisterSettingsWithVerification();

        await UpdateWebsiteToInvalidValue(settings);
        
        var client = await GetClient(settings);

        var verification = client.PluginVerifications.Single();
        var result = await RunVerification(settings.ClientName, verification.Name);
        
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Is.EqualTo("An invalid request URI was provided. Either the request URI must be an absolute URI or BaseAddress must be set."));
    }

    [Test]
    public async Task ShallReturnInternalServerErrorWhenRegisteringWithNonCompilingDynamicVerification()
    {
        var dataContract = new SettingsClientDefinitionDataContract
        {
            Name = "SomeClient",
            Settings = new List<SettingDefinitionDataContract>()
            {
                new()
                {
                    Name = "Website",
                    Description = "some setting"
                }
            },
            DynamicVerifications = new List<SettingDynamicVerificationDefinitionDataContract>()
            {
                new()
                {
                    Name = "Some verifier",
                    Code = "Some invalid code",
                    TargetRuntime = TargetRuntime.Dotnet6
                }
            },
            PluginVerifications = new List<SettingPluginVerificationDefinitionDataContract>()
        };
        var json = JsonConvert.SerializeObject(dataContract);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("clientSecret", Guid.NewGuid().ToString());
        var response = await httpClient.PostAsync("/api/clients", data);
        
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
        var result = await response.Content.ReadAsStringAsync();
        var parsedResult = JsonConvert.DeserializeObject<ProblemResponse>(result);
        Assert.That(parsedResult.detail.StartsWith("Compile error(s) detected in settings verification code"));
    }

    [Test]
    public async Task ShallReturnBadRequestWhenRequestingToRunNonExistingVerifier()
    {
        var settings = await RegisterSettingsWithVerification();
        var uri = $"/api/clients/{HttpUtility.UrlEncode(settings.ClientName)}/nonexsitingverification";
        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", BearerToken);
        var response = await httpClient.PutAsync(uri, null);
        
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    private async Task<SettingsClientDefinitionDataContract> GetClient(SettingsWithVerifications settings)
    {
        var clients = await GetAllClients();
        var client = clients.FirstOrDefault(a => a.Name == settings.ClientName);
        return client;
    }

    private async Task UpdateWebsiteToInvalidValue(SettingsWithVerifications settings)
    {
        var settingToUpdate = new List<SettingDataContract>
        {
            new()
            {
                Name = nameof(settings.WebsiteAddress),
                Value = "www.doesnotexist"
            }
        };

        await SetSettings(settings.ClientName, settingToUpdate);
    }

    public class ProblemResponse
    {
        public string type { get; set; }
        public string title { get; set; }
        public string status { get; set; }
        public string detail { get; set; }
        public string traceId { get; set; }
    }
}