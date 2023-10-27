using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Fig.Contracts.Authentication;
using Fig.Contracts.ImportExport;
using Fig.Contracts.Settings;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class ImportExportTests : IntegrationTestBase
{
    [Test]
    public async Task ShallExportClient()
    {
        await RegisterSettings<AllSettingsAndTypes>();

        var data = await ExportData(false);

        Assert.That(data.ExportedAt, Is.GreaterThan(DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(1))));
        Assert.That(data.ExportedAt, Is.LessThan(DateTime.UtcNow.Add(TimeSpan.FromSeconds(1))));

        Assert.That(data.Clients.Count, Is.EqualTo(1));
        Assert.That(data.Clients.First().Settings.Count, Is.EqualTo(13));
    }

    [Test]
    public async Task ShallExportClients()
    {
        var allSettings = await RegisterSettings<AllSettingsAndTypes>();
        var threeSettings = await RegisterSettings<ThreeSettings>();

        var data = await ExportData(false);

        Assert.That(data.Clients.Count, Is.EqualTo(2));
        Assert.That(data.Clients.FirstOrDefault(a => a.Name == allSettings.ClientName)!.Settings.Count, Is.EqualTo(13));
        Assert.That(data.Clients.FirstOrDefault(a => a.Name == threeSettings.ClientName)!.Settings.Count,
            Is.EqualTo(3));
    }

    [Test]
    public async Task ShallOnlyAllowExportFromAuthenticatedUsers()
    {
        await RegisterSettings<AllSettingsAndTypes>();

        using var httpClient = GetHttpClient();

        var result = await httpClient.GetAsync("/data");

        Assert.That((int) result.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized),
            "Export is not available to unauthorized users");
    }

    [Test]
    public async Task ShallOnlyAllowExportFromAdministrators()
    {
        await RegisterSettings<AllSettingsAndTypes>();

        var naughtyUser = NewUser("naughtyUser");
        await CreateUser(naughtyUser);

        var loginResult = await Login(naughtyUser.Username, naughtyUser.Password);

        using var httpClient = GetHttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", loginResult.Token);

        var result = await httpClient.GetAsync("/data");

        Assert.That((int) result.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized),
            "Only administrators are able to export data");
    }

    [Test]
    public async Task ShallImportUsingClearAndImportImportType()
    {
        await RegisterSettings<AllSettingsAndTypes>();

        var data1 = await ExportData(true);
        data1.ImportType = ImportType.ClearAndImport;

        data1.Clients[0].Name = "UpdatedName";

        await ImportData(data1);

        var data2 = await ExportData(true);

        data1.ExportedAt = DateTime.MinValue;
        data2.ExportedAt = DateTime.MinValue;
        data2.ImportType = ImportType.ClearAndImport;

        AssertJsonEquivalence(data1, data2);
    }

    [Test]
    public async Task ShallImportUsingAddNewImportType()
    {
        var allSettings = await RegisterSettings<AllSettingsAndTypes>();
        var threeSettings = await RegisterSettings<ThreeSettings>();

        var data1 = await ExportData(false);

        await DeleteClient(allSettings.ClientName);

        data1.ImportType = ImportType.AddNew;
        var clientToUpdate = data1.Clients.FirstOrDefault(a => a.Name == threeSettings.ClientName);
        clientToUpdate?.Settings.Clear();

        await ImportData(data1);

        var data2 = await ExportData(false);

        Assert.That(data2.Clients.Count, Is.EqualTo(2));

        var allSettingsClient = data2.Clients.FirstOrDefault(a => a.Name == allSettings.ClientName);
        Assert.That(allSettingsClient, Is.Not.Null, "Client should have been imported");
        Assert.That(allSettingsClient?.Settings.Count, Is.EqualTo(13));

        var threeSettingsClient = data2.Clients.FirstOrDefault(a => a.Name == threeSettings.ClientName);
        Assert.That(threeSettingsClient, Is.Not.Null, "Name change should have been ignored");
        Assert.That(threeSettingsClient?.Settings.Count, Is.EqualTo(3));
    }

    [Test]
    public async Task ShallImportUsingReplaceExistingImportType()
    {
        var allSettings = await RegisterSettings<AllSettingsAndTypes>();
        var threeSettings = await RegisterSettings<ThreeSettings>();

        var data1 = await ExportData(false);

        var clientA = await RegisterSettings<ClientA>();

        data1.ImportType = ImportType.ReplaceExisting;
        var clientToUpdate = data1.Clients.First(a => a.Name == threeSettings.ClientName);
        clientToUpdate.Settings.Clear();

        await ImportData(data1);

        var data2 = await ExportData(false);


        Assert.That(data2.Clients.Count, Is.EqualTo(3));

        var allSettingsClient = data2.Clients.FirstOrDefault(a => a.Name == allSettings.ClientName);
        Assert.That(allSettingsClient, Is.Not.Null, "Client should have been re-imported");
        Assert.That(allSettingsClient!.Settings.Count, Is.EqualTo(13));

        var threeSettingsClient = data2.Clients.FirstOrDefault(a => a.Name == threeSettings.ClientName);
        Assert.That(threeSettingsClient, Is.Not.Null);
        Assert.That(threeSettingsClient!.Settings.Count, Is.EqualTo(0), "Client should have been updated");

        var clientAClient = data2.Clients.FirstOrDefault(a => a.Name == clientA.ClientName);
        Assert.That(clientAClient, Is.Not.Null, "Client should not have been touched");
        Assert.That(clientAClient!.Settings.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task ShallImportAndExportVerifications()
    {
        var settings = await RegisterSettings<SettingsWithVerification>();

        var data1 = await ExportData(true);

        await DeleteClient(settings.ClientName);

        await ImportData(data1);

        var data2 = await ExportData(true);

        Assert.That(data2.Clients.Count, Is.EqualTo(1));
        Assert.That(data2.Clients.First().Verifications.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task ShallEncryptSecretsForExports()
    {
        const string secretDefaultValue = "cat";
        await RegisterSettings<SecretSettings>();

        var encryptedData = await ExportData(false);

        Assert.That(encryptedData.Clients.Count, Is.EqualTo(1));
        Assert.That(
            encryptedData.Clients.Single().Settings
                .First(a => a.Name == nameof(SecretSettings.SecretWithDefault)).Value,
            Is.Not.EqualTo(secretDefaultValue));
        Assert.That(encryptedData.Clients.Single().Settings
            .First(a => a.Name == nameof(SecretSettings.SecretWithDefault))
            .IsEncrypted, Is.True);
    }

    [Test]
    public async Task ShallExcludeSecretSettings()
    {
        var settings = await RegisterSettings<SecretSettings>();

        var encryptedData = await ExportData(true);

        Assert.That(encryptedData.Clients.Count, Is.EqualTo(1));
        Assert.That(
            encryptedData.Clients.Single().Settings
                .Count, Is.EqualTo(1));
        Assert.That(encryptedData.Clients.Single().Settings.Single().Name, Is.EqualTo(nameof(settings.NoSecret)));
    }

    [Test]
    public async Task ShallNotDeleteAnySettingsOnImportFailure()
    {
        await RegisterSettings<SettingsWithVerification>();
        var settings = await RegisterSettings<AllSettingsAndTypes>();

        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.SecretSetting), new StringSettingDataContract("secret value"))
        };

        await SetSettings(settings.ClientName, settingsToUpdate);

        var data1 = await ExportData(false);
        data1.ImportType = ImportType.ClearAndImport;
        var secretSetting = data1.Clients.FirstOrDefault(a => a.Name == settings.ClientName)!.Settings
            .FirstOrDefault(a => a.Name == nameof(settings.SecretSetting));
        secretSetting!.Value = new StringSettingDataContract("notencrypted");
        secretSetting.IsEncrypted = true;

        var result = await ImportData(data1);

        Assert.That(result.ErrorMessage, Is.Not.Null);
        var clients = (await GetAllClients()).ToList();
        Assert.That(clients.Count(), Is.EqualTo(2));
        var allSettingsClient = clients.FirstOrDefault(a => a.Name == settings.ClientName);
        Assert.That(allSettingsClient!.Settings.Count, Is.EqualTo(13));
    }

    [Test]
    public async Task ShallThrowExceptionWhenTryingToImportClientsThatDoNotMatchUserFilter()
    {
        var settings = await RegisterSettings<ThreeSettings>();
        await RegisterSettings<ClientA>();
        
        var user = NewUser();
        user.ClientFilter = settings.ClientName;
        await CreateUser(user);
        var loginResult = await Login(user.Username, user.Password);

        var data = await ExportData(false);

        var result = await ImportData(data, loginResult.Token, false);
        
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task ShallOnlyExportClientsForUser()
    {
        var settings = await RegisterSettings<ThreeSettings>();
        await RegisterSettings<ClientA>();
        
        var user = NewUser(role: Role.Administrator, clientFilter: settings.ClientName);
        await CreateUser(user);
        var loginResult = await Login(user.Username, user.Password);
        
        var data = await ExportData(false, loginResult.Token);
        
        Assert.That(data.Clients.Count, Is.EqualTo(1));
        Assert.That(data.Clients.Single().Name, Is.EqualTo(settings.ClientName));
    }
}