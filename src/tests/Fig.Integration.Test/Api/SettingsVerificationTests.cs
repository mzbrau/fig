using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Contracts.SettingVerification;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

public class SettingsVerificationTests : IntegrationTestBase
{
    [Test]
    public async Task ShallRegisterVerifications()
    {
        var settings = await RegisterSettings<SettingsWithVerifications>();
        var client = await GetClient(settings);

        Assert.That(client, Is.Not.Null);
        Assert.That(client.DynamicVerifications.Count, Is.EqualTo(1));
        Assert.That(client.PluginVerifications.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task ShallVerifySuccessWithDynamicVerifier()
    {
        var settings = await RegisterSettings<SettingsWithVerifications>();
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
        var settings = await RegisterSettings<SettingsWithVerifications>();

        await UpdateWebsiteToInvalidValue(settings);

        var client = await GetClient(settings);

        var verification = client.DynamicVerifications.Single();
        var result = await RunVerification(settings.ClientName, verification.Name);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message?.StartsWith("Exception during code execution") == true);
    }

    [Test]
    public async Task ShallVerifySuccessWithPluginVerifier()
    {
        var settings = await RegisterSettings<SettingsWithVerifications>();
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
        var settings = await RegisterSettings<SettingsWithVerifications>();

        await UpdateWebsiteToInvalidValue(settings);

        var client = await GetClient(settings);

        var verification = client.PluginVerifications.Single();
        var result = await RunVerification(settings.ClientName, verification.Name);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message,
            Is.EqualTo(
                "An invalid request URI was provided. Either the request URI must be an absolute URI or BaseAddress must be set."));
    }

    [Test]
    public async Task ShallReturnBadRequestWhenRegisteringWithNonCompilingDynamicVerification()
    {
        var settings = new List<SettingDefinitionDataContract>
        {
            new("Website",
                "some setting",
                false,
                null,
                null,
                typeof(string))
        };
        var dynamicVerifications = new List<SettingDynamicVerificationDefinitionDataContract>
        {
            new("Some verifier",
                "some verification",
                "Some invalid code",
                TargetRuntime.Dotnet6,
                new List<string> {"Website "})
        };

        var dataContract = new SettingsClientDefinitionDataContract("SomeClient",
            null,
            settings,
            new List<SettingPluginVerificationDefinitionDataContract>(),
            dynamicVerifications);

        var json = JsonConvert.SerializeObject(dataContract);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("clientSecret", Guid.NewGuid().ToString());
        var response = await httpClient.PostAsync("/clients", data);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var error = await GetErrorResult(response);
        Assert.That(error?.Message.StartsWith("Compile error(s) detected in settings verification code") == true);
    }

    [Test]
    public async Task ShallReturnNotFoundWhenRequestingToRunNonExistingVerifier()
    {
        var settings = await RegisterSettings<SettingsWithVerifications>();
        var uri = $"/clients/{Uri.EscapeDataString(settings.ClientName)}/nonexsitingverification";

        await ApiClient.PutAndVerify(uri, null, HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ShallSupportMultiplePluginVerifications()
    {
        var settings = await RegisterSettings<ClientAWith2PluginVerifications>();

        var clients = (await GetAllClients()).ToList();

        var verifications = clients.Single().PluginVerifications;
        Assert.That(verifications.Count, Is.EqualTo(2));
        Assert.That(verifications[0].Name, Is.Not.EqualTo(verifications[1].Name));

        foreach (var verification in clients.Single().DynamicVerifications)
        {
            var result = await RunVerification(settings.ClientName, verification.Name);
            Assert.That(result.Success, Is.True);
        }
    }

    private async Task UpdateWebsiteToInvalidValue(SettingsWithVerifications settings)
    {
        var settingToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.WebsiteAddress), "www.doesnotexist")
        };

        await SetSettings(settings.ClientName, settingToUpdate);
    }
}