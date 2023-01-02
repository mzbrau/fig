using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Client;
using Fig.Client.Attributes;
using Fig.Contracts.Common;
using Fig.Contracts.Settings;
using Fig.Test.Common;
using NUnit.Framework;

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

        var settings = await RegisterSettings<AnimalsTest>(secret);

        var originalSettings = await GetSettingsForClient(settings.ClientName, secret);
        settings.Update(originalSettings);
        Assert.That(settings.Pets, Is.EqualTo(animals.LookupTable.First().Key));

        var client = (await GetAllClients()).ToList().First();

        CollectionAssert.AreEquivalent(
            animals.LookupTable.Select(a => $"{a.Key} -> {a.Value}").ToList(),
            client.Settings.Single().ValidValues);
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

        var settings = await RegisterSettings<AnimalsTest>(secret);

        var originalSettings = await GetSettingsForClient(settings.ClientName, secret);
        settings.Update(originalSettings);
        Assert.That(settings.Pets, Is.EqualTo(animals.LookupTable.First().Key));

        var client = (await GetAllClients()).ToList().Single();

        var validValues = client.Settings.Single().ValidValues;
        if (validValues != null)
        {
            var settingsToUpdate = new List<SettingDataContract>
            {
                new(nameof(settings.Pets), validValues.Last())
            };

            await SetSettings(settings.ClientName, settingsToUpdate);
        }

        var updatedSettings = await GetSettingsForClient(settings.ClientName, secret);
        settings.Update(updatedSettings);
        Assert.That(settings.Pets, Is.EqualTo(animals.LookupTable.Last().Key));
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

        var settings = await RegisterSettings<TemperaturesTest>(secret);

        var originalSettings = await GetSettingsForClient(settings.ClientName, secret);
        settings.Update(originalSettings);
        Assert.That(settings.Temps, Is.EqualTo(int.Parse(temperatures.LookupTable.First().Key)));

        var client = (await GetAllClients()).ToList().Single();

        var firstItem = temperatures.LookupTable.First();
        Assert.That(client.Settings.Single().Value, Is.EqualTo($"{firstItem.Key} -> {firstItem.Value}"));
        Assert.That(client.Settings.Single().ValueType, Is.EqualTo(typeof(string)));

        var validValues = client.Settings.Single().ValidValues;
        if (validValues != null)
        {
            var settingsToUpdate = new List<SettingDataContract>
            {
                new(nameof(settings.Temps), validValues.Last())
            };

            await SetSettings(settings.ClientName, settingsToUpdate);
        }

        var updatedSettings = await GetSettingsForClient(settings.ClientName, secret);

        Assert.That(updatedSettings.Single().Value, Is.EqualTo(int.Parse(temperatures.LookupTable.Last().Key)));
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

        var settings = await RegisterSettings<HappyTests>(secret);

        var originalSettings = await GetSettingsForClient(settings.ClientName, secret);
        settings.Update(originalSettings);
        Assert.That(settings.IsHappy, Is.EqualTo(bool.Parse(temperatures.LookupTable.First().Key)));

        var client = (await GetAllClients()).ToList().Single();

        var firstItem = temperatures.LookupTable.First();
        Assert.That(client.Settings.Single().Value, Is.EqualTo($"{firstItem.Key} -> {firstItem.Value}"));
        Assert.That(client.Settings.Single().ValueType, Is.EqualTo(typeof(string)));

        var validValues = client.Settings.Single().ValidValues;
        if (validValues != null)
        {
            var settingsToUpdate = new List<SettingDataContract>
            {
                new(nameof(settings.IsHappy), validValues.Last())
            };

            await SetSettings(settings.ClientName, settingsToUpdate);
        }

        var updatedSettings = await GetSettingsForClient(settings.ClientName, secret);

        Assert.That(updatedSettings.Single().Value, Is.EqualTo(bool.Parse(temperatures.LookupTable.Last().Key)));
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

        var settings = await RegisterSettings<StatesTest>(secret);

        var originalSettings = await GetSettingsForClient(settings.ClientName, secret);
        settings.Update(originalSettings);
        Assert.That(settings.StateIds, Is.EqualTo(long.Parse(states.LookupTable.First().Key)));

        var client = (await GetAllClients()).ToList().Single();

        var firstItem = states.LookupTable.First();
        Assert.That(client.Settings.Single().Value, Is.EqualTo($"{firstItem.Key} -> {firstItem.Value}"));
        Assert.That(client.Settings.Single().ValueType, Is.EqualTo(typeof(string)));

        var validValues = client.Settings.Single().ValidValues;
        if (validValues != null)
        {
            var settingsToUpdate = new List<SettingDataContract>
            {
                new(nameof(settings.StateIds), validValues.Skip(1).First())
            };

            await SetSettings(settings.ClientName, settingsToUpdate);
        }

        var updatedSettings = await GetSettingsForClient(settings.ClientName, secret);

        Assert.That(updatedSettings.Single().Value, Is.EqualTo(long.Parse(states.LookupTable.Skip(1).First().Key)));
    }

    [Test]
    public async Task ShallHandleNoMatchingLookupForStrings()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<AnimalsTest>(secret);

        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.Pets), "Hippo")
        };

        await SetSettings(settings.ClientName, settingsToUpdate);

        var updatedSettings = await GetSettingsForClient(settings.ClientName, secret);
        settings.Update(updatedSettings);

        Assert.That(settings.Pets, Is.EqualTo("Hippo"));
    }

    [Test]
    public async Task ShallHandleNoMatchingLookupForIntegers()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<TemperaturesTest>(secret);

        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.Temps), 9)
        };

        await SetSettings(settings.ClientName, settingsToUpdate);

        var updatedSettings = await GetSettingsForClient(settings.ClientName, secret);
        settings.Update(updatedSettings);

        Assert.That(settings.Temps, Is.EqualTo(9));
    }

    [Test]
    public async Task ShallKeepInvalidOptionInList()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<TemperaturesTest>(secret);

        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(settings.Temps), 9)
        };

        await SetSettings(settings.ClientName, settingsToUpdate);

        var lookupTable = new Dictionary<string, string>
        {
            {"6", "Cold"},
            {"20", "Nice"},
            {"35", "Hot"}
        };

        var temperatures = new LookupTableDataContract(null, "Temperatures", lookupTable);

        await AddLookupTable(temperatures);

        var originalSettings = await GetSettingsForClient(settings.ClientName, secret);
        settings.Update(originalSettings);
        Assert.That(settings.Temps, Is.EqualTo(9));

        var client = (await GetAllClients()).ToList().Single();

        Assert.That(client.Settings.Single().Value, Is.EqualTo("9 -> [INVALID]"));
        Assert.That(client.Settings.Single().ValueType, Is.EqualTo(typeof(string)));

        var validValues = client.Settings.Single().ValidValues;
        if (validValues != null)
        {
            var settingsToUpdate2 = new List<SettingDataContract>
            {
                new(nameof(settings.Temps), validValues.Last())
            };

            await SetSettings(settings.ClientName, settingsToUpdate2);
        }


        var updatedSettings = await GetSettingsForClient(settings.ClientName, secret);
        settings.Update(updatedSettings);

        Assert.That(settings.Temps, Is.EqualTo(int.Parse(temperatures.LookupTable.Last().Key)));
    }

    public class AnimalsTest : SettingsBase
    {
        public override string ClientName => "AnimalsTest";

        [Setting("Pets", "Spot")]
        [LookupTable("Animals")]
        public string? Pets { get; set; }
    }

    public class TemperaturesTest : SettingsBase
    {
        public override string ClientName => "TemperaturesTest";

        [Setting("Temps", 6)]
        [LookupTable("Temperatures")]
        public int Temps { get; set; }
    }

    public class HappyTests : SettingsBase
    {
        public override string ClientName => "IsHappyTest";

        [Setting("IsHappy", true)]
        [LookupTable("IsHappy")]
        public bool IsHappy { get; set; }
    }

    public class StatesTest : SettingsBase
    {
        public override string ClientName => "IdsTest";

        [Setting("States", 99)]
        [LookupTable("States")]
        public long StateIds { get; set; }
    }
}