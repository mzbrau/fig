using Fig.Common.Events;
using Fig.Contracts.Health;
using Fig.Contracts.Scheduling;
using Fig.Contracts.SettingClients;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Contracts.SettingVerification;
using Fig.Web.Builders;
using Fig.Web.Converters;
using Fig.Web.Events;
using Fig.Web.Models.Clients;
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
    private readonly IApiVersionFacade _apiVersionFacade;
    private readonly ISchedulingFacade _schedulingFacade;
    private readonly NotificationService _notificationService;
    private readonly ISettingHistoryConverter _settingHistoryConverter;
    private readonly ISettingsDefinitionConverter _settingsDefinitionConverter;
    private readonly ISettingVerificationConverter _settingVerificationConverter;
    private bool _isLoadInProgress;
    
    public SettingClientFacade(IHttpService httpService,
        ISettingsDefinitionConverter settingsDefinitionConverter,
        ISettingHistoryConverter settingHistoryConverter,
        ISettingVerificationConverter settingVerificationConverter,
        ISettingGroupBuilder groupBuilder,
        NotificationService notificationService,
        INotificationFactory notificationFactory,
        IClientStatusFacade clientStatusFacade,
        IEventDistributor eventDistributor,
        IApiVersionFacade apiVersionFacade,
        ISchedulingFacade schedulingFacade)
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
        _apiVersionFacade = apiVersionFacade;
        _schedulingFacade = schedulingFacade;
        _eventDistributor.Subscribe(EventConstants.LogoutEvent, () =>
        {
            SettingClients.Clear();
            SelectedSettingClient = null;
        });
    }
    
    public List<SettingClientConfigurationModel> SettingClients { get; } = new();
    
    public List<ISearchableSetting> SearchableSettings { get; } = new();
    
    public SettingClientConfigurationModel? SelectedSettingClient { get; set; }
    
    public event EventHandler<(string, double)>? OnLoadProgressed;

    public async Task LoadAllClients()
    {
        if (_isLoadInProgress || 
            !_apiVersionFacade.AreSettingsStale && SettingClients.Count > 0)
            return;

        _isLoadInProgress = true;

        try
        {
            await LoadAllClientsInternal();
        }
        finally
        {
            _isLoadInProgress = false;
        }

        await Task.Run(async () =>
        {
            await LoadAndNotifyAboutScheduledChanges();
        });
    }

    private async Task LoadAndNotifyAboutScheduledChanges()
    {
        await _schedulingFacade.GetAllDeferredChanges();
        SettingClients.ForEach(a => a.ClearScheduledChanges());

        foreach (var change in _schedulingFacade.DeferredChanges)
        {
            var client = SettingClients.FirstOrDefault(a => a.Name == change.ClientName && a.Instance == change.Instance);
            client?.NotifyAboutScheduledChange(change);
        }
    }

    public async Task DeleteClient(SettingClientConfigurationModel client)
    {
        await _httpService.Delete(GetClientUri(client, string.Empty));
        await _eventDistributor.PublishAsync(EventConstants.SettingsChanged);
    }

    public async Task<Dictionary<SettingClientConfigurationModel, List<string>>> SaveClient(
        SettingClientConfigurationModel client, ChangeDetailsModel changeDetails)
    {
        var changedSettings = client.GetChangedSettings();

        foreach (var (clientWithChanges, changesForClient) in changedSettings)
            await SaveChangedSettings(clientWithChanges, changesForClient.ToList(), changeDetails);

        await _eventDistributor.PublishAsync(EventConstants.SettingsChanged);

        await CheckClientRunSessions();
        await LoadAndNotifyAboutScheduledChanges();
        
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
            client.CurrentHealth = ConvertHealth(clientRunSessions.Select(a => a.Health).ToList());
            client.AllRunSessionsRunningLatest = clientRunSessions.All(a => a.RunningLatestSettings);
        }
    }

    private FigHealthStatus ConvertHealth(List<RunSessionHealthModel> runSessionHealthModels)
    {
        if (runSessionHealthModels.Count == 0)
        {
            return FigHealthStatus.Unknown;
        }
        
        if (runSessionHealthModels.All(a => a.Status == FigHealthStatus.Healthy))
        {
            return FigHealthStatus.Healthy;
        }

        if (runSessionHealthModels.Any(a => a.Status == FigHealthStatus.Unhealthy))
        {
            return FigHealthStatus.Unhealthy;
        }

        if (runSessionHealthModels.Any(a => a.Status == FigHealthStatus.Degraded))
        {
            return FigHealthStatus.Degraded;
        }

        return FigHealthStatus.Unknown;
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

    private async Task LoadAllClientsInternal()
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
        SearchableSettings.AddRange(SettingClients.SelectMany(a => a.Settings).OfType<ISearchableSetting>());
        
        await _eventDistributor.PublishAsync(EventConstants.SettingsLoaded);
        
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

                baseClient.Instances.Add(client.Instance!);
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
    }

    private async Task SaveChangedSettings(SettingClientConfigurationModel client,
        List<SettingDataContract> changedSettings, ChangeDetailsModel changeDetails)
    {
        if (changedSettings.Any())
        {
            var schedule = new ScheduleDataContract(changeDetails.ApplyAtUtc, changeDetails.RevertAtUtc);
            var contract = new SettingValueUpdatesDataContract(changedSettings, changeDetails.Message, schedule);
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