using System.Web;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Contracts.SettingVerification;
using Fig.Web.Builders;
using Fig.Web.Converters;
using Fig.Web.Models;
using Fig.Web.Notifications;
using Radzen;

namespace Fig.Web.Services;

public class SettingsDataService : ISettingsDataService
{
    private readonly ISettingGroupBuilder _groupBuilder;
    private readonly IHttpService _httpService;
    private readonly INotificationFactory _notificationFactory;
    private readonly NotificationService _notificationService;
    private readonly ISettingHistoryConverter _settingHistoryConverter;
    private readonly ISettingsDefinitionConverter _settingsDefinitionConverter;
    private readonly ISettingVerificationConverter _settingVerificationConverter;

    public SettingsDataService(IHttpService httpService,
        ISettingsDefinitionConverter settingsDefinitionConverter,
        ISettingHistoryConverter settingHistoryConverter,
        ISettingVerificationConverter settingVerificationConverter,
        ISettingGroupBuilder groupBuilder,
        NotificationService notificationService,
        INotificationFactory notificationFactory)
    {
        _httpService = httpService;
        _settingsDefinitionConverter = settingsDefinitionConverter;
        _settingHistoryConverter = settingHistoryConverter;
        _settingVerificationConverter = settingVerificationConverter;
        _groupBuilder = groupBuilder;
        _notificationService = notificationService;
        _notificationFactory = notificationFactory;
    }

    public IList<SettingClientConfigurationModel>? SettingsClients { get; private set; }

    public async Task LoadAllClients()
    {
        var settings = await LoadSettings();
        var clients = _settingsDefinitionConverter.Convert(settings);
        clients.AddRange(_groupBuilder.BuildGroups(clients));
        SettingsClients = clients;
    }

    public async Task DeleteClient(SettingClientConfigurationModel client)
    {
        await _httpService.Delete(GetClientUri(client, string.Empty));
    }

    public async Task<Dictionary<SettingClientConfigurationModel, List<string>>> SaveClient(
        SettingClientConfigurationModel client)
    {
        var changedSettings = client.GetChangedSettings();

        foreach (var (clientWithChanges, changesForClient) in changedSettings)
            await SaveChangedSettings(clientWithChanges, changesForClient.ToList());

        return changedSettings.ToDictionary(
            a => a.Key,
            b => b.Value.Select(x => x.Name).ToList());
    }

    public async Task<VerificationResultModel> RunVerification(SettingClientConfigurationModel? client, string name)
    {
        try
        {
            var result =
                await _httpService.Put<VerificationResultDataContract>(GetClientUri(client, $"/verifications/{name}"),
                    null);
            return _settingVerificationConverter.Convert(result);
        }
        catch (Exception ex)
        {
            _notificationService.Notify(_notificationFactory.Failure("Failed to run verification", ex.Message));
        }

        return new VerificationResultModel
        {
            Message = "NOT RUN SUCCESSFULLY"
        };
    }

    public async Task<List<SettingHistoryModel>> GetSettingHistory(SettingClientConfigurationModel client, string name)
    {
        try
        {
            var history =
                await _httpService.Get<IEnumerable<SettingValueDataContract>>(GetClientUri(client,
                    $"/settings/{name}/history"));
            return history?.Select(a => _settingHistoryConverter.Convert(a)).ToList() ??
                   new List<SettingHistoryModel>();
        }
        catch (Exception ex)
        {
            _notificationService.Notify(_notificationFactory.Failure("Failed to get setting history", ex.Message));
        }

        return new List<SettingHistoryModel>();
    }

    public async Task<List<VerificationResultModel>> GetVerificationHistory(SettingClientConfigurationModel client,
        string name)
    {
        try
        {
            var history =
                await _httpService.Get<IEnumerable<VerificationResultDataContract>>(GetClientUri(client,
                    $"/verifications/{name}/history"));
            return history?.Select(a => _settingVerificationConverter.Convert(a)).ToList() ??
                   new List<VerificationResultModel>();
        }
        catch (Exception ex)
        {
            _notificationService.Notify(_notificationFactory.Failure("Failed to get verification history", ex.Message));
        }

        return new List<VerificationResultModel>();
    }

    private async Task SaveChangedSettings(SettingClientConfigurationModel client,
        List<SettingDataContract> changedSettings)
    {
        if (changedSettings.Any())
            await _httpService.Put(GetClientUri(client), changedSettings);
    }

    private async Task<List<SettingsClientDefinitionDataContract>> LoadSettings()
    {
        return await _httpService.Get<List<SettingsClientDefinitionDataContract>>("/clients");
    }

    private List<SettingsClientDefinitionDataContract> GenerateFakeData()
    {
        return new List<SettingsClientDefinitionDataContract>
        {
            new()
            {
                Name = "MyService1",
                Settings = new List<SettingDefinitionDataContract>
                {
                    new()
                    {
                        Name = "StringSetting",
                        Description = "This is a string setting",
                        Value = "StringValue",
                        IsSecret = true
                    },
                    new()
                    {
                        Name = "StringSetting2",
                        Description = "This is a string setting 2",
                        Value = "StringValue2",
                        ValidationRegex = @"\d{3}",
                        ValidationExplanation = "Should have 3 digits"
                    },
                    new()
                    {
                        Name = "IntSetting",
                        Description = "This is int setting",
                        Value = 5,
                        ValidationRegex = @"\d{3}",
                        ValidationExplanation = "Should have 3 digits"
                    },
                    new()
                    {
                        Name = "BoolSetting",
                        Description = "This is bool setting",
                        Value = true
                    },
                    new()
                    {
                        Name = "Drop Down Setting",
                        Description = "This is a drop down setting",
                        Value = "Dog",
                        ValidValues = new List<string>
                        {
                            "Dog",
                            "Cat",
                            "Rabbit"
                        }
                    }
                }
            },
            new()
            {
                Name = "MyService2",
                Settings = new List<SettingDefinitionDataContract>
                {
                    new()
                    {
                        Name = "StringSetting3",
                        Description = "This is a string setting 3",
                        Value = "StringValue3"
                    },
                    new()
                    {
                        Name = "StringSetting4",
                        Description = "This is a string setting 4",
                        Value = "StringValue4"
                    }
                }
            }
        };
    }

    private string GetClientUri(SettingClientConfigurationModel client, string postRoute = "/settings")
    {
        var clientName = HttpUtility.UrlEncode(client.Name);
        var uri = $"/clients/{clientName}{postRoute}";

        if (client.Instance != null)
            uri += $"?instance={HttpUtility.UrlEncode(client.Instance)}";

        return uri;
    }
}