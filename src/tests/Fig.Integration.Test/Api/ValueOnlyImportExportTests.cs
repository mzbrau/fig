using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Fig.Contracts.Authentication;
using Fig.Contracts.ImportExport;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class ValueOnlyImportExportTests : IntegrationTestBase
{
    [Test]
    public async Task ShallExportClient()
    {
        await RegisterSettings<AllSettingsAndTypes>();

        var data = await ExportValueOnlyData(false);

        Assert.That(data.ExportedAt, Is.GreaterThan(DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(1))));
        Assert.That(data.ExportedAt, Is.LessThan(DateTime.UtcNow.Add(TimeSpan.FromSeconds(1))));

        Assert.That(data.Clients.Count, Is.EqualTo(1));
        Assert.That(data.Clients.First().Settings.Count, Is.EqualTo(12));
    }

    [Test]
    public async Task ShallExportClients()
    {
        var allSettings = await RegisterSettings<AllSettingsAndTypes>();
        var threeSettings = await RegisterSettings<ThreeSettings>();

        var data = await ExportValueOnlyData(false);

        Assert.That(data.Clients.Count, Is.EqualTo(2));
        Assert.That(data.Clients.FirstOrDefault(a => a.Name == allSettings.ClientName)!.Settings.Count, Is.EqualTo(12));
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

        var data = await ExportValueOnlyData(false);

        const string updatedStringValue = "Update";
        const bool updateBoolValue = false;

        UpdateProperty(data, nameof(allSettings.StringSetting), updatedStringValue);
        UpdateProperty(data, nameof(allSettings.BoolSetting), updateBoolValue);

        await ImportValueOnlyData(data);

        var clients = await GetAllClients();
        var firstClient = clients.Single();
        
        Assert.That(firstClient.Settings.First(a => a.Name == nameof(allSettings.StringSetting)).Value?.GetValue(), Is.EqualTo(updatedStringValue));
        Assert.That(firstClient.Settings.First(a => a.Name == nameof(allSettings.BoolSetting)).Value?.GetValue(), Is.EqualTo(updateBoolValue));
    }

    [Test]
    public async Task ShallDeferImportForNotRegisteredClients()
    {
        var allSettings = await RegisterSettings<AllSettingsAndTypes>();

        var data = await ExportValueOnlyData(false);

        const string updatedStringValue = "Update";
        const bool updateBoolValue = false;

        UpdateProperty(data, nameof(allSettings.StringSetting), updatedStringValue);
        UpdateProperty(data, nameof(allSettings.BoolSetting), updateBoolValue);

        await DeleteClient(allSettings.ClientName);
        await ImportValueOnlyData(data);

        var deferredImports = await GetDeferredImports();
        Assert.That(deferredImports.Count, Is.EqualTo(1));
        Assert.That(deferredImports.Single().Name, Is.EqualTo(allSettings.ClientName));
    }

    [Test]
    public async Task ShallOnlyReturnDeferredImportsThatMatchClientFilter()
    {
        var allSettings = await RegisterSettings<AllSettingsAndTypes>();
        var clientA = await RegisterSettings<ClientA>();

        var data = await ExportValueOnlyData(false);

        await DeleteAllClients();
        await ImportValueOnlyData(data);

        var user = NewUser(role: Role.Administrator, clientFilter: allSettings.ClientName);
        await CreateUser(user);
        var loginResult = await Login(user.Username, user.Password);
        
        var deferredImports = await GetDeferredImports(loginResult.Token);
        Assert.That(deferredImports.Count, Is.EqualTo(1));
        Assert.That(deferredImports.Single().Name, Is.EqualTo(allSettings.ClientName));
    }
    
    [Test]
    public async Task ShallThrowExceptionWhenTryingToValueOnlyImportClientsThatDoNotMatchUserFilter()
    {
        var settings = await RegisterSettings<ThreeSettings>();
        await RegisterSettings<ClientA>();
        
        var user = NewUser();
        user.ClientFilter = settings.ClientName;
        await CreateUser(user);
        var loginResult = await Login(user.Username, user.Password);

        var data = await ExportValueOnlyData(false);

        var result = await ImportValueOnlyData(data, loginResult.Token);
        
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
    
    [Test]
    public async Task ShallOnlyValueOnlyExportClientsForUser()
    {
        var settings = await RegisterSettings<ThreeSettings>();
        await RegisterSettings<ClientA>();
        
        var user = NewUser(role: Role.Administrator, clientFilter: settings.ClientName);
        await CreateUser(user);
        var loginResult = await Login(user.Username, user.Password);
        
        var data = await ExportValueOnlyData(false, loginResult.Token);
        
        Assert.That(data.Clients.Count, Is.EqualTo(1));
        Assert.That(data.Clients.Single().Name, Is.EqualTo(settings.ClientName));
    }

    [Test]
    public async Task ShallApplyDeferredSettingsOnRegistration()
    {
        var allSettings = await RegisterSettings<AllSettingsAndTypes>();

        var data = await ExportValueOnlyData(false);

        const string updatedStringValue = "Update";
        const bool updateBoolValue = false;

        UpdateProperty(data, nameof(allSettings.StringSetting), updatedStringValue);
        UpdateProperty(data, nameof(allSettings.BoolSetting), updateBoolValue);

        await DeleteClient(allSettings.ClientName);
        await ImportValueOnlyData(data);

        await RegisterSettings<AllSettingsAndTypes>();

        var clients = await GetAllClients();
        var firstClient = clients.Single();
        
        Assert.That(firstClient.Settings.First(a => a.Name == nameof(allSettings.StringSetting)).Value?.GetValue(), Is.EqualTo(updatedStringValue));
        Assert.That(firstClient.Settings.First(a => a.Name == nameof(allSettings.BoolSetting)).Value?.GetValue(), Is.EqualTo(updateBoolValue));
    }
    
    [Test]
    public async Task ShallDeleteDeferredRegistrationAfterApply()
    {
        var allSettings = await RegisterSettings<AllSettingsAndTypes>();

        var data = await ExportValueOnlyData(false);

        const string updatedStringValue = "Update";
        const bool updateBoolValue = false;
        
        UpdateProperty(data, nameof(allSettings.StringSetting), updatedStringValue);
        UpdateProperty(data, nameof(allSettings.BoolSetting), updateBoolValue);

        await DeleteClient(allSettings.ClientName);
        await ImportValueOnlyData(data);

        await RegisterSettings<AllSettingsAndTypes>();

        var deferredImports = await GetDeferredImports();
        Assert.That(deferredImports.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task ShallUpdateAllPropertyTypes()
    {
        var secret = GetNewSecret();
        var allSettings = await RegisterSettings<AllSettingsAndTypes>(secret);

        var data = await ExportValueOnlyData(false);

        const string updatedStringValue = "Update";
        const int updatedNumber = 200;
        const long updatedLong = 1000;
        const double updatedDouble = 11.2;
        DateTime updatedDateTime = new DateTime(2000, 01, 01);
        TimeSpan updatedTimespan = TimeSpan.FromSeconds(30);
        const bool updateBoolValue = false;
        List<KeyValuePair<string, string>> updatedKeyValuePairs = new List<KeyValuePair<string, string>>()
        {
            new("a", "b"),
            new("c", "d"),

        };
        string updatedKvpJson = JsonConvert.SerializeObject(updatedKeyValuePairs);
        List<SomeSetting> updatedComplexSetting = new List<SomeSetting>()
        {
            new()
            {
                Key = "e",
                Value = "f",
                MyInt = 44
            }
        };

        UpdateProperty(data, nameof(allSettings.StringSetting), updatedStringValue);
        UpdateProperty(data, nameof(allSettings.IntSetting), updatedNumber);
        UpdateProperty(data, nameof(allSettings.LongSetting), updatedLong);
        UpdateProperty(data, nameof(allSettings.DoubleSetting), updatedDouble);
        UpdateProperty(data, nameof(allSettings.DateTimeSetting), updatedDateTime);
        UpdateProperty(data, nameof(allSettings.TimespanSetting), updatedTimespan);
        UpdateProperty(data, nameof(allSettings.BoolSetting), updateBoolValue);
        UpdateProperty(data, nameof(allSettings.KvpCollectionSetting), updatedKvpJson);
        UpdateProperty(data, nameof(allSettings.ObjectListSetting), updatedComplexSetting);

        await ImportValueOnlyData(data);

        var settings = await GetSettingsForClient(allSettings.ClientName, secret);
        allSettings.Update(settings);

        Assert.That(allSettings.StringSetting, Is.EqualTo(updatedStringValue));
        Assert.That(allSettings.IntSetting, Is.EqualTo(updatedNumber));
        Assert.That(allSettings.LongSetting, Is.EqualTo(updatedLong));
        Assert.That(allSettings.DoubleSetting, Is.EqualTo(updatedDouble));
        Assert.That(allSettings.DateTimeSetting, Is.EqualTo(updatedDateTime));
        Assert.That(allSettings.TimespanSetting, Is.EqualTo(updatedTimespan));
        Assert.That(allSettings.BoolSetting, Is.EqualTo(updateBoolValue));
        AssertJsonEquivalence(allSettings.KvpCollectionSetting, updatedKeyValuePairs);
        AssertJsonEquivalence(allSettings.ObjectListSetting, updatedComplexSetting);
    }

    [Test]
    public async Task ShallEncryptSecretSettingsForExport()
    {
        const string secretDefaultValue = "cat";
        await RegisterSettings<SecretSettings>();

        var encryptedData = await ExportValueOnlyData(false);

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
    public async Task ShallExcludeSecretSettingsForExport()
    {
        var settings = await RegisterSettings<SecretSettings>();

        var encryptedData = await ExportValueOnlyData(true);

        Assert.That(encryptedData.Clients.Count, Is.EqualTo(1));
        Assert.That(
            encryptedData.Clients.Single().Settings
                .Count, Is.EqualTo(1));
        Assert.That(encryptedData.Clients.Single().Settings.Single().Name, Is.EqualTo(nameof(settings.NoSecret)));
    }

    private void UpdateProperty(FigValueOnlyDataExportDataContract data, string propertyName, object value)
    {
        data.Clients.Single().Settings.First(a => a.Name == propertyName).Value = value;
    }
}