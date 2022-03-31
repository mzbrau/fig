using System.Web;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Contracts.SettingVerification;
using Fig.Web.Builders;
using Fig.Web.Converters;
using Fig.Web.Models.Setting;
using Fig.Web.Notifications;
using Fig.Web.Services;
using Radzen;

namespace Fig.Web.Facades;

public class SettingClientFacade : ISettingClientFacade
{
    private readonly ISettingGroupBuilder _groupBuilder;
    private readonly IHttpService _httpService;
    private readonly INotificationFactory _notificationFactory;
    private readonly NotificationService _notificationService;
    private readonly ISettingHistoryConverter _settingHistoryConverter;
    private readonly ISettingsDefinitionConverter _settingsDefinitionConverter;
    private readonly ISettingVerificationConverter _settingVerificationConverter;

    public SettingClientFacade(IHttpService httpService,
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
    
    public List<SettingClientConfigurationModel> SettingClients { get; } = new();
    
    public SettingClientConfigurationModel? SelectedSettingClient { get; set; }

    public async Task LoadAllClients()
    {
        SettingClients.Clear();
        var settings = await LoadSettings();
        var clients = _settingsDefinitionConverter.Convert(settings);
        clients.AddRange(_groupBuilder.BuildGroups(clients));
        foreach (var client in clients.OrderBy(client => client.Name))
        {
            SettingClients.Add(client);
        }
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

    private string GetClientUri(SettingClientConfigurationModel client, string postRoute = "/settings")
    {
        var clientName = HttpUtility.UrlEncode(client.Name);
        var uri = $"/clients/{clientName}{postRoute}";

        if (client.Instance != null)
            uri += $"?instance={HttpUtility.UrlEncode(client.Instance)}";

        return uri;
    }
}