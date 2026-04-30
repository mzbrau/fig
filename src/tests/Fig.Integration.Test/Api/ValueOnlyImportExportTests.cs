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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        if (data.Clients.Count > 1)
        {
            var clients = string.Join(",", data.Clients.Select(a => $"{a.Name} ({a.Instance})"));
            Assert.Fail(
                $"Expected only 1 client in export data but found {clients})");
        }
        
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
    public async Task ShallEncryptSecretDataGridColumnsForValueOnlyExport()
    {
        await RegisterSettings<SecretSettings>();

        var encryptedData = await ExportValueOnlyData();

        var loginsWithDefault = encryptedData.Clients.Single().Settings
            .First(a => a.Name == nameof(SecretSettings.LoginsWithDefault));

        Assert.That(loginsWithDefault.IsEncrypted, Is.True,
            "DataGrid with secret columns should have IsEncrypted set on value-only export");

        var rows = (loginsWithDefault.Value as JArray)?.ToObject<List<Dictionary<string, object?>>>();
        Assert.That(rows, Is.Not.Null);

        var defaultLogins = SecretSettings.GetDefaultLogins();
        var index = 0;
        foreach (var row in rows!)
        {
            Assert.That(row[nameof(Fig.Test.Common.TestSettings.Login.Username)], Is.EqualTo(defaultLogins[index].Username),
                "Non-secret columns should remain in plain text");
            Assert.That(row[nameof(Fig.Test.Common.TestSettings.Login.Password)]?.ToString(), Is.Not.EqualTo(defaultLogins[index].Password),
                "Secret columns should be encrypted");
            index++;
        }
    }

    [Test]
    public async Task ShallDecryptSecretDataGridColumnsOnValueOnlyImport()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<SecretSettings>(secret);

        var encryptedData = await ExportValueOnlyData();

        await ImportValueOnlyData(encryptedData);

        var settingsAfterImport = await GetSettingsForClient(settings.ClientName, secret);
        var listSetting = settingsAfterImport.FirstOrDefault(a => a.Name == nameof(SecretSettings.LoginsWithDefault));
        var listSettingValue = (listSetting?.Value as DataGridSettingDataContract)?.GetValue() as List<Dictionary<string, object?>>;

        var defaultLogins = SecretSettings.GetDefaultLogins();
        var index = 0;
        foreach (var row in listSettingValue ?? [])
        {
            Assert.That(row[nameof(Fig.Test.Common.TestSettings.Login.Username)], Is.EqualTo(defaultLogins[index].Username));
            Assert.That(row[nameof(Fig.Test.Common.TestSettings.Login.Password)], Is.EqualTo(defaultLogins[index].Password));
            Assert.That(row[nameof(Fig.Test.Common.TestSettings.Login.AnotherSecret)], Is.EqualTo(defaultLogins[index].AnotherSecret));
            index++;
        }
    }

    [Test]
    public async Task ShallImportUnencryptedSecretDataGridColumnsOnValueOnlyImport()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<SecretSettings>(secret);

        var plainData = await ExportValueOnlyData();

        var loginsWithDefault = plainData.Clients.Single().Settings
            .First(a => a.Name == nameof(SecretSettings.LoginsWithDefault));

        // Simulate an unencrypted import (backward compatibility)
        var defaultLogins = SecretSettings.GetDefaultLogins();
        loginsWithDefault.Value = defaultLogins.Select(l => new Dictionary<string, object?>
        {
            [nameof(Fig.Test.Common.TestSettings.Login.Username)] = l.Username,
            [nameof(Fig.Test.Common.TestSettings.Login.Password)] = "newPassword",
            [nameof(Fig.Test.Common.TestSettings.Login.AnotherSecret)] = l.AnotherSecret
        }).ToList();
        loginsWithDefault.IsEncrypted = false;

        await ImportValueOnlyData(plainData);

        var settingsAfterImport = await GetSettingsForClient(settings.ClientName, secret);
        var listSetting = settingsAfterImport.FirstOrDefault(a => a.Name == nameof(SecretSettings.LoginsWithDefault));
        var listSettingValue = (listSetting?.Value as DataGridSettingDataContract)?.GetValue() as List<Dictionary<string, object?>>;

        var index = 0;
        foreach (var row in listSettingValue ?? [])
        {
            Assert.That(row[nameof(Fig.Test.Common.TestSettings.Login.Username)], Is.EqualTo(defaultLogins[index].Username));
            Assert.That(row[nameof(Fig.Test.Common.TestSettings.Login.Password)], Is.EqualTo("newPassword"),
                "Unencrypted secret column values should be imported as-is when IsEncrypted is false");
            index++;
        }
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
    public async Task ShallExportInitOnlyExportMetadataForValueOnlyExports()
    {
        var settings = await RegisterSettings<InitOnlyExportTestSettings>();

        var data = await ExportValueOnlyData();
        var client = data.Clients.Single(c => c.Name == settings.ClientName);
        var initOnlySetting = client.Settings.Single(s => s.Name == nameof(InitOnlyExportTestSettings.BootstrapValue));
        var regularSetting = client.Settings.Single(s => s.Name == nameof(InitOnlyExportTestSettings.RegularValue));

        Assert.That(initOnlySetting.InitOnlyExport, Is.True);
        Assert.That(regularSetting.InitOnlyExport, Is.Null);
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

    [Test]
    public async Task ShallDeferImportForNotRegisteredClientWithInstance()
    {
        var allSettings = await RegisterSettings<AllSettingsAndTypes>();

        var data = await ExportValueOnlyData();

        const string updatedStringValue = "InstanceUpdate";
        const bool updateBoolValue = false;

        // Create a client with an instance by replacing the client
        var originalClient = data.Clients.FirstOrDefault(c => c.Name == allSettings.ClientName)
            ?? throw new InvalidOperationException($"Client {allSettings.ClientName} not found in export");
        var updatedSettings = originalClient.Settings.ToList();
        updatedSettings.First(a => a.Name == nameof(allSettings.StringSetting)).Value = updatedStringValue;
        updatedSettings.First(a => a.Name == nameof(allSettings.BoolSetting)).Value = updateBoolValue;
        
        data.Clients.Clear();
        data.Clients.Add(new SettingClientValueExportDataContract(originalClient.Name, "Instance1", updatedSettings));

        await DeleteClient(allSettings.ClientName);
        await ImportValueOnlyData(data);

        var deferredImports = await GetDeferredImports();
        Assert.That(deferredImports.Count, Is.EqualTo(1));
        Assert.That(deferredImports.Single().Name, Is.EqualTo(allSettings.ClientName));
        Assert.That(deferredImports.Single().Instance, Is.EqualTo("Instance1"));
    }

    [Test]
    public async Task ShallCreateInstanceOverrideWhenApplyingDeferredImportWithInstance()
    {
        var allSettings = await RegisterSettings<AllSettingsAndTypes>();

        var data = await ExportValueOnlyData();

        const string updatedStringValue = "InstanceUpdate";
        const bool updateBoolValue = false;

        // Create a deferred import with an instance by replacing the client
        var originalClient = data.Clients.FirstOrDefault(c => c.Name == allSettings.ClientName)
            ?? throw new InvalidOperationException($"Client {allSettings.ClientName} not found in export");
        var updatedSettings = originalClient.Settings.ToList();
        updatedSettings.First(a => a.Name == nameof(allSettings.StringSetting)).Value = updatedStringValue;
        updatedSettings.First(a => a.Name == nameof(allSettings.BoolSetting)).Value = updateBoolValue;
        
        data.Clients.Clear();
        data.Clients.Add(new SettingClientValueExportDataContract(originalClient.Name, "Instance1", updatedSettings));

        await DeleteClient(allSettings.ClientName);
        await ImportValueOnlyData(data);

        // Now register the base client
        await RegisterSettings<AllSettingsAndTypes>();

        var clients = (await GetAllClients()).ToList();
        Assert.That(clients.Count, Is.EqualTo(2), "Should have base client and instance override");
        
        var baseClient = clients.FirstOrDefault(c => c.Instance == null);
        var instanceClient = clients.FirstOrDefault(c => c.Instance == "Instance1");
        
        Assert.That(baseClient, Is.Not.Null, "Base client should exist");
        Assert.That(instanceClient, Is.Not.Null, "Instance client should be created");
        
        // Base client should have default values
        Assert.That(baseClient!.Settings.First(a => a.Name == nameof(allSettings.StringSetting)).Value?.GetValue(), 
            Is.EqualTo("Cat"), "Base client should keep default values");
        Assert.That(baseClient.Settings.First(a => a.Name == nameof(allSettings.BoolSetting)).Value?.GetValue(), 
            Is.EqualTo(true), "Base client should keep default values");
        
        // Instance client should have updated values from deferred import
        Assert.That(instanceClient!.Settings.First(a => a.Name == nameof(allSettings.StringSetting)).Value?.GetValue(), 
            Is.EqualTo(updatedStringValue), "Instance client should have deferred import values");
        Assert.That(instanceClient.Settings.First(a => a.Name == nameof(allSettings.BoolSetting)).Value?.GetValue(), 
            Is.EqualTo(updateBoolValue), "Instance client should have deferred import values");
    }

    [Test]
    public async Task ShallCreateMultipleInstanceOverridesWhenApplyingMultipleDeferredImportsWithDifferentInstances()
    {
        var allSettings = await RegisterSettings<AllSettingsAndTypes>();

        // Create first deferred import for Instance1
        var data1 = await ExportValueOnlyData();
        var originalClient1 = data1.Clients.FirstOrDefault(c => c.Name == allSettings.ClientName)
            ?? throw new InvalidOperationException($"Client {allSettings.ClientName} not found in export");
        var settings1 = originalClient1.Settings.ToList();
        settings1.First(a => a.Name == nameof(allSettings.StringSetting)).Value = "Instance1Value";
        settings1.First(a => a.Name == nameof(allSettings.IntSetting)).Value = 100;
        data1.Clients.Clear();
        data1.Clients.Add(new SettingClientValueExportDataContract(originalClient1.Name, "Instance1", settings1));

        // Create second deferred import for Instance2
        var data2 = await ExportValueOnlyData();
        var originalClient2 = data2.Clients.FirstOrDefault(c => c.Name == allSettings.ClientName)
            ?? throw new InvalidOperationException($"Client {allSettings.ClientName} not found in export");
        var settings2 = originalClient2.Settings.ToList();
        settings2.First(a => a.Name == nameof(allSettings.StringSetting)).Value = "Instance2Value";
        settings2.First(a => a.Name == nameof(allSettings.IntSetting)).Value = 200;
        data2.Clients.Clear();
        data2.Clients.Add(new SettingClientValueExportDataContract(originalClient2.Name, "Instance2", settings2));

        await DeleteClient(allSettings.ClientName);
        await ImportValueOnlyData(data1);
        await ImportValueOnlyData(data2);

        var deferredImports = await GetDeferredImports();
        Assert.That(deferredImports.Count, Is.EqualTo(2), "Should have two deferred imports");

        // Now register the base client - should create both instances
        await RegisterSettings<AllSettingsAndTypes>();

        var clients = (await GetAllClients()).ToList();
        Assert.That(clients.Count, Is.EqualTo(3), "Should have base client and two instance overrides");
        
        var baseClient = clients.FirstOrDefault(c => c.Instance == null);
        var instance1Client = clients.FirstOrDefault(c => c.Instance == "Instance1");
        var instance2Client = clients.FirstOrDefault(c => c.Instance == "Instance2");
        
        Assert.That(baseClient, Is.Not.Null, "Base client should exist");
        Assert.That(instance1Client, Is.Not.Null, "Instance1 client should be created");
        Assert.That(instance2Client, Is.Not.Null, "Instance2 client should be created");
        
        // Verify each has correct values
        Assert.That(baseClient!.Settings.First(a => a.Name == nameof(allSettings.StringSetting)).Value?.GetValue(), 
            Is.EqualTo("Cat"), "Base client should keep default value");
        Assert.That(instance1Client!.Settings.First(a => a.Name == nameof(allSettings.StringSetting)).Value?.GetValue(), 
            Is.EqualTo("Instance1Value"));
        Assert.That(instance1Client.Settings.First(a => a.Name == nameof(allSettings.IntSetting)).Value?.GetValue(), 
            Is.EqualTo(100));
        Assert.That(instance2Client!.Settings.First(a => a.Name == nameof(allSettings.StringSetting)).Value?.GetValue(), 
            Is.EqualTo("Instance2Value"));
        Assert.That(instance2Client.Settings.First(a => a.Name == nameof(allSettings.IntSetting)).Value?.GetValue(), 
            Is.EqualTo(200));
    }

    [Test]
    public async Task ShallApplyMultipleDeferredImportsWithMixedInstancesAndBaseClient()
    {
        var allSettings = await RegisterSettings<AllSettingsAndTypes>();

        // Create deferred import for base client (no instance)
        var dataBase = await ExportValueOnlyData();
        // Filter to only the client we care about
        var clientBase = dataBase.Clients.FirstOrDefault(c => c.Name == allSettings.ClientName)
            ?? throw new InvalidOperationException($"Client {allSettings.ClientName} not found in export");
        dataBase.Clients.Clear();
        dataBase.Clients.Add(clientBase);
        UpdateProperty(dataBase, nameof(allSettings.StringSetting), "BaseValue");
        UpdateProperty(dataBase, nameof(allSettings.DoubleSetting), 3.14);

        // Create deferred import for Instance1
        var dataInstance = await ExportValueOnlyData();
        var originalClient = dataInstance.Clients.FirstOrDefault(c => c.Name == allSettings.ClientName)
            ?? throw new InvalidOperationException($"Client {allSettings.ClientName} not found in export");
        var settings = originalClient.Settings.ToList();
        settings.First(a => a.Name == nameof(allSettings.StringSetting)).Value = "Instance1Value";
        settings.First(a => a.Name == nameof(allSettings.IntSetting)).Value = 999;
        dataInstance.Clients.Clear();
        dataInstance.Clients.Add(new SettingClientValueExportDataContract(originalClient.Name, "Instance1", settings));

        await DeleteClient(allSettings.ClientName);
        await ImportValueOnlyData(dataBase);
        await ImportValueOnlyData(dataInstance);

        var deferredImports = await GetDeferredImports();
        Assert.That(deferredImports.Count, Is.EqualTo(2), "Should have two deferred imports");

        // Now register the base client - should apply both imports
        await RegisterSettings<AllSettingsAndTypes>();

        var clients = (await GetAllClients()).ToList();
        Assert.That(clients.Count, Is.EqualTo(2), "Should have base client and one instance override");
        
        var baseClient = clients.FirstOrDefault(c => c.Instance == null);
        var instance1Client = clients.FirstOrDefault(c => c.Instance == "Instance1");
        
        Assert.That(baseClient, Is.Not.Null, "Base client should exist");
        Assert.That(instance1Client, Is.Not.Null, "Instance1 client should be created");
        
        // Base client should have values from base deferred import
        Assert.That(baseClient!.Settings.First(a => a.Name == nameof(allSettings.StringSetting)).Value?.GetValue(), 
            Is.EqualTo("BaseValue"), "Base client should have deferred import value");
        Assert.That(baseClient.Settings.First(a => a.Name == nameof(allSettings.DoubleSetting)).Value?.GetValue(), 
            Is.EqualTo(3.14), "Base client should have deferred import value");
        
        // Instance client should have values from instance deferred import
        Assert.That(instance1Client!.Settings.First(a => a.Name == nameof(allSettings.StringSetting)).Value?.GetValue(), 
            Is.EqualTo("Instance1Value"));
        Assert.That(instance1Client.Settings.First(a => a.Name == nameof(allSettings.IntSetting)).Value?.GetValue(), 
            Is.EqualTo(999));
    }

    [Test]
    public async Task ShallDeleteAllDeferredImportsAfterApplyingThemOnRegistration()
    {
        var allSettings = await RegisterSettings<AllSettingsAndTypes>();

        // Create multiple deferred imports
        var data1 = await ExportValueOnlyData();
        // Filter to only the client we care about to avoid issues with leftover clients from other tests
        var client1 = data1.Clients.FirstOrDefault(c => c.Name == allSettings.ClientName)
            ?? throw new InvalidOperationException($"Client {allSettings.ClientName} not found in export");
        data1.Clients.Clear();
        data1.Clients.Add(client1);
        UpdateProperty(data1, nameof(allSettings.StringSetting), "BaseValue");

        var data2 = await ExportValueOnlyData();
        var originalClient2 = data2.Clients.FirstOrDefault(c => c.Name == allSettings.ClientName) 
            ?? throw new InvalidOperationException($"Client {allSettings.ClientName} not found in export");
        var settings2 = originalClient2.Settings.ToList();
        settings2.First(a => a.Name == nameof(allSettings.StringSetting)).Value = "Instance1Value";
        data2.Clients.Clear();
        data2.Clients.Add(new SettingClientValueExportDataContract(originalClient2.Name, "Instance1", settings2));

        var data3 = await ExportValueOnlyData();
        var originalClient3 = data3.Clients.FirstOrDefault(c => c.Name == allSettings.ClientName) 
            ?? throw new InvalidOperationException($"Client {allSettings.ClientName} not found in export");
        var settings3 = originalClient3.Settings.ToList();
        settings3.First(a => a.Name == nameof(allSettings.StringSetting)).Value = "Instance2Value";
        data3.Clients.Clear();
        data3.Clients.Add(new SettingClientValueExportDataContract(originalClient3.Name, "Instance2", settings3));

        await DeleteClient(allSettings.ClientName);
        await ImportValueOnlyData(data1);
        await ImportValueOnlyData(data2);
        await ImportValueOnlyData(data3);

        await WaitForCondition(async () => (await GetDeferredImports()).Count == 3, TimeSpan.FromSeconds(1));
        var deferredImportsBeforeRegistration = await GetDeferredImports();
        Assert.That(deferredImportsBeforeRegistration.Count, Is.EqualTo(3), "Should have three deferred imports before registration");

        // Register the client
        await RegisterSettings<AllSettingsAndTypes>();

        // All deferred imports should be deleted
        var deferredImportsAfterRegistration = await GetDeferredImports();
        Assert.That(deferredImportsAfterRegistration.Count, Is.EqualTo(0), "All deferred imports should be deleted after registration");
    }

    [Test]
    public async Task ShallApplyDeferredImportsInCorrectOrderBasedOnImportTime()
    {
        var allSettings = await RegisterSettings<AllSettingsAndTypes>();

        var data = await ExportValueOnlyData();
        // Filter to only the client we care about to avoid issues with leftover clients from other tests
        var client1 = data.Clients.FirstOrDefault(c => c.Name == allSettings.ClientName)
            ?? throw new InvalidOperationException($"Client {allSettings.ClientName} not found in export");
        data.Clients.Clear();
        data.Clients.Add(client1);

        // First import - set string to "First"
        UpdateProperty(data, nameof(allSettings.StringSetting), "First");
        UpdateProperty(data, nameof(allSettings.IntSetting), 1);

        await DeleteClient(allSettings.ClientName);
        await ImportValueOnlyData(data);
        
        // Small delay to ensure different import times
        await Task.Delay(100);

        // Second import - set string to "Second", int to 2
        var data2 = await ExportValueOnlyData();
        data2.Clients.Clear();
        data2.Clients.Add(new SettingClientValueExportDataContract(allSettings.ClientName, null, 
            new List<SettingValueExportDataContract>
            {
                new(nameof(allSettings.StringSetting), "Second", false, null),
                new(nameof(allSettings.IntSetting), 2, false, null)
            }));
        await ImportValueOnlyData(data2);

        await Task.Delay(100);

        // Third import - only update string to "Third"
        var data3 = await ExportValueOnlyData();
        data3.Clients.Clear();
        data3.Clients.Add(new SettingClientValueExportDataContract(allSettings.ClientName, null, 
            new List<SettingValueExportDataContract>
            {
                new(nameof(allSettings.StringSetting), "Third", false, null)
            }));
        await ImportValueOnlyData(data3);

        var deferredImports = await GetDeferredImports();
        Assert.That(deferredImports.Count, Is.EqualTo(3), "Should have three deferred imports");

        // Register the client - imports should be applied in order
        await RegisterSettings<AllSettingsAndTypes>();

        var clients = await GetAllClients();
        var client = clients.FirstOrDefault(c => c.Name == allSettings.ClientName && c.Instance == null)
            ?? throw new InvalidOperationException($"Client {allSettings.ClientName} not found after registration");
        
        // The final value should be "Third" (from last import) and int should be 2 (from second import, not overridden by third)
        Assert.That(client.Settings.First(a => a.Name == nameof(allSettings.StringSetting)).Value?.GetValue(), 
            Is.EqualTo("Third"), "String should have the value from the last import");
        Assert.That(client.Settings.First(a => a.Name == nameof(allSettings.IntSetting)).Value?.GetValue(), 
            Is.EqualTo(2), "Int should have the value from the second import (not touched by third)");
    }

    [Test]
    public async Task ShallExportValueOnlyDataWithoutTypeInDataGrid()
    {
        await RegisterSettings<AllSettingsAndTypes>();

        var rawJson = await ExportValueOnlyDataRawJson();
        var parsed = JObject.Parse(rawJson);

        var objectListValue = parsed["Clients"]?[0]?["Settings"]?
            .First(s => s["Name"]?.ToString() == nameof(AllSettingsAndTypes.ObjectListSetting))?["Value"];

        Assert.That(objectListValue, Is.Not.Null, "ObjectListSetting value should exist in export");
        Assert.That(rawJson, Does.Not.Contain("$type"),
            "Value-only export should not contain $type discriminators");
    }

    [Test]
    public async Task ShallImportOldFormatExportWithTypeInDataGrid()
    {
        var secret = GetNewSecret();
        var (allSettings, configuration) = InitializeConfigurationProvider<AllSettingsAndTypes>(secret);

        // Construct JSON simulating the old export format with explicit $type on data grid dictionaries
        var oldFormatJson = @"{
  ""ExportedAt"": ""2024-01-01T00:00:00Z"",
  ""ImportType"": 3,
  ""Version"": 1,
  ""Clients"": [
    {
      ""Name"": ""AllSettingsAndTypes"",
      ""Settings"": [
        {
          ""Name"": ""ObjectListSetting"",
          ""Value"": [
            {
              ""$type"": ""System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib],[System.Object, System.Private.CoreLib]], System.Private.CoreLib"",
              ""Key"": ""imported"",
              ""Value"": ""via old format"",
              ""MyInt"": 42
            }
          ]
        }
      ]
    }
  ]
}";

        await ImportValueOnlyDataRawJson(oldFormatJson);

        configuration.Reload();

        Assert.That(allSettings.CurrentValue.ObjectListSetting, Is.Not.Null,
            "ObjectListSetting should be populated after old-format import");
        Assert.That(allSettings.CurrentValue.ObjectListSetting!.Count, Is.EqualTo(1));
        Assert.That(allSettings.CurrentValue.ObjectListSetting[0].Key, Is.EqualTo("imported"));
        Assert.That(allSettings.CurrentValue.ObjectListSetting[0].Value, Is.EqualTo("via old format"));
        Assert.That(allSettings.CurrentValue.ObjectListSetting[0].MyInt, Is.EqualTo(42));
    }

    private void UpdateProperty(FigValueOnlyDataExportDataContract data, string propertyName, object value)
    {
        data.Clients.Single().Settings.First(a => a.Name == propertyName).Value = value;
    }

    #region Custom Decryption Key Tests

    [Test]
    public async Task ShallReturnRequiresDecryptionKeyForValueOnlyImportWithDifferentSecret()
    {
        await RegisterSettings<SecretSettings>();

        var export = await ExportValueOnlyData();

        var originalServerSecret = Settings.Secret;
        Settings.PreviousSecret = string.Empty;
        Settings.Secret = Guid.NewGuid().ToString("N");
        ConfigReloader.Reload(Settings);
        await ApiClient.Authenticate();

        try
        {
            var result = await ImportValueOnlyData(export);

            Assert.That(result.RequiresDecryptionKey, Is.True);
            Assert.That(result.ErrorMessage, Is.Not.Null);
        }
        finally
        {
            try { await DeleteAllClients(); } catch { /* Data may be encrypted with original key */ }
            Settings.Secret = originalServerSecret;
            ConfigReloader.Reload(Settings);
            await ApiClient.Authenticate();
        }
    }

    [Test]
    public async Task ShallReturnRequiresDecryptionKeyForValueOnlyImportWithWrongCustomDecryptionKey()
    {
        await RegisterSettings<SecretSettings>();

        var export = await ExportValueOnlyData();

        var originalServerSecret = Settings.Secret;
        Settings.PreviousSecret = string.Empty;
        Settings.Secret = Guid.NewGuid().ToString("N");
        ConfigReloader.Reload(Settings);
        await ApiClient.Authenticate();

        try
        {
            export.DecryptionKey = Guid.NewGuid().ToString("N");
            var result = await ImportValueOnlyData(export);

            Assert.That(result.RequiresDecryptionKey, Is.True);
            Assert.That(result.ErrorMessage, Is.Not.Null);
        }
        finally
        {
            try { await DeleteAllClients(); } catch { /* Data may be encrypted with original key */ }
            Settings.Secret = originalServerSecret;
            ConfigReloader.Reload(Settings);
            await ApiClient.Authenticate();
        }
    }

    [Test]
    public async Task ShallImportValueOnlyWithCustomDecryptionKey()
    {
        // Simulate "source" environment: register settings and export values
        var secret = GetNewSecret();
        var settings = await RegisterSettings<SecretSettings>(secret);
        var originalSecretWithDefault = settings.SecretWithDefault;

        var export = await ExportValueOnlyData();

        // Switch to "target" environment with a different server secret
        await DeleteAllClients();

        var originalServerSecret = Settings.Secret;
        Settings.PreviousSecret = string.Empty;
        Settings.Secret = Guid.NewGuid().ToString("N");
        ConfigReloader.Reload(Settings);
        await ApiClient.Authenticate();

        // Register a fresh client in the target environment (encrypted with new key)
        await RegisterSettings<SecretSettings>(secret);

        try
        {
            export.DecryptionKey = originalServerSecret;
            var result = await ImportValueOnlyData(export);

            Assert.That(result.RequiresDecryptionKey, Is.False);
            Assert.That(result.ErrorMessage, Is.Null);

            var settingsAfterImport = await GetSettingsForClient(settings.ClientName, secret);

            Assert.That(
                settingsAfterImport.FirstOrDefault(a => a.Name == nameof(SecretSettings.SecretWithDefault))?.Value?.GetValue(),
                Is.EqualTo(originalSecretWithDefault));
        }
        finally
        {
            try { await DeleteAllClients(); } catch { /* Best effort cleanup */ }
            Settings.Secret = originalServerSecret;
            ConfigReloader.Reload(Settings);
            await ApiClient.Authenticate();
        }
    }

    #endregion

    #region DataGrid Custom Decryption Key Tests

    [Test]
    public async Task ShallReturnRequiresDecryptionKeyForValueOnlyDataGridSecrets()
    {
        var secret = GetNewSecret();
        await RegisterSettings<SecretSettings>(secret);

        var export = await ExportValueOnlyData();

        await DeleteAllClients();

        var originalServerSecret = Settings.Secret;
        Settings.PreviousSecret = string.Empty;
        Settings.Secret = Guid.NewGuid().ToString("N");
        ConfigReloader.Reload(Settings);
        await ApiClient.Authenticate();

        await RegisterSettings<SecretSettings>(secret);

        try
        {
            var result = await ImportValueOnlyData(export);

            Assert.That(result.RequiresDecryptionKey, Is.True);
            Assert.That(result.ErrorMessage, Is.Not.Null);
        }
        finally
        {
            try { await DeleteAllClients(); } catch { /* Best effort cleanup */ }
            Settings.Secret = originalServerSecret;
            ConfigReloader.Reload(Settings);
            await ApiClient.Authenticate();
        }
    }

    [Test]
    public async Task ShallImportValueOnlyDataGridSecretsWithCustomDecryptionKey()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<SecretSettings>(secret);

        var export = await ExportValueOnlyData();

        await DeleteAllClients();

        var originalServerSecret = Settings.Secret;
        Settings.PreviousSecret = string.Empty;
        Settings.Secret = Guid.NewGuid().ToString("N");
        ConfigReloader.Reload(Settings);
        await ApiClient.Authenticate();

        await RegisterSettings<SecretSettings>(secret);

        try
        {
            export.DecryptionKey = originalServerSecret;
            var result = await ImportValueOnlyData(export);

            Assert.That(result.RequiresDecryptionKey, Is.False);
            Assert.That(result.ErrorMessage, Is.Null);

            var settingsAfterImport = await GetSettingsForClient(settings.ClientName, secret);
            var listSetting = settingsAfterImport.FirstOrDefault(a => a.Name == nameof(SecretSettings.LoginsWithDefault));
            var listSettingValue = (listSetting?.Value as DataGridSettingDataContract)?.GetValue() as List<Dictionary<string, object?>>;

            var defaultLogins = SecretSettings.GetDefaultLogins();
            var index = 0;
            foreach (var row in listSettingValue ?? [])
            {
                Assert.That(row[nameof(Fig.Test.Common.TestSettings.Login.Username)], Is.EqualTo(defaultLogins[index].Username));
                Assert.That(row[nameof(Fig.Test.Common.TestSettings.Login.Password)], Is.EqualTo(defaultLogins[index].Password));
                Assert.That(row[nameof(Fig.Test.Common.TestSettings.Login.AnotherSecret)], Is.EqualTo(defaultLogins[index].AnotherSecret));
                index++;
            }
        }
        finally
        {
            try { await DeleteAllClients(); } catch { /* Best effort cleanup */ }
            Settings.Secret = originalServerSecret;
            ConfigReloader.Reload(Settings);
            await ApiClient.Authenticate();
        }
    }

    #endregion
}
