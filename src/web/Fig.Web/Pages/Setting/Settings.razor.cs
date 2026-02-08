using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fig.Common.Events;
using Fig.Common.Timer;
using Fig.Contracts.Authentication;
using Fig.Contracts.Health;
using Fig.Web.Events;
using Fig.Web.ExtensionMethods;
using Fig.Web.Facades;
using Fig.Web.Models.Setting;
using Fig.Web.Notifications;
using Fig.Web.Services;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using Toolbelt.Blazor.HotKeys2;

namespace Fig.Web.Pages.Setting;

public partial class Settings : ComponentBase, IAsyncDisposable
{
    private readonly Subject<ChangeEventArgs> _filterTerm = new();
    private string _instanceName = string.Empty;
    private bool _isDeleteInProgress;
    private bool _isSaveAllInProgress;
    private bool _isSaveInProgress;
    private bool _isLoadingSettings;
    private double _loadProgress;
    private string _loadingMessage = string.Empty;
    private string? _searchedSetting;
    private bool _showAdvanced;
    private string _settingFilter = string.Empty;
    private bool _showModifiedOnly;
    private CategoryFilterModel? _selectedCategory;
    private List<CategoryFilterModel> _availableCategories = new();
    
    // Floating toolbar state
    private ElementReference _toolbarRef;
    private bool _isToolbarFloating;
    
    // Collapsible instances state
    private readonly HashSet<string> _expandedClientNames = new();
    private string? _listFilterText;
    
    // Resize splitter state
    private ElementReference _resizeHandleRef;
    private double _leftPanelWidth = 25.0; // Default 25% width
    private bool _isResizing = false;
    private double _startX = 0;
    private double _startWidth = 0;
    
    // Filter textbox reference for tooltip
    private RadzenTextBox _filterTextBox = null!;
    
    // Multi-select state
    private readonly HashSet<SettingClientConfigurationModel> _selectedClients = new();
    private readonly HashSet<SettingClientConfigurationModel> _hoveredItems = new();
    private bool _isShiftPressed;

    private double _toolbarOffsetTop;
    private IJSObjectReference? _scrollModule;
    private DotNetObjectReference<Settings>? _dotNetObjectReference;
    
    private Fig.Common.Timer.ITimer? _timer;
    private HotKeysContext? _hotKeysContext;
    private IDisposable? _subscription;
    private IJSObjectReference? _doubleShiftCleanup;
    private DateTime _lastSearchDialogOpen = DateTime.MinValue;
    
    // Store references to event callbacks for proper unsubscription
    private Action? _refreshViewCallback;
    private Func<Task>? _searchCallback;
    
    // Double-shift detection timeout (in milliseconds)
    private const int DoubleShiftTimeoutMs = 500;

    private bool IsReadOnlyUser => AccountService.AuthenticatedUser?.Role == Role.ReadOnly;
    private bool IsSaveDisabled => IsReadOnlyUser || (HasMultipleClientsSelected ? !GetSelectedClients().Any(c => c.IsDirty) : SelectedSettingClient?.IsDirty != true);
    private bool IsNoClientSelected => SelectedSettingClient == null;
    private bool IsSaveAllDisabled => IsReadOnlyUser || SettingClients.Any(a => a.IsDirty) != true;

    private bool IsInstanceDisabled => IsReadOnlyUser || 
                                       HasMultipleClientsSelected ||
                                       SelectedSettingClient is not {Instance: null} ||
                                       SelectedSettingClient?.IsGroup == true;
    
    private bool IsClientSecretChangeDisabled => IsReadOnlyUser || 
                                                 HasMultipleClientsSelected ||
                                                 SelectedSettingClient == null || 
                                                 SelectedSettingClient.IsGroup;

    private bool IsDeleteDisabled => IsReadOnlyUser || !AreDeleteTargetsAvailable();
    
    private bool HasMultipleClientsSelected => _selectedClients.Count > 1;
    
    private bool IsToolbarFilterDisabled => HasMultipleClientsSelected;
    
    private bool IsToolbarAdvancedDisabled => HasMultipleClientsSelected;
    
    private bool IsToolbarExpandCollapseDisabled => IsNoClientSelected || HasMultipleClientsSelected;
    
    private bool IsToolbarDescriptionDisabled => IsNoClientSelected || HasMultipleClientsSelected;
    
    private bool IsTimelineDisabled => IsNoClientSelected || HasMultipleClientsSelected;

    private List<SettingClientConfigurationModel> SettingClients => SettingClientFacade.SettingClients;

    private List<SettingClientConfigurationModel>? FilteredSettingClients { get; set; }

    private SettingClientConfigurationModel? SelectedSettingClient
    {
        get => SettingClientFacade.SelectedSettingClient;
        set
        {
            ClearSettingFilter();
            ClearShowDifferencesFromBase();
            ClearCategoryFilter();
            
            // Disable animations for all settings before switching clients
            if (value != null)
            {
                foreach (var setting in value.Settings)
                {
                    setting.ShouldAnimateVisibilityChanges = false;
                }
            }
            
            SettingClientFacade.SelectedSettingClient = value;
            
            // Re-enable animations after a short delay to allow the switch to complete
            _ = Task.Run(async () =>
            {
                await Task.Delay(50); // Short delay to ensure visibility updates complete
                if (value != null)
                {
                    foreach (var setting in value.Settings)
                    {
                        setting.ShouldAnimateVisibilityChanges = true;
                    }
                }
            });
            
            // Ensure instances are visible when selected by expanding their base
            EnsureParentExpanded(value);
            if (!string.IsNullOrWhiteSpace(_listFilterText) && SelectedSettingClient is not null)
            {
                _searchedSetting = SelectedSettingClient.GetFilterSettingMatch(_listFilterText);
            }
            else
            {
                _searchedSetting = null;
            }
            
            // Populate available categories for the selected client
            PopulateAvailableCategories();
            
            FilterSettings();
        }
    }
    
    // Computed list for the left panel with instances collapsed by default
    private List<SettingClientConfigurationModel> VisibleSettingClients
    {
        get
        {
            var results = new List<SettingClientConfigurationModel>();
            if (SettingClients == null || SettingClients.Count == 0)
                return results;

            // Pre-group instances by base client name for O(n) access
            var instanceLookup = SettingClients
                .Where(c => c is { IsGroup: false, Instance: not null })
                .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(c => c.Instance, StringComparer.OrdinalIgnoreCase).ToList(),
                    StringComparer.OrdinalIgnoreCase);

            // We preserve existing order while grouping instances under their base
            var seenBases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in SettingClients)
            {
                // Skip instances here; they'll be added under their base
                if (item is { Instance: not null, IsGroup: false })
                    continue;

                // Add groups as-is
                if (item.IsGroup)
                {
                    if (IsNameMatch(item))
                        results.Add(item);
                    continue;
                }

                // Base client (Instance == null)
                if (!seenBases.Add(item.Name))
                    continue;

                // Determine if this base or any of its instances match the filter
                var hasMatchingInstance = false;
                instanceLookup.TryGetValue(item.Name, out var instances);
                if (!string.IsNullOrWhiteSpace(_listFilterText) && instances != null)
                {
                    hasMatchingInstance = instances.Any(IsNameMatch);
                }

                // Add base if it matches or any instance matches, or no filter
                if (IsNameMatch(item) || hasMatchingInstance || string.IsNullOrWhiteSpace(_listFilterText))
                {
                    results.Add(item);
                }

                // Decide if instances should be shown: expanded state OR filter reveals matching instances
                var showInstances = IsClientExpanded(item.Name) || (!string.IsNullOrWhiteSpace(_listFilterText) && hasMatchingInstance);
                if (showInstances && instances != null)
                {
                    foreach (var inst in instances)
                    {
                        if (string.IsNullOrWhiteSpace(_listFilterText) || IsNameMatch(inst))
                            results.Add(inst);
                    }
                }
            }

            return results;
        }
    }

    [Inject]
    private NotificationService NotificationService { get; set; } = null!;

    [Inject]
    private INotificationFactory NotificationFactory { get; set; } = null!;

    [Inject]
    private DialogService DialogService { get; set; } = null!;

    [Inject]
    private ISettingClientFacade SettingClientFacade { get; set; } = null!;

    [Inject] 
    private IJSRuntime JavascriptRuntime { get; set; } = null!;

    [Inject] 
    private IClientStatusFacade ClientStatusFacade { get; set; } = null!;

    [Inject] 
    private ITimerFactory TimerFactory { get; set; } = null!;

    [Inject]
    private IAccountService AccountService { get; set; } = null!;
    
    [Inject] 
    private IOptions<WebSettings> WebSettings { get; set; } = null!;
    
    [Inject]
    private IEventDistributor EventDistributor { get; set; } = null!;

    [Inject]
    private HotKeys HotKeys { get; set; } = null!;
    
    [Inject]
    private TooltipService TooltipService { get; set; } = null!;
    
    [Inject]
    private IConfigurationFacade ConfigurationFacade { get; set; } = null!;

    private RadzenAutoComplete SearchAutoComplete { get; set; } = null!;    
    
    private FigHealthStatus? AggregateHealthStatus
    {
        get
        {
            if (SettingClients.Count == 0)
                return null;
            if (SettingClients.Any(c => c.CurrentHealth == FigHealthStatus.Unhealthy))
                return FigHealthStatus.Unhealthy;
            if (SettingClients.Any(c => c.CurrentHealth == FigHealthStatus.Degraded))
                return FigHealthStatus.Degraded;
            if (SettingClients.Any(c => c.CurrentHealth == FigHealthStatus.Healthy))
                return FigHealthStatus.Healthy;
            return null;
        }
    }
    
    [JSInvokable]
    public async Task OnResizeMove(double clientX)
    {
        if (!_isResizing)
            return;
            
        const int minWidthPercent = 15;
        const int maxWidthPercent = 50;
        const double changeThresholdPercent = 0.1;
        
        // Calculate the container width (viewport width)
        var containerWidth = await JavascriptRuntime.InvokeAsync<double>("getViewportWidth");
        
        // Calculate the delta as percentage
        var deltaX = clientX - _startX;
        var deltaPercent = (deltaX / containerWidth) * 100;
        
        // Calculate new width with constraints (min 15%, max 50%)
        var newWidth = Math.Max(minWidthPercent, Math.Min(maxWidthPercent, _startWidth + deltaPercent));
        
        if (Math.Abs(newWidth - _leftPanelWidth) > changeThresholdPercent) // Only update if changed significantly
        {
            _leftPanelWidth = newWidth;
            await InvokeAsync(StateHasChanged);
        }
    }
    
    [JSInvokable]
    public void OnResizeEnd()
    {
        _isResizing = false;
    }

    public async ValueTask DisposeAsync()
    {
        _timer?.Stop();
        _timer?.Dispose();
        if (_hotKeysContext != null)
            await _hotKeysContext.DisposeAsync();
        _subscription?.Dispose();
        
        // Unsubscribe from event distributor events
        if (_refreshViewCallback != null)
        {
            EventDistributor.Unsubscribe(EventConstants.RefreshView, _refreshViewCallback);
        }
        if (_searchCallback != null)
        {
            EventDistributor.Unsubscribe(EventConstants.Search, _searchCallback);
        }
        
        // Clean up JavaScript shift key tracking
        try
        {
            await JavascriptRuntime.InvokeVoidAsync("cleanupShiftKeyTracking");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to cleanup shift key tracking: {ex.Message}");
        }
        
        // Clean up double-shift detection
        if (_doubleShiftCleanup != null)
        {
            try
            {
                await JavascriptRuntime.InvokeVoidAsync("cleanupSettingsDoubleShiftDetection", _doubleShiftCleanup);
                await _doubleShiftCleanup.DisposeAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to cleanup double shift detection: {ex.Message}");
            }
        }
        
        // Clean up scroll handler
        if (_scrollModule != null)
        {
            try
            {
                await _scrollModule.InvokeVoidAsync("cleanup");
                await _scrollModule.DisposeAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to cleanup scroll module: {ex.Message}");
            }
        }
        
        // Dispose the DotNetObjectReference
        _dotNetObjectReference?.Dispose();
    }
    
    [JSInvokable]
    public async Task OnDoubleShiftDetected()
    {
        if (ShouldDebounceSearchDialog())
            return;
            
        await ShowSearchDialog();
    }
    
    [JSInvokable]
    public void OnShiftKeyChanged(bool isPressed)
    {
        _isShiftPressed = isPressed;
    }
    
    protected override async Task OnInitializedAsync()
    {
        _loadingMessage = "Getting data from the server...";
        SettingClientFacade.OnLoadProgressed += HandleLoadProgressed;
        _isLoadingSettings = true;
        _loadProgress = 0;
        if (SettingClients.All(a => !a.IsDirty))
        {
            await SettingClientFacade.LoadAllClients();
        }

        // Set FilteredSettingClients immediately to prevent showing wrong empty state
        FilteredSettingClients = SettingClients;

        EnsureParentExpanded(SelectedSettingClient);
        ApplyPendingParentExpansion();
        
        _isLoadingSettings = false;

        SettingClientFacade.OnLoadProgressed -= HandleLoadProgressed;

        StateHasChanged();

        await SettingClientFacade.LoadClientDescriptions();

        await SettingClientFacade.LoadAndNotifyAboutScheduledChanges();

        await Task.Delay(500);
        
        foreach (var client in SettingClients)
            client.RegisterEventAction(SettingRequest);
        
        _timer = TimerFactory.Create(async () =>
        {
            await Task.Delay(5000);
            await SettingClientFacade.CheckClientRunSessions();
            StateHasChanged();
        }, TimeSpan.FromSeconds(15));
        _timer.Start();
        
        await SettingClientFacade.CheckClientRunSessions();
        ShowAdvancedChanged(false);

        await Task.Delay(500);

        _refreshViewCallback = StateHasChanged;
        _searchCallback = ShowSearch;
        EventDistributor.Subscribe(EventConstants.RefreshView, _refreshViewCallback);
        EventDistributor.Subscribe(EventConstants.Search, _searchCallback);

        SetUpKeyboardShortcuts();
        
        _subscription = _filterTerm
            .Throttle(TimeSpan.FromMilliseconds(600))
            .Subscribe(ts => {
                FilterSettings(ts.Value?.ToString());
                InvokeAsync(StateHasChanged);
            });
        
        
        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await InitializeScrollHandler();
            await InitializeResizeHandler();
        }
        
        if (!string.IsNullOrWhiteSpace(_searchedSetting))
        {
            await ScrollToElementId(_searchedSetting);
            _searchedSetting = null;
        }
    }

    private void SetUpKeyboardShortcuts()
    {
        _hotKeysContext = this.HotKeys.CreateContext();
        _hotKeysContext.Add(ModCode.Alt, Code.S, (Func<ValueTask>)(async () => await OnSave()));
        _hotKeysContext.Add(ModCode.Alt, Code.A, (Func<ValueTask>)(async () => await OnSaveAll()));
        _hotKeysContext.Add(ModCode.Alt, Code.I, (Func<ValueTask>)(async () => await OnAddInstance()));
        _hotKeysContext.Add(ModCode.Alt, Code.D, (Func<ValueTask>)(async () => await ShowDescription(SelectedSettingClient?.Name, SelectedSettingClient?.Description)));
        _hotKeysContext.Add(ModCode.Alt, Code.E, () => SelectedSettingClient?.ExpandAll());
        _hotKeysContext.Add(ModCode.Alt, Code.C, () => SelectedSettingClient?.CollapseAll());
        _hotKeysContext.Add(ModCode.Alt, Code.F, ShowSearchDialog);
        
        // Set up double-shift detection
        SetUpDoubleShiftDetection();
    }
    
    private void SetUpDoubleShiftDetection()
    {
        // Set up JavaScript-based double-shift detection and shift key tracking
        _ = Task.Run(async () =>
        {
            await Task.Delay(100); // Small delay to ensure DOM is ready
            
            // Create a single DotNetObjectReference for reuse
            _dotNetObjectReference = DotNetObjectReference.Create(this);
            
            _doubleShiftCleanup = await JavascriptRuntime.InvokeAsync<IJSObjectReference>("setupSettingsDoubleShiftDetection", 
                _dotNetObjectReference, DoubleShiftTimeoutMs);
            
            // Initialize shift key tracking for performance optimization
            await JavascriptRuntime.InvokeVoidAsync("initializeShiftKeyTracking", _dotNetObjectReference);
        });
    }

    private void HandleLoadProgressed(object? sender, (string message, double percent) progress)
    {
        _loadProgress = Math.Round(progress.percent);
        _loadingMessage = $"Loading {progress.message}...";
        StateHasChanged();
    }

    private async Task<object> SettingRequest(SettingEventModel settingEventArgs)
    {
        if (settingEventArgs.EventType == SettingEventType.SettingHistoryRequested)
        {
            if (settingEventArgs.Client != null)
                return await SettingClientFacade.GetSettingHistory(settingEventArgs.Client, settingEventArgs.Name);

            return Task.CompletedTask;
        }

        if (settingEventArgs.EventType == SettingEventType.ShowErrorNotification)
        {
            ShowNotification(NotificationFactory.Failure(settingEventArgs.Name, settingEventArgs.Message));
            return Task.CompletedTask;
        }

        if (settingEventArgs.EventType == SettingEventType.SelectSetting)
        {
            ShowGroup(settingEventArgs.Name);
        }
        else if (settingEventArgs.EventType == SettingEventType.DependencyVisibilityChanged)
        {
            // Force immediate UI refresh for dependency visibility changes
            await InvokeAsync(StateHasChanged);
        }
        else
        {
            await InvokeAsync(StateHasChanged);
        }

        return Task.CompletedTask;
    }

    private void ShowAdvancedChanged(bool showAdvanced)
    {
        SettingClients.ForEach(a => a.ShowAdvancedChanged(showAdvanced));
        
        // Publish event to notify divider visibility manager
        EventDistributor.Publish(EventConstants.SettingsAdvancedVisibilityChanged);
    }

    private void ShowDifferentOnlyChanged(bool showModifiedOnly)
    {
        _showModifiedOnly = showModifiedOnly;
        if (SelectedSettingClient != null)
        {
            foreach (var setting in SelectedSettingClient.Settings)
            {
                setting.FilterByBaseValueMatch(_showModifiedOnly);
            }
        }
        
        // Publish event to notify divider visibility manager
        EventDistributor.Publish(EventConstants.SettingsBaseValueFilterChanged);
    }

    private async Task OnFilter(LoadDataArgs args)
    {
        _listFilterText = args.Filter;

        await InvokeAsync(StateHasChanged);
    }

    private async ValueTask OnSave()
    {
        var changeDetails = new ChangeDetailsModel();
        List<ChangeModel> pendingChanges;
        
        if (HasMultipleClientsSelected)
        {
            // Get changes from all selected clients
            pendingChanges = new List<ChangeModel>();
            foreach (var client in _selectedClients.Where(c => c.IsDirty))
            {
                pendingChanges.AddRange(client.GetChangedSettings().ToChangeModelList(ClientStatusFacade.ClientRunSessions));
            }
        }
        else
        {
            // Single client save logic (existing)
            pendingChanges = SelectedSettingClient?.GetChangedSettings().ToChangeModelList(ClientStatusFacade.ClientRunSessions) ?? new List<ChangeModel>();
        }
        
        if (pendingChanges.Count == 0 || await AskUserForChangeMessage(pendingChanges, changeDetails) != true)
            return;
            
        _isSaveInProgress = true;
        
        try
        {
            var allChanges = new Dictionary<SettingClientConfigurationModel, List<string>>();
            
            if (HasMultipleClientsSelected)
            {
                // Save all selected clients with changes
                foreach (var client in _selectedClients.Where(c => c.IsDirty))
                {
                    var changes = await SaveClient(client, changeDetails);
                    foreach (var change in changes)
                    {
                        allChanges[change.Key] = change.Value;
                    }
                }
            }
            else
            {
                // Save single selected client
                var changes = await SaveClient(SelectedSettingClient, changeDetails);
                foreach (var change in changes)
                {
                    allChanges[change.Key] = change.Value;
                }
            }

            if (changeDetails.ApplyAtUtc is not null)
            {
                foreach (var setting in allChanges.Keys.SelectMany(a => a.Settings))
                    setting.UndoChanges();
            }
            else
            {
                foreach (var change in allChanges)
                    change.Key.MarkAsSaved(change.Value);
            }

            if (HasMultipleClientsSelected)
            {
                // Mark groups as saved if they're selected
                foreach (var client in _selectedClients.Where(c => c.IsGroup))
                {
                    client.MarkAsSaved(client.Settings.Select(a => a.Name).ToList());
                }
            }
            else if (SelectedSettingClient?.IsGroup == true)
            {
                SelectedSettingClient?.MarkAsSaved(SelectedSettingClient.Settings.Select(a => a.Name).ToList());
            }

            var totalSettings = allChanges.Values.Select(a => a.Count).Sum();
            var clientCount = HasMultipleClientsSelected ? _selectedClients.Count(c => c.IsDirty) : 1;
            
            ShowNotification(NotificationFactory.Success("Save",
                $"Successfully saved {totalSettings} setting(s) from {clientCount} client(s)."));
        }
        catch (Exception ex)
        {
            ShowNotification(NotificationFactory.Failure("Save", $"Save Failed: {ex.Message}"));
            Console.WriteLine(ex);
        }
        finally
        {
            _isSaveInProgress = false;
        }
    }

    private async ValueTask OnSaveAll()
    {
        var changeDetails = new ChangeDetailsModel();
        var pendingChanges = new List<ChangeModel>();
        foreach (var client in SettingClients)
            pendingChanges.AddRange(client.GetChangedSettings().ToChangeModelList(ClientStatusFacade.ClientRunSessions));
        
        if (await AskUserForChangeMessage(pendingChanges, changeDetails) != true)
            return;
        
        _isSaveAllInProgress = true;

        try
        {
            var successes = new List<int>();
            var failures = new List<string>();
            foreach (var client in SettingClients.Where(a => !a.IsGroup))
                try
                {
                    foreach (var clientGroup in await SaveClient(client, changeDetails))
                    {
                        successes.Add(clientGroup.Value.Count);
                        clientGroup.Key.MarkAsSaved(clientGroup.Value);
                    }
                }
                catch (Exception ex)
                {
                    failures.Add(ex.Message);
                }

            RefreshGroups();

            if (failures.Any())
                ShowNotification(NotificationFactory.Failure("Save All",
                    $"Failed to save {failures.Count} clients. {successes.Sum()} settings saved."));
            else if (successes.Any(a => a > 0))
                ShowNotification(NotificationFactory.Success("Save All",
                    $"Successfully saved {successes.Sum()} setting(s) from {successes.Count(a => a > 0)} client(s)."));
        }
        finally
        {
            _isSaveAllInProgress = false;
        }
    }

    private async ValueTask OnAddInstance()
    {
        if (SelectedSettingClient != null)
        {
            if (!await GetInstanceName(SelectedSettingClient.Name))
            {
                _instanceName = string.Empty;
                return;
            }

            var instance = await SelectedSettingClient.CreateInstance(_instanceName);
            instance.RegisterEventAction(SettingRequest);
            var existingIndex = SettingClients.IndexOf(SelectedSettingClient);
            SettingClients.Insert(existingIndex + 1, instance);
            ShowNotification(NotificationFactory.Success("Instance",
                $"New instance for client '{SelectedSettingClient.Name}' created."));
            _instanceName = string.Empty;
            SelectedSettingClient = instance;
        }

        await InvokeAsync(StateHasChanged);
    }

    private List<string> GetAvailableRunningInstances(string clientName)
    {
        // Get all running instances for this client
        var runningInstances = ClientStatusFacade.ClientRunSessions
            .Where(session => session.Name == clientName && !string.IsNullOrEmpty(session.Instance))
            .Select(session => session.Instance!)
            .Distinct()
            .ToList();

        // Get existing instances for this client from the settings
        var existingInstances = SettingClients
            .Where(client => client.Name == clientName && !string.IsNullOrEmpty(client.Instance))
            .Select(client => client.Instance!)
            .Distinct()
            .ToList();

        // Return instances that are running but don't already exist in settings
        return runningInstances
            .Where(instance => !existingInstances.Contains(instance))
            .OrderBy(instance => instance)
            .ToList();
    }

    private async Task OnChangeSecret()
    {
        if (SelectedSettingClient != null)
        {
            await PerformSecretChange(SelectedSettingClient.Name);
        }
    }

    private async Task OnDelete()
    {
        List<SettingClientConfigurationModel> clientsToDelete;
        string confirmationMessage;
        
        if (HasMultipleClientsSelected)
        {
            // Filter out group entries from selected clients
            clientsToDelete = _selectedClients.Where(c => !c.IsGroup).ToList();
            
            // Check if no clients remain after filtering out groups
            if (!clientsToDelete.Any())
            {
                ShowNotification(NotificationFactory.Failure("Delete Denied", 
                    "No clients to delete — selected items are groups only"));
                return;
            }
            
            // Check if any selected clients have instances
            var clientsWithInstances = clientsToDelete.Where(c => c.Instances.Any()).ToList();
            if (clientsWithInstances.Any())
            {
                var clientNames = string.Join(", ", clientsWithInstances.Select(c => c.Name));
                ShowNotification(NotificationFactory.Failure("Delete Denied",
                    $"Cannot delete clients with instances: {clientNames}. Delete the instances first."));
                return;
            }
            
            confirmationMessage = $"Are you sure you want to delete {clientsToDelete.Count} selected clients?";
        }
        else if (SelectedSettingClient != null)
        {
            if (SelectedSettingClient.Instances.Any())
            {
                ShowNotification(NotificationFactory.Failure("Delete Denied",
                    "Cannot delete a client with instances. Delete the instances first."));
                return;
            }
            
            clientsToDelete = new List<SettingClientConfigurationModel> { SelectedSettingClient };
            var instancePart = $" (Instance: {SelectedSettingClient.Instance})";
            confirmationMessage = $"Are you sure you want to delete {SelectedSettingClient.Name}{(SelectedSettingClient.Instance != null ? instancePart : string.Empty)}?";
        }
        else
        {
            return;
        }

        if (!await GetDeleteConfirmation(confirmationMessage))
            return;

        try
        {
            _isDeleteInProgress = true;
            var deletedClientNames = new List<string>();
            
            foreach (var client in clientsToDelete)
            {
                var clientName = client.Name;
                var clientInstance = client.Instance;

                await SettingClientFacade.DeleteClient(client);
                SettingClients.Remove(client);
                
                if (!string.IsNullOrEmpty(client.Instance))
                {
                    var baseClient = SettingClients.FirstOrDefault(a => a.Name == client.Name && a.Instance == null && a.Instances.Contains(client.Instance));
                    baseClient?.Instances.Remove(client.Instance);
                }
                
                // Remove from selected clients if it's there
                _selectedClients.Remove(client);
                
                var instanceNotification = clientInstance != null ? $" (instance '{clientInstance}')" : string.Empty;
                deletedClientNames.Add($"'{clientName}'{instanceNotification}");
            }
            
            // Clear selection state
            if (HasMultipleClientsSelected)
            {
                _selectedClients.Clear();
            }
            else
            {
                SelectedSettingClient = null;
            }
            
            RefreshGroups();
            
            var message = clientsToDelete.Count == 1 
                ? $"Client {deletedClientNames.First()} deleted successfully."
                : $"Successfully deleted {clientsToDelete.Count} clients: {string.Join(", ", deletedClientNames)}.";
                
            ShowNotification(NotificationFactory.Success("Delete", message));
        }
        catch (Exception ex)
        {
            ShowNotification(NotificationFactory.Failure("Delete", $"Delete Failed: {ex.Message}"));
            Console.WriteLine(ex);
        }
        finally
        {
            _isDeleteInProgress = false;
        }
    }

    private async Task<Dictionary<SettingClientConfigurationModel, List<string>>> SaveClient(
        SettingClientConfigurationModel? client, ChangeDetailsModel changeDetails)
    {
        if (client != null)
            return await SettingClientFacade.SaveClient(client, changeDetails);

        return new Dictionary<SettingClientConfigurationModel, List<string>>();
    }

    private void ShowNotification(NotificationMessage message)
    {
        NotificationService.Notify(message);
    }

    private void ShowGroup(string groupName)
    {
        var group = SettingClients.FirstOrDefault(a => a.Name == groupName);
        if (group != null)
        {
            SelectedSettingClient = group;
            InvokeAsync(StateHasChanged);
        }
    }

    private void EnsureParentExpanded(SettingClientConfigurationModel? client)
    {
        if (client?.Instance != null)
            _expandedClientNames.Add(client.Name);
    }

    private void ApplyPendingParentExpansion()
    {
        var pending = SettingClientFacade.PendingExpandedClientName;
        if (!string.IsNullOrWhiteSpace(pending))
        {
            _expandedClientNames.Add(pending);
            SettingClientFacade.PendingExpandedClientName = null;
        }
    }
    
    private void RefreshGroups()
    {
        foreach (var settingGroup in SettingClients.Where(a => a.IsGroup).ToList())
        {
            settingGroup.Refresh();
            if (settingGroup.Settings.Count == 0)
                SettingClients.Remove(settingGroup);
        }
    }

    private async Task ScrollToElementId(string elementId)
    {
        await JavascriptRuntime.InvokeVoidAsync("scrollIntoView", elementId);
    }
    
    private void FilterSettings(string? filter = null)
    {
        if (filter is not null)
            _settingFilter = filter;

        SelectedSettingClient?.FilterSettings(_settingFilter);
        SelectedSettingClient?.FilterSettingsByCategory(_selectedCategory?.Name);
        
        // Publish event to notify divider visibility manager
        EventDistributor.Publish(EventConstants.SettingsFilterChanged);
    }

    private void ClearSettingFilter()
    {
        _settingFilter = string.Empty;
        FilterSettings();
    }
    
    private void ClearCategoryFilter()
    {
        _selectedCategory = null;
    }
    
    private void OnCategoryFilterChanged()
    {
        FilterSettings();
    }
    
    private void PopulateAvailableCategories()
    {
        _availableCategories = SelectedSettingClient?.GetUniqueCategories() ?? new List<CategoryFilterModel>();
    }
    
    private int GetVisibleSettingsCount()
    {
        if (SelectedSettingClient == null)
            return 0;
            
        return SelectedSettingClient.Settings.Count(s => !s.Hidden);
    }
    
    private int GetTotalSettingsCount()
    {
        if (SelectedSettingClient == null)
            return 0;
            
        return SelectedSettingClient.Settings.Count;
    }
    
    private void ClearShowDifferencesFromBase()
    {
        ShowDifferentOnlyChanged(false);
    }
    
    private async Task ShowSearch()
    {
        if (ShouldDebounceSearchDialog())
            return;
            
        await ShowSearchDialog();
    }
    
    private bool ShouldDebounceSearchDialog()
    {
        // Debounce to prevent multiple rapid calls
        var now = DateTime.Now;
        if ((now - _lastSearchDialogOpen).TotalMilliseconds < 500)
            return true;
            
        _lastSearchDialogOpen = now;
        return false;
    }
    
    private async Task ShowTimelineDialog()
    {
        if (SelectedSettingClient == null)
            return;

        await ConfigurationFacade.LoadConfiguration();
        
        await DialogService.OpenAsync<SettingTimelineDialog>($"{SelectedSettingClient.Name} Timeline",
            new Dictionary<string, object>()
            {
                { "ClientName", SelectedSettingClient.Name },
                { "Instance", SelectedSettingClient.Instance ?? string.Empty },
                { "TimelineDurationDays", ConfigurationFacade.ConfigurationModel.TimelineDurationDays }
            },
            new DialogOptions()
            {
                Width = "90%",
                Height = "90%",
                Resizable = true,
                Draggable = true,
                CloseDialogOnOverlayClick = false,
                CloseDialogOnEsc = true
            });
    }

    private void OnLoadData(LoadDataArgs args)
    {
        var filter = args.Filter.ToLowerInvariant();
        if (filter.Length < 2)
            return;
        
        // Only support one of each type of search token apart from general
        string? clientToken = null,
            settingToken = null,
            descriptionToken = null,
            instanceToken = null,
            valueToken = null;
        List<string> generalTokens = new();
        
        var tokens = filter.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        foreach (var token in tokens)
        {
            if ((token.StartsWith("client:") || token.StartsWith("c:")) && clientToken == null)
                clientToken = token[(token.IndexOf(':') + 1)..];
            else if ((token.StartsWith("setting:") || token.StartsWith("s:")) && settingToken == null)
                settingToken = token[(token.IndexOf(':') + 1)..];
            else if ((token.StartsWith("description:") || token.StartsWith("d:")) && descriptionToken == null)
                descriptionToken = token[(token.IndexOf(':') + 1)..];
            else if ((token.StartsWith("instance:") || token.StartsWith("i:")) && instanceToken == null)
                instanceToken = token[(token.IndexOf(':') + 1)..];
            else if ((token.StartsWith("value:") || token.StartsWith("v:")) && valueToken == null)
                valueToken = token[(token.IndexOf(':') + 1)..];
            else
                generalTokens.Add(token);
        }

#pragma warning disable BL0005
        SearchAutoComplete.Data = SettingClientFacade.SearchableSettings.Where(setting =>
#pragma warning restore BL0005
            setting.IsSearchMatch(clientToken,
                settingToken,
                descriptionToken,
                instanceToken,
                valueToken,
                generalTokens));
    }

    private async Task OnSelectedSearchItemChanged(object arg)
    {
        if (arg is ISearchableSetting setting)
        {
            // Close the dialog immediately
            DialogService.Close();
            
            SelectedSettingClient = setting.Parent;
            EnsureParentExpanded(setting.Parent);
            setting.Expand();
            if (setting.Advanced)
            {
                _showAdvanced = true;
                ShowAdvancedChanged(_showAdvanced);
            }
            
            // Use a slight delay then scroll to the element
            await Task.Delay(150);
            await ScrollToElementId(setting.ScrollId);
            await JavascriptRuntime.InvokeVoidAsync("highlightSetting", setting.ScrollId);
        }
    }
    
    #region Floating Toolbar Methods
    
    private async Task InitializeScrollHandler()
    {
        try
        {
            _scrollModule = await JavascriptRuntime.InvokeAsync<IJSObjectReference>("import", "./js/floating-toolbar.js");
            
            // Ensure we have a DotNetObjectReference created (fallback if SetUpDoubleShiftDetection wasn't called)
            _dotNetObjectReference ??= DotNetObjectReference.Create(this);
            
            await _scrollModule.InvokeVoidAsync("initialize", _toolbarRef, _dotNetObjectReference);
            
            // Small delay to ensure DOM is fully rendered
            await Task.Delay(50);
            
            // Get initial toolbar position
            _toolbarOffsetTop = await _scrollModule.InvokeAsync<double>("getElementTop", _toolbarRef);
        }
        catch (Exception ex)
        {
            // Log error but don't break the page functionality
            Console.WriteLine($"Failed to initialize floating toolbar: {ex.Message}");
        }
    }
    
    [JSInvokable]
    public async Task OnScroll(double scrollY)
    {
        // Add a small buffer (10px) to make the transition feel more natural
        var shouldFloat = scrollY > (_toolbarOffsetTop - 10);
        
        if (shouldFloat != _isToolbarFloating)
        {
            _isToolbarFloating = shouldFloat;
            await InvokeAsync(StateHasChanged);
        }
    }
    
    private string GetToolbarCssClass()
    {
        return $"toolbar-container {(_isToolbarFloating ? "toolbar-floating" : "toolbar-fixed")}";
    }
    
    private string GetToolbarStyle()
    {
        if (_isToolbarFloating)
        {
            return "position: fixed; top: 0; left: 0; right: 0; z-index: 1000;";
        }
        return "position: relative;";
    }
    
    #endregion

    private async Task InitializeResizeHandler()
    {
        try
        {
            // Load the resize handler JavaScript module
            var resizeModule = await JavascriptRuntime.InvokeAsync<IJSObjectReference>("import", "./js/resize-handler.js");
            
            // Ensure we have a DotNetObjectReference
            _dotNetObjectReference ??= DotNetObjectReference.Create(this);
            
            await resizeModule.InvokeVoidAsync("initialize", _dotNetObjectReference);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize resize handler: {ex.Message}");
        }
    }
    
    private async Task OnResizeStart(MouseEventArgs e)
    {
        _isResizing = true;
        _startX = e.ClientX;
        _startWidth = _leftPanelWidth;
        
        // Add global mouse listeners via JavaScript
        await JavascriptRuntime.InvokeVoidAsync("startResize", _dotNetObjectReference);
    }

    private bool IsCollapsedState(List<ISetting> settings)
    {
        // Check if most settings are in compact view - if so, dividers should be collapsed too
        var compactCount = settings.Count(s => s.IsCompactView);
        var expandedCount = settings.Count(s => !s.IsCompactView);
        
        // If more than 60% are in compact view, consider the overall state as collapsed
        return compactCount > expandedCount * 1.5;
    }
    
    private List<ISetting> GetSettingsAfterHeading(List<ISetting> allSettings, int headingIndex)
    {
        var referencedSettings = new List<ISetting>();
        
        // Find settings after this heading up to the next heading (or end of list)
        for (int i = headingIndex; i < allSettings.Count; i++)
        {
            var setting = allSettings[i];
            
            // Stop if we encounter another heading
            if (i != headingIndex && setting.Heading != null)
            {
                break;
            }
            
            referencedSettings.Add(setting);
        }
        
        return referencedSettings;
    }
    
    private bool IsClientExpanded(string clientName)
    {
        return _expandedClientNames.Contains(clientName);
    }

    private async Task ToggleClientExpand(string clientName)
    {
        if (!_expandedClientNames.Add(clientName))
            _expandedClientNames.Remove(clientName);

        await InvokeAsync(StateHasChanged);
    }

    private int GetInstanceCount(SettingClientConfigurationModel baseClient)
    {
        if (baseClient == null)
            return 0;

        return GetInstancesOf(baseClient.Name).Count;
    }

    private bool IsNameMatch(SettingClientConfigurationModel model)
    {
        if (string.IsNullOrWhiteSpace(_listFilterText))
            return true;
        
        var filter = _listFilterText.Trim();
        if ((model.DisplayName ?? model.Name).Contains(filter, StringComparison.OrdinalIgnoreCase))
            return true;

        // Also match on instance name so filtering by instance reveals it under the base
        if (!string.IsNullOrWhiteSpace(model.Instance) &&
            model.Instance.Contains(filter, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private string GetItemIndent(SettingClientConfigurationModel model)
    {
        // Slight indent for instances to show hierarchy
        return model.Instance != null ? "margin-left: 16px;" : string.Empty;
    }
    
    private string GetRunSessionsBadgeText(SettingClientConfigurationModel model)
    {
        // Only relevant for base clients
        if (model.Instance != null || model.IsGroup)
            return model.CurrentRunSessions.ToString();
        
        // Find instances of this client
        var instances = GetInstancesOf(model.Name);
        if (instances.Count == 0)
            return model.CurrentRunSessions.ToString();

        // If any instances have running sessions, append '+' to indicate more running beyond base
        var instanceRunSessions = instances.Sum(i => i.CurrentRunSessions);
        if (instanceRunSessions > 0)
            return $"{model.CurrentRunSessions}+";

        // No running instance sessions, show base count only
        return model.CurrentRunSessions.ToString();
    }

    private string GetRunSessionsTooltipText(SettingClientConfigurationModel model)
    {
        // For instances/groups, keep the original semantics
        if (model.IsGroup || model.Instance != null)
        {
            if (model.CurrentRunSessions == 0)
            {
                // Show last run session info if available
                if (model.LastRunSessionDisconnected.HasValue)
                {
                    return GetTimeAgoMessage(model);
                }
                return "No currently running clients";
            }
            return model.AllRunSessionsRunningLatest
                ? $"{model.CurrentRunSessions} currently running client(s), all running latest settings"
                : $"{model.CurrentRunSessions} currently running client(s), some with stale settings";
        }

        // Base client: include instance information if present
        var instances = GetInstancesOf(model.Name);
        var instanceRunSessions = instances.Sum(i => i.CurrentRunSessions);

        if (model.CurrentRunSessions == 0)
        {
            if (instanceRunSessions > 0)
            {
                var message = $"No currently running clients for base settings. {instanceRunSessions} running with different instance settings.";
                // Show last run session info for base client if available
                if (model.LastRunSessionDisconnected.HasValue)
                {
                    return GetTimeAgoMessage(model, message);
                }
                return message;
            }
            // No running sessions at all
            if (model.LastRunSessionDisconnected.HasValue)
            {
                return GetTimeAgoMessage(model);
            }
            return "No currently running clients";
        }

        var basePart = model.AllRunSessionsRunningLatest
            ? $"{model.CurrentRunSessions} currently running client(s), all running latest settings for base settings"
            : $"{model.CurrentRunSessions} currently running client(s), some with stale settings for base settings";

        if (instanceRunSessions > 0)
            return basePart + $". {instanceRunSessions} running with different instance settings.";
        return basePart;
    }

    private string GetTimeAgoMessage(SettingClientConfigurationModel model, string? customPrefix = null)
    {
        var prefix = string.IsNullOrWhiteSpace(customPrefix) ? "No currently running clients." : customPrefix;
        if (!model.LastRunSessionDisconnected.HasValue)
            return prefix;
        
        var timeAgo = (DateTime.UtcNow - model.LastRunSessionDisconnected.Value).Humanize();
        var machineInfo = string.IsNullOrWhiteSpace(model.LastRunSessionMachineName) 
            ? "" 
            : $" on {model.LastRunSessionMachineName}";
        
        return $"{prefix} Last seen {timeAgo} ago{machineInfo}";
    }
    
    private List<SettingClientConfigurationModel> GetInstancesOf(string clientName)
    {
        return SettingClients
            .Where(c => c is { IsGroup: false, Instance: not null } && c.Name == clientName)
            .OrderBy(c => c.Instance, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
    
    private int GetTotalClientsCount()
    {
        if (SettingClients == null || SettingClients.Count == 0)
            return 0;
            
        // Count unique base clients + instances + groups (not including the collapsed instances)
        var uniqueClients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var instanceCount = 0;
        var groupCount = 0;
        
        foreach (var client in SettingClients)
        {
            if (client.IsGroup)
            {
                groupCount++;
            }
            else if (client.Instance != null)
            {
                instanceCount++;
            }
            else
            {
                uniqueClients.Add(client.Name);
            }
        }
        
        return uniqueClients.Count + instanceCount + groupCount;
    }
    
    private IEnumerable<SettingClientConfigurationModel> GetSelectedClients()
    {
        return _selectedClients.AsEnumerable();
    }
    
    private bool AreDeleteTargetsAvailable()
    {
        if (HasMultipleClientsSelected)
        {
            return GetSelectedClients().Any(c => !c.IsGroup);
        }
        
        return SelectedSettingClient != null && !SelectedSettingClient.IsGroup;
    }
    
    private void HandleCheckboxChange(SettingClientConfigurationModel client, bool isSelected)
    {
        if (_isShiftPressed && isSelected)
        {
            // Select all visible clients when shift+clicking
            SelectAllVisibleClients();
        }
        else
        {
            // Regular toggle behavior
            ToggleClientSelection(client, isSelected);
        }
    }
    
    private void SelectAllVisibleClients()
    {
        // Clear current selection first
        _selectedClients.Clear();
        
        // Add all visible clients (excluding groups) to selection
        foreach (var client in VisibleSettingClients.Where(c => !c.IsGroup))
        {
            _selectedClients.Add(client);
        }
        
        // Clear single selection if multiple clients are selected
        if (HasMultipleClientsSelected)
        {
            SelectedSettingClient = null;
        }
        
        StateHasChanged();
    }
    
    private void ToggleClientSelection(SettingClientConfigurationModel client, bool isSelected)
    {
        if (isSelected)
        {
            _selectedClients.Add(client);
        }
        else
        {
            _selectedClients.Remove(client);
        }
        
        // Clear single selection if multiple clients are selected
        if (HasMultipleClientsSelected)
        {
            SelectedSettingClient = null;
        }
        
        StateHasChanged();
    }
    
    private bool IsClientInSelection(SettingClientConfigurationModel client)
    {
        return _selectedClients.Contains(client);
    }
    
    private void SetClientHovered(SettingClientConfigurationModel client, bool isHovered)
    {
        if (isHovered)
        {
            _hoveredItems.Add(client);
        }
        else
        {
            _hoveredItems.Remove(client);
        }
        StateHasChanged();
    }
    
    private bool IsClientHovered(SettingClientConfigurationModel client)
    {
        return _hoveredItems.Contains(client);
    }
    
    private bool ShouldShowCheckbox(SettingClientConfigurationModel client)
    {
        return IsClientInSelection(client) || IsClientHovered(client);
    }
    
    private int GetSelectedClientsWithChangesCount()
    {
        return _selectedClients.Count(c => c.IsDirty);
    }
    
    private string GetSaveButtonText()
    {
        if (HasMultipleClientsSelected)
        {
            var count = GetSelectedClientsWithChangesCount();
            return count > 0 ? $"Save ({count})" : "Save";
        }
        
        // Show count for single client selection
        var dirtyCount = SelectedSettingClient?.DirtySettingCount ?? 0;
        return dirtyCount > 0 ? $"Save ({dirtyCount})" : "Save";
    }
    
    private string GetSaveAllButtonText()
    {
        var totalCount = SettingClients.Sum(c => c.DirtySettingCount);
        return totalCount > 0 ? $"Save All ({totalCount})" : "Save All";
    }
    
    private bool GetCheckboxValue(SettingClientConfigurationModel client)
    {
        return IsClientInSelection(client);
    }
    
    private string GetClientRowClasses(SettingClientConfigurationModel client)
    {
        var classes = new List<string>();
        
        if (IsClientInSelection(client))
            classes.Add("selected");
            
        if (IsClientHovered(client))
            classes.Add("hovered");
            
        return string.Join(" ", classes);
    }
    
    private string GetCheckboxContainerClasses(SettingClientConfigurationModel client)
    {
        var classes = new List<string> { ShouldShowCheckbox(client) ? "visible" : "hidden" };

        if (IsClientInSelection(client))
            classes.Add("selected");
            
        return string.Join(" ", classes);
    }
    
    private string GetClientContentClasses(SettingClientConfigurationModel client)
    {
        var classes = new List<string>();
        
        if (IsClientInSelection(client))
            classes.Add("selected");
        else if (IsClientHovered(client))
            classes.Add("hovered");
        else
            classes.Add("normal");
            
        return string.Join(" ", classes);
    }
}