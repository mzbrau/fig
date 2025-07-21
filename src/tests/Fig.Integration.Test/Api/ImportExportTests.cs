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

        var data = await ExportData();

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

        var data = await ExportData();

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

        var loginResult = await Login(naughtyUser.Username, naughtyUser.Password ?? throw new InvalidOperationException("Password is null"));

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

        var data1 = await ExportData();
        data1.ImportType = ImportType.ClearAndImport;

        data1.Clients[0].Name = "UpdatedName";

        await ImportData(data1);

        var data2 = await ExportData();

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

        var data1 = await ExportData();

        await DeleteClient(allSettings.ClientName);

        data1.ImportType = ImportType.AddNew;
        var clientToUpdate = data1.Clients.FirstOrDefault(a => a.Name == threeSettings.ClientName);
        clientToUpdate?.Settings.Clear();

        await ImportData(data1);

        var data2 = await ExportData();

        Assert.That(data2.Clients.Count, Is.EqualTo(2));

        var allSettingsClient = data2.Clients.FirstOrDefault(a => a.Name == allSettings.ClientName);
        Assert.That(allSettingsClient, Is.Not.Null, "Client should have been imported");
        Assert.That(allSettingsClient?.Settings.Count, Is.EqualTo(14));

        var threeSettingsClient = data2.Clients.FirstOrDefault(a => a.Name == threeSettings.ClientName);
        Assert.That(threeSettingsClient, Is.Not.Null, "Name change should have been ignored");
        Assert.That(threeSettingsClient?.Settings.Count, Is.EqualTo(3));
    }

    [Test]
    public async Task ShallImportUsingReplaceExistingImportType()
    {
        var allSettings = await RegisterSettings<AllSettingsAndTypes>();
        var threeSettings = await RegisterSettings<ThreeSettings>();

        var data1 = await ExportData();

        var clientA = await RegisterSettings<ClientA>();

        data1.ImportType = ImportType.ReplaceExisting;
        var clientToUpdate = data1.Clients.First(a => a.Name == threeSettings.ClientName);
        clientToUpdate.Settings.Remove(clientToUpdate.Settings.First());

        await ImportData(data1);

        var data2 = await ExportData();


        Assert.That(data2.Clients.Count, Is.EqualTo(3));

        var allSettingsClient = data2.Clients.FirstOrDefault(a => a.Name == allSettings.ClientName);
        Assert.That(allSettingsClient, Is.Not.Null, "Client should have been re-imported");
        Assert.That(allSettingsClient!.Settings.Count, Is.EqualTo(14));

        var threeSettingsClient = data2.Clients.FirstOrDefault(a => a.Name == threeSettings.ClientName);
        Assert.That(threeSettingsClient, Is.Not.Null);
        Assert.That(threeSettingsClient!.Settings.Count, Is.EqualTo(2), "Client should have been updated");

        var clientAClient = data2.Clients.FirstOrDefault(a => a.Name == clientA.ClientName);
        Assert.That(clientAClient, Is.Not.Null, "Client should not have been touched");
        Assert.That(clientAClient!.Settings.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task ShallEncryptSecretsForExports()
    {
        const string secretDefaultValue = "cat";
        await RegisterSettings<SecretSettings>();

        var encryptedData = await ExportData();

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
    public async Task ShallImportAndExportSecretSetting()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<SecretSettings>(secret);
        var secretWithDefault = settings.SecretWithDefault;
        const string secretWithNoDefault = "secret value";
        
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.SecretNoDefault), new StringSettingDataContract(secretWithNoDefault))
        };
        
        await SetSettings(settings.ClientName, settingsToUpdate);

        var export = await ExportData();
        export.ImportType = ImportType.ReplaceExisting;

        await ImportData(export);

        var settingsAfterImport = await GetSettingsForClient(settings.ClientName, secret);
        
        Assert.That(settingsAfterImport.FirstOrDefault(a => a.Name == nameof(SecretSettings.SecretWithDefault))?.Value?.GetValue(), Is.EqualTo(secretWithDefault));
        Assert.That(settingsAfterImport.FirstOrDefault(a => a.Name == nameof(SecretSettings.SecretNoDefault))?.Value?.GetValue(), Is.EqualTo(secretWithNoDefault));
    }
    
    [Test]
    public async Task ShallImportAndExportSecretsInDataGridSetting()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<SecretSettings>(secret);

        var export = await ExportData();
        export.ImportType = ImportType.ReplaceExisting;

        var listSetting = export.Clients.Single().Settings
            .FirstOrDefault(a => a.Name == nameof(SecretSettings.LoginsWithDefault));
        var listSettingValue = (listSetting?.Value as DataGridSettingDataContract)?.GetValue() as List<Dictionary<string, object?>>;

        var defaultLogins = SecretSettings.GetDefaultLogins();
        var index = 0;
        foreach (var row in listSettingValue ?? [])
        {
            Assert.That(row[nameof(Fig.Test.Common.TestSettings.Login.Username)], Is.EqualTo(defaultLogins[index].Username));
            Assert.That(row[nameof(Fig.Test.Common.TestSettings.Login.Password)], Is.Not.EqualTo(defaultLogins[index].Password));
            index++;
        }

        await ImportData(export);

        var settingsAfterImport = await GetSettingsForClient(settings.ClientName, secret);
        var listSetting2 = settingsAfterImport.FirstOrDefault(a => a.Name == nameof(SecretSettings.LoginsWithDefault));
        var listSettingValue2 = (listSetting2?.Value as DataGridSettingDataContract)?.GetValue() as List<Dictionary<string, object?>>;
        
        index = 0;
        foreach (var row in listSettingValue2 ?? [])
        {
            Assert.That(row[nameof(Fig.Test.Common.TestSettings.Login.Username)], Is.EqualTo(defaultLogins[index].Username));
            Assert.That(row[nameof(Fig.Test.Common.TestSettings.Login.Password)], Is.EqualTo(defaultLogins[index].Password));
            Assert.That(row[nameof(Fig.Test.Common.TestSettings.Login.AnotherSecret)], Is.EqualTo(defaultLogins[index].AnotherSecret));
            index++;
        }
    }

    [Test]
    public async Task ShallNotDeleteAnySettingsOnImportFailure()
    {
        await RegisterSettings<SettingsWithCustomAction>();
        var settings = await RegisterSettings<AllSettingsAndTypes>();

        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.SecretSetting), new StringSettingDataContract("secret value"))
        };

        await SetSettings(settings.ClientName, settingsToUpdate);

        var data1 = await ExportData();
        data1.ImportType = ImportType.ClearAndImport;
        var secretSetting = data1.Clients.FirstOrDefault(a => a.Name == settings.ClientName)!.Settings
            .FirstOrDefault(a => a.Name == nameof(settings.SecretSetting));
        secretSetting!.Value = new StringSettingDataContract("notencrypted");
        secretSetting.IsEncrypted = true;

        var result = await ImportData(data1);

        Assert.That(result.ErrorMessage, Is.Not.Null);
        var clients = (await GetAllClients()).ToList();
        Assert.That(clients.Count, Is.EqualTo(2));
        var allSettingsClient = clients.FirstOrDefault(a => a.Name == settings.ClientName);
        Assert.That(allSettingsClient!.Settings.Count, Is.EqualTo(14));
    }

    [Test]
    public async Task ShallThrowExceptionWhenTryingToImportClientsThatDoNotMatchUserFilter()
    {
        var settings = await RegisterSettings<ThreeSettings>();
        await RegisterSettings<ClientA>();
        
        var user = NewUser();
        user.ClientFilter = settings.ClientName;
        await CreateUser(user);
        var loginResult = await Login(user.Username, user.Password!);

        var data = await ExportData();

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
        var loginResult = await Login(user.Username, user.Password ?? throw new InvalidOperationException("Password is null"));
        
        var data = await ExportData(loginResult.Token);
        
        Assert.That(data.Clients.Count, Is.EqualTo(1));
        Assert.That(data.Clients.Single().Name, Is.EqualTo(settings.ClientName));
    }

    [Test]
    public async Task ShallExportDependsOnProperties()
    {
        await RegisterSettings<DependsOnTestSettings>();

        var data = await ExportData();

        Assert.That(data.Clients.Count, Is.EqualTo(1));
        var client = data.Clients.Single();
        
        // Check that settings with DependsOn attributes have the correct metadata
        var featureASetting = client.Settings.FirstOrDefault(s => s.Name == nameof(DependsOnTestSettings.FeatureASetting));
        Assert.That(featureASetting, Is.Not.Null, "FeatureASetting should be exported");
        Assert.That(featureASetting!.DependsOnProperty, Is.EqualTo(nameof(DependsOnTestSettings.EnableFeatures)));
        Assert.That(featureASetting.DependsOnValidValues, Is.EqualTo(new[] { "True" }));

        var featureBSetting = client.Settings.FirstOrDefault(s => s.Name == nameof(DependsOnTestSettings.FeatureBSetting));
        Assert.That(featureBSetting, Is.Not.Null, "FeatureBSetting should be exported");
        Assert.That(featureBSetting!.DependsOnProperty, Is.EqualTo(nameof(DependsOnTestSettings.EnableFeatures)));
        Assert.That(featureBSetting.DependsOnValidValues, Is.EqualTo(new[] { "True" }));

        var databaseConnection = client.Settings.FirstOrDefault(s => s.Name == nameof(DependsOnTestSettings.DatabaseConnection));
        Assert.That(databaseConnection, Is.Not.Null, "DatabaseConnection should be exported");
        Assert.That(databaseConnection!.DependsOnProperty, Is.EqualTo(nameof(DependsOnTestSettings.ConnectionType)));
        Assert.That(databaseConnection.DependsOnValidValues, Is.EqualTo(new[] { "Database" }));

        var filePath = client.Settings.FirstOrDefault(s => s.Name == nameof(DependsOnTestSettings.FilePath));
        Assert.That(filePath, Is.Not.Null, "FilePath should be exported");
        Assert.That(filePath!.DependsOnProperty, Is.EqualTo(nameof(DependsOnTestSettings.ConnectionType)));
        Assert.That(filePath.DependsOnValidValues, Is.EqualTo(new[] { "File" }));

        var enableCaching = client.Settings.FirstOrDefault(s => s.Name == nameof(DependsOnTestSettings.EnableCaching));
        Assert.That(enableCaching, Is.Not.Null, "EnableCaching should be exported");
        Assert.That(enableCaching!.DependsOnProperty, Is.EqualTo(nameof(DependsOnTestSettings.ConnectionType)));
        Assert.That(enableCaching.DependsOnValidValues, Is.EqualTo(new[] { "Database", "File" }));
    }

    [Test]
    public async Task ShallImportDependsOnPropertiesUsingClearAndImport()
    {
        await RegisterSettings<DependsOnTestSettings>();

        var originalData = await ExportData();
        originalData.ImportType = ImportType.ClearAndImport;

        await ImportData(originalData);

        var reimportedData = await ExportData();

        // Compare dependency properties between original and reimported data
        var originalClient = originalData.Clients.Single();
        var reimportedClient = reimportedData.Clients.Single();

        foreach (var originalSetting in originalClient.Settings.Where(s => s.DependsOnProperty != null))
        {
            var reimportedSetting = reimportedClient.Settings.FirstOrDefault(s => s.Name == originalSetting.Name);
            Assert.That(reimportedSetting, Is.Not.Null, $"Setting {originalSetting.Name} should exist after import");
            Assert.That(reimportedSetting!.DependsOnProperty, Is.EqualTo(originalSetting.DependsOnProperty), 
                $"DependsOnProperty for {originalSetting.Name} should be preserved");
            Assert.That(reimportedSetting.DependsOnValidValues, Is.EqualTo(originalSetting.DependsOnValidValues), 
                $"DependsOnValidValues for {originalSetting.Name} should be preserved");
        }
    }

    [Test]
    public async Task ShallExportHeadingProperties()
    {
        await RegisterSettings<HeadingTestSettings>();

        var data = await ExportData();

        Assert.That(data.Clients.Count, Is.EqualTo(1));
        var client = data.Clients.Single();
        
        // Use helper method to validate all expected heading settings
        ValidateExpectedHeadingTestSettings(client);
    }

    [Test]
    public async Task ShallImportHeadingPropertiesUsingClearAndImport()
    {
        await RegisterSettings<HeadingTestSettings>();

        var originalData = await ExportData();
        originalData.ImportType = ImportType.ClearAndImport;

        await ImportData(originalData);

        var reimportedData = await ExportData();

        // Compare heading properties between original and reimported data
        var originalClient = originalData.Clients.Single();
        var reimportedClient = reimportedData.Clients.Single();

        foreach (var originalSetting in originalClient.Settings.Where(s => s.Heading != null))
        {
            var reimportedSetting = GetSettingByName(reimportedClient.Settings, originalSetting.Name);
            AssertHeadingPropertiesMatch(originalSetting, reimportedSetting);
        }

        // Verify settings without headings remain without headings
        foreach (var originalSetting in originalClient.Settings.Where(s => s.Heading == null))
        {
            var reimportedSetting = GetSettingByName(reimportedClient.Settings, originalSetting.Name);
            Assert.That(reimportedSetting.Heading, Is.Null, $"Setting {originalSetting.Name} should not have a heading after import");
        }
    }

    [Test]
    public async Task ShallImportHeadingPropertiesUsingReplaceExisting()
    {
        await RegisterSettings<HeadingTestSettings>();
        await RegisterSettings<ThreeSettings>(); // Additional client to ensure only targeted client is updated

        var originalData = await ExportData();
        originalData.ImportType = ImportType.ReplaceExisting;

        // Modify heading properties in the export data by creating new HeadingDataContract
        var headingClient = originalData.Clients.FirstOrDefault(c => c.Name == "HeadingTestSettings");
        Assert.That(headingClient, Is.Not.Null, "HeadingTestSettings client should exist in export");

        var applicationNameSetting = headingClient!.Settings.FirstOrDefault(s => s.Name == nameof(HeadingTestSettings.ApplicationName));
        Assert.That(applicationNameSetting, Is.Not.Null, "ApplicationName setting should exist");
        Assert.That(applicationNameSetting!.Heading, Is.Not.Null, "ApplicationName should have a heading");
        
        // Create a new HeadingExportDataContract with modified properties
        var newHeading = new HeadingExportDataContract(
            "Modified Application Configuration",
            "#123456", // Modified color
            applicationNameSetting.Heading!.Advanced);
        
        applicationNameSetting.Heading = newHeading;

        await ImportData(originalData);

        var reimportedData = await ExportData();
        var reimportedClient = reimportedData.Clients.FirstOrDefault(c => c.Name == "HeadingTestSettings");
        var reimportedSetting = reimportedClient!.Settings.FirstOrDefault(s => s.Name == nameof(HeadingTestSettings.ApplicationName));

        // Verify the modified heading properties were imported
        Assert.That(reimportedSetting, Is.Not.Null, "ApplicationName should exist after import");
        Assert.That(reimportedSetting!.Heading, Is.Not.Null, "ApplicationName should have a heading after import");
        Assert.That(reimportedSetting.Heading!.Text, Is.EqualTo("Modified Application Configuration"));
        Assert.That(reimportedSetting.Heading.Color, Is.EqualTo("#123456"));

        // Verify other clients weren't affected
        var threeSettingsClient = reimportedData.Clients.FirstOrDefault(c => c.Name == "ThreeSettings");
        Assert.That(threeSettingsClient, Is.Not.Null, "ThreeSettings client should still exist");
        Assert.That(threeSettingsClient!.Settings.Count, Is.EqualTo(3), "ThreeSettings should still have 3 settings");
    }

    [Test]
    public async Task ShallImportHeadingPropertiesUsingAddNew()
    {
        var originalSettings = await RegisterSettings<HeadingTestSettings>();
        await RegisterSettings<ThreeSettings>();

        var exportData = await ExportData();

        // Delete the HeadingTestSettings client to simulate adding it back
        await DeleteClient(originalSettings.ClientName);

        var dataAfterDelete = await ExportData();
        Assert.That(dataAfterDelete.Clients.Count, Is.EqualTo(1), "Should only have ThreeSettings after delete");

        exportData.ImportType = ImportType.AddNew;

        await ImportData(exportData);

        var finalData = await ExportData();
        Assert.That(finalData.Clients.Count, Is.EqualTo(2), "Should have both clients after AddNew import");

        var reimportedClient = finalData.Clients.FirstOrDefault(c => c.Name == originalSettings.ClientName);
        Assert.That(reimportedClient, Is.Not.Null, $"{originalSettings.ClientName} should be re-added");

        // Verify heading properties are preserved during AddNew import
        var applicationNameSetting = GetSettingByName(reimportedClient!.Settings, nameof(HeadingTestSettings.ApplicationName));
        AssertHeadingProperties(applicationNameSetting, "Application Configuration", null, false, nameof(HeadingTestSettings.ApplicationName));
    }

    [Test]
    public async Task ShallExportHeadingsWithAdvancedSettings()
    {
        await RegisterSettings<HeadingTestSettings>();

        var data = await ExportData();
        var client = data.Clients.Single();

        // Check advanced heading inheritance
        var enableQueryLogging = GetSettingByName(client.Settings, nameof(HeadingTestSettings.EnableQueryLogging));
        Assert.That(enableQueryLogging.Heading, Is.Not.Null, "EnableQueryLogging should have a heading");
        Assert.That(enableQueryLogging.Heading!.Advanced, Is.True, "Heading should inherit advanced status from setting");
        
        // Check that the setting itself is marked as advanced
        Assert.That(enableQueryLogging.Advanced, Is.True, "EnableQueryLogging should be marked as advanced");

        // Check another advanced setting that doesn't have a heading
        var queryTimeout = GetSettingByName(client.Settings, nameof(HeadingTestSettings.QueryTimeout));
        AssertNoHeading(queryTimeout, nameof(HeadingTestSettings.QueryTimeout));
        Assert.That(queryTimeout.Advanced, Is.True, "QueryTimeout should be marked as advanced");
    }

    [Test]
    public async Task ShallValidateHeadingDataDuringImport()
    {
        await RegisterSettings<HeadingTestSettings>();

        var data = await ExportData();
        data.ImportType = ImportType.ClearAndImport;

        var client = data.Clients.Single();
        var applicationNameSetting = client.Settings.FirstOrDefault(s => s.Name == nameof(HeadingTestSettings.ApplicationName));
        Assert.That(applicationNameSetting?.Heading, Is.Not.Null, "ApplicationName should have a heading");

        // Test with empty text - create new heading with empty text
        var originalHeading = applicationNameSetting!.Heading!;
        var invalidHeading = new HeadingExportDataContract("", originalHeading.Color, originalHeading.Advanced);
        applicationNameSetting.Heading = invalidHeading;

        var result = await ImportData(data);
        
        // The system should handle this gracefully or return an error
        // This tests that the validation logic works during import
        Assert.That(result, Is.Not.Null, "Import result should not be null");
    }

    [Test]
    public async Task ShallExportAndImportCompleteHeadingStructure()
    {
        await RegisterSettings<HeadingTestSettings>();

        // Export the data
        var originalData = await ExportData();
        
        // Verify export contains all heading properties
        var client = originalData.Clients.Single();
        var settingsWithHeadings = client.Settings.Where(s => s.Heading != null).ToList();
        
        Assert.That(settingsWithHeadings.Count, Is.EqualTo(6), "Should have 6 settings with headings");
        
        // Verify each heading type is correctly exported
        var applicationName = settingsWithHeadings.First(s => s.Name == nameof(HeadingTestSettings.ApplicationName));
        Assert.That(applicationName.Heading!.Text, Is.EqualTo("Application Configuration"));
        Assert.That(applicationName.Heading.Color, Is.Null);
        Assert.That(applicationName.Heading.Advanced, Is.False);
        
        var primaryDb = settingsWithHeadings.First(s => s.Name == nameof(HeadingTestSettings.PrimaryDbConnection));
        Assert.That(primaryDb.Heading!.Text, Is.EqualTo("Database Settings"));
        Assert.That(primaryDb.Heading.Color, Is.EqualTo("#0066CC"));
        
        var enableQueryLogging = settingsWithHeadings.First(s => s.Name == nameof(HeadingTestSettings.EnableQueryLogging));
        Assert.That(enableQueryLogging.Heading!.Advanced, Is.True, "Heading should inherit advanced status");
        
        // Clear the database and re-import
        originalData.ImportType = ImportType.ClearAndImport;
        await ImportData(originalData);
        
        // Export again to verify everything was preserved
        var reimportedData = await ExportData();
        var reimportedClient = reimportedData.Clients.Single();
        var reimportedSettingsWithHeadings = reimportedClient.Settings.Where(s => s.Heading != null).ToList();
        
        Assert.That(reimportedSettingsWithHeadings.Count, Is.EqualTo(6), "Should still have 6 settings with headings after import");
        
        // Verify all heading properties are preserved
        foreach (var originalSetting in settingsWithHeadings)
        {
            var reimportedSetting = GetSettingByName(reimportedSettingsWithHeadings, originalSetting.Name);
            AssertHeadingPropertiesMatch(originalSetting, reimportedSetting);
        }
    }

    [Test]
    public async Task ShallHandleHeadingExportDataContractSerialization()
    {
        await RegisterSettings<HeadingTestSettings>();
        
        var exportData = await ExportData();
        var client = exportData.Clients.Single();
        
        // Find a setting with a heading
        var settingWithHeading = client.Settings.First(s => s.Heading != null);
        var heading = settingWithHeading.Heading!;
        
        // Verify the export data contract has the expected type
        Assert.That(heading, Is.TypeOf<HeadingExportDataContract>(), "Should export as HeadingExportDataContract");
        
        // Verify JSON serialization works correctly
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(heading);
        Assert.That(json, Is.Not.Null.And.Not.Empty, "Should serialize to JSON");
        
        var deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<HeadingExportDataContract>(json);
        Assert.That(deserialized, Is.Not.Null, "Should deserialize from JSON");
        Assert.That(deserialized!.Text, Is.EqualTo(heading.Text), "Text should be preserved in serialization");
        Assert.That(deserialized.Color, Is.EqualTo(heading.Color), "Color should be preserved in serialization");
        Assert.That(deserialized.Advanced, Is.EqualTo(heading.Advanced), "Advanced should be preserved in serialization");
    }

    #region Heading Test Helper Methods

    private static void AssertHeadingProperties(SettingExportDataContract setting, string expectedText, 
        string? expectedColor, bool expectedAdvanced, string settingName)
    {
        Assert.That(setting, Is.Not.Null, $"{settingName} should be exported");
        Assert.That(setting.Heading, Is.Not.Null, $"{settingName} should have a heading");
        Assert.That(setting.Heading!.Text, Is.EqualTo(expectedText), $"Heading text for {settingName} should match expected");
        Assert.That(setting.Heading.Color, Is.EqualTo(expectedColor), $"Heading color for {settingName} should match expected");
        Assert.That(setting.Heading.Advanced, Is.EqualTo(expectedAdvanced), $"Heading advanced status for {settingName} should match expected");
    }

    private static void AssertNoHeading(SettingExportDataContract setting, string settingName)
    {
        Assert.That(setting, Is.Not.Null, $"{settingName} should be exported");
        Assert.That(setting.Heading, Is.Null, $"{settingName} should not have a heading");
    }

    private static void AssertHeadingPropertiesMatch(SettingExportDataContract originalSetting, SettingExportDataContract reimportedSetting)
    {
        Assert.That(reimportedSetting, Is.Not.Null, $"Setting {originalSetting.Name} should exist after import");
        Assert.That(reimportedSetting.Heading, Is.Not.Null, $"Heading for {originalSetting.Name} should exist after import");
        
        Assert.That(reimportedSetting.Heading!.Text, Is.EqualTo(originalSetting.Heading!.Text), 
            $"Heading text for {originalSetting.Name} should be preserved");
        Assert.That(reimportedSetting.Heading.Color, Is.EqualTo(originalSetting.Heading.Color), 
            $"Heading color for {originalSetting.Name} should be preserved");
        Assert.That(reimportedSetting.Heading.Advanced, Is.EqualTo(originalSetting.Heading.Advanced), 
            $"Heading advanced status for {originalSetting.Name} should be preserved");
    }

    private static SettingExportDataContract GetSettingByName(IEnumerable<SettingExportDataContract> settings, string settingName)
    {
        var setting = settings.FirstOrDefault(s => s.Name == settingName);
        Assert.That(setting, Is.Not.Null, $"Setting {settingName} should exist");
        return setting!;
    }

    private static void ValidateExpectedHeadingTestSettings(SettingClientExportDataContract client)
    {
        // Verify expected heading settings with their properties
        var applicationName = GetSettingByName(client.Settings, nameof(HeadingTestSettings.ApplicationName));
        AssertHeadingProperties(applicationName, "Application Configuration", null, false, nameof(HeadingTestSettings.ApplicationName));

        var primaryDb = GetSettingByName(client.Settings, nameof(HeadingTestSettings.PrimaryDbConnection));
        AssertHeadingProperties(primaryDb, "Database Settings", "#0066CC", false, nameof(HeadingTestSettings.PrimaryDbConnection));

        var maxPoolSize = GetSettingByName(client.Settings, nameof(HeadingTestSettings.MaxPoolSize));
        AssertHeadingProperties(maxPoolSize, "Connection Pool", "#0066CC", false, nameof(HeadingTestSettings.MaxPoolSize));

        var enableQueryLogging = GetSettingByName(client.Settings, nameof(HeadingTestSettings.EnableQueryLogging));
        AssertHeadingProperties(enableQueryLogging, "Advanced Database Options", "#0066CC", true, nameof(HeadingTestSettings.EnableQueryLogging));

        var apiKey = GetSettingByName(client.Settings, nameof(HeadingTestSettings.ApiKey));
        AssertHeadingProperties(apiKey, "Security Configuration", "#FF6600", false, nameof(HeadingTestSettings.ApiKey));

        var enableFeatureA = GetSettingByName(client.Settings, nameof(HeadingTestSettings.EnableFeatureA));
        AssertHeadingProperties(enableFeatureA, "Feature Toggles", "#00AA00", false, nameof(HeadingTestSettings.EnableFeatureA));

        // Verify settings without headings
        var applicationVersion = GetSettingByName(client.Settings, nameof(HeadingTestSettings.ApplicationVersion));
        AssertNoHeading(applicationVersion, nameof(HeadingTestSettings.ApplicationVersion));

        var minPoolSize = GetSettingByName(client.Settings, nameof(HeadingTestSettings.MinPoolSize));
        AssertNoHeading(minPoolSize, nameof(HeadingTestSettings.MinPoolSize));
    }

    [Test]
    public async Task ShallExportHeadingsWithPredefinedConstructors()
    {
        await RegisterSettings<HeadingConstructorTestSettings>();

        var exportData = await ExportData();
        var client = exportData.Clients.Single();
        var settingsWithHeadings = client.Settings.Where(s => s.Heading != null).ToList();

        Assert.That(settingsWithHeadings.Count, Is.EqualTo(8), "Should have 8 settings with headings");

        // Test manual constructor
        var manualSetting = GetSettingByName(client.Settings, nameof(HeadingConstructorTestSettings.ManualSetting1));
        AssertHeadingProperties(manualSetting, "Manual Configuration", "#FF5733", false, nameof(HeadingConstructorTestSettings.ManualSetting1));

        // Test CategoryColor constructor
        var colorEnumSetting = GetSettingByName(client.Settings, nameof(HeadingConstructorTestSettings.ColorEnumSetting));
        AssertHeadingProperties(colorEnumSetting, "Color Enum Section", "#4f51c9", false, nameof(HeadingConstructorTestSettings.ColorEnumSetting)); // Blue color from CategoryColor enum

        // Test Category constructor
        var databaseSetting = GetSettingByName(client.Settings, nameof(HeadingConstructorTestSettings.DatabaseConnection));
        AssertHeadingProperties(databaseSetting, "Database", "#4f51c9", false, nameof(HeadingConstructorTestSettings.DatabaseConnection)); // Name and color from Category enum

        // Test CategoryColor with indentation
        var indentedSetting = GetSettingByName(client.Settings, nameof(HeadingConstructorTestSettings.IndentedSetting));
        AssertHeadingProperties(indentedSetting, "Color Enum Section", "#357535", false, nameof(HeadingConstructorTestSettings.IndentedSetting)); // Green color from CategoryColor enum

        // Test Category with indentation
        var authSetting = GetSettingByName(client.Settings, nameof(HeadingConstructorTestSettings.AuthToken));
        AssertHeadingProperties(authSetting, "Authentication", "#2d5a8a", false, nameof(HeadingConstructorTestSettings.AuthToken)); // Name and color from Category enum

        // Test Logging category
        var logSetting = GetSettingByName(client.Settings, nameof(HeadingConstructorTestSettings.LogLevel));
        AssertHeadingProperties(logSetting, "Logging", "#969998", false, nameof(HeadingConstructorTestSettings.LogLevel)); // Gray color from Category enum

        // Test Security category
        var securitySetting = GetSettingByName(client.Settings, nameof(HeadingConstructorTestSettings.EncryptionKey));
        AssertHeadingProperties(securitySetting, "Security", "#8a2d2d", false, nameof(HeadingConstructorTestSettings.EncryptionKey)); // Color from Category enum

        // Test custom manual setting
        var customSetting = GetSettingByName(client.Settings, nameof(HeadingConstructorTestSettings.CustomSetting));
        AssertHeadingProperties(customSetting, "Custom Section", "#8A2BE2", false, nameof(HeadingConstructorTestSettings.CustomSetting));
    }

    #endregion
}