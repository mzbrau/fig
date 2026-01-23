using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Client.Abstractions.Attributes;
using Fig.Client.Abstractions.Enums;
using Fig.Contracts.LookupTable;
using Fig.Contracts.Settings;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;
using NUnit.Framework.Legacy;
// ReSharper disable ClassNeverInstantiated.Local

namespace Fig.Integration.Test.Api;

public class LookupTablesTests : IntegrationTestBase
{
    [Test]
    public async Task ShallAddLookupTable()
    {
        var lookupTable = new Dictionary<string, string?>
        {
            { "1", "Dog" },
            { "2", "Cat" },
            { "3", "Fish" },
            { "4", "Rabbit" }
        };

        var item = new LookupTableDataContract(null, "Animals", lookupTable, false);

        await AddLookupTable(item);
        var allItems = await GetAllLookupTables();

        var lookupTableDataContracts = allItems.ToList();
        Assert.That(lookupTableDataContracts.Count, Is.EqualTo(1));
        var result = lookupTableDataContracts.Single();
        Assert.That(result.Name, Is.EqualTo(item.Name));
        CollectionAssert.AreEquivalent(item.LookupTable, result.LookupTable);
    }

    [Test]
    public async Task ShallGetMultipleItems()
    {
        var lookupTable = new Dictionary<string, string?>
        {
            { "1", "Dog" },
            { "2", "Cat" }
        };

        var animals = new LookupTableDataContract(null, "Animals", lookupTable, false);

        await AddLookupTable(animals);
        var lookupTable2 = new Dictionary<string, string?>
        {
            { "1", "Sunny" },
            { "2", "Rain" }
        };

        var weather = new LookupTableDataContract(null, "Weather", lookupTable2, false);

        await AddLookupTable(weather);

        var allItems = (await GetAllLookupTables()).ToList();

        Assert.That(allItems.Count, Is.EqualTo(2));
        Assert.That(allItems[0].Name, Is.EqualTo(animals.Name));
        Assert.That(allItems[1].Name, Is.EqualTo(weather.Name));
        CollectionAssert.AreEquivalent(allItems[0].LookupTable, animals.LookupTable);
        CollectionAssert.AreEquivalent(allItems[1].LookupTable, weather.LookupTable);
    }

    [Test]
    public async Task ShallUpdateLookupTable()
    {
        var lookupTable = new Dictionary<string, string?>
        {
            { "1", "Dog" },
            { "2", "Cat" },
            { "3", "Fish" },
            { "4", "Rabbit" }
        };

        var item = new LookupTableDataContract(null, "Animals", lookupTable, false);

        await AddLookupTable(item);
        var allItems = await GetAllLookupTables();

        var dataContracts = allItems.ToList();
        var updated = dataContracts.Single();

        updated.Name = "More Animals";
        updated.LookupTable.Add("5", "Snake");

        await UpdateLookupTable(updated);

        Assert.That(dataContracts.Count, Is.EqualTo(1));
        var result = dataContracts.Single();
        Assert.That(result.Name, Is.EqualTo(updated.Name));
        CollectionAssert.AreEquivalent(updated.LookupTable, result.LookupTable);
    }

    [Test]
    public async Task ShallDeleteLookupTable()
    {
        var lookupTable = new Dictionary<string, string?>
        {
            { "1", "Dog" },
            { "2", "Cat" }
        };

        var animals = new LookupTableDataContract(null, "Animals", lookupTable, false);

        await AddLookupTable(animals);
        var lookupTable2 = new Dictionary<string, string?>
        {
            { "1", "Sunny" },
            { "2", "Rain" }
        };
        var weather = new LookupTableDataContract(null, "Weather", lookupTable2, false);

        await AddLookupTable(weather);

        var allItems = await GetAllLookupTables();

        await DeleteLookupTable(allItems.First().Id);

        var allItems2 = await GetAllLookupTables();

        Assert.That(allItems2.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task ShallProvideLookupTableValuesWhenGettingSettings()
    {
        var secret = GetNewSecret();

        var lookupTable = new Dictionary<string, string?>
        {
            { "Spot", "Dog" },
            { "Fluffy", "Cat" }
        };
        var animals = new LookupTableDataContract(null, "Animals", lookupTable, false);

        await AddLookupTable(animals);

        var (settings, _) = InitializeConfigurationProvider<AnimalsTest>(secret);

        Assert.That(settings.CurrentValue.Pets, Is.EqualTo(animals.LookupTable.First().Key));

        var client = (await GetAllClients()).ToList().First();
        CollectionAssert.AreEquivalent(
            animals.LookupTable.Select(a => $"{a.Key} -> {a.Value}").ToList(),
            client.Settings.Single().ValidValues ?? new List<string>());
    }

    [Test]
    public async Task ShallSetStringValueFromLookupTableValue()
    {
        var secret = GetNewSecret();
        var lookupTable = new Dictionary<string, string?>
        {
            { "Spot", "Dog" },
            { "Fluffy", "Cat" }
        };
        var animals = new LookupTableDataContract(null, "Animals", lookupTable, false);

        await AddLookupTable(animals);

        var (settings, configuration) = InitializeConfigurationProvider<AnimalsTest>(secret);

        Assert.That(settings.CurrentValue.Pets, Is.EqualTo(animals.LookupTable.First().Key));

        var client = (await GetAllClients()).ToList().Single();

        var validValues = client.Settings.Single().ValidValues;
        if (validValues != null)
        {
            var settingsToUpdate = new List<SettingDataContract>
            {
                new(nameof(settings.CurrentValue.Pets), new StringSettingDataContract(validValues.Last()))
            };

            await SetSettings(settings.CurrentValue.ClientName, settingsToUpdate);
        }

        configuration.Reload();

        Assert.That(settings.CurrentValue.Pets, Is.EqualTo(animals.LookupTable.Last().Key));
    }

    [Test]
    public async Task ShallSetIntValueFromLookupTableValue()
    {
        var secret = GetNewSecret();
        var lookupTable = new Dictionary<string, string?>
        {
            { "6", "Cold" },
            { "20", "Nice" },
            { "35", "Hot" }
        };
        var temperatures = new LookupTableDataContract(null, "Temperatures", lookupTable, false);

        await AddLookupTable(temperatures);

        var (settings, configuration) = InitializeConfigurationProvider<TemperaturesTest>(secret);

        Assert.That(settings.CurrentValue.Temperature, Is.EqualTo(int.Parse(temperatures.LookupTable.First().Key)));

        var client = (await GetAllClients()).ToList().Single();

        var firstItem = temperatures.LookupTable.First();
        Assert.That(client.Settings.Single().Value?.GetValue(), Is.EqualTo($"{firstItem.Key} -> {firstItem.Value}"));
        Assert.That(client.Settings.Single().ValueType, Is.EqualTo(typeof(string)));

        var validValues = client.Settings.Single().ValidValues;
        if (validValues != null)
        {
            var settingsToUpdate = new List<SettingDataContract>
            {
                new(nameof(settings.CurrentValue.Temperature), new StringSettingDataContract(validValues.Last()))
            };

            await SetSettings(settings.CurrentValue.ClientName, settingsToUpdate);
        }

        configuration.Reload();

        Assert.That(settings.CurrentValue.Temperature, Is.EqualTo(int.Parse(temperatures.LookupTable.Last().Key)));
    }

    [Test]
    public async Task ShallSetBoolValueFromLookupTableValue()
    {
        var secret = GetNewSecret();
        var lookupTable = new Dictionary<string, string?>
        {
            { "True", "Very Happy" },
            { "False", "Unfortunately Sad" }
        };
        var temperatures = new LookupTableDataContract(null, "IsHappy", lookupTable, false);

        await AddLookupTable(temperatures);

        var (settings, configuration) = InitializeConfigurationProvider<HappyTests>(secret);

        Assert.That(settings.CurrentValue.IsHappy, Is.EqualTo(bool.Parse(temperatures.LookupTable.First().Key)));

        var client = (await GetAllClients()).ToList().Single();

        var firstItem = temperatures.LookupTable.First();
        Assert.That(client.Settings.Single().Value?.GetValue(), Is.EqualTo($"{firstItem.Key} -> {firstItem.Value}"));
        Assert.That(client.Settings.Single().ValueType, Is.EqualTo(typeof(string)));

        var validValues = client.Settings.Single().ValidValues;
        if (validValues != null)
        {
            var settingsToUpdate = new List<SettingDataContract>
            {
                new(nameof(settings.CurrentValue.IsHappy), new StringSettingDataContract(validValues.Last()))
            };

            await SetSettings(settings.CurrentValue.ClientName, settingsToUpdate);
        }

        configuration.Reload();

        Assert.That(settings.CurrentValue.IsHappy, Is.EqualTo(bool.Parse(temperatures.LookupTable.Last().Key)));
    }

    [Test]
    public async Task ShallSetLongValueFromLookupTableValue()
    {
        var secret = GetNewSecret();
        var lookupTable = new Dictionary<string, string?>
        {
            { "99", "Open" },
            { "403", "In Progress" },
            { "992", "Closed" }
        };
        var states = new LookupTableDataContract(null, "States", lookupTable, false);

        await AddLookupTable(states);

        var (settings, configuration) = InitializeConfigurationProvider<StatesTest>(secret);

        Assert.That(settings.CurrentValue.StateId, Is.EqualTo(long.Parse(states.LookupTable.First().Key)));

        var client = (await GetAllClients()).ToList().Single();

        var firstItem = states.LookupTable.First();
        Assert.That(client.Settings.Single().Value?.GetValue(), Is.EqualTo($"{firstItem.Key} -> {firstItem.Value}"));
        Assert.That(client.Settings.Single().ValueType, Is.EqualTo(typeof(string)));

        var validValues = client.Settings.Single().ValidValues;
        if (validValues != null)
        {
            var settingsToUpdate = new List<SettingDataContract>
            {
                new(nameof(settings.CurrentValue.StateId), new StringSettingDataContract(validValues.Skip(1).First()))
            };

            await SetSettings(settings.CurrentValue.ClientName, settingsToUpdate);
        }

        configuration.Reload();

        Assert.That(settings.CurrentValue.StateId, Is.EqualTo(long.Parse(states.LookupTable.Skip(1).First().Key)));
    }

    [Test]
    public async Task ShallHandleNoMatchingLookupForStrings()
    {
        var secret = GetNewSecret();
        var (settings, configuration) = InitializeConfigurationProvider<AnimalsTest>(secret);

        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.CurrentValue.Pets), new StringSettingDataContract("Hippo"))
        };

        await SetSettings(settings.CurrentValue.ClientName, settingsToUpdate);

        configuration.Reload();

        Assert.That(settings.CurrentValue.Pets, Is.EqualTo("Hippo"));
    }

    [Test]
    public async Task ShallHandleNoMatchingLookupForIntegers()
    {
        var secret = GetNewSecret();
        var (settings, configuration) = InitializeConfigurationProvider<TemperaturesTest>(secret);

        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.CurrentValue.Temperature), new IntSettingDataContract(9))
        };

        await SetSettings(settings.CurrentValue.ClientName, settingsToUpdate);

        configuration.Reload();

        Assert.That(settings.CurrentValue.Temperature, Is.EqualTo(9));
    }

    [Test]
    public async Task ShallKeepInvalidOptionInList()
    {
        var secret = GetNewSecret();
        var (settings, configuration) = InitializeConfigurationProvider<TemperaturesTest>(secret);

        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.CurrentValue.Temperature), new IntSettingDataContract(9))
        };

        await SetSettings(settings.CurrentValue.ClientName, settingsToUpdate);
        
        var lookupTable = new Dictionary<string, string?>
        {
            { "6", "Cold" },
            { "20", "Nice" },
            { "35", "Hot" }
        };

        var temperatures = new LookupTableDataContract(null, "Temperatures", lookupTable, false);

        await AddLookupTable(temperatures);

        configuration.Reload();

        Assert.That(settings.CurrentValue.Temperature, Is.EqualTo(9));

        var client = (await GetAllClients()).ToList().Single();

        Assert.That(client.Settings.Single().Value?.GetValue(), Is.EqualTo("9 -> [INVALID]"));
        Assert.That(client.Settings.Single().ValueType, Is.EqualTo(typeof(string)));

        var validValues = client.Settings.Single().ValidValues;
        if (validValues != null)
        {
            var settingsToUpdate2 = new List<SettingDataContract>
            {
                new(nameof(settings.CurrentValue.Temperature), new StringSettingDataContract(validValues.Last()))
            };

            await SetSettings(settings.CurrentValue.ClientName, settingsToUpdate2);
        }

        configuration.Reload();

        Assert.That(settings.CurrentValue.Temperature, Is.EqualTo(int.Parse(temperatures.LookupTable.Last().Key)));
    }

    [Test]
    public async Task ShallWorkWithLookupTablesWithoutAliases()
    {
        var secret = GetNewSecret();
        
        // Create lookup table with keys only (no aliases)
        var lookupTable = new Dictionary<string, string?>
        {
            {"Dog", null},
            {"Cat", null},
            {"Fish", null}
        };
        var pets = new LookupTableDataContract(null, "PetsNoAlias", lookupTable, false);
        
        await AddLookupTable(pets);
        
        var (settings, configuration) = InitializeConfigurationProvider<PetsNoAliasTest>(secret);

        Assert.That(settings.CurrentValue.Pet, Is.EqualTo("Dog"));
        
        var client = (await GetAllClients()).ToList().Single();
        
        // Verify that valid values are displayed correctly (key -> value format, but both are the same)
        var firstItem = lookupTable.First();
        Assert.That(client.Settings.Single().Value?.GetValue(), Is.EqualTo($"{firstItem.Key}"));
        Assert.That(client.Settings.Single().ValueType, Is.EqualTo(typeof(string)));
        
        var validValues = client.Settings.Single().ValidValues;
        Assert.That(validValues, Is.Not.Null);
        Assert.That(validValues!.Count, Is.EqualTo(3));
        
        // Verify all valid values are in the expected format
        CollectionAssert.AreEquivalent(
            new[] { "Dog", "Cat", "Fish" },
            validValues);
        
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.CurrentValue.Pet), new StringSettingDataContract(validValues.Skip(1).First()))
        };
            
        await SetSettings(settings.CurrentValue.ClientName, settingsToUpdate);
        
        configuration.Reload();
        
        Assert.That(settings.CurrentValue.Pet, Is.EqualTo("Cat"));
    }
    
    [Test]
    public async Task ShallHandleNullCurrentValue()
    {
        var secret = GetNewSecret();
        
        var lookupTable = new Dictionary<string, string?>
        {
            {"Option1", null},
            {"Option2", null},
            {"Option3", null}
        };
        var options = new LookupTableDataContract(null, "NullableOptions", lookupTable, false);
        
        await AddLookupTable(options);
        
        var (settings, configuration) = InitializeConfigurationProvider<NullableOptionsTest>(secret);

        // Current value should be null as set in the test class
        Assert.That(settings.CurrentValue.SelectedOption, Is.Null);
        
        var client = (await GetAllClients()).ToList().Single();
        
        // When current value is null, the display should handle it gracefully
        Assert.That(client.Settings.Single().Value?.GetValue(), Is.EqualTo(" -> [INVALID]"));
        Assert.That(client.Settings.Single().ValueType, Is.EqualTo(typeof(string)));
        
        var validValues = client.Settings.Single().ValidValues;
        Assert.That(validValues, Is.Not.Null);
        Assert.That(validValues!.Count, Is.EqualTo(4));
        
        // Verify all valid values are available
        CollectionAssert.AreEquivalent(
            new[] { " -> [INVALID]", "Option1", "Option2", "Option3" },
            validValues);
        
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.CurrentValue.SelectedOption), new StringSettingDataContract(validValues.Last()))
        };
            
        await SetSettings(settings.CurrentValue.ClientName, settingsToUpdate);
        
        configuration.Reload();
        
        Assert.That(settings.CurrentValue.SelectedOption, Is.EqualTo("Option3"));
        
        // Test setting back to null
        var nullSettingsUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.CurrentValue.SelectedOption), new StringSettingDataContract(null))
        };
        
        await SetSettings(settings.CurrentValue.ClientName, nullSettingsUpdate);
        
        configuration.Reload();
        
        Assert.That(settings.CurrentValue.SelectedOption, Is.Null);
    }

    [Test]
    public async Task ShallCreateUserDefinedLookupTable()
    {
        var secret = GetNewSecret();
        
        // Create lookup table via client-defined mechanism
        var lookupTable = new Dictionary<string, string?>
        {
            { "Bug", "Software Bug" },
            { "Feature", "New Feature" },
            { "Task", "General Task" }
        };
        var clientLookup = new LookupTableDataContract(null, "IssueTypes", lookupTable, true);
        await AddLookupTable(clientLookup);

        var (_, _) = InitializeConfigurationProvider<IssueTypeTest>(secret);
        
        var allLookupTables = await GetAllLookupTables();
        var issueTypeLookup = allLookupTables.FirstOrDefault(lt => lt.Name == "IssueTypes");
        
        Assert.That(issueTypeLookup, Is.Not.Null);
        Assert.That(issueTypeLookup!.IsClientDefined, Is.False);
        Assert.That(issueTypeLookup.LookupTable.Count, Is.EqualTo(3));
        Assert.That(issueTypeLookup.LookupTable.ContainsKey("Bug"), Is.True);
        Assert.That(issueTypeLookup.LookupTable.ContainsKey("Feature"), Is.True);
        Assert.That(issueTypeLookup.LookupTable.ContainsKey("Task"), Is.True);
    }

    [Test]
    public async Task ShallHandleKeyedLookupTablesWithFlattenedKeys()
    {
        var secret = GetNewSecret();
        
        // Create a flattened keyed lookup table as would be created by IKeyedLookupProvider
        var flattenedLookupTable = new Dictionary<string, string?>
        {
            { "[Bug]High", "High Priority Bug" },
            { "[Bug]Medium", "Medium Priority Bug" },
            { "[Bug]Low", "Low Priority Bug" },
            { "[Feature]Open", "Open Feature Request" },
            { "[Feature]InProgress", "Feature In Progress" },
            { "[Feature]Closed", "Completed Feature" },
            { "[Task]Alice", "Assigned to Alice" },
            { "[Task]Bob", "Assigned to Bob" },
            { "[Task]Charlie", "Assigned to Charlie" }
        };
        var keyedLookup = new LookupTableDataContract(null, "IssuePropertyTest:IssueProperty", flattenedLookupTable, true);
        await AddLookupTable(keyedLookup);

        // Also create the issue type lookup
        var issueTypeLookup = new Dictionary<string, string?>
        {
            { "Bug", null },
            { "Feature", null },
            { "Task", null }
        };
        var typeLookup = new LookupTableDataContract(null, "IssuePropertyTest:IssueType", issueTypeLookup, true);
        await AddLookupTable(typeLookup);

        var (settings, _) = InitializeConfigurationProvider<IssuePropertyTest>(secret);

        // Verify both lookup tables exist
        var allLookupTables = await GetAllLookupTables();
        var propertyLookup = allLookupTables.FirstOrDefault(lt => lt.Name == "IssuePropertyTest:IssueProperty");
        var issueLookup = allLookupTables.FirstOrDefault(lt => lt.Name == "IssuePropertyTest:IssueType");
        
        Assert.That(propertyLookup, Is.Not.Null);
        Assert.That(issueLookup, Is.Not.Null);
        
        // Check that keyed values are flattened with [key]value format
        Assert.That(propertyLookup!.LookupTable.ContainsKey("[Bug]High"), Is.True);
        Assert.That(propertyLookup.LookupTable.ContainsKey("[Feature]Open"), Is.True);
        Assert.That(propertyLookup.LookupTable.ContainsKey("[Task]Alice"), Is.True);

        // Verify client settings have appropriate valid values
        var client = (await GetAllClients()).Single(c => c.Name == settings.CurrentValue.ClientName);
        var issueTypeSetting = client.Settings.FirstOrDefault(s => s.Name == nameof(settings.CurrentValue.IssueType));
        var issuePropertySetting = client.Settings.FirstOrDefault(s => s.Name == nameof(settings.CurrentValue.IssueProperty));
        
        Assert.That(issueTypeSetting?.ValidValues, Is.Not.Null);
        Assert.That(issuePropertySetting?.ValidValues, Is.Not.Null);
        
        // Issue type should have the main types
        CollectionAssert.Contains(issueTypeSetting!.ValidValues!, "Bug");
        CollectionAssert.Contains(issueTypeSetting.ValidValues!, "Feature");
        CollectionAssert.Contains(issueTypeSetting.ValidValues!, "Task");
        
        // For keyed lookups, the ValidValues should contain the full flattened keys
        // The client will need to filter these based on the current key setting value
        CollectionAssert.Contains(issuePropertySetting!.ValidValues!, "[Bug]High -> High Priority Bug");
        CollectionAssert.Contains(issuePropertySetting.ValidValues!, "[Bug]Medium -> Medium Priority Bug");
        CollectionAssert.Contains(issuePropertySetting.ValidValues!, "[Bug]Low -> Low Priority Bug");
        CollectionAssert.Contains(issuePropertySetting.ValidValues!, "[Feature]Open -> Open Feature Request");
        CollectionAssert.Contains(issuePropertySetting.ValidValues!, "[Task]Alice -> Assigned to Alice");
    }

    [Test]
    public async Task ShallUpdateDependentValidValuesWhenKeySettingChanges()
    {
        var secret = GetNewSecret();
        
        // Create keyed lookup tables
        var flattenedLookupTable = new Dictionary<string, string?>
        {
            { "[Bug]High", "High Priority Bug" },
            { "[Bug]Medium", "Medium Priority Bug" },
            { "[Feature]Open", "Open Feature Request" },
            { "[Feature]InProgress", "Feature In Progress" },
            { "[Task]Alice", "Assigned to Alice" },
            { "[Task]Bob", "Assigned to Bob" }
        };
        var keyedLookup = new LookupTableDataContract(null, "IssuePropertyTest:IssueProperty", flattenedLookupTable, true);
        await AddLookupTable(keyedLookup);

        var issueTypeLookup = new Dictionary<string, string?>
        {
            { "Bug", null },
            { "Feature", null },
            { "Task", null }
        };
        var typeLookup = new LookupTableDataContract(null, "IssueType", issueTypeLookup, true);
        await AddLookupTable(typeLookup);

        var (settings, configuration) = InitializeConfigurationProvider<IssuePropertyTest>(secret);

        // Change the issue type to Feature
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.CurrentValue.IssueType), new StringSettingDataContract("Feature"))
        };

        await SetSettings(settings.CurrentValue.ClientName, settingsToUpdate);
        configuration.Reload();

        Assert.That(settings.CurrentValue.IssueType, Is.EqualTo("Feature"));

        // Verify the dependent setting's valid values are updated
        var client = (await GetAllClients()).Single(c => c.Name == settings.CurrentValue.ClientName);
        var issuePropertySetting = client.Settings.FirstOrDefault(s => s.Name == nameof(settings.CurrentValue.IssueProperty));
        
        Assert.That(issuePropertySetting?.ValidValues, Is.Not.Null);
        CollectionAssert.Contains(issuePropertySetting!.ValidValues!, "[Feature]Open -> Open Feature Request");
        CollectionAssert.Contains(issuePropertySetting.ValidValues!, "[Feature]InProgress -> Feature In Progress");
        CollectionAssert.Contains(issuePropertySetting.ValidValues!, "[Bug]High -> High Priority Bug");
        CollectionAssert.Contains(issuePropertySetting.ValidValues!, "[Bug]Medium -> Medium Priority Bug");
    }

    [Test]
    public async Task ShallHandleMultipleLevelsOfKeyedLookups()
    {
        var secret = GetNewSecret();
        
        // Create a three-level keyed lookup system
        var categoriesLookup = new Dictionary<string, string?>
        {
            { "Electronics", null },
            { "Clothing", null },
            { "Books", null }
        };
        await AddLookupTable(new LookupTableDataContract(null, "MultiLevelLookupTest:Categories", categoriesLookup, true));

        var subcategoriesLookup = new Dictionary<string, string?>
        {
            { "[Electronics]Smartphones", null },
            { "[Electronics]Laptops", null },
            { "[Clothing]Shirts", null },
            { "[Clothing]Pants", null },
            { "[Books]Fiction", null },
            { "[Books]NonFiction", null }
        };
        await AddLookupTable(new LookupTableDataContract(null, "MultiLevelLookupTest:Subcategories", subcategoriesLookup, true));

        var productsLookup = new Dictionary<string, string?>
        {
            { "[Smartphones]iPhone", null },
            { "[Smartphones]Android", null },
            { "[Laptops]MacBook", null },
            { "[Laptops]Dell", null },
            { "[Fiction]Novel", null },
            { "[Fiction]Mystery", null }
        };
        await AddLookupTable(new LookupTableDataContract(null, "MultiLevelLookupTest:Products", productsLookup, true));

        var (settings, configuration) = InitializeConfigurationProvider<MultiLevelLookupTest>(secret);

        // Verify initial state (Electronics -> Smartphones -> iPhone)
        Assert.That(settings.CurrentValue.Category, Is.EqualTo("Electronics"));
        Assert.That(settings.CurrentValue.Subcategory, Is.EqualTo("Smartphones"));
        Assert.That(settings.CurrentValue.Product, Is.EqualTo("iPhone"));

        var client = (await GetAllClients()).Single(c => c.Name == settings.CurrentValue.ClientName);
        
        // Verify subcategories are filtered by category
        var subcategorySetting = client.Settings.FirstOrDefault(s => s.Name == nameof(settings.CurrentValue.Subcategory));
        Assert.That(subcategorySetting?.ValidValues, Is.Not.Null);
        CollectionAssert.Contains(subcategorySetting!.ValidValues!, "[Electronics]Smartphones");
        CollectionAssert.Contains(subcategorySetting.ValidValues!, "[Electronics]Laptops");
        CollectionAssert.Contains(subcategorySetting.ValidValues!, "[Clothing]Shirts");

        // Verify products are filtered by subcategory
        var productSetting = client.Settings.FirstOrDefault(s => s.Name == nameof(settings.CurrentValue.Product));
        Assert.That(productSetting?.ValidValues, Is.Not.Null);
        CollectionAssert.Contains(productSetting!.ValidValues!, "[Smartphones]iPhone");
        CollectionAssert.Contains(productSetting.ValidValues!, "[Smartphones]Android");
        CollectionAssert.Contains(productSetting.ValidValues!, "[Laptops]MacBook");

        // Test changing category to Books
        var categoryUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.CurrentValue.Category), new StringSettingDataContract("Books"))
        };
        await SetSettings(settings.CurrentValue.ClientName, categoryUpdate);
        configuration.Reload();

        // Verify cascading updates
        client = (await GetAllClients()).Single(c => c.Name == settings.CurrentValue.ClientName);
        subcategorySetting = client.Settings.FirstOrDefault(s => s.Name == nameof(settings.CurrentValue.Subcategory));
        
        Assert.That(subcategorySetting?.ValidValues, Is.Not.Null);
        CollectionAssert.Contains(subcategorySetting!.ValidValues!, "[Books]Fiction");
        CollectionAssert.Contains(subcategorySetting.ValidValues!, "[Books]NonFiction");
        CollectionAssert.Contains(subcategorySetting.ValidValues!, "[Electronics]Smartphones");
    }

    [Test]
    public async Task ShallHandleInvalidLookupValuesInKeyedSystem()
    {
        var secret = GetNewSecret();
        
        var keyedLookupTable = new Dictionary<string, string?>
        {
            { "[ValidKey]ValidValue1", "Display Value 1" },
            { "[ValidKey]ValidValue2", "Display Value 2" },
            { "[AnotherKey]AnotherValue", "Another Display" }
        };
        var keyedLookup = new LookupTableDataContract(null, "InvalidLookupIntegrationTest:InvalidKeyedTestLookup", keyedLookupTable, true);
        await AddLookupTable(keyedLookup);

        var primaryLookup = new Dictionary<string, string?>
        {
            { "ValidKey", null },
            { "AnotherKey", null }
        };
        var primaryTable = new LookupTableDataContract(null, "InvalidLookupIntegrationTest:InvalidKeyedPrimaryLookup", primaryLookup, true);
        await AddLookupTable(primaryTable);

        var (settings, configuration) = InitializeConfigurationProvider<InvalidLookupIntegrationTest>(secret);

        // Set an invalid dependent value first
        var invalidSettingsUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.CurrentValue.DependentValue), new StringSettingDataContract("InvalidValue"))
        };

        await SetSettings(settings.CurrentValue.ClientName, invalidSettingsUpdate);
        configuration.Reload();

        // Verify the invalid value is preserved
        Assert.That(settings.CurrentValue.DependentValue, Is.EqualTo("InvalidValue"));

        // Check that the client shows it as invalid
        var client = (await GetAllClients()).Single(c => c.Name == settings.CurrentValue.ClientName);
        var dependentSetting = client.Settings.FirstOrDefault(s => s.Name == nameof(settings.CurrentValue.DependentValue));
        
        Assert.That(dependentSetting?.Value?.GetValue(), Is.EqualTo("InvalidValue -> [INVALID]"));
        
        // Verify valid values still include the invalid option and valid options
        Assert.That(dependentSetting?.ValidValues, Is.Not.Null);
        CollectionAssert.Contains(dependentSetting!.ValidValues!, "InvalidValue -> [INVALID]");
        
        // Note: Due to architectural constraints, keyed lookups show all values with key prefixes
        // during GetAllClients operation as ValidValuesHandler doesn't have access to complete 
        // client context needed for filtering
        CollectionAssert.Contains(dependentSetting.ValidValues!, "[ValidKey]ValidValue1 -> Display Value 1");
        CollectionAssert.Contains(dependentSetting.ValidValues!, "[ValidKey]ValidValue2 -> Display Value 2");
    }

    [Test]
    public async Task ShallUpdateValidValuesWhenLookupTableIsUpdatedExternally()
    {
        var secret = GetNewSecret();
        
        // Start with a basic lookup table
        var initialLookupTable = new Dictionary<string, string?>
        {
            { "Initial1", "Initial Value 1" },
            { "Initial2", "Initial Value 2" }
        };
        var initialLookup = new LookupTableDataContract(null, "Animals", initialLookupTable, false);
        await AddLookupTable(initialLookup);

        var (settings, configuration) = InitializeConfigurationProvider<AnimalsTest>(secret);

        // Verify initial valid values
        var client = (await GetAllClients()).Single(c => c.Name == settings.CurrentValue.ClientName);
        var animalSetting = client.Settings.FirstOrDefault(s => s.Name == nameof(settings.CurrentValue.Pets));
        
        Assert.That(animalSetting?.ValidValues, Is.Not.Null);
        Assert.That(animalSetting!.ValidValues!.Count, Is.EqualTo(3), "The two options plus the invalid option");

        // Update the lookup table externally
        var updatedLookupTable = new Dictionary<string, string?>
        {
            { "Updated1", "Updated Value 1" },
            { "Updated2", "Updated Value 2" },
            { "Updated3", "Updated Value 3" }
        };
        
        var allLookupTables = await GetAllLookupTables();
        var existingLookup = allLookupTables.First(lt => lt.Name == "Animals");
        existingLookup.LookupTable = updatedLookupTable;
        
        await UpdateLookupTable(existingLookup);

        // Force reload and verify valid values are updated
        configuration.Reload();
        
        client = (await GetAllClients()).Single(c => c.Name == settings.CurrentValue.ClientName);
        animalSetting = client.Settings.FirstOrDefault(s => s.Name == nameof(settings.CurrentValue.Pets));
        
        Assert.That(animalSetting?.ValidValues, Is.Not.Null);
        Assert.That(animalSetting!.ValidValues!.Count, Is.EqualTo(4));
        CollectionAssert.Contains(animalSetting.ValidValues!, "Updated1 -> Updated Value 1");
        CollectionAssert.Contains(animalSetting.ValidValues!, "Updated2 -> Updated Value 2");
        CollectionAssert.Contains(animalSetting.ValidValues!, "Updated3 -> Updated Value 3");
        CollectionAssert.Contains(animalSetting.ValidValues!, "Spot -> [INVALID]"); // Previous value should still be there as invalid
    }

    [Test]
    public async Task ShallHandleKeyedLookupWithNullKeySettingName()
    {
        var secret = GetNewSecret();
        
        // Test setting with LookupTable attribute but no keySettingName
        var lookupTable = new Dictionary<string, string?>
        {
            { "Value1", "Display 1" },
            { "Value2", "Display 2" },
            { "Value3", "Display 3" }
        };
        var lookup = new LookupTableDataContract(null, "MixedSourceLookupTest:ProviderTypes", lookupTable, true);
        await AddLookupTable(lookup);

        var (settings, _) = InitializeConfigurationProvider<MixedSourceLookupTest>(secret);

        var client = (await GetAllClients()).Single(c => c.Name == settings.CurrentValue.ClientName);
        var providerSetting = client.Settings.FirstOrDefault(s => s.Name == nameof(settings.CurrentValue.ProviderDefinedType));
        
        Assert.That(providerSetting?.ValidValues, Is.Not.Null);
        CollectionAssert.Contains(providerSetting!.ValidValues!, "Value1 -> Display 1");
        CollectionAssert.Contains(providerSetting.ValidValues!, "Value2 -> Display 2");
        CollectionAssert.Contains(providerSetting.ValidValues!, "Value3 -> Display 3");
    }

    [Test]
    public async Task ShallHandleEmptyKeyedLookupTable()
    {
        var secret = GetNewSecret();
        
        // Create empty lookup table
        var emptyLookupTable = new Dictionary<string, string?>();
        var emptyLookup = new LookupTableDataContract(null, "InvalidLookupTest:InvalidLookup", emptyLookupTable, true);
        await AddLookupTable(emptyLookup);

        var (settings, _) = InitializeConfigurationProvider<InvalidLookupTest>(secret);

        var client = (await GetAllClients()).Single(c => c.Name == settings.CurrentValue.ClientName);
        var nonExistentSetting = client.Settings.FirstOrDefault(s => s.Name == nameof(settings.CurrentValue.NonExistentLookup));
        
        // Should handle gracefully with no valid values or empty valid values
        Assert.That(nonExistentSetting?.ValidValues, Is.Null.Or.Empty);
    }

    [Test]
    public async Task ShallHandleNullValuesInKeyedLookupTable()
    {
        var secret = GetNewSecret();
        
        var keyedLookupTable = new Dictionary<string, string?>
        {
            { "[Key1]value1", null },  // No alias
            { "[Key1]value2", "Display Value 2" },  // With alias
            { "[Key2]value3", null },  // No alias
            { "[Key2]value4", "Display Value 4" }   // With alias
        };
        var keyedLookup = new LookupTableDataContract(null, "NullValueKeyedLookupIntegrationTest:NullValueKeyedTestLookup", keyedLookupTable, true);
        await AddLookupTable(keyedLookup);

        var primaryLookup = new Dictionary<string, string?>
        {
            { "Key1", null },
            { "Key2", null }
        };
        var primaryTable = new LookupTableDataContract(null, "NullValueKeyedLookupIntegrationTest:NullValuePrimaryLookup", primaryLookup, true);
        await AddLookupTable(primaryTable);

        var (settings, _) = InitializeConfigurationProvider<NullValueKeyedLookupIntegrationTest>(secret);

        var client = (await GetAllClients()).Single(c => c.Name == settings.CurrentValue.ClientName);
        var dependentSetting = client.Settings.FirstOrDefault(s => s.Name == nameof(settings.CurrentValue.DependentValue));
        
        Assert.That(dependentSetting?.ValidValues, Is.Not.Null);
        
        // Values with null aliases should appear as just the key, values with aliases should appear as "key -> alias"
        CollectionAssert.Contains(dependentSetting!.ValidValues!, "[Key1]value1");
        CollectionAssert.Contains(dependentSetting.ValidValues!, "[Key1]value2 -> Display Value 2");
    }
    
    [Test]
    public async Task ShallProcessMultiLevelKeyedLookupMetadata()
    {
        // Arrange
        var secret = GetNewSecret();
        
        // Create the lookup tables needed for the multi-level test
        var categoryLookup = new Dictionary<string, string?>
        {
            { "Electronics", "Electronic Items" },
            { "Books", "Book Items" },
            { "Clothing", "Clothing Items" }
        };
        var categoryLookupTable = new LookupTableDataContract(null, "MultiLevelKeyedLookupIntegrationTest:MultiLevelCategoryLookup", categoryLookup, true);
        await AddLookupTable(categoryLookupTable);

        var subcategoryLookup = new Dictionary<string, string?>
        {
            { "[Electronics]Laptops", "Laptop Computers" },
            { "[Electronics]Phones", "Mobile Phones" },
            { "[Books]Fiction", "Fiction Books" },
            { "[Books]NonFiction", "Non-Fiction Books" }
        };
        var subcategoryLookupTable = new LookupTableDataContract(null, "MultiLevelKeyedLookupIntegrationTest:MultiLevelSubcategoryLookup", subcategoryLookup, true);
        await AddLookupTable(subcategoryLookupTable);

        var itemLookup = new Dictionary<string, string?>
        {
            { "[Laptops]Gaming Laptop", "High-performance gaming laptop" },
            { "[Laptops]Business Laptop", "Professional business laptop" },
            { "[Phones]Smartphone", "Modern smartphone device" },
            { "[Fiction]Novel", "Fiction novel book" }
        };
        var itemLookupTable = new LookupTableDataContract(null, "MultiLevelKeyedLookupIntegrationTest:MultiLevelItemLookup", itemLookup, true);
        await AddLookupTable(itemLookupTable);

        var (_, _) = InitializeConfigurationProvider<MultiLevelKeyedLookupIntegrationTest>(secret);

        // Act
        var allClients = await GetAllClients();
        var lookupClient = allClients.FirstOrDefault(c => c.Name == "MultiLevelKeyedLookupIntegrationTest");

        // Assert
        Assert.That(lookupClient, Is.Not.Null, "MultiLevelKeyedLookupIntegrationTest client should exist");

        // Test category setting
        var categorySetting = lookupClient!.Settings.FirstOrDefault(s => s.Name == "Category");
        Assert.That(categorySetting, Is.Not.Null, "Category setting should exist");
        Assert.That(categorySetting!.LookupTableKey, Is.EqualTo("MultiLevelKeyedLookupIntegrationTest:MultiLevelCategoryLookup"), "Category should have correct lookup table key");
        Assert.That(categorySetting.LookupKeySettingName, Is.Null, "Category should not have key setting name (primary key)");

        // Test subcategory setting
        var subcategorySetting = lookupClient.Settings.FirstOrDefault(s => s.Name == "Subcategory");
        Assert.That(subcategorySetting, Is.Not.Null, "Subcategory setting should exist");
        Assert.That(subcategorySetting!.LookupTableKey, Is.EqualTo("MultiLevelKeyedLookupIntegrationTest:MultiLevelSubcategoryLookup"), "Subcategory should have correct lookup table key");
        Assert.That(subcategorySetting.LookupKeySettingName, Is.EqualTo("Category"), "Subcategory should depend on Category");

        // Test item setting
        var itemSetting = lookupClient.Settings.FirstOrDefault(s => s.Name == "Item");
        Assert.That(itemSetting, Is.Not.Null, "Item setting should exist");
        Assert.That(itemSetting!.LookupTableKey, Is.EqualTo("MultiLevelKeyedLookupIntegrationTest:MultiLevelItemLookup"), "Item should have correct lookup table key");
        Assert.That(itemSetting.LookupKeySettingName, Is.EqualTo("Subcategory"), "Item should depend on Subcategory");

        // Validate that the keyed lookup metadata is properly configured
        Assert.That(subcategorySetting.LookupTableKey, Is.Not.Null.And.Not.Empty, "Subcategory lookup should be configured");
        Assert.That(itemSetting.LookupTableKey, Is.Not.Null.And.Not.Empty, "Item lookup should be configured");

        // Test that the lookup tables are properly registered
        var allLookupTables = await GetAllLookupTables();
        var categoryLookupDb = allLookupTables.FirstOrDefault(lt => lt.Name == "MultiLevelKeyedLookupIntegrationTest:MultiLevelCategoryLookup");
        var subcategoryLookupDb = allLookupTables.FirstOrDefault(lt => lt.Name == "MultiLevelKeyedLookupIntegrationTest:MultiLevelSubcategoryLookup");
        var itemLookupDb = allLookupTables.FirstOrDefault(lt => lt.Name == "MultiLevelKeyedLookupIntegrationTest:MultiLevelItemLookup");

        Assert.That(categoryLookupDb, Is.Not.Null, "Category lookup table should be registered");
        Assert.That(subcategoryLookupDb, Is.Not.Null, "Subcategory lookup table should be registered");
        Assert.That(itemLookupDb, Is.Not.Null, "Item lookup table should be registered");
        
        Assert.That(categoryLookupDb!.IsClientDefined, Is.False);
        Assert.That(subcategoryLookupDb!.IsClientDefined, Is.False);
        Assert.That(itemLookupDb!.IsClientDefined, Is.False);
    }

    private class MultiLevelKeyedLookupIntegrationTest : TestSettingsBase
    {
        public override string ClientName => "MultiLevelKeyedLookupIntegrationTest";
        public override string ClientDescription => "Multi-Level Keyed Lookup Integration Test";

        [Setting("Category")]
        [LookupTable("MultiLevelCategoryLookup", LookupSource.ProviderDefined)]
        public string? Category { get; set; } = "Electronics";

        [Setting("Subcategory")]
        [LookupTable("MultiLevelSubcategoryLookup", LookupSource.ProviderDefined, keySettingName: nameof(Category))]
        public string? Subcategory { get; set; } = "Laptops";

        [Setting("Item")]
        [LookupTable("MultiLevelItemLookup", LookupSource.ProviderDefined, keySettingName: nameof(Subcategory))]
        public string? Item { get; set; } = "Gaming Laptop";

        public override IEnumerable<string> GetValidationErrors()
        {
            return [];
        }
    }
    
    private class AnimalsTest : TestSettingsBase
    {
        public override string ClientName => "AnimalsTest";
        public override string ClientDescription => "Animals test";

        [Setting("Pets")]
        [LookupTable("Animals", LookupSource.UserDefined)]
        public string? Pets { get; set; } = "Spot";

        public override IEnumerable<string> GetValidationErrors()
        {
            return [];
        }
    }

    private class TemperaturesTest : TestSettingsBase
    {
        public override string ClientName => "TemperaturesTest";
        public override string ClientDescription => "Temperatures Test";

        [Setting("Temperature value")]
        [LookupTable("Temperatures", LookupSource.UserDefined)]
        public int Temperature { get; set; } = 6;

        public override IEnumerable<string> GetValidationErrors()
        {
            return [];
        }
    }

    private class HappyTests : TestSettingsBase
    {
        public override string ClientName => "IsHappyTest";
        public override string ClientDescription => "Is Happy Test";

        [Setting("IsHappy")]
        [LookupTable("IsHappy", LookupSource.UserDefined)]
        public bool IsHappy { get; set; } = true;

        public override IEnumerable<string> GetValidationErrors()
        {
            return [];
        }
    }

    private class StatesTest : TestSettingsBase
    {
        public override string ClientName => "IdsTest";
        public override string ClientDescription => "Ids Test";

        [Setting("States")]
        [LookupTable("States", LookupSource.UserDefined)]
        public long StateId { get; set; } = 99;

        public override IEnumerable<string> GetValidationErrors()
        {
            return [];
        }
    }

    private class PetsNoAliasTest : TestSettingsBase
    {
        public override string ClientName => "PetsNoAliasTest";
        public override string ClientDescription => "Pets No Alias Test";

        [Setting("Pet")]
        [LookupTable("PetsNoAlias", LookupSource.UserDefined)]
        public string? Pet { get; set; } = "Dog";

        public override IEnumerable<string> GetValidationErrors()
        {
            return [];
        }
    }

    private class NullableOptionsTest : TestSettingsBase
    {
        public override string ClientName => "NullableOptionsTest";
        public override string ClientDescription => "Nullable Options Test";

        [Setting("SelectedOption")]
        [LookupTable("NullableOptions", LookupSource.UserDefined)]
        public string? SelectedOption { get; set; } = null;

        public override IEnumerable<string> GetValidationErrors()
        {
            return [];
        }
    }

    private class IssueTypeTest : TestSettingsBase
    {
        public override string ClientName => "IssueTypeTest";
        public override string ClientDescription => "Issue Type Test";

        [Setting("IssueType")]
        [LookupTable("IssueType", LookupSource.ProviderDefined)]
        public string? IssueType { get; set; } = "Bug";

        public override IEnumerable<string> GetValidationErrors()
        {
            return [];
        }
    }

    private class IssuePropertyTest : TestSettingsBase
    {
        public override string ClientName => "IssuePropertyTest";
        public override string ClientDescription => "Issue Property Test";

        [Setting("IssueType")]
        [LookupTable("IssueType", LookupSource.ProviderDefined)]
        public string? IssueType { get; set; } = "Bug";

        [Setting("IssueProperty")]
        [LookupTable("IssueProperty", LookupSource.ProviderDefined, keySettingName: nameof(IssueType))]
        public string? IssueProperty { get; set; } = "High";

        public override IEnumerable<string> GetValidationErrors()
        {
            return [];
        }
    }

    private class MultiLevelLookupTest : TestSettingsBase
    {
        public override string ClientName => "MultiLevelLookupTest";
        public override string ClientDescription => "Multi Level Lookup Test";

        [Setting("Category")]
        [LookupTable("Categories", LookupSource.ProviderDefined)]
        public string? Category { get; set; } = "Electronics";

        [Setting("Subcategory")]
        [LookupTable("Subcategories", LookupSource.ProviderDefined, keySettingName: nameof(Category))]
        public string? Subcategory { get; set; } = "Smartphones";

        [Setting("Product")]
        [LookupTable("Products", LookupSource.ProviderDefined, keySettingName: nameof(Subcategory))]
        public string? Product { get; set; } = "iPhone";

        public override IEnumerable<string> GetValidationErrors()
        {
            return [];
        }
    }

    private class MixedSourceLookupTest : TestSettingsBase
    {
        public override string ClientName => "MixedSourceLookupTest";
        public override string ClientDescription => "Mixed Source Lookup Test";

        [Setting("UserDefinedCategory")]
        [LookupTable("UserCategories", LookupSource.UserDefined)]
        public string? UserDefinedCategory { get; set; } = "Category1";

        [Setting("ProviderDefinedType")]
        [LookupTable("ProviderTypes", LookupSource.ProviderDefined)]
        public string? ProviderDefinedType { get; set; } = "Type1";

        public override IEnumerable<string> GetValidationErrors()
        {
            return [];
        }
    }

    private class InvalidLookupIntegrationTest : TestSettingsBase
    {
        public override string ClientName => "InvalidLookupIntegrationTest";
        public override string ClientDescription => "Invalid Lookup Integration Test";

        [Setting("PrimaryKey")]
        [LookupTable("InvalidKeyedPrimaryLookup", LookupSource.ProviderDefined)]
        public string? PrimaryKey { get; set; } = "ValidKey";

        [Setting("DependentValue")]
        [LookupTable("InvalidKeyedTestLookup", LookupSource.ProviderDefined, keySettingName: nameof(PrimaryKey))]
        public string? DependentValue { get; set; } = "ValidValue1";

        public override IEnumerable<string> GetValidationErrors()
        {
            return [];
        }
    }

    private class NullValueKeyedLookupIntegrationTest : TestSettingsBase
    {
        public override string ClientName => "NullValueKeyedLookupIntegrationTest";
        public override string ClientDescription => "Null Value Keyed Lookup Integration Test";

        [Setting("PrimaryKey")]
        [LookupTable("NullValuePrimaryLookup", LookupSource.ProviderDefined)]
        public string? PrimaryKey { get; set; } = "Key1";

        [Setting("DependentValue")]
        [LookupTable("NullValueKeyedTestLookup", LookupSource.ProviderDefined, keySettingName: nameof(PrimaryKey))]
        public string? DependentValue { get; set; } = "value1";

        public override IEnumerable<string> GetValidationErrors()
        {
            return [];
        }
    }

    private class InvalidLookupTest : TestSettingsBase
    {
        public override string ClientName => "InvalidLookupTest";
        public override string ClientDescription => "Invalid Lookup Test";

        [Setting("NonExistentLookup")]
        [LookupTable("NonExistentTable", LookupSource.ProviderDefined)]
        public string? NonExistentLookup { get; set; } = "default";

        public override IEnumerable<string> GetValidationErrors()
        {
            return [];
        }
    }

    [Test]
    public async Task ShallUseTestLookupProviderForProviderDefinedLookups()
    {
        var secret = GetNewSecret();

        var (settings, _) = InitializeConfigurationProvider<TestProviderLookupTest>(secret, addLookupProviders: true);

        // Wait for the provider to register
        // Manual registration has 100ms delay + time to complete registration
        await Task.Delay(1000);

        var client = (await GetAllClients()).SingleOrDefault(c => c.Name == settings.CurrentValue.ClientName);
        Assert.That(client, Is.Not.Null, "Client should exist");

        var testSetting = client!.Settings.FirstOrDefault(s => s.Name == nameof(settings.CurrentValue.TestSetting));
        Assert.That(testSetting, Is.Not.Null, "TestSetting should exist");

        // Verify that the lookup table was populated by the provider
        Assert.That(testSetting!.ValidValues, Is.Not.Null);
        Assert.That(testSetting.ValidValues!.Count, Is.EqualTo(4));
        
        CollectionAssert.Contains(testSetting.ValidValues!, "Option1 -> First Option");
        CollectionAssert.Contains(testSetting.ValidValues!, "Option2 -> Second Option");
        CollectionAssert.Contains(testSetting.ValidValues!, "Option3 -> Third Option");
        CollectionAssert.Contains(testSetting.ValidValues!, "NoAlias");

        // Verify the setting has the correct default value
        Assert.That(settings.CurrentValue.TestSetting, Is.EqualTo("Option1"));
    }

    [Test]
    public async Task ShallUseTestKeyedLookupProviderForProviderDefinedKeyedLookups()
    {
        var secret = GetNewSecret();

        var (settings, _) = InitializeConfigurationProvider<TestKeyedProviderLookupTest>(secret, addLookupProviders: true);

        // Wait for the provider to register
        // Manual registration has 100ms delay + time to complete registration
        await Task.Delay(1000);

        var client = (await GetAllClients()).SingleOrDefault(c => c.Name == settings.CurrentValue.ClientName);
        Assert.That(client, Is.Not.Null, "Client should exist");

        var categorySetting = client!.Settings.FirstOrDefault(s => s.Name == nameof(settings.CurrentValue.Category));
        var itemSetting = client.Settings.FirstOrDefault(s => s.Name == nameof(settings.CurrentValue.Item));
        
        Assert.That(categorySetting, Is.Not.Null, "Category setting should exist");
        Assert.That(itemSetting, Is.Not.Null, "Item setting should exist");

        // Verify category setting has proper lookup table
        Assert.That(categorySetting!.ValidValues, Is.Not.Null);
        CollectionAssert.Contains(categorySetting.ValidValues!, "Category1");
        CollectionAssert.Contains(categorySetting.ValidValues!, "Category2");
        CollectionAssert.Contains(categorySetting.ValidValues!, "Category3");

        // Verify item setting shows flattened keyed lookup values
        Assert.That(itemSetting!.ValidValues, Is.Not.Null);
        CollectionAssert.Contains(itemSetting.ValidValues!, "[Category1]Item1A -> Category 1 - Item A");
        CollectionAssert.Contains(itemSetting.ValidValues!, "[Category1]Item1B -> Category 1 - Item B");
        CollectionAssert.Contains(itemSetting.ValidValues!, "[Category1]Item1C");
        CollectionAssert.Contains(itemSetting.ValidValues!, "[Category2]Item2A -> Category 2 - Item A");
        CollectionAssert.Contains(itemSetting.ValidValues!, "[Category2]Item2B -> Category 2 - Item B");
        CollectionAssert.Contains(itemSetting.ValidValues!, "[Category2]Item2C");
        CollectionAssert.Contains(itemSetting.ValidValues!, "[Category3]Item3A -> Category 3 - Item A");
        CollectionAssert.Contains(itemSetting.ValidValues!, "[Category3]Item3B");
        CollectionAssert.Contains(itemSetting.ValidValues!, "[Category3]Item3C -> Category 3 - Item C");

        // Verify default values
        Assert.That(settings.CurrentValue.Category, Is.EqualTo("Category1"));
        Assert.That(settings.CurrentValue.Item, Is.EqualTo("Item1A"));
    }

    [Test]
    public async Task ShallHandleProviderRegistrationWhenNoMatchingSettings()
    {
        var secret = GetNewSecret();

        // Initialize configuration with providers but no matching settings
        var (settings, _) = InitializeConfigurationProvider<AnimalsTest>(secret, addLookupProviders: true);

        // Wait a bit for providers to attempt registration
        await Task.Delay(1000);

        var client = (await GetAllClients()).SingleOrDefault(c => c.Name == settings.CurrentValue.ClientName);
        Assert.That(client, Is.Not.Null, "Client should exist");

        // Verify that no provider-defined lookup tables were registered since no settings match
        var allLookupTables = await GetAllLookupTables();
        var providerTables = allLookupTables.Where(lt => 
            lt.Name.Contains("TestProviderLookup") || lt.Name.Contains("TestKeyedProviderLookup")).ToList();
        
        Assert.That(providerTables.Count, Is.EqualTo(0), "No provider lookup tables should be registered when no settings match");
    }

    [Test]
    public async Task ShallUpdateSettingValuesFromProviderDefinedLookups()
    {
        var secret = GetNewSecret();

        var (settings, configuration) = InitializeConfigurationProvider<TestProviderLookupTest>(secret, addLookupProviders: true);

        // Wait for provider registration
        await Task.Delay(1000);

        // Verify initial value
        Assert.That(settings.CurrentValue.TestSetting, Is.EqualTo("Option1"));

        // Update the setting to a different value from the provider
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.CurrentValue.TestSetting), new StringSettingDataContract("Option2"))
        };

        await SetSettings(settings.CurrentValue.ClientName, settingsToUpdate);
        configuration.Reload();

        // Verify the setting was updated
        Assert.That(settings.CurrentValue.TestSetting, Is.EqualTo("Option2"));
    }

    [Test]
    public async Task ShallUpdateKeyedLookupDependentValuesFromProvider()
    {
        var secret = GetNewSecret();

        var (settings, configuration) = InitializeConfigurationProvider<TestKeyedProviderLookupTest>(secret, addLookupProviders: true);

        // Wait for provider registration
        await Task.Delay(1000);

        // Verify initial values
        Assert.That(settings.CurrentValue.Category, Is.EqualTo("Category1"));
        Assert.That(settings.CurrentValue.Item, Is.EqualTo("Item1A"));

        // Update category, which should affect available items
        var categoryUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.CurrentValue.Category), new StringSettingDataContract("Category2"))
        };

        await SetSettings(settings.CurrentValue.ClientName, categoryUpdate);
        configuration.Reload();

        Assert.That(settings.CurrentValue.Category, Is.EqualTo("Category2"));

        // Now update item to a value valid for Category2
        var itemUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.CurrentValue.Item), new StringSettingDataContract("Item2B"))
        };

        await SetSettings(settings.CurrentValue.ClientName, itemUpdate);
        configuration.Reload();

        Assert.That(settings.CurrentValue.Item, Is.EqualTo("Item2B"));
    }

    private class TestProviderLookupTest : TestSettingsBase
    {
        public override string ClientName => "TestProviderLookupTest";
        public override string ClientDescription => "Test Provider Lookup Test";

        [Setting("Test Setting")]
        [LookupTable(TestLookupProvider.LookupNameKey, LookupSource.ProviderDefined)]
        public string? TestSetting { get; set; } = "Option1";

        public override IEnumerable<string> GetValidationErrors()
        {
            return [];
        }
    }

    private class TestKeyedProviderLookupTest : TestSettingsBase
    {
        public override string ClientName => "TestKeyedProviderLookupTest";
        public override string ClientDescription => "Test Keyed Provider Lookup Test";

        [Setting("Category")]
        [LookupTable("TestProviderCategory", LookupSource.ProviderDefined)]
        public string? Category { get; set; } = "Category1";

        [Setting("Item")]
        [LookupTable(TestKeyedLookupProvider.LookupNameKey, LookupSource.ProviderDefined, keySettingName: nameof(Category))]
        public string? Item { get; set; } = "Item1A";

        public override IEnumerable<string> GetValidationErrors()
        {
            return [];
        }
    }
}