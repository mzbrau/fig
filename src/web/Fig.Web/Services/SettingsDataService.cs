using Fig.Contracts.SettingDefinitions;
using Fig.Web.Converters;
using Fig.Web.Models;
using System.Web;

namespace Fig.Web.Services;

public class SettingsDataService : ISettingsDataService
{
    private ISettingsDefinitionConverter _settingsDefinitionConverter;
    private IHttpService _httpService;

    public SettingsDataService(IHttpService httpService, ISettingsDefinitionConverter settingsDefinitionConverter)
    {
        _httpService = httpService;
        _settingsDefinitionConverter = settingsDefinitionConverter;
    }

    public IList<SettingClientConfigurationModel>? SettingsClients { get; private set; }

    public async Task LoadAllClients()
    {
        var settings = await LoadSettings();
        SettingsClients = _settingsDefinitionConverter.Convert(settings);
    }

    private async Task<List<SettingsClientDefinitionDataContract>> LoadSettings()
    {
        return await _httpService.Get<List<SettingsClientDefinitionDataContract>>("/clients");
    }

    private List<SettingsClientDefinitionDataContract> GenerateFakeData()
    {
        return new List<SettingsClientDefinitionDataContract>()
        {
            new SettingsClientDefinitionDataContract()
            {
                Name = "MyService1",
                Settings = new List<SettingDefinitionDataContract>()
                {
                    new SettingDefinitionDataContract()
                    {
                        Name = "StringSetting",
                        Description = "This is a string setting",
                        Value = "StringValue",
                        IsSecret = true,
                    },
                    new SettingDefinitionDataContract()
                    {
                        Name = "StringSetting2",
                        Description = "This is a string setting 2",
                        Value = "StringValue2",
                        ValidationRegex = @"\d{3}",
                        ValidationExplanation = "Should have 3 digits"
                    },
                    new SettingDefinitionDataContract()
                    {
                        Name = "IntSetting",
                        Description = "This is int setting",
                        Value = 5,
                        ValidationRegex = @"\d{3}",
                        ValidationExplanation = "Should have 3 digits"
                    },
                    new SettingDefinitionDataContract()
                    {
                        Name = "BoolSetting",
                        Description = "This is bool setting",
                        Value = true
                    },
                    new SettingDefinitionDataContract()
                    {
                        Name = "Drop Down Setting",
                        Description = "This is a drop down setting",
                        Value = "Dog",
                        ValidValues = new List<string>()
                        {
                            "Dog",
                            "Cat",
                            "Rabbit"
                        }
                    }
                }
            },
            new SettingsClientDefinitionDataContract()
            {
                Name = "MyService2",
                Settings = new List<SettingDefinitionDataContract>()
                {
                    new SettingDefinitionDataContract()
                    {
                        Name = "StringSetting3",
                        Description = "This is a string setting 3",
                        Value = "StringValue3"
                    },
                    new SettingDefinitionDataContract()
                    {
                        Name = "StringSetting4",
                        Description = "This is a string setting 4",
                        Value = "StringValue4"
                    }
                }
            }
        };
    }

    public async Task DeleteClient(SettingClientConfigurationModel client)
    {
        await _httpService.Delete(GetClientUri(client, String.Empty));
    }

    public async Task SaveClient(SettingClientConfigurationModel client)
    {
        var changedSettings = client.GetChangedSettings().ToList();

        if (changedSettings.Any())
        {
            await _httpService.Put(GetClientUri(client), changedSettings);
        }
    }

    private string GetClientUri(SettingClientConfigurationModel client, string postRoute = "/settings")
    {
        var clientName = HttpUtility.UrlEncode(client.Name);
        var uri = $"/clients/{clientName}{postRoute}";

        if (client.Instance != null)
        {
            uri += $"?instance={HttpUtility.UrlEncode(client.Instance)}";
        }

        return uri;
    }
}