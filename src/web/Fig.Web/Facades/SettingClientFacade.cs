using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using Fig.Common.Events;
using Fig.Common.NetStandard.Scripting;
using Fig.Contracts.Diagnostics;
using Fig.Contracts.Health;
using Fig.Contracts.Scheduling;
using Fig.Contracts.SettingClients;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.SettingGroups;
using Fig.Contracts.Settings;
using Fig.Web.Converters;
using Fig.Web.Events;
using Fig.Web.ExtensionMethods;
using Fig.Web.Models.Clients;
using Fig.Web.Models.Setting;
using Fig.Web.Notifications;
using Fig.Web.Services;
using Microsoft.Extensions.Options;
using Radzen;

namespace Fig.Web.Facades;

public class SettingClientFacade : ISettingClientFacade
{
    private readonly IHttpService _httpService;
    private readonly INotificationFactory _notificationFactory;
    private readonly IClientStatusFacade _clientStatusFacade;
    private readonly IEventDistributor _eventDistributor;
    private readonly IApiVersionFacade _apiVersionFacade;
    private readonly ISchedulingFacade _schedulingFacade;
    private readonly NotificationService _notificationService;
    private readonly ISettingHistoryConverter _settingHistoryConverter;
    private readonly ISettingsDefinitionConverter _settingsDefinitionConverter;
    private readonly IScriptRunner _scriptRunner;
    private readonly WebSettings _webSettings;
    private readonly IDisplayScriptStatusService _displayScriptStatusService;
    private bool _isLoadInProgress;
    private bool _forceReload;
    private bool _initializationPending;
    private bool _clientDescriptionsLoaded;
    private Task? _loadClientDescriptionsTask;
    private PendingWebClientLoadTiming? _pendingLoadTiming;
    private CancellationTokenSource? _pendingLoadTimingFlushCts;
    
    public SettingClientFacade(IHttpService httpService,
        ISettingsDefinitionConverter settingsDefinitionConverter,
        ISettingHistoryConverter settingHistoryConverter,
        IScriptRunner scriptRunner,
        IOptions<WebSettings> webSettings,
        NotificationService notificationService,
        INotificationFactory notificationFactory,
        IClientStatusFacade clientStatusFacade,
        IEventDistributor eventDistributor,
        IApiVersionFacade apiVersionFacade,
        ISchedulingFacade schedulingFacade,
        IDisplayScriptStatusService displayScriptStatusService)
    {
        _httpService = httpService;
        _settingsDefinitionConverter = settingsDefinitionConverter;
        _settingHistoryConverter = settingHistoryConverter;
        _scriptRunner = scriptRunner;
        _webSettings = webSettings.Value;
        _notificationService = notificationService;
        _notificationFactory = notificationFactory;
        _clientStatusFacade = clientStatusFacade;
        _eventDistributor = eventDistributor;
        _apiVersionFacade = apiVersionFacade;
        _schedulingFacade = schedulingFacade;
        _displayScriptStatusService = displayScriptStatusService;
        _eventDistributor.Subscribe(EventConstants.LogoutEvent, () =>
        {
            SettingClients.Clear();
            SelectedSettingClient = null;
            _clientDescriptionsLoaded = false;
            _displayScriptStatusService.Reset();
        });
    }
    
    public List<SettingClientConfigurationModel> SettingClients { get; } = new();
    
    public List<ISearchableSetting> SearchableSettings { get; } = new();
    
    public SettingClientConfigurationModel? SelectedSettingClient { get; set; }

    public string? PendingExpandedClientName { get; set; }
    
    public event EventHandler<(string, double)>? OnLoadProgressed;
    
    public event EventHandler? OnDescriptionsLoaded;

    public async Task LoadAllClients(bool initializeScripts = true)
    {
        if (_isLoadInProgress || 
            !_forceReload && !_apiVersionFacade.AreSettingsStale && SettingClients.Count > 0)
            return;

        _isLoadInProgress = true;
        _forceReload = false;

        try
        {
            await LoadAllClientsInternal();
            if (initializeScripts)
                await InitializeAllClientsAsync();
            // InitializeAllClientsAsync schedules the timing flush after script tallies are set.
        }
        finally
        {
            _isLoadInProgress = false;
        }
    }

    public async Task InitializeAllClientsAsync()
    {
        if (!_initializationPending)
            return;

        _initializationPending = false;

        var initializeWatch = Stopwatch.StartNew();
        foreach (var client in SettingClients)
            await client.InitializeAsync();
        var initializeSettingsMs = initializeWatch.ElapsedMilliseconds;

        if (_pendingLoadTiming is null)
            return;

        _pendingLoadTiming.InitializeSettingsMs = initializeSettingsMs;
        _pendingLoadTiming.DisplayScriptsExecuted = _displayScriptStatusService.ExecutedCount;
        _pendingLoadTiming.DisplayScriptsSucceeded = _displayScriptStatusService.SucceededCount;
        _pendingLoadTiming.DisplayScriptsFailed = _displayScriptStatusService.FailedCount;
        _pendingLoadTiming.DisplayScriptsSkipped = _displayScriptStatusService.SkippedCount;
        _pendingLoadTiming.TotalDurationMs =
            (long)(DateTime.UtcNow - _pendingLoadTiming.StartedAtUtc).TotalMilliseconds;

        SchedulePendingLoadTimingFlush();
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

    public void MarkGroupsChanged()
    {
        _forceReload = true;
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
            // If the setting has no named instances, it applies to all run sessions regardless of their instance
            if (settingInstances.Count == 1)
                return true;

            // If the run session has no instance it is using base settings and only matches the base setting client
            if (string.IsNullOrWhiteSpace(runSessionInstance))
                return string.IsNullOrWhiteSpace(clientInstance);

            // Run session with a specific instance matches the setting with the same instance
            if (runSessionInstance == clientInstance)
                return true;

            // Run session has an instance not defined in settings → falls back to base
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
        if (_clientDescriptionsLoaded)
            return;

        if (_loadClientDescriptionsTask is not null)
        {
            await _loadClientDescriptionsTask;
            return;
        }

        _loadClientDescriptionsTask = LoadClientDescriptionsInternal();
        try
        {
            await _loadClientDescriptionsTask;
        }
        finally
        {
            _loadClientDescriptionsTask = null;
        }
    }

    private async Task LoadClientDescriptionsInternal()
    {
        var startedAtUtc = DateTime.UtcNow;
        var stageWatch = Stopwatch.StartNew();
        var descriptions = await LoadDescriptions();
        var descriptionList = descriptions.Clients.ToList();
        var descriptionByName = descriptionList
            .GroupBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Description, StringComparer.OrdinalIgnoreCase);

        foreach (var client in SettingClients)
        {
            if (descriptionByName.TryGetValue(client.Name, out var description))
            {
                client.Description = description;
            }
        }

        _clientDescriptionsLoaded = true;

        var handler = OnDescriptionsLoaded;
        handler?.Invoke(this, EventArgs.Empty);

        var descriptionClientCount = descriptionList.Count;
        var descriptionResponseChars = descriptionList.Sum(c =>
            (c.Name?.Length ?? 0) + (c.Description?.Length ?? 0));
        var durationMs = stageWatch.ElapsedMilliseconds;

        if (_pendingLoadTiming is not null)
        {
            CancelPendingLoadTimingFlush();
            _pendingLoadTiming.Stages.Add(new WebClientLoadTimingStageDataContract(
                WebClientLoadTimingStageNames.LoadClientDescriptions,
                durationMs));
            _pendingLoadTiming.TotalDurationMs += durationMs;
            _pendingLoadTiming.DescriptionClientCount = descriptionClientCount;
            _pendingLoadTiming.DescriptionResponseChars = descriptionResponseChars;
            await ReportPendingLoadTimingAsync();
            return;
        }

        try
        {
            var contract = new WebClientLoadTimingDataContract(
                startedAtUtc,
                durationMs,
                SettingClients.Count(c => !c.IsGroup),
                SettingClients.Sum(c => c.Settings.Count),
                [
                    new WebClientLoadTimingStageDataContract(
                        WebClientLoadTimingStageNames.LoadClientDescriptions,
                        durationMs)
                ],
                descriptionClientCount,
                descriptionResponseChars);
            await _httpService.Post("/diagnostics/web-client-load", contract);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to report web client description load timing: {ex.Message}");
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
        CancelPendingLoadTimingFlush();
        _pendingLoadTiming = null;
        _initializationPending = false;
        _clientDescriptionsLoaded = false;
        _displayScriptStatusService.Reset();

        var startedAtUtc = DateTime.UtcNow;
        var totalWatch = Stopwatch.StartNew();
        var stages = new List<WebClientLoadTimingStageDataContract>();

        var selectedClientName = SelectedSettingClient?.Name;
        SelectedSettingClient = null;
        SettingClients.Clear();

        // Overlap setting-group fetch with clients fetch + convert.
        var settingsTimedTask = LoadSettingsTimed();
        var groupsTimedTask = LoadSettingGroupsTimed();

        var stageWatch = Stopwatch.StartNew();
        var settingsTimed = await settingsTimedTask;
        var settings = settingsTimed.Value ?? new List<SettingsClientDefinitionDataContract>();
        stages.Add(new WebClientLoadTimingStageDataContract(
            WebClientLoadTimingStageNames.HttpFetchClients,
            stageWatch.ElapsedMilliseconds));

        stageWatch.Restart();
        StringExtensionMethods.ResetDescriptionHtmlTiming();
        SettingsDefinitionConverter.ResetModelBuildTiming();
        var clients = await _settingsDefinitionConverter.Convert(settings,
            progress => OnLoadProgressed?.Invoke(this, progress));
        var convertDescriptionHtmlMs = StringExtensionMethods.TakeDescriptionHtmlElapsedMs();
        var convertModelBuildMs = SettingsDefinitionConverter.TakeModelBuildElapsedMs();
        stages.Add(new WebClientLoadTimingStageDataContract(
            WebClientLoadTimingStageNames.ConvertToModels,
            stageWatch.ElapsedMilliseconds));

        stageWatch.Restart();
        var (groupContracts, groupsHttpMs) = await groupsTimedTask;
        stages.Add(new WebClientLoadTimingStageDataContract(
            WebClientLoadTimingStageNames.HttpFetchSettingGroups,
            // Residual wait after clients+convert. True HTTP duration is SettingGroupsHttpMs.
            stageWatch.ElapsedMilliseconds));

        stageWatch.Restart();
        clients.AddRange(BuildGroupsFromContracts(clients, groupContracts));
        stages.Add(new WebClientLoadTimingStageDataContract(
            WebClientLoadTimingStageNames.ConstructGroupModels,
            stageWatch.ElapsedMilliseconds));

        stageWatch.Restart();
        LinkInstanceSettingsToTheirBaseSettings(clients);
        stages.Add(new WebClientLoadTimingStageDataContract(
            WebClientLoadTimingStageNames.LinkInstances,
            stageWatch.ElapsedMilliseconds));

        // Populate the UI list before display scripts so the page can paint first.
        // Scripts run in InitializeAllClientsAsync (immediately or after first paint).
        stageWatch.Restart();
        foreach (var client in clients.OrderBy(client => client.Name))
        {
            SettingClients.Add(client);
        }
        UpdateSelectedSettingClient();
        CheckForDisabledScripts();
        SearchableSettings.Clear();
        SearchableSettings.AddRange(SettingClients.SelectMany(a => a.Settings).OfType<ISearchableSetting>());
        stages.Add(new WebClientLoadTimingStageDataContract(
            WebClientLoadTimingStageNames.InitializeModels,
            stageWatch.ElapsedMilliseconds));

        await _eventDistributor.PublishAsync(EventConstants.SettingsLoaded);

        _pendingLoadTiming = new PendingWebClientLoadTiming(
            startedAtUtc,
            totalWatch.ElapsedMilliseconds,
            SettingClients.Count(c => !c.IsGroup),
            SettingClients.Sum(c => c.Settings.Count),
            stages)
        {
            SettingGroupsHttpMs = groupsHttpMs,
            ConvertDescriptionHtmlMs = convertDescriptionHtmlMs,
            HttpFetchRequestMs = settingsTimed.RequestMs,
            HttpFetchDeserializeMs = settingsTimed.DeserializeMs,
            HttpFetchBodyReadMs = settingsTimed.BodyReadMs,
            HttpFetchParseMs = settingsTimed.ParseMs,
            ConvertModelBuildMs = convertModelBuildMs
        };
        _initializationPending = true;

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
    }

    private static void LinkInstanceSettingsToTheirBaseSettings(List<SettingClientConfigurationModel> clients)
    {
        var baseClientsByName = clients
            .Where(c => string.IsNullOrEmpty(c.Instance) && !c.IsGroup)
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var client in clients.Where(c => !string.IsNullOrEmpty(c.Instance)))
        {
            if (!baseClientsByName.TryGetValue(client.Name, out var baseClient))
                continue;

            baseClient.Instances.Add(client.Instance!);
            var baseSettingsByName = baseClient.Settings
                .ToDictionary(s => s.Name, StringComparer.Ordinal);

            foreach (var setting in client.Settings)
            {
                if (baseSettingsByName.TryGetValue(setting.Name, out var baseSetting))
                {
                    setting.BaseSetting = baseSetting;
                }
            }
        }
    }

    private async Task<List<SettingGroupDataContract>> LoadSettingGroups()
    {
        return await _httpService.Get<List<SettingGroupDataContract>>("settinggroups")
               ?? new List<SettingGroupDataContract>();
    }

    private async Task<(List<SettingGroupDataContract> Groups, long ElapsedMs)> LoadSettingGroupsTimed()
    {
        var stopwatch = Stopwatch.StartNew();
        var groups = await LoadSettingGroups();
        return (groups, stopwatch.ElapsedMilliseconds);
    }

    private List<SettingClientConfigurationModel> BuildGroupsFromContracts(
        List<SettingClientConfigurationModel> clients,
        List<SettingGroupDataContract> groupContracts)
    {
        var result = new List<SettingClientConfigurationModel>();

        // Pre-index clients and their settings for O(1) lookup during group construction.
        var clientIndex = clients
            .Where(c => string.IsNullOrEmpty(c.Instance) && !c.IsGroup)
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var settingIndex = clientIndex.Values
            .ToDictionary(
                c => c.Name,
                c => c.Settings.ToDictionary(s => s.Name, StringComparer.Ordinal),
                StringComparer.OrdinalIgnoreCase);

        foreach (var group in groupContracts)
        {
            var settingGroup = new SettingClientConfigurationModel(
                group.Name,
                CreateGroupDescription(group),
                null,
                false,
                _scriptRunner,
                true,
                _displayScriptStatusService);

            var settings = new List<ISetting>();

            for (var groupSettingIndex = 0; groupSettingIndex < group.GroupedSettings.Count; groupSettingIndex++)
            {
                var gs = group.GroupedSettings[groupSettingIndex];
                var managedSettings = new List<ISetting>();
                ISetting? templateSetting = null;

                foreach (var ss in gs.SourceSettings)
                {
                    if (!settingIndex.TryGetValue(ss.ClientName, out var clientSettings)) continue;

                    if (!clientSettings.TryGetValue(ss.SettingName, out var setting)) continue;

                    templateSetting ??= setting;
                    managedSettings.Add(setting);
                }

                if (templateSetting == null) continue;

                var cloned = templateSetting.Clone(settingGroup, false, templateSetting.IsReadOnly);
                cloned.DisplayOrder = groupSettingIndex;
                cloned.IsCompactView = _webSettings.DefaultDisplayCollapsed;

                if (!string.IsNullOrWhiteSpace(gs.Name) &&
                    !string.Equals(gs.Name, templateSetting.Name, StringComparison.Ordinal))
                {
                    cloned.SetDisplayName(gs.Name);
                }

                if (gs.Description != null)
                {
                    cloned.SetDescription(gs.Description);
                }

                cloned.SetGroupManagedSettings(managedSettings, group.Name);
                settings.Add(cloned);
            }

            settingGroup.Settings = settings;
            result.Add(settingGroup);
        }

        return result;
    }

    private static string CreateGroupDescription(SettingGroupDataContract group)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# Setting Group: {group.Name}");
        builder.AppendLine();
        if (!string.IsNullOrWhiteSpace(group.Description))
        {
            builder.AppendLine(group.Description);
            builder.AppendLine();
        }
        builder.AppendLine($"Group consists of {group.GroupedSettings.Count} setting(s) used by the following clients:");

        var clientNames = group.GroupedSettings
            .SelectMany(gs => gs.SourceSettings)
            .Select(ss => ss.ClientName)
            .Distinct()
            .OrderBy(n => n);

        foreach (var name in clientNames)
        {
            builder.AppendLine($"- {name}");
        }

        return builder.ToString();
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

    private async Task<TimedHttpResult<List<SettingsClientDefinitionDataContract>>> LoadSettingsTimed()
    {
        return await _httpService.GetLargeTimed<List<SettingsClientDefinitionDataContract>>("/clients");
    }

    private async Task<List<SettingsClientDefinitionDataContract>> LoadSettings()
    {
        var timed = await LoadSettingsTimed();
        return timed.Value ?? new List<SettingsClientDefinitionDataContract>();
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

    private void SchedulePendingLoadTimingFlush()
    {
        CancelPendingLoadTimingFlush();
        if (_pendingLoadTiming is null)
            return;

        _pendingLoadTimingFlushCts = new CancellationTokenSource();
        var token = _pendingLoadTimingFlushCts.Token;
        _ = FlushPendingLoadTimingAfterDelayAsync(token);
    }

    private async Task FlushPendingLoadTimingAfterDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Short delay so load completion settles before posting; descriptions are reported separately on demand.
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            await ReportPendingLoadTimingAsync();
        }
        catch (OperationCanceledException)
        {
            // A new load or description report cancelled the idle flush.
        }
    }

    private void CancelPendingLoadTimingFlush()
    {
        if (_pendingLoadTimingFlushCts is null)
            return;

        _pendingLoadTimingFlushCts.Cancel();
        _pendingLoadTimingFlushCts.Dispose();
        _pendingLoadTimingFlushCts = null;
    }

    private async Task ReportPendingLoadTimingAsync()
    {
        var pending = _pendingLoadTiming;
        _pendingLoadTiming = null;
        if (pending is null)
            return;

        try
        {
            var contract = new WebClientLoadTimingDataContract(
                pending.StartedAtUtc,
                pending.TotalDurationMs,
                pending.ClientCount,
                pending.SettingCount,
                pending.Stages,
                pending.DescriptionClientCount,
                pending.DescriptionResponseChars,
                pending.SettingGroupsHttpMs,
                pending.ConvertDescriptionHtmlMs,
                pending.HttpFetchRequestMs,
                pending.HttpFetchDeserializeMs,
                pending.HttpFetchBodyReadMs,
                pending.HttpFetchParseMs,
                pending.ConvertModelBuildMs,
                pending.InitializeSettingsMs,
                pending.DisplayScriptsExecuted,
                pending.DisplayScriptsSucceeded,
                pending.DisplayScriptsFailed,
                pending.DisplayScriptsSkipped);
            await _httpService.Post("/diagnostics/web-client-load", contract);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to report web client load timing: {ex.Message}");
        }
    }

    private sealed class PendingWebClientLoadTiming
    {
        public PendingWebClientLoadTiming(
            DateTime startedAtUtc,
            long totalDurationMs,
            int clientCount,
            int settingCount,
            List<WebClientLoadTimingStageDataContract> stages)
        {
            StartedAtUtc = startedAtUtc;
            TotalDurationMs = totalDurationMs;
            ClientCount = clientCount;
            SettingCount = settingCount;
            Stages = stages;
        }

        public DateTime StartedAtUtc { get; }

        public long TotalDurationMs { get; set; }

        public int ClientCount { get; }

        public int SettingCount { get; }

        public List<WebClientLoadTimingStageDataContract> Stages { get; }

        public int? DescriptionClientCount { get; set; }

        public long? DescriptionResponseChars { get; set; }

        public long? SettingGroupsHttpMs { get; set; }

        public long? ConvertDescriptionHtmlMs { get; set; }

        public long? HttpFetchRequestMs { get; set; }

        public long? HttpFetchDeserializeMs { get; set; }

        public long? HttpFetchBodyReadMs { get; set; }

        public long? HttpFetchParseMs { get; set; }

        public long? ConvertModelBuildMs { get; set; }

        public long? InitializeSettingsMs { get; set; }

        public int? DisplayScriptsExecuted { get; set; }

        public int? DisplayScriptsSucceeded { get; set; }

        public int? DisplayScriptsFailed { get; set; }

        public int? DisplayScriptsSkipped { get; set; }
    }
}
