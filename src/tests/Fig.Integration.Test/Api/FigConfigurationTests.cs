using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class FigConfigurationTests : IntegrationTestBase
{
    [Test]
    public async Task ShallPreventNewRegistrations()
    {
        var secret = GetNewSecret();
        await RegisterSettings<ThreeSettings>(secret);

        await SetConfiguration(CreateConfiguration(allowNewRegistrations: false));

        await DeleteAllClients();

        var result2 = await TryRegisterSettings<ThreeSettings>(secret);
        Assert.That(result2.IsSuccessStatusCode, Is.False, "While disabled, registrations should be prevented");
    }

    [Test]
    public async Task ShallPreventUpdatedRegistrations()
    {
        var secret = GetNewSecret();
        await RegisterSettings<ClientXWithTwoSettings>(secret);
        await RegisterSettings<ClientXWithThreeSettings>(secret);

        await SetConfiguration(CreateConfiguration(allowUpdatedRegistrations: false));

        var result1 = await TryRegisterSettings<ClientXWithTwoSettings>(secret);

        Assert.That(result1.IsSuccessStatusCode, Is.False, "Updated registrations should be prevented");

        var result2 = await TryRegisterSettings<ClientXWithThreeSettings>(secret);

        Assert.That(result2.IsSuccessStatusCode, Is.True, "Identical registrations should succeed");
    }

    [Test]
    public async Task ShallPreventFileImports()
    {
        var path = GetConfigImportPath();
        await RegisterSettings<ThreeSettings>();
        var data = await ExportData(true);

        var import = JsonConvert.SerializeObject(data);

        await DeleteAllClients();

        await SetConfiguration(CreateConfiguration(allowFileImports: false));

        await File.WriteAllTextAsync(Path.Combine(path, "dataImport.json"), import);

        // Wait enough time for the file to be imported.
        await Task.Delay(200);

        var clients = (await GetAllClients()).ToList();

        Assert.That(clients.Count, Is.Zero);
    }

    [Test]
    public async Task ShallPreventOfflineSettings()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ThreeSettings>(secret);

        var clientStatus = CreateStatusRequest(FiveHundredMillisecondsAgo(), DateTime.UtcNow, 5000, true);

        var status1 = await GetStatus(settings.ClientName, secret, clientStatus);

        Assert.That(status1.AllowOfflineSettings, Is.True);

        await SetConfiguration(CreateConfiguration(allowOfflineSettings: false));

        var status2 = await GetStatus(settings.ClientName, secret, clientStatus);

        Assert.That(status2.AllowOfflineSettings, Is.False);
    }

    [Test]
    public async Task ShallOnlyAllowConfigurationUpdatesFromAdministrators()
    {
        var naughtyUser = NewUser(Guid.NewGuid().ToString());
        await CreateUser(naughtyUser);

        var loginResult = await Login(naughtyUser.Username, naughtyUser.Password);

        var result = await SetConfiguration(CreateConfiguration(allowNewRegistrations: false), loginResult.Token, false);

        Assert.That(result.IsSuccessStatusCode, Is.False);
        Assert.That((int)result.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized),
            "Only administrators can set configuration");
    }
}