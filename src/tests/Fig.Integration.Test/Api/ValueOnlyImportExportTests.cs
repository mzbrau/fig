using System;
using System.Linq;
using System.Threading.Tasks;
using Fig.Integration.Test.Api.TestSettings;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class ValueOnlyImportExportTests : IntegrationTestBase
{
    [Test]
    public async Task ShallExportClient()
    {
        await RegisterSettings<AllSettingsAndTypes>();

        var data = await ExportValueOnlyData();

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

        var data = await ExportValueOnlyData();

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

        var result = await httpClient.GetAsync("/valueonlydata");

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

        var result = await httpClient.GetAsync("/valueonlydata");

        Assert.That((int) result.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized),
            "Only administrators are able to export data");
    }
    

    [Test]
    public async Task ShallImportValueOnlySettingsForRegisteredClient()
    {
        var allSettings = await RegisterSettings<AllSettingsAndTypes>();

        var data = await ExportValueOnlyData();

        const string updatedStringValue = "Update";
        const bool updateBoolValue = false;
        
        data.Clients.Single().Settings.First(a => a.Name == nameof(allSettings.StringSetting)).Value = updatedStringValue;
        data.Clients.Single().Settings.First(a => a.Name == nameof(allSettings.BoolSetting)).Value = updateBoolValue;

        await ImportValueOnlyData(data);

        var clients = await GetAllClients();
        var firstClient = clients.Single();
        
        Assert.That(firstClient.Settings.First(a => a.Name == nameof(allSettings.StringSetting)).Value, Is.EqualTo(updatedStringValue));
        Assert.That(firstClient.Settings.First(a => a.Name == nameof(allSettings.BoolSetting)).Value, Is.EqualTo(updateBoolValue));
    }

    [Test]
    public async Task ShallDeferImportForNotRegisteredClients()
    {
        var allSettings = await RegisterSettings<AllSettingsAndTypes>();

        var data = await ExportValueOnlyData();

        const string updatedStringValue = "Update";
        const bool updateBoolValue = false;
        
        data.Clients.Single().Settings.First(a => a.Name == nameof(allSettings.StringSetting)).Value = updatedStringValue;
        data.Clients.Single().Settings.First(a => a.Name == nameof(allSettings.BoolSetting)).Value = updateBoolValue;

        await DeleteClient(allSettings.ClientName);
        await ImportValueOnlyData(data);


        var deferredImports = await GetDeferredImports();
        Assert.That(deferredImports.Count, Is.EqualTo(1));
        Assert.That(deferredImports.Single().Name, Is.EqualTo(allSettings.ClientName));
    }

    [Test]
    public async Task ShallApplyDeferredSettingsOnRegistration()
    {
        var allSettings = await RegisterSettings<AllSettingsAndTypes>();

        var data = await ExportValueOnlyData();

        const string updatedStringValue = "Update";
        const bool updateBoolValue = false;
        
        data.Clients.Single().Settings.First(a => a.Name == nameof(allSettings.StringSetting)).Value = updatedStringValue;
        data.Clients.Single().Settings.First(a => a.Name == nameof(allSettings.BoolSetting)).Value = updateBoolValue;

        await DeleteClient(allSettings.ClientName);
        await ImportValueOnlyData(data);

        await RegisterSettings<AllSettingsAndTypes>();

        var clients = await GetAllClients();
        var firstClient = clients.Single();
        
        Assert.That(firstClient.Settings.First(a => a.Name == nameof(allSettings.StringSetting)).Value, Is.EqualTo(updatedStringValue));
        Assert.That(firstClient.Settings.First(a => a.Name == nameof(allSettings.BoolSetting)).Value, Is.EqualTo(updateBoolValue));
    }
    
    [Test]
    public async Task ShallDeleteDeferredRegistrationAfterApply()
    {
        var allSettings = await RegisterSettings<AllSettingsAndTypes>();

        var data = await ExportValueOnlyData();

        const string updatedStringValue = "Update";
        const bool updateBoolValue = false;
        
        data.Clients.Single().Settings.First(a => a.Name == nameof(allSettings.StringSetting)).Value = updatedStringValue;
        data.Clients.Single().Settings.First(a => a.Name == nameof(allSettings.BoolSetting)).Value = updateBoolValue;

        await DeleteClient(allSettings.ClientName);
        await ImportValueOnlyData(data);

        await RegisterSettings<AllSettingsAndTypes>();

        var deferredImports = await GetDeferredImports();
        Assert.That(deferredImports.Count, Is.EqualTo(0));
    }
}