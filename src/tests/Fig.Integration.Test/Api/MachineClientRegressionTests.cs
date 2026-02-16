using System.Net;
using System;
using System.Linq;
using System.Threading.Tasks;
using Fig.Client.Abstractions.Data;
using Fig.Contracts.Authentication;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

public class MachineClientRegressionTests : IntegrationTestBase
{
    [Test]
    public async Task ShallRegisterClientWithClientSecretInKeycloakMode()
    {
        EnableKeycloakModeForTests();
        var clientSecret = GetNewSecret();

        var settings = await RegisterSettings<ThreeSettings>(clientSecret);
        var adminToken = CreateKeycloakToken(Role.Administrator);
        var clients = (await GetAllClients(tokenOverride: adminToken)).ToList();

        Assert.That(clients.Any(a => a.Name == settings.ClientName), Is.True);
    }

    [Test]
    public async Task ShallGetClientSettingsWithClientSecretInKeycloakMode()
    {
        EnableKeycloakModeForTests();
        var clientSecret = GetNewSecret();

        var settings = await RegisterSettings<ThreeSettings>(clientSecret);
        var settingValues = await GetSettingsForClient(settings.ClientName, clientSecret);

        Assert.That(settingValues.Count, Is.GreaterThan(0));
    }

    [Test]
    public async Task ShallUpdateStatusWithClientSecretInKeycloakMode()
    {
        EnableKeycloakModeForTests();
        var clientSecret = GetNewSecret();

        var settings = await RegisterSettings<ThreeSettings>(clientSecret);
        var statusRequest = CreateStatusRequest(DateTime.UtcNow, DateTime.UtcNow, 1000, liveReload: true);

        var status = await GetStatus(settings.ClientName, clientSecret, statusRequest);

        Assert.That(status, Is.Not.Null);
        Assert.That(status.PollIntervalMs, Is.Not.Null);
    }
}
