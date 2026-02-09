using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Fig.Common.Events;
using Fig.Common.NetStandard.Scripting;
using Fig.Contracts.Health;
using Fig.Contracts.Scheduling;
using Fig.Contracts.SettingClients;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Web.Builders;
using Fig.Web.Converters;
using Fig.Web.Events;
using Fig.Web.ExtensionMethods;
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
    private bool _isLoadInProgress;
    
    public SettingClientFacade(IHttpService httpService,
        ISettingsDefinitionConverter settingsDefinitionConverter,
        ISettingHistoryConverter settingHistoryConverter,
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

    public string? PendingExpandedClientName { get; set; }
    
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
    }

    public void ApplyPendingValueFromCompare(string clientName, string? instance, string settingName, string? rawValue)
    {
        var client = SettingClients
            .FirstOrDefault(c => c.Name == clientName && c.Instance == instance);

        if (client is null)
            throw new InvalidOperationException($"Client '{clientName}' (instance '{instance}') not found.");

        var setting = client.Settings.FirstOrDefault(s => s.Name == settingName);

        if (setting is null)
            throw new InvalidOperationException($"Setting '{settingName}' not found on client '{clientName}'.");

        if (setting is Models.Setting.ConfigurationModels.DataGrid.DataGridSettingConfigurationModel dataGridSetting)
        {
            ApplyDataGridValue(dataGridSetting, rawValue);
            UpdateGroupSettingFromCompare(setting, rawValue, null);
        }
        else
        {
            if (TryConvertRawValue(setting, rawValue, out var convertedValue, out var errorMessage))
            {
                setting.SetValue(convertedValue);
                UpdateGroupSettingFromCompare(setting, null, convertedValue);
            }
            else
            {
#pragma warning disable CS4014
                setting.Parent.SettingEvent(new SettingEventModel(setting.Name, errorMessage ?? "Failed to apply compared value.", SettingEventType.ShowErrorNotification));
#pragma warning restore CS4014
            }
        }
    }

    private static void ApplyDataGridValue(
        Models.Setting.ConfigurationModels.DataGrid.DataGridSettingConfigurationModel dataGridSetting,
        string? rawValue)
    {
        if (string.IsNullOrEmpty(rawValue))
        {
            dataGridSetting.SetValue(new List<Dictionary<string, IDataGridValueModel>>());
            return;
        }

        // The export value for data-grid settings is typically the JSON produced from the
        // original export contract (e.g. RawExportJson), representing a
        // List<Dictionary<string, object?>>. We need to rebuild the value using the setting's
        // definition so each cell gets the correct IDataGridValueModel wrapper.
        //
        // First, we attempt to treat rawValue as JSON and deserialize it into the raw row data.
        // If rawValue is not valid JSON (for example, if it is a human-readable display string
        // instead of the raw export JSON), deserialization will fail and we fall back to
        // clearing the grid so the user can re-import or correct the data through the normal flow.
        try
        {
            var rawRows = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object?>>>(rawValue);
            if (rawRows != null)
            {
                var editableValue = BuildDataGridEditableValue(dataGridSetting, rawRows);
                dataGridSetting.SetValue(editableValue);
                return;
            }
        }
        catch
        {
            // rawValue was not valid JSON — fall through
        }

        // Fallback: set empty and mark dirty so the user knows to review
        dataGridSetting.SetValue(new List<Dictionary<string, IDataGridValueModel>>());
    }

    private void UpdateGroupSettingFromCompare(ISetting setting, string? rawValue, object? convertedValue)
    {
        if (string.IsNullOrWhiteSpace(setting.Group))
            return;

        var groupClient = SettingClients.FirstOrDefault(c => c.IsGroup && c.Name == setting.Group);
        if (groupClient is null)
            return;

        var groupSetting = groupClient.Settings.FirstOrDefault(s => s.Name == setting.Name);
        if (groupSetting is null)
            return;

        if (groupSetting is Models.Setting.ConfigurationModels.DataGrid.DataGridSettingConfigurationModel groupDataGrid)
        {
            if (TryBuildDataGridEditableValue(groupDataGrid, rawValue, out var editableValue))
                groupSetting.SetValueFromManagedSetting(editableValue);
            else
                groupSetting.SetValueFromManagedSetting(new List<Dictionary<string, IDataGridValueModel>>());

            return;
        }

        groupSetting.SetValueFromManagedSetting(convertedValue);
    }

    private static bool TryBuildDataGridEditableValue(
        Models.Setting.ConfigurationModels.DataGrid.DataGridSettingConfigurationModel dataGridSetting,
        string? rawValue,
        out List<Dictionary<string, IDataGridValueModel>> editableValue)
    {
        editableValue = new List<Dictionary<string, IDataGridValueModel>>();

        if (string.IsNullOrEmpty(rawValue))
            return true;

        try
        {
            var rawRows = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object?>>>(rawValue);
            if (rawRows == null)
                return false;

            editableValue = BuildDataGridEditableValue(dataGridSetting, rawRows);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static List<Dictionary<string, IDataGridValueModel>> BuildDataGridEditableValue(
        Models.Setting.ConfigurationModels.DataGrid.DataGridSettingConfigurationModel dataGridSetting,
        List<Dictionary<string, object?>> rawRows)
    {
        var definition = dataGridSetting.DataGridConfiguration as Models.Setting.ConfigurationModels.DataGrid.DataGridConfigurationModel;
        var columns = definition?.Columns ?? new List<IDataGridColumn>();

        var result = new List<Dictionary<string, IDataGridValueModel>>();
        foreach (var row in rawRows)
        {
            var newRow = new Dictionary<string, IDataGridValueModel>();
            foreach (var column in columns)
            {
                row.TryGetValue(column.Name, out var value);
                newRow[column.Name] = column.Type.ConvertToDataGridValueModel(
                    column.IsReadOnly,
                    dataGridSetting,
                    value,
                    column.ValidValues,
                    column.EditorLineCount,
                    column.ValidationRegex,
                    column.ValidationExplanation,
                    column.IsSecret);
            }
            result.Add(newRow);
        }

        return result;
    }

    private static bool TryConvertRawValue(ISetting setting, string? rawValue, out object? convertedValue, out string? errorMessage)
    {
        convertedValue = null;
        errorMessage = null;

        var targetType = GetSettingValueType(setting);
        if (targetType == null)
        {
            errorMessage = "Setting type could not be determined.";
            return false;
        }

        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        var isNullable = Nullable.GetUnderlyingType(targetType) != null || !targetType.IsValueType;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            if (isNullable)
            {
                convertedValue = null;
                return true;
            }

            errorMessage = $"Empty value cannot be converted to {underlyingType.Name}.";
            return false;
        }

        if (underlyingType == typeof(string))
        {
            convertedValue = rawValue;
            return true;
        }

        if (underlyingType.IsEnum)
        {
            if (Enum.TryParse(underlyingType, rawValue, true, out var enumValue))
            {
                convertedValue = enumValue;
                return true;
            }

            errorMessage = $"Value '{rawValue}' is not valid for {underlyingType.Name}.";
            return false;
        }

        var converter = TypeDescriptor.GetConverter(underlyingType);
        if (converter.CanConvertFrom(typeof(string)))
        {
            try
            {
                convertedValue = converter.ConvertFrom(null, CultureInfo.InvariantCulture, rawValue);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to convert value to {underlyingType.Name}. {ex.Message}";
                return false;
            }
        }

        try
        {
            convertedValue = Convert.ChangeType(rawValue, underlyingType, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to convert value to {underlyingType.Name}. {ex.Message}";
            return false;
        }
    }

    private static Type? GetSettingValueType(ISetting setting)
    {
        var propertyInfo = setting.GetType().GetProperty("ValueType", BindingFlags.Instance | BindingFlags.Public);
        return propertyInfo?.GetValue(setting) as Type;
    }

    public async Task LoadAndNotifyAboutScheduledChanges()
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
    
    public async Task CheckClientRunSessions()
    {
        await _clientStatusFacade.Refresh();
        var runSessions = _clientStatusFacade.ClientRunSessions;

        var clientToInstances =
            SettingClients.GroupBy(a => a.Name).ToDictionary(a => a.Key, b => b.Select(i => i.Instance).ToList());
        foreach (var client in SettingClients)
        {
            var settingInstances = clientToInstances[client.Name];
            var clientRunSessions = runSessions.Where(a => a.Name == client.Name && AreSettingsUsedByClient(settingInstances, a.Instance, client.Instance)).ToList();
            client.CurrentRunSessions = clientRunSessions.Count;
            client.CurrentHealth = ConvertHealth(clientRunSessions.Select(a => a.Health).ToList());
            client.AllRunSessionsRunningLatest = clientRunSessions.All(a => a.RunningLatestSettings);
            
            // Map LastRunSessionDisconnected and LastRunSessionMachineName from client status
            var lastSeen = _clientStatusFacade.GetLastSeen(client.Name, client.Instance);
            if (lastSeen != null)
            {
                client.LastRunSessionDisconnected = lastSeen.LastSeen;
                client.LastRunSessionMachineName = lastSeen.LastSeenMachineName;
            }
        }
        
        bool AreSettingsUsedByClient(List<string?> settingInstances, string? runSessionInstance, string? clientInstance)
        {
            // If the setting has no named instances, it applies to all instances of the client
            if (settingInstances.Count == 1 || string.IsNullOrWhiteSpace(runSessionInstance))
                return true;
            
            // If the run session has no instance, it is the base instance and only matches settings with no instance
            if (runSessionInstance == clientInstance)
                return true;

            return string.IsNullOrWhiteSpace(clientInstance) && !settingInstances.Contains(runSessionInstance);
        }
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

    public async Task LoadClientDescriptions()
    {
        var descriptions = await LoadDescriptions();
        foreach (var client in SettingClients)
        {
            var description = descriptions.Clients.FirstOrDefault(a => a.Name == client.Name);
            if (description != null)
            {
                client.Description = description.Description;
            }
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
        return await _httpService.GetLarge<List<SettingsClientDefinitionDataContract>>("/clients") ??
               new List<SettingsClientDefinitionDataContract>();
    }
    
    private async Task<ClientsDescriptionDataContract> LoadDescriptions()
    {
        return await _httpService.GetLarge<ClientsDescriptionDataContract>("/clients/descriptions") ??
               new ClientsDescriptionDataContract([]);
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