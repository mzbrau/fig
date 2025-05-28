using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Client.Attributes;
using Fig.Contracts.LookupTable;
using Fig.Contracts.Settings;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Fig.Integration.Test.Api;

public class LookupTablesTests : IntegrationTestBase
{
    [Test]
    public async Task ShallAddLookupTable()
    {
        var lookupTable = new Dictionary<string, string>
        {
            {"1", "Dog"},
            {"2", "Cat"},
            {"3", "Fish"},
            {"4", "Rabbit"}
        };

        var item = new LookupTableDataContract(null, "Animals", lookupTable);

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
        var lookupTable = new Dictionary<string, string>
        {
            {"1", "Dog"},
            {"2", "Cat"}
        };

        var animals = new LookupTableDataContract(null, "Animals", lookupTable);

        await AddLookupTable(animals);
        var lookupTable2 = new Dictionary<string, string>
        {
            {"1", "Sunny"},
            {"2", "Rain"}
        };

        var weather = new LookupTableDataContract(null, "Weather", lookupTable2);

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
        var lookupTable = new Dictionary<string, string>
        {
            {"1", "Dog"},
            {"2", "Cat"},
            {"3", "Fish"},
            {"4", "Rabbit"}
        };

        var item = new LookupTableDataContract(null, "Animals", lookupTable);

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
        var lookupTable = new Dictionary<string, string>
        {
            {"1", "Dog"},
            {"2", "Cat"}
        };

        var animals = new LookupTableDataContract(null, "Animals", lookupTable);

        await AddLookupTable(animals);
        var lookupTable2 = new Dictionary<string, string>
        {
            {"1", "Sunny"},
            {"2", "Rain"}
        };
        var weather = new LookupTableDataContract(null, "Weather", lookupTable2);

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

        var lookupTable = new Dictionary<string, string>
        {
            {"Spot", "Dog"},
            {"Fluffy", "Cat"}
        };
        var animals = new LookupTableDataContract(null, "Animals", lookupTable);

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
        var lookupTable = new Dictionary<string, string>
        {
            {"Spot", "Dog"},
            {"Fluffy", "Cat"}
        };
        var animals = new LookupTableDataContract(null, "Animals", lookupTable);

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
        var lookupTable = new Dictionary<string, string>
        {
            {"6", "Cold"},
            {"20", "Nice"},
            {"35", "Hot"}
        };
        var temperatures = new LookupTableDataContract(null, "Temperatures", lookupTable);
    
        await AddLookupTable(temperatures);
        
        var (settings, configuration) = InitializeConfigurationProvider<TemperaturesTest>(secret);

        Assert.That(settings.CurrentValue.Temp, Is.EqualTo(int.Parse(temperatures.LookupTable.First().Key)));
    
        var client = (await GetAllClients()).ToList().Single();
    
        var firstItem = temperatures.LookupTable.First();
        Assert.That(client.Settings.Single().Value?.GetValue(), Is.EqualTo($"{firstItem.Key} -> {firstItem.Value}"));
        Assert.That(client.Settings.Single().ValueType, Is.EqualTo(typeof(string)));
    
        var validValues = client.Settings.Single().ValidValues;
        if (validValues != null)
        {
            var settingsToUpdate = new List<SettingDataContract>
            {
                new(nameof(settings.CurrentValue.Temp), new StringSettingDataContract(validValues.Last()))
            };
    
            await SetSettings(settings.CurrentValue.ClientName, settingsToUpdate);
        }

        configuration.Reload();

        Assert.That(settings.CurrentValue.Temp, Is.EqualTo(int.Parse(temperatures.LookupTable.Last().Key)));
    }
    
    [Test]
    public async Task ShallSetBoolValueFromLookupTableValue()
    {
        var secret = GetNewSecret();
        var lookupTable = new Dictionary<string, string>
        {
            {"True", "Very Happy"},
            {"False", "Unfortunately Sad"}
        };
        var temperatures = new LookupTableDataContract(null, "IsHappy", lookupTable);
    
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
        var lookupTable = new Dictionary<string, string>
        {
            {"99", "Open"},
            {"403", "In Progress"},
            {"992", "Closed"}
        };
        var states = new LookupTableDataContract(null, "States", lookupTable);
    
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
            new(nameof(settings.CurrentValue.Temp), new IntSettingDataContract(9))
        };
    
        await SetSettings(settings.CurrentValue.ClientName, settingsToUpdate);
    
        configuration.Reload();

        Assert.That(settings.CurrentValue.Temp, Is.EqualTo(9));
    }
    
    [Test]
    public async Task ShallKeepInvalidOptionInList()
    {
        var secret = GetNewSecret();
        var (settings, configuration) = InitializeConfigurationProvider<TemperaturesTest>(secret);

        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.CurrentValue.Temp), new IntSettingDataContract(9))
        };
    
        await SetSettings(settings.CurrentValue.ClientName, settingsToUpdate);
    
        var lookupTable = new Dictionary<string, string>
        {
            {"6", "Cold"},
            {"20", "Nice"},
            {"35", "Hot"}
        };
    
        var temperatures = new LookupTableDataContract(null, "Temperatures", lookupTable);
    
        await AddLookupTable(temperatures);
    
        configuration.Reload();

        Assert.That(settings.CurrentValue.Temp, Is.EqualTo(9));
    
        var client = (await GetAllClients()).ToList().Single();
    
        Assert.That(client.Settings.Single().Value?.GetValue(), Is.EqualTo("9 -> [INVALID]"));
        Assert.That(client.Settings.Single().ValueType, Is.EqualTo(typeof(string)));
    
        var validValues = client.Settings.Single().ValidValues;
        if (validValues != null)
        {
            var settingsToUpdate2 = new List<SettingDataContract>
            {
                new(nameof(settings.CurrentValue.Temp), new StringSettingDataContract(validValues.Last()))
            };
    
            await SetSettings(settings.CurrentValue.ClientName, settingsToUpdate2);
        }

        configuration.Reload();

        Assert.That(settings.CurrentValue.Temp, Is.EqualTo(int.Parse(temperatures.LookupTable.Last().Key)));
    }

    public class AnimalsTest : TestSettingsBase
    {
        public override string ClientName => "AnimalsTest";
        public override string ClientDescription => "Animals test";

        [Setting("Pets")]
        [LookupTable("Animals")]
        public string? Pets { get; set; } = "Spot";

        public override IEnumerable<string> GetValidationErrors()
        {
            return [];
        }
    }

    public class TemperaturesTest : TestSettingsBase
    {
        public override string ClientName => "TemperaturesTest";
        public override string ClientDescription => "Temperatures Test";

        [Setting("Temps")]
        [LookupTable("Temperatures")]
        public int Temp { get; set; } = 6;

        public override IEnumerable<string> GetValidationErrors()
        {
            return [];
        }
    }

    public class HappyTests : TestSettingsBase
    {
        public override string ClientName => "IsHappyTest";
        public override string ClientDescription => "Is Happy Test";

        [Setting("IsHappy")]
        [LookupTable("IsHappy")]
        public bool IsHappy { get; set; } = true;

        public override IEnumerable<string> GetValidationErrors()
        {
            return [];
        }
    }

    public class StatesTest : TestSettingsBase
    {
        public override string ClientName => "IdsTest";
        public override string ClientDescription => "Ids Test";

        [Setting("States")]
        [LookupTable("States")]
        public long StateId { get; set; } = 99;

        public override IEnumerable<string> GetValidationErrors()
        {
            return [];
        }
    }
}