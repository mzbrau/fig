using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Client;
using Fig.Client.Attributes;
using Fig.Contracts.Common;
using Fig.Contracts.Settings;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

public class CommonEnumerationsTests : IntegrationTestBase
{
    [Test]
    public async Task ShallAddCommonEnumeration()
    {
        var enumeration = new Dictionary<string, string>
        {
            {"1", "Dog"},
            {"2", "Cat"},
            {"3", "Fish"},
            {"4", "Rabbit"}
        };

        var item = new CommonEnumerationDataContract(null, "Animals", enumeration);

        await AddCommonEnumeration(item);
        var allItems = await GetAllCommonEnumerations();

        var commonEnumerationDataContracts = allItems.ToList();
        Assert.That(commonEnumerationDataContracts.Count, Is.EqualTo(1));
        var result = commonEnumerationDataContracts.Single();
        Assert.That(result.Name, Is.EqualTo(item.Name));
        CollectionAssert.AreEquivalent(item.Enumeration, result.Enumeration);
    }

    [Test]
    public async Task ShallGetMultipleItems()
    {
        var enumeration = new Dictionary<string, string>
        {
            {"1", "Dog"},
            {"2", "Cat"}
        };

        var animals = new CommonEnumerationDataContract(null, "Animals", enumeration);

        await AddCommonEnumeration(animals);
        var enumeration2 = new Dictionary<string, string>
        {
            {"1", "Sunny"},
            {"2", "Rain"}
        };

        var weather = new CommonEnumerationDataContract(null, "Weather", enumeration2);

        await AddCommonEnumeration(weather);

        var allItems = (await GetAllCommonEnumerations()).ToList();

        Assert.That(allItems.Count, Is.EqualTo(2));
        Assert.That(allItems[0].Name, Is.EqualTo(animals.Name));
        Assert.That(allItems[1].Name, Is.EqualTo(weather.Name));
        CollectionAssert.AreEquivalent(allItems[0].Enumeration, animals.Enumeration);
        CollectionAssert.AreEquivalent(allItems[1].Enumeration, weather.Enumeration);
    }

    [Test]
    public async Task ShallUpdateCommonEnumeration()
    {
        var enumeration = new Dictionary<string, string>
        {
            {"1", "Dog"},
            {"2", "Cat"},
            {"3", "Fish"},
            {"4", "Rabbit"}
        };

        var item = new CommonEnumerationDataContract(null, "Animals", enumeration);

        await AddCommonEnumeration(item);
        var allItems = await GetAllCommonEnumerations();

        var commonEnumerationDataContracts = allItems.ToList();
        var updated = commonEnumerationDataContracts.Single();

        updated.Name = "More Animals";
        updated.Enumeration.Add("5", "Snake");

        await UpdateCommonEnumeration(updated);

        Assert.That(commonEnumerationDataContracts.Count, Is.EqualTo(1));
        var result = commonEnumerationDataContracts.Single();
        Assert.That(result.Name, Is.EqualTo(updated.Name));
        CollectionAssert.AreEquivalent(updated.Enumeration, result.Enumeration);
    }

    [Test]
    public async Task ShallDeleteCommonEnumeration()
    {
        var enumeration = new Dictionary<string, string>
        {
            {"1", "Dog"},
            {"2", "Cat"}
        };

        var animals = new CommonEnumerationDataContract(null, "Animals", enumeration);

        await AddCommonEnumeration(animals);
        var enumeration2 = new Dictionary<string, string>
        {
            {"1", "Sunny"},
            {"2", "Rain"}
        };
        var weather = new CommonEnumerationDataContract(null, "Weather", enumeration2);

        await AddCommonEnumeration(weather);

        var allItems = await GetAllCommonEnumerations();

        await DeleteCommonEnumeration(allItems.First().Id);

        var allItems2 = await GetAllCommonEnumerations();

        Assert.That(allItems2.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task ShallProvideCommonEnumerationValuesWhenGettingSettings()
    {
        var secret = GetNewSecret();

        var enumeration = new Dictionary<string, string>
        {
            {"Spot", "Dog"},
            {"Fluffy", "Cat"}
        };
        var animals = new CommonEnumerationDataContract(null, "Animals", enumeration);

        await AddCommonEnumeration(animals);

        var settings = await RegisterSettings<AnimalsTest>(secret);

        var originalSettings = await GetSettingsForClient(settings.ClientName, secret);
        settings.Update(originalSettings);
        Assert.That(settings.Pets, Is.EqualTo(animals.Enumeration.First().Key));

        var client = (await GetAllClients()).ToList().First();

        CollectionAssert.AreEquivalent(
            animals.Enumeration.Select(a => $"{a.Key} -> {a.Value}").ToList(),
            client.Settings.Single().ValidValues);
    }

    [Test]
    public async Task ShallSetStringValueFromCommonEnumerationValue()
    {
        var secret = GetNewSecret();
        var enumeration = new Dictionary<string, string>
        {
            {"Spot", "Dog"},
            {"Fluffy", "Cat"}
        };
        var animals = new CommonEnumerationDataContract(null, "Animals", enumeration);

        await AddCommonEnumeration(animals);

        var settings = await RegisterSettings<AnimalsTest>(secret);

        var originalSettings = await GetSettingsForClient(settings.ClientName, secret);
        settings.Update(originalSettings);
        Assert.That(settings.Pets, Is.EqualTo(animals.Enumeration.First().Key));

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
        Assert.That(settings.Pets, Is.EqualTo(animals.Enumeration.Last().Key));
    }

    [Test]
    public async Task ShallSetIntValueFromCommonEnumerationValue()
    {
        var secret = GetNewSecret();
        var enumeration = new Dictionary<string, string>
        {
            {"6", "Cold"},
            {"20", "Nice"},
            {"35", "Hot"}
        };
        var temperatures = new CommonEnumerationDataContract(null, "Temperatures", enumeration);

        await AddCommonEnumeration(temperatures);

        var settings = await RegisterSettings<TemperaturesTest>(secret);

        var originalSettings = await GetSettingsForClient(settings.ClientName, secret);
        settings.Update(originalSettings);
        Assert.That(settings.Temps, Is.EqualTo(int.Parse(temperatures.Enumeration.First().Key)));

        var client = (await GetAllClients()).ToList().Single();

        var firstItem = temperatures.Enumeration.First();
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

        Assert.That(updatedSettings.Single().Value, Is.EqualTo(int.Parse(temperatures.Enumeration.Last().Key)));
    }

    [Test]
    public async Task ShallSetBoolValueFromCommonEnumerationValue()
    {
        var secret = GetNewSecret();
        var enumeration = new Dictionary<string, string>
        {
            {"True", "Very Happy"},
            {"False", "Unfortunately Sad"}
        };
        var temperatures = new CommonEnumerationDataContract(null, "IsHappy", enumeration);

        await AddCommonEnumeration(temperatures);

        var settings = await RegisterSettings<HappyTests>(secret);

        var originalSettings = await GetSettingsForClient(settings.ClientName, secret);
        settings.Update(originalSettings);
        Assert.That(settings.IsHappy, Is.EqualTo(bool.Parse(temperatures.Enumeration.First().Key)));

        var client = (await GetAllClients()).ToList().Single();

        var firstItem = temperatures.Enumeration.First();
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

        Assert.That(updatedSettings.Single().Value, Is.EqualTo(bool.Parse(temperatures.Enumeration.Last().Key)));
    }

    [Test]
    public async Task ShallSetLongValueFromCommonEnumerationValue()
    {
        var secret = GetNewSecret();
        var enumeration = new Dictionary<string, string>
        {
            {"99", "Open"},
            {"403", "In Progress"},
            {"992", "Closed"}
        };
        var states = new CommonEnumerationDataContract(null, "States", enumeration);

        await AddCommonEnumeration(states);

        var settings = await RegisterSettings<StatesTest>(secret);

        var originalSettings = await GetSettingsForClient(settings.ClientName, secret);
        settings.Update(originalSettings);
        Assert.That(settings.StateIds, Is.EqualTo(long.Parse(states.Enumeration.First().Key)));

        var client = (await GetAllClients()).ToList().Single();

        var firstItem = states.Enumeration.First();
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

        Assert.That(updatedSettings.Single().Value, Is.EqualTo(long.Parse(states.Enumeration.Skip(1).First().Key)));
    }

    [Test]
    public async Task ShallHandleNoMatchingEnumerationForStrings()
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
    public async Task ShallHandleNoMatchingEnumerationForIntegers()
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

        var enumeration = new Dictionary<string, string>
        {
            {"6", "Cold"},
            {"20", "Nice"},
            {"35", "Hot"}
        };

        var temperatures = new CommonEnumerationDataContract(null, "Temperatures", enumeration);

        await AddCommonEnumeration(temperatures);

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

        Assert.That(settings.Temps, Is.EqualTo(int.Parse(temperatures.Enumeration.Last().Key)));
    }

    public class AnimalsTest : SettingsBase
    {
        public override string ClientName => "AnimalsTest";

        [Setting("Pets", "Spot")]
        [CommonEnumeration("Animals")]
        public string? Pets { get; set; }
    }

    public class TemperaturesTest : SettingsBase
    {
        public override string ClientName => "TemperaturesTest";

        [Setting("Temps", 6)]
        [CommonEnumeration("Temperatures")]
        public int Temps { get; set; }
    }

    public class HappyTests : SettingsBase
    {
        public override string ClientName => "IsHappyTest";

        [Setting("IsHappy", true)]
        [CommonEnumeration("IsHappy")]
        public bool IsHappy { get; set; }
    }

    public class StatesTest : SettingsBase
    {
        public override string ClientName => "IdsTest";

        [Setting("States", 99)]
        [CommonEnumeration("States")]
        public long StateIds { get; set; }
    }
}