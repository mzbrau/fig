using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Contracts.LookupTable;
using Fig.Contracts.Settings;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class SettingCollectionsWithValidValuesTests : IntegrationTestBase
{
    [Test]
    public async Task ShallApplyLookupTableToStringList()
    {
        var secret = GetNewSecret();
        var lookupTable = new Dictionary<string, string>
        {
            {"Spot", "Dog"},
            {"Fluffy", "Cat"},
            {"Hoppy", "Rabbit"}
        };
        var animals = new LookupTableDataContract(null, "AnimalNames", lookupTable);

        await AddLookupTable(animals);

        var settings = await RegisterSettings<ClientWithCollections>(secret);

        var client = (await GetAllClients()).ToList().Single();

        var animalNamesSetting = client.Settings.First(a => a.Name == nameof(settings.AnimalNames));
        Assert.That(animalNamesSetting.ValidValues.Count, Is.EqualTo(lookupTable.Count));
        Assert.That(animalNamesSetting.DataGridDefinition.Columns.Single().ValidValues.Count, Is.EqualTo(lookupTable.Count));

        var validValues = animalNamesSetting.DataGridDefinition.Columns.Single().ValidValues;
        Assert.That(validValues.First(), Is.EqualTo("Spot -> Dog"));
        
        var settingsToUpdate = new List<SettingDataContract>()
        {
            new(nameof(settings.AnimalNames), new DataGridSettingDataContract(
                new List<Dictionary<string, object?>>()
                {
                    new()
                    {
                        { "Values", validValues.First() }
                    },
                    new()
                    {
                        { "Values", validValues.Last() }
                    }
                }))
        };
        await SetSettings(settings.ClientName, settingsToUpdate);
        
        var updatedSettings = await GetSettingsForClient(settings.ClientName, secret);
        settings.Update(updatedSettings);
        Assert.That(settings.AnimalNames.Count, Is.EqualTo(2));
        Assert.That(settings.AnimalNames[0], Is.EqualTo(lookupTable.First().Key));
        Assert.That(settings.AnimalNames[1], Is.EqualTo(lookupTable.Last().Key));
    }

    [Test]
    public async Task ShallApplyLookupTableToFirstItemInObjectList()
    {
        var secret = GetNewSecret();
        var lookupTable = new Dictionary<string, string>
        {
            {"Spot", "Dog"},
            {"Fluffy", "Cat"},
            {"Hoppy", "Rabbit"}
        };
        var animals = new LookupTableDataContract(null, "AnimalNames", lookupTable);

        await AddLookupTable(animals);

        var settings = await RegisterSettings<ClientWithCollections>(secret);

        var client = (await GetAllClients()).ToList().Single();

        var animalDetailsSetting = client.Settings.First(a => a.Name == nameof(settings.AnimalDetails));
        Assert.That(animalDetailsSetting.ValidValues.Count, Is.EqualTo(lookupTable.Count));
        Assert.That(animalDetailsSetting.DataGridDefinition.Columns.First().ValidValues.Count, Is.EqualTo(lookupTable.Count));

        var validValues = animalDetailsSetting.DataGridDefinition.Columns.First().ValidValues;
        
        Assert.That(validValues.First(), Is.EqualTo("Spot -> Dog"));

        var settingsToUpdate = new List<SettingDataContract>()
        {
            new(nameof(settings.AnimalDetails), new DataGridSettingDataContract(
                new List<Dictionary<string, object?>>()
                {
                    new()
                    {
                        { "Name", validValues.First() },
                        { "Category", "Pet" },
                        { "HeightCm", 10 },
                    },
                    new()
                    {
                        { "Name", validValues.Last() },
                        { "Category", "Farm" },
                        { "HeightCm", 20 },
                    }
                }))
        };
        await SetSettings(settings.ClientName, settingsToUpdate);
        
        var updatedSettings = await GetSettingsForClient(settings.ClientName, secret);
        settings.Update(updatedSettings);
        Assert.That(settings.AnimalDetails.Count, Is.EqualTo(2));
        Assert.That(settings.AnimalDetails[0].Name, Is.EqualTo(lookupTable.First().Key));
        Assert.That(settings.AnimalDetails[0].Category, Is.EqualTo("Pet"));
        Assert.That(settings.AnimalDetails[0].HeightCm, Is.EqualTo(10));
        Assert.That(settings.AnimalDetails[1].Name, Is.EqualTo(lookupTable.Last().Key));
        Assert.That(settings.AnimalDetails[1].Category, Is.EqualTo("Farm"));
        Assert.That(settings.AnimalDetails[1].HeightCm, Is.EqualTo(20));
    }

    [Test]
    public async Task ShallApplyValidValuesToStringList()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ClientWithCollections>(secret);

        var client = (await GetAllClients()).ToList().Single();

        var cityNamesSetting = client.Settings.First(a => a.Name == nameof(settings.CityNames));
        Assert.That(cityNamesSetting.ValidValues.Count, Is.EqualTo(3));
        Assert.That(cityNamesSetting.DataGridDefinition.Columns.Single().ValidValues.Count, Is.EqualTo(3));

        var validValues = cityNamesSetting.DataGridDefinition.Columns.Single().ValidValues;
        Assert.That(validValues.First(), Is.EqualTo("Melbourne"));
        
        var settingsToUpdate = new List<SettingDataContract>()
        {
            new(nameof(settings.CityNames), new DataGridSettingDataContract(
                new List<Dictionary<string, object?>>()
                {
                    new()
                    {
                        { "Values", validValues.First() }
                    },
                    new()
                    {
                        { "Values", validValues.Last() }
                    }
                }))
        };
        await SetSettings(settings.ClientName, settingsToUpdate);
        
        var updatedSettings = await GetSettingsForClient(settings.ClientName, secret);
        settings.Update(updatedSettings);
        Assert.That(settings.CityNames.Count, Is.EqualTo(2));
        Assert.That(settings.CityNames[0], Is.EqualTo(validValues.First()));
        Assert.That(settings.CityNames[1], Is.EqualTo(validValues.Last()));
    }

    [Test]
    public async Task TaskShallApplyValidValuesToItemInObjectList()
    {
        var secret = GetNewSecret();
        var settings = await RegisterSettings<ClientWithCollections>(secret);

        var client = (await GetAllClients()).ToList().Single();

        var cityDetailsSetting = client.Settings.First(a => a.Name == nameof(settings.CityDetails));
        Assert.That(cityDetailsSetting.ValidValues.Count, Is.EqualTo(3));
        Assert.That(cityDetailsSetting.DataGridDefinition.Columns.First().ValidValues.Count, Is.EqualTo(3));

        var validValues = cityDetailsSetting.DataGridDefinition.Columns.First().ValidValues;
        
        Assert.That(validValues.First(), Is.EqualTo("London"));
        Assert.That(validValues.Last(), Is.EqualTo("Berlin"));

        var settingsToUpdate = new List<SettingDataContract>()
        {
            new(nameof(settings.CityDetails), new DataGridSettingDataContract(
                new List<Dictionary<string, object?>>()
                {
                    new()
                    {
                        { "Name", validValues.First() },
                        { "Country", "UK" },
                    },
                    new()
                    {
                        { "Name", validValues.Last() },
                        { "Country", "Germany" },
                    }
                }))
        };
        await SetSettings(settings.ClientName, settingsToUpdate);
        
        var updatedSettings = await GetSettingsForClient(settings.ClientName, secret);
        settings.Update(updatedSettings);
        Assert.That(settings.CityDetails.Count, Is.EqualTo(2));
        Assert.That(settings.CityDetails[0].Name, Is.EqualTo(validValues.First()));
        Assert.That(settings.CityDetails[0].Country, Is.EqualTo("UK"));
        Assert.That(settings.CityDetails[1].Name, Is.EqualTo(validValues.Last()));
        Assert.That(settings.CityDetails[1].Country, Is.EqualTo("Germany"));
    }
}