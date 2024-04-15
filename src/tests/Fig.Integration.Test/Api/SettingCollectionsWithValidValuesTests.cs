using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
        
        var (settings, configuration) = InitializeConfigurationProvider<ClientWithCollections>(secret);

        var client = (await GetAllClients()).ToList().Single();
    
        var animalNamesSetting = client.Settings.First(a => a.Name == nameof(settings.CurrentValue.AnimalNames));
        Assert.That(animalNamesSetting.ValidValues?.Count, Is.EqualTo(lookupTable.Count));
        Assert.That(animalNamesSetting.DataGridDefinition?.Columns.Single().ValidValues?.Count, Is.EqualTo(lookupTable.Count));
    
        var validValues = animalNamesSetting.DataGridDefinition?.Columns.Single().ValidValues;
        Assert.That(validValues?.First(), Is.EqualTo("Spot -> Dog"));
        
        var settingsToUpdate = new List<SettingDataContract>()
        {
            new(nameof(settings.CurrentValue.AnimalNames), new DataGridSettingDataContract(
                new List<Dictionary<string, object?>>()
                {
                    new()
                    {
                        { "Values", validValues?.First() }
                    },
                    new()
                    {
                        { "Values", validValues?.Last() }
                    }
                }))
        };
        await SetSettings(settings.CurrentValue.ClientName, settingsToUpdate);
        
        configuration.Reload();

        Assert.That(settings.CurrentValue.AnimalNames.Count, Is.EqualTo(2));
        Assert.That(settings.CurrentValue.AnimalNames[0], Is.EqualTo(lookupTable.First().Key));
        Assert.That(settings.CurrentValue.AnimalNames[1], Is.EqualTo(lookupTable.Last().Key));
    }

    [Test]
    public async Task ShallApplyValidValuesToStringList()
    {
        var secret = GetNewSecret();
        var (settings, configuration) = InitializeConfigurationProvider<ClientWithCollections>(secret);

        var client = (await GetAllClients()).ToList().Single();
    
        var cityNamesSetting = client.Settings.First(a => a.Name == nameof(settings.CurrentValue.CityNames));
        Assert.That(cityNamesSetting.ValidValues?.Count, Is.EqualTo(3));
        Assert.That(cityNamesSetting.DataGridDefinition?.Columns.Single().ValidValues?.Count, Is.EqualTo(3));
    
        var validValues = cityNamesSetting.DataGridDefinition?.Columns.Single().ValidValues;
        Assert.That(validValues?.First(), Is.EqualTo("Melbourne"));
        
        var settingsToUpdate = new List<SettingDataContract>()
        {
            new(nameof(settings.CurrentValue.CityNames), new DataGridSettingDataContract(
                new List<Dictionary<string, object?>>()
                {
                    new()
                    {
                        { "Values", validValues?.First() }
                    },
                    new()
                    {
                        { "Values", validValues?.Last() }
                    }
                }))
        };
        await SetSettings(settings.CurrentValue.ClientName, settingsToUpdate);
        
        configuration.Reload();

        Assert.That(settings.CurrentValue.CityNames.Count, Is.EqualTo(2));
        Assert.That(settings.CurrentValue.CityNames[0], Is.EqualTo(validValues?.First()));
        Assert.That(settings.CurrentValue.CityNames[1], Is.EqualTo(validValues?.Last()));
    }

    [Test]
    public async Task TaskShallApplyValidValuesToItemInObjectList()
    {
        var secret = GetNewSecret();
        var (settings, configuration) = InitializeConfigurationProvider<ClientWithCollections>(secret);

        var client = (await GetAllClients()).ToList().Single();
    
        var cityDetailsSetting = client.Settings.First(a => a.Name == nameof(settings.CurrentValue.CityDetails));
        Assert.That(cityDetailsSetting.DataGridDefinition?.Columns.First().ValidValues?.Count, Is.EqualTo(3));
    
        var validValues = cityDetailsSetting.DataGridDefinition?.Columns.First().ValidValues;
        
        Assert.That(validValues?.First(), Is.EqualTo("London"));
        Assert.That(validValues?.Last(), Is.EqualTo("Berlin"));
    
        var settingsToUpdate = new List<SettingDataContract>()
        {
            new(nameof(settings.CurrentValue.CityDetails), new DataGridSettingDataContract(
                new List<Dictionary<string, object?>>()
                {
                    new()
                    {
                        { "Name", validValues?.First() },
                        { "Country", "UK" },
                    },
                    new()
                    {
                        { "Name", validValues?.Last() },
                        { "Country", "Germany" },
                    }
                }))
        };
        await SetSettings(settings.CurrentValue.ClientName, settingsToUpdate);
        
        configuration.Reload();

        Assert.That(settings.CurrentValue.CityDetails.Count, Is.EqualTo(2));
        Assert.That(settings.CurrentValue.CityDetails[0].Name, Is.EqualTo(validValues?.First()));
        Assert.That(settings.CurrentValue.CityDetails[0].Country, Is.EqualTo("UK"));
        Assert.That(settings.CurrentValue.CityDetails[1].Name, Is.EqualTo(validValues?.Last()));
        Assert.That(settings.CurrentValue.CityDetails[1].Country, Is.EqualTo("Germany"));
    }
    
    [Test]
    public async Task ShallCorrectlyLoadAndSaveEnumsInDataGrids()
    {
        var secret = GetNewSecret();
        var (settings, configuration) = InitializeConfigurationProvider<ClientWithCollections>(secret);

        var settingsToUpdate = new List<SettingDataContract>()
        {
            new(nameof(settings.CurrentValue.CityDetails), new DataGridSettingDataContract(
                new List<Dictionary<string, object?>>()
                {
                    new()
                    {
                        { "Name", "London" },
                        { "Country", "UK" },
                        { "Size", "Large" }
                    },
                    new()
                    {
                        { "Name", "Geelong" },
                        { "Country", "Australia" },
                        { "Size", "Small" }
                    }
                }))
        };

        await SetSettings(settings.CurrentValue.ClientName, settingsToUpdate);

        configuration.Reload();
        
        Assert.That(settings.CurrentValue.CityDetails!.Count, Is.EqualTo(2));
        Assert.That(settings.CurrentValue.CityDetails![0].Size, Is.EqualTo(Size.Large));
    }
}