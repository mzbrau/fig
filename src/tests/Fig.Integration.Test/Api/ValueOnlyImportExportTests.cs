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

        var data = await ExportValueOnlyData();

        Assert.That(data.ExportedAt, Is.GreaterThan(DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(1))));
        Assert.That(data.ExportedAt, Is.LessThan(DateTime.UtcNow.Add(TimeSpan.FromSeconds(1))));

        Assert.That(data.Clients.Count, Is.EqualTo(1));
        Assert.That(data.Clients.First().Settings.Count, Is.EqualTo(14));
    }

    [Test]
    public async Task ShallExportClients()
    {
        var allSettings = await RegisterSettings<AllSettingsAndTypes>();
        var threeSettings = await RegisterSettings<ThreeSettings>();

        var data = await ExportValueOnlyData();

        Assert.That(data.Clients.Count, Is.EqualTo(2));
        Assert.That(data.Clients.FirstOrDefault(a => a.Name == allSettings.ClientName)!.Settings.Count, Is.EqualTo(14));
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

        var loginResult = await Login(naughtyUser.Username, naughtyUser.Password ?? throw new InvalidOperationException("Password is null"));

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

        var data = await ExportValueOnlyData();

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
    public async Task ShallApplyMultipleDeferImportsForNotRegisteredClients()
    {
        var allSettings = await RegisterSettings<AllSettingsAndTypes>();

        var data = await ExportValueOnlyData();

        const string updatedStringValue = "Update";
        const bool updateBoolValue = false;
        const int updateIntValue = 19;

        UpdateProperty(data, nameof(allSettings.StringSetting), "value to be overridden");
        UpdateProperty(data, nameof(allSettings.BoolSetting), updateBoolValue);

        await DeleteClient(allSettings.ClientName);
        await ImportValueOnlyData(data);
        
        UpdateProperty(data, nameof(allSettings.StringSetting), updatedStringValue);
        UpdateProperty(data, nameof(allSettings.IntSetting), updateIntValue);
        await ImportValueOnlyData(data);

        var deferredImports = await GetDeferredImports();
        Assert.That(deferredImports.Count, Is.EqualTo(2));
        
        await RegisterSettings<AllSettingsAndTypes>();
        var settings = (await GetAllClients()).Single().Settings;
        Assert.That(
            settings.FirstOrDefault(a => a.Name == nameof(allSettings.StringSetting))?.Value?.GetValue(),
            Is.EqualTo(updatedStringValue));
        Assert.That(
            settings.FirstOrDefault(a => a.Name == nameof(allSettings.BoolSetting))?.Value?.GetValue(),
            Is.EqualTo(updateBoolValue));
        Assert.That(
            settings.FirstOrDefault(a => a.Name == nameof(allSettings.IntSetting))?.Value?.GetValue(),
            Is.EqualTo(updateIntValue));
    }

    [Test]
    public async Task ShallOnlyReturnDeferredImportsThatMatchClientFilter()
    {
        var allSettings = await RegisterSettings<AllSettingsAndTypes>();

        var data = await ExportValueOnlyData();

        await DeleteAllClients();
        await ImportValueOnlyData(data);

        var user = NewUser(role: Role.Administrator, clientFilter: allSettings.ClientName);
        await CreateUser(user);
        var loginResult = await Login(user.Username, user.Password ?? throw new InvalidOperationException("Password is null"));
        
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
        var loginResult = await Login(user.Username, user.Password ?? throw new InvalidOperationException("Password is null"));

        var data = await ExportValueOnlyData();

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
        var loginResult = await Login(user.Username, user.Password ?? throw new InvalidOperationException("Password is null"));
        
        var data = await ExportValueOnlyData(loginResult.Token);
        
        Assert.That(data.Clients.Count, Is.EqualTo(1));
        Assert.That(data.Clients.Single().Name, Is.EqualTo(settings.ClientName));
    }

    [Test]
    public async Task ShallApplyDeferredSettingsOnRegistration()
    {
        var allSettings = await RegisterSettings<AllSettingsAndTypes>();

        var data = await ExportValueOnlyData();

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

        var data = await ExportValueOnlyData();

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
        var (allSettings, configuration) = InitializeConfigurationProvider<AllSettingsAndTypes>(secret);

        var data = await ExportValueOnlyData();
    
        const string updatedStringValue = "Update";
        const int updatedNumber = 200;
        const long updatedLong = 1000;
        const double updatedDouble = 11.2;
        DateTime updatedDateTime = new DateTime(2000, 01, 01);
        TimeSpan updatedTimespan = TimeSpan.FromSeconds(30);
        const bool updateBoolValue = false;
        var someSetting = new SomeSetting
        {
            Key = "some key",
            Value = "some value",
            MyInt = 99
        };
        List<SomeSetting> updatedComplexSetting = new List<SomeSetting>()
        {
            new()
            {
                Key = "e",
                Value = "f",
                MyInt = 44
            }
        };
        Pets updatedPet = Pets.Fish;
        var updatedJson = JsonConvert.SerializeObject(someSetting);
    
        UpdateProperty(data, nameof(allSettings.CurrentValue.StringSetting), updatedStringValue);
        UpdateProperty(data, nameof(allSettings.CurrentValue.IntSetting), updatedNumber);
        UpdateProperty(data, nameof(allSettings.CurrentValue.LongSetting), updatedLong);
        UpdateProperty(data, nameof(allSettings.CurrentValue.DoubleSetting), updatedDouble);
        UpdateProperty(data, nameof(allSettings.CurrentValue.DateTimeSetting), updatedDateTime);
        UpdateProperty(data, nameof(allSettings.CurrentValue.TimespanSetting), updatedTimespan);
        UpdateProperty(data, nameof(allSettings.CurrentValue.BoolSetting), updateBoolValue);
        UpdateProperty(data, nameof(allSettings.CurrentValue.JsonSetting), updatedJson);
        UpdateProperty(data, nameof(allSettings.CurrentValue.EnumSetting), updatedPet);
        UpdateProperty(data, nameof(allSettings.CurrentValue.ObjectListSetting), updatedComplexSetting);
    
        await ImportValueOnlyData(data);
    
        configuration.Reload();
    
        Assert.That(allSettings.CurrentValue.StringSetting, Is.EqualTo(updatedStringValue));
        Assert.That(allSettings.CurrentValue.IntSetting, Is.EqualTo(updatedNumber));
        Assert.That(allSettings.CurrentValue.LongSetting, Is.EqualTo(updatedLong));
        Assert.That(allSettings.CurrentValue.DoubleSetting, Is.EqualTo(updatedDouble));
        Assert.That(allSettings.CurrentValue.DateTimeSetting, Is.EqualTo(updatedDateTime));
        Assert.That(allSettings.CurrentValue.TimespanSetting, Is.EqualTo(updatedTimespan));
        Assert.That(allSettings.CurrentValue.BoolSetting, Is.EqualTo(updateBoolValue));
        AssertJsonEquivalence(allSettings.CurrentValue.JsonSetting, someSetting);
        AssertJsonEquivalence(allSettings.CurrentValue.ObjectListSetting, updatedComplexSetting);
    }

    [Test]
    public async Task ShallEncryptSecretSettingsForExport()
    {
        const string secretDefaultValue = "cat";
        await RegisterSettings<SecretSettings>();

        var encryptedData = await ExportValueOnlyData();

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
    public async Task ShallPropagateExternallyManagedSettings()
    {
        await RegisterSettings<AllSettingsAndTypes>();

        var data = await ExportValueOnlyData();

        data.IsExternallyManaged = true;
        data.Clients.First().Settings.FirstOrDefault(a => a.Name == nameof(AllSettingsAndTypes.DoubleSetting))!.IsExternallyManaged = false;

        await ImportValueOnlyData(data);

        var clients = (await GetAllClients()).ToList();
        
        Assert.That(clients!.Single().Settings.FirstOrDefault(a => a.Name == nameof(AllSettingsAndTypes.DoubleSetting))!.IsExternallyManaged, Is.False);
        Assert.That(clients!.Single().Settings.Last().IsExternallyManaged, Is.True);
    }

    [Test]
    public async Task ShallNotImportUpdateValuesInitOnlyForRegisteredClient()
    {
        await RegisterSettings<ThreeSettings>();

        var data = await ExportValueOnlyData();
        const string updatedStringValue = "Update";
        data.ImportType = ImportType.UpdateValuesInitOnly;
        data.Clients.Single().Settings.First(a => a.Name == nameof(ThreeSettings.AStringSetting)).Value = updatedStringValue;

        await ImportValueOnlyData(data);

        var clients = await GetAllClients();
        var firstClient = clients.Single();
        
        Assert.That(firstClient.Settings.First(a => a.Name == nameof(ThreeSettings.AStringSetting)).Value?.GetValue(), 
            Is.Not.EqualTo(updatedStringValue), "UpdateValuesInitOnly should not update already registered clients");
    }

    [Test]
    public async Task ShallStoreDeferredImportForUpdateValuesInitOnly()
    {
        var settings = await RegisterSettings<ThreeSettings>();

        var data = await ExportValueOnlyData();
        const string updatedStringValue = "Update";
        data.ImportType = ImportType.UpdateValuesInitOnly;
        data.Clients.Single().Settings.First(a => a.Name == nameof(ThreeSettings.AStringSetting)).Value = updatedStringValue;
        
        await DeleteClient(settings.ClientName);
        await ImportValueOnlyData(data);
        
        await RegisterSettings<ThreeSettings>();

        var clients = await GetAllClients();
        var firstClient = clients.Single();
        
        Assert.That(firstClient.Settings.First(a => a.Name == nameof(ThreeSettings.AStringSetting)).Value?.GetValue(), 
            Is.EqualTo(updatedStringValue), "Deferred import should be applied if using updateValuesInitOnly");
    }

    [Test]
    public async Task ShallExportAllSettingsWhenNotExcludingEnvironmentSpecific()
    {
        await RegisterSettings<AllSettingsAndTypes>();

        var data = await ExportValueOnlyData(excludeEnvironmentSpecific: false);

        Assert.That(data.Clients.Count, Is.EqualTo(1));
        var client = data.Clients.Single();
        Assert.That(client.Settings.Count, Is.EqualTo(14)); // All settings should be included (including the new environment-specific one)
        
        // Verify environment-specific setting is present
        Assert.That(client.Settings.Any(s => s.Name == nameof(AllSettingsAndTypes.EnvironmentSpecificSetting)), Is.True);
    }

    [Test]
    public async Task ShallExcludeEnvironmentSpecificSettingsFromExport()
    {
        await RegisterSettings<AllSettingsAndTypes>();

        var data = await ExportValueOnlyData(excludeEnvironmentSpecific: true);

        Assert.That(data.Clients.Count, Is.EqualTo(1));
        var client = data.Clients.Single();
        Assert.That(client.Settings.Count, Is.EqualTo(13)); // One less setting due to exclusion
        
        // Verify environment-specific setting is excluded
        Assert.That(client.Settings.Any(s => s.Name == nameof(AllSettingsAndTypes.EnvironmentSpecificSetting)), Is.False);
        
        // Verify other settings are still present
        Assert.That(client.Settings.Any(s => s.Name == nameof(AllSettingsAndTypes.StringSetting)), Is.True);
        Assert.That(client.Settings.Any(s => s.Name == nameof(AllSettingsAndTypes.IntSetting)), Is.True);
    }

    [Test]
    public async Task ShallExcludeEnvironmentSpecificSettingsFromMultipleClients()
    {
        await RegisterSettings<AllSettingsAndTypes>();
        await RegisterSettings<ThreeSettings>();

        var data = await ExportValueOnlyData(excludeEnvironmentSpecific: true);

        Assert.That(data.Clients.Count, Is.EqualTo(2));
        
        var allSettingsClient = data.Clients.FirstOrDefault(c => c.Name == "AllSettingsAndTypes");
        Assert.That(allSettingsClient, Is.Not.Null);
        Assert.That(allSettingsClient!.Settings.Count, Is.EqualTo(13)); // Excluding environment-specific setting
        Assert.That(allSettingsClient.Settings.Any(s => s.Name == nameof(AllSettingsAndTypes.EnvironmentSpecificSetting)), Is.False);
        
        var threeSettingsClient = data.Clients.FirstOrDefault(c => c.Name == "ThreeSettings");
        Assert.That(threeSettingsClient, Is.Not.Null);
        Assert.That(threeSettingsClient!.Settings.Count, Is.EqualTo(3)); // All settings from ThreeSettings (none are environment-specific)
    }

    [Test]
    public async Task ShallMaintainSettingValuesWhenExcludingEnvironmentSpecific()
    {
        var settings = await RegisterSettings<AllSettingsAndTypes>();

        var data = await ExportValueOnlyData(excludeEnvironmentSpecific: true);

        var client = data.Clients.Single();
        var stringSetting = client.Settings.First(s => s.Name == nameof(AllSettingsAndTypes.StringSetting));
        var intSetting = client.Settings.First(s => s.Name == nameof(AllSettingsAndTypes.IntSetting));
        
        Assert.That(stringSetting.Value, Is.EqualTo("Cat"));
        Assert.That(intSetting.Value, Is.EqualTo(34));
        
        // Verify environment-specific setting is not present
        Assert.That(client.Settings.Any(s => s.Name == nameof(AllSettingsAndTypes.EnvironmentSpecificSetting)), Is.False);
    }

    [Test]
    public async Task ShallHandleImportWithExcludedEnvironmentSpecificSettings()
    {
        await RegisterSettings<AllSettingsAndTypes>();

        // Export with environment-specific excluded
        var data = await ExportValueOnlyData(excludeEnvironmentSpecific: true);
        
        // Update some values
        const string updatedStringValue = "Updated";
        const int updatedIntValue = 999;
        
        UpdateProperty(data, nameof(AllSettingsAndTypes.StringSetting), updatedStringValue);
        UpdateProperty(data, nameof(AllSettingsAndTypes.IntSetting), updatedIntValue);

        // Import the data
        await ImportValueOnlyData(data);

        // Verify the changes were applied
        var clients = await GetAllClients();
        var client = clients.Single();
        
        Assert.That(client.Settings.First(a => a.Name == nameof(AllSettingsAndTypes.StringSetting)).Value?.GetValue(), Is.EqualTo(updatedStringValue));
        Assert.That(client.Settings.First(a => a.Name == nameof(AllSettingsAndTypes.IntSetting)).Value?.GetValue(), Is.EqualTo(updatedIntValue));
        
        // Verify environment-specific setting remained unchanged
        Assert.That(client.Settings.First(a => a.Name == nameof(AllSettingsAndTypes.EnvironmentSpecificSetting)).Value?.GetValue(), Is.EqualTo("EnvSpecific"));
    }

    private void UpdateProperty(FigValueOnlyDataExportDataContract data, string propertyName, object value)
    {
        data.Clients.Single().Settings.First(a => a.Name == propertyName).Value = value;
    }
}