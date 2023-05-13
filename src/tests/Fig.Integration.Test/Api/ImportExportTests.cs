using System;
using System.Linq;
using System.Threading.Tasks;
using Fig.Contracts.ImportExport;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class ImportExportTests : IntegrationTestBase
{
    [Test]
    public async Task ShallExportClient()
    {
        await RegisterSettings<AllSettingsAndTypes>();

        var data = await ExportData(true);

        Assert.That(data.ExportedAt, Is.GreaterThan(DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(1))));
        Assert.That(data.ExportedAt, Is.LessThan(DateTime.UtcNow.Add(TimeSpan.FromSeconds(1))));

        Assert.That(data.Clients.Count, Is.EqualTo(1));
        Assert.That(data.Clients.First().Settings.Count, Is.EqualTo(11));
    }

    [Test]
    public async Task ShallExportClients()
    {
        var allSettings = await RegisterSettings<AllSettingsAndTypes>();
        var threeSettings = await RegisterSettings<ThreeSettings>();

        var data = await ExportData(true);

        Assert.That(data.Clients.Count, Is.EqualTo(2));
        Assert.That(data.Clients.FirstOrDefault(a => a.Name == allSettings.ClientName)!.Settings.Count, Is.EqualTo(11));
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

        var data1Json = JsonConvert.SerializeObject(data1);
        var data2Json = JsonConvert.SerializeObject(data2);

        Assert.That(data2Json, Is.EqualTo(data1Json));
    }

    [Test]
    public async Task ShallImportUsingAddNewImportType()
    {
        var allSettings = await RegisterSettings<AllSettingsAndTypes>();
        var threeSettings = await RegisterSettings<ThreeSettings>();

        var data1 = await ExportData(true);

        await DeleteClient(allSettings.ClientName);

        data1.ImportType = ImportType.AddNew;
        var clientToUpdate = data1.Clients.FirstOrDefault(a => a.Name == threeSettings.ClientName);
        clientToUpdate?.Settings.Clear();

        await ImportData(data1);

        var data2 = await ExportData(true);

        Assert.That(data2.Clients.Count, Is.EqualTo(2));

        var allSettingsClient = data2.Clients.FirstOrDefault(a => a.Name == allSettings.ClientName);
        Assert.That(allSettingsClient, Is.Not.Null, "Client should have been imported");
        Assert.That(allSettingsClient?.Settings.Count, Is.EqualTo(11));

        var threeSettingsClient = data2.Clients.FirstOrDefault(a => a.Name == threeSettings.ClientName);
        Assert.That(threeSettingsClient, Is.Not.Null, "Name change should have been ignored");
        Assert.That(threeSettingsClient?.Settings.Count, Is.EqualTo(3));
    }

    [Test]
    public async Task ShallImportUsingReplaceExistingImportType()
    {
        var allSettings = await RegisterSettings<AllSettingsAndTypes>();
        var threeSettings = await RegisterSettings<ThreeSettings>();

        var data1 = await ExportData(true);

        var clientA = await RegisterSettings<ClientA>();

        data1.ImportType = ImportType.ReplaceExisting;
        var clientToUpdate = data1.Clients.First(a => a.Name == threeSettings.ClientName);
        clientToUpdate.Settings.Clear();

        await ImportData(data1);

        var data2 = await ExportData(true);


        Assert.That(data2.Clients.Count, Is.EqualTo(3));

        var allSettingsClient = data2.Clients.FirstOrDefault(a => a.Name == allSettings.ClientName);
        Assert.That(allSettingsClient, Is.Not.Null, "Client should have been re-imported");
        Assert.That(allSettingsClient!.Settings.Count, Is.EqualTo(11));

        var threeSettingsClient = data2.Clients.FirstOrDefault(a => a.Name == threeSettings.ClientName);
        Assert.That(threeSettingsClient, Is.Not.Null);
        Assert.That(threeSettingsClient!.Settings.Count, Is.EqualTo(0), "Client should have been updated");

        var clientAClient = data2.Clients.FirstOrDefault(a => a.Name == clientA.ClientName);
        Assert.That(clientAClient, Is.Not.Null, "Client should not have been touched");
        Assert.That(clientAClient!.Settings.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task ShallImportAndExportValidators()
    {
        var settings = await RegisterSettings<SettingsWithVerifications>();

        var data1 = await ExportData(true);

        await DeleteClient(settings.ClientName);

        await ImportData(data1);

        var data2 = await ExportData(true);

        Assert.That(data2.Clients.Count, Is.EqualTo(1));
        Assert.That(data2.Clients.First().DynamicVerifications.Count, Is.EqualTo(1));
        Assert.That(data2.Clients.First().PluginVerifications.Count, Is.EqualTo(1));
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
            .IsEncrypted, Is.Not.Null);

        var decryptedData = await ExportData(true);

        Assert.That(decryptedData.Clients.Count, Is.EqualTo(1));
        Assert.That(
            decryptedData.Clients.Single().Settings
                .First(a => a.Name == nameof(SecretSettings.SecretWithDefault)).Value?.GetValue(),
            Is.EqualTo(secretDefaultValue));
        Assert.That(decryptedData.Clients.Single().Settings
            .First(a => a.Name == nameof(SecretSettings.SecretWithDefault))
            .IsEncrypted, Is.False);
    }
}