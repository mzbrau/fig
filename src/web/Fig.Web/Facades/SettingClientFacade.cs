using Fig.Common.Events;
using Fig.Contracts.SettingClients;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Contracts.SettingVerification;
using Fig.Web.Builders;
using Fig.Web.Converters;
using Fig.Web.Events;
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
    private readonly IClientStatusFacade _clientStatusFacade;
    private readonly IEventDistributor _eventDistributor;
    private readonly NotificationService _notificationService;
    private readonly ISettingHistoryConverter _settingHistoryConverter;
    private readonly ISettingsDefinitionConverter _settingsDefinitionConverter;
    private readonly ISettingVerificationConverter _settingVerificationConverter;
    private readonly List<string> _clientsWithConfigErrors = new();
    
    public SettingClientFacade(IHttpService httpService,
        ISettingsDefinitionConverter settingsDefinitionConverter,
        ISettingHistoryConverter settingHistoryConverter,
        ISettingVerificationConverter settingVerificationConverter,
        ISettingGroupBuilder groupBuilder,
        NotificationService notificationService,
        INotificationFactory notificationFactory,
        IClientStatusFacade clientStatusFacade,
        IEventDistributor eventDistributor)
    {
        _httpService = httpService;
        _settingsDefinitionConverter = settingsDefinitionConverter;
        _settingHistoryConverter = settingHistoryConverter;
        _settingVerificationConverter = settingVerificationConverter;
        _groupBuilder = groupBuilder;
        _notificationService = notificationService;
        _notificationFactory = notificationFactory;
        _clientStatusFacade = clientStatusFacade;
        _eventDistributor = eventDistributor;
        _eventDistributor.Subscribe(EventConstants.LogoutEvent, () =>
        {
            SettingClients.Clear();
            SelectedSettingClient = null;
        });
    }
    
    public List<SettingClientConfigurationModel> SettingClients { get; } = new();
    
    public SettingClientConfigurationModel? SelectedSettingClient { get; set; }
    public event EventHandler<double>? OnLoadProgressed;

    public async Task LoadAllClients()
    {
        var selectedClientName = SelectedSettingClient?.Name;
        SelectedSettingClient = null;
        SettingClients.Clear();
        var settings = await LoadSettings();
        var clients = await _settingsDefinitionConverter.Convert(settings,
        progress => OnLoadProgressed?.Invoke(this, progress));
        clients.AddRange(_groupBuilder.BuildGroups(clients));

        LinkInstanceSettingsToTheirBaseSettings();
        
        clients.ForEach(a => a.Initialize());
        foreach (var client in clients.OrderBy(client => client.Name))
        {
            SettingClients.Add(client);
        }
        UpdateSelectedSettingClient();
        CheckForDisabledScripts();

        void UpdateSelectedSettingClient()
        {
            if (selectedClientName is not null)
                SelectedSettingClient = SettingClients.FirstOrDefault(a => a.Name == selectedClientName);
        }

        void CheckForDisabledScripts()
        {
            if (clients.Any(a => a.HasDisplayScripts))
            {
                if (clients.SelectMany(a => a.Settings).All(a => string.IsNullOrEmpty(a.DisplayScript)))
                {
                    _notificationService.Notify(_notificationFactory.Warning("Display Scripts Disabled",
                        "Some clients had display scripts but they have been disabled. They can be enabled in the fig configuration page."));
                }
            }
        }

        void LinkInstanceSettingsToTheirBaseSettings()
        {
            // Link instance settings to their parent settings
            foreach (var client in clients.Where(c => !string.IsNullOrEmpty(c.Instance)))
            {
                var baseClient = clients.FirstOrDefault(c => 
                    c.Name == client.Name && 
                    string.IsNullOrEmpty(c.Instance));
                if (baseClient == null) 
                    continue;
                
                foreach (var setting in client.Settings)
                {
                    var baseSetting = baseClient.Settings.FirstOrDefault(s => s.Name == setting.Name);
                    if (baseSetting != null)
                    {
                        setting.BaseSetting = baseSetting;
                    }
                }
            }
        }
        
        _eventDistributor.Publish(EventConstants.SettingsLoaded);
    }

    public async Task DeleteClient(SettingClientConfigurationModel client)
    {
        await _httpService.Delete(GetClientUri(client, string.Empty));
        _eventDistributor.Publish(EventConstants.SettingsChanged);
    }

    public async Task<Dictionary<SettingClientConfigurationModel, List<string>>> SaveClient(
        SettingClientConfigurationModel client, string changeMessage)
    {
        var changedSettings = client.GetChangedSettings();

        foreach (var (clientWithChanges, changesForClient) in changedSettings)
            await SaveChangedSettings(clientWithChanges, changesForClient.ToList(), changeMessage);

        _eventDistributor.Publish(EventConstants.SettingsChanged);

        await CheckClientRunSessions();
        
        return changedSettings.ToDictionary(
            a => a.Key,
            b => b.Value.Select(x => x.Name).ToList());
    }

    public async Task<VerificationResultModel> RunVerification(SettingClientConfigurationModel? client, string name)
    {
        if (client is null)
            return new VerificationResultModel(message: "NOT RUN SUCCESSFULLY - Client was null");;
        
        try
        {
            var result =
                await _httpService.Put<VerificationResultDataContract>(GetClientUri(client, $"/verifications/{Uri.EscapeDataString(name)}"),
                    null);
            if (result is not null)
                return _settingVerificationConverter.Convert(result);
        }
        catch (Exception ex)
        {
            _notificationService.Notify(_notificationFactory.Failure("Failed to run verification", ex.Message));
        }

        return new VerificationResultModel(message: "NOT RUN SUCCESSFULLY");
    }

    public async Task<List<SettingHistoryModel>> GetSettingHistory(SettingClientConfigurationModel client, string name)
    {
        try
        {
            var history =
                await _httpService.Get<IEnumerable<SettingValueDataContract>>(GetClientUri(client,
                    $"/settings/{Uri.EscapeDataString(name)}/history"));
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
                    $"/verifications/{Uri.EscapeDataString(name)}/history"));
            return history?.Select(a => _settingVerificationConverter.Convert(a)).ToList() ??
                   new List<VerificationResultModel>();
        }
        catch (Exception ex)
        {
            _notificationService.Notify(_notificationFactory.Failure("Failed to get verification history", ex.Message));
        }

        return new List<VerificationResultModel>();
    }
    
    public async Task CheckClientRunSessions()
    {
        await _clientStatusFacade.Refresh();
        var runSessions = _clientStatusFacade.ClientRunSessions;

        foreach (var client in SettingClients)
        {
            var clientRunSessions = runSessions.Where(a => a.Name == client.Name && a.Instance == client.Instance).ToList();
            client.CurrentRunSessions = clientRunSessions.Count;
            client.HasConfigurationError = clientRunSessions.Any(a => a.HasConfigurationError);
            client.AllRunSessionsRunningLatest = clientRunSessions.All(a => a.RunningLatestSettings);
        }

        var clientsWithErrors = runSessions
            .Where(a => a.HasConfigurationError)
            .Select(a => a.Name)
            .ToList();
        if (clientsWithErrors.Except(_clientsWithConfigErrors).Any())
        {
            ShowConfigErrorNotification(clientsWithErrors);
        }
        
        _clientsWithConfigErrors.Clear();
        _clientsWithConfigErrors.AddRange(clientsWithErrors);
    }

    public async Task<ClientSecretChangeResponseDataContract> ChangeClientSecret(string clientName, string newClientSecret,
        DateTime oldClientSecretExpiry)
    {
        var request =
            new ClientSecretChangeRequestDataContract(newClientSecret, oldClientSecretExpiry.ToUniversalTime());

        var result =
            await _httpService.Put<ClientSecretChangeResponseDataContract>(
                $"/clients/{Uri.EscapeDataString(clientName)}/secret", request);

        if (result is null)
            throw new Exception("Invalid response from API");
        
        return result;
    }

    private void ShowConfigErrorNotification(List<string> clientsWithErrors)
    {
        if (clientsWithErrors.Count == 1)
        {
            _notificationService.Notify(_notificationFactory.Warning("Configuration Error Detected",
                $"Client {clientsWithErrors.Single()} has reported a configuration error."));
        }
        else if (clientsWithErrors.Count > 1)
        {
            _notificationService.Notify(_notificationFactory.Warning("Configuration Errors Detected",
                $"{clientsWithErrors.Count} clients " +
                $"({string.Join(",", clientsWithErrors)}) have reported a configuration error."));
        }
    }

    private async Task SaveChangedSettings(SettingClientConfigurationModel client,
        List<SettingDataContract> changedSettings, string changeMessage)
    {
        if (changedSettings.Any())
        {
            var contract = new SettingValueUpdatesDataContract(changedSettings, changeMessage);
            await _httpService.Put(GetClientUri(client), contract);
        }
    }

    private async Task<List<SettingsClientDefinitionDataContract>> LoadSettings()
    {
        return await _httpService.Get<List<SettingsClientDefinitionDataContract>>("/clients") ??
               new List<SettingsClientDefinitionDataContract>();
    }

    private string GetClientUri(SettingClientConfigurationModel client, string postRoute = "/settings")
    {
        var clientName = Uri.EscapeDataString(client.Name);
        var uri = $"/clients/{clientName}{postRoute}";

        if (client.Instance != null)
            uri += $"?instance={Uri.EscapeDataString(client.Instance)}";

        return uri;
    }
}