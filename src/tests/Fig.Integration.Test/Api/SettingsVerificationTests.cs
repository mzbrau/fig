using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Fig.Contracts.Authentication;
using Fig.Contracts.Settings;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

public class SettingsVerificationTests : IntegrationTestBase
{
    [Test]
    public async Task ShallRegisterVerifications()
    {
        var settings = await RegisterSettings<SettingsWithVerification>();
        var client = await GetClient(settings);

        Assert.That(client, Is.Not.Null);
        Assert.That(client.Verifications.Count, Is.EqualTo(1));
    }

    [Test]
    // NOTE: Will fail when not connected to the internet.
    public async Task ShallVerifySuccessWithVerifier()
    {
        var settings = await RegisterSettings<SettingsWithVerification>();
        var client = await GetClient(settings);

        var verification = client.Verifications.Single();
        var result = await RunVerification(settings.ClientName, verification.Name);

        Assert.That(result.Success, Is.True, "Should pass, is there an internet connection?");
        Assert.That(result.Message, Is.EqualTo("Succeeded"));
        Assert.That(result.Logs.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task ShallVerifyFailureWithVerifier()
    {
        var settings = await RegisterSettings<SettingsWithVerification>();

        await UpdateWebsiteToInvalidValue(settings);

        var client = await GetClient(settings);

        var verification = client.Verifications.Single();
        var result = await RunVerification(settings.ClientName, verification.Name);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message,
            Is.EqualTo(
                "An invalid request URI was provided. Either the request URI must be an absolute URI or BaseAddress must be set."));
    }

    [Test]
    public async Task ShallReturnNotFoundWhenRequestingToRunNonExistingVerifier()
    {
        var settings = await RegisterSettings<SettingsWithVerification>();
        var uri = $"/clients/{Uri.EscapeDataString(settings.ClientName)}/nonexsitingverification";

        await ApiClient.PutAndVerify(uri, null, HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ShallSupportMultipleVerifications()
    {
        var settings = await RegisterSettings<ClientAWith2Verifications>();

        var clients = (await GetAllClients()).ToList();

        var verifications = clients.Single().Verifications;
        Assert.That(verifications.Count, Is.EqualTo(2));
        Assert.That(verifications[0].Name, Is.Not.EqualTo(verifications[1].Name));

        foreach (var verification in clients.Single().Verifications)
        {
            var result = await RunVerification(settings.ClientName, verification.Name);
            Assert.That(result.Success, Is.True, $"Verification {verification.Name} should succeed. Message:{string.Join(",", result.Logs)}");
        }
    }

    [Test]
    public async Task ShallNotAllowRunningVerificationsForNonMatchingClientsForUser()
    {
        var settings = await RegisterSettings<SettingsWithVerification>();
        var client = await GetClient(settings);

        var user = NewUser(role: Role.User, clientFilter: $"someNotMatchingFilter");
        await CreateUser(user);
        var loginResult = await Login(user.Username, user.Password ?? throw new InvalidOperationException("Password is null"));
        
        var verification = client.Verifications.Single();
        var response = await RunVerification(settings.ClientName, verification.Name, loginResult.Token);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    private async Task UpdateWebsiteToInvalidValue(SettingsWithVerification settings)
    {
        var settingToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.WebsiteAddress), new StringSettingDataContract("www.doesnotexist"))
        };

        await SetSettings(settings.ClientName, settingToUpdate);
    }
}