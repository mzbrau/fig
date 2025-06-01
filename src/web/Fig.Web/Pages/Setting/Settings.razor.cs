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
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using Toolbelt.Blazor.HotKeys2;

namespace Fig.Web.Pages.Setting;

public partial class Settings : IAsyncDisposable
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
    private string? _currentFilter;
    private string _settingFilter = string.Empty;
    private bool _showModifiedOnly;
    
    private Fig.Common.Timer.ITimer? _timer;
    private HotKeysContext? _hotKeysContext;
    private IDisposable? _subscription;

    private bool IsReadOnlyUser => AccountService.AuthenticatedUser?.Role == Role.ReadOnly;
    private bool IsSaveDisabled => IsReadOnlyUser || SelectedSettingClient?.IsDirty != true;
    private bool IsClientSelected => SelectedSettingClient == null;
    private bool IsSaveAllDisabled => IsReadOnlyUser || SettingClients.Any(a => a.IsDirty) != true;

    private bool IsInstanceDisabled => IsReadOnlyUser || 
                                       SelectedSettingClient is not {Instance: null} ||
                                       SelectedSettingClient?.IsGroup == true;
    
    private bool IsClientSecretChangeDisabled => IsReadOnlyUser || 
                                                 SelectedSettingClient == null || 
                                                 SelectedSettingClient.IsGroup;

    private bool IsDeleteDisabled => IsReadOnlyUser || 
                                     SelectedSettingClient == null || 
                                     SelectedSettingClient.IsGroup;

    private List<SettingClientConfigurationModel> SettingClients => SettingClientFacade.SettingClients;

    private List<SettingClientConfigurationModel>? FilteredSettingClients { get; set; }

    private SettingClientConfigurationModel? SelectedSettingClient
    {
        get => SettingClientFacade.SelectedSettingClient;
        set
        {
            ClearSettingFilter();
            ClearShowDifferencesFromBase();
            SettingClientFacade.SelectedSettingClient = value;
            if (!string.IsNullOrWhiteSpace(_currentFilter) && SelectedSettingClient is not null)
            {
                _searchedSetting = SelectedSettingClient.GetFilterSettingMatch(_currentFilter);
            }
            else
            {
                _searchedSetting = null;
            }
            FilterSettings();
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
    public IJSRuntime JavascriptRuntime { get; set; } = null!;

    [Inject] 
    public IClientStatusFacade ClientStatusFacade { get; set; } = null!;

    [Inject] 
    public ITimerFactory TimerFactory { get; set; } = null!;

    [Inject]
    public IAccountService AccountService { get; set; } = null!;
    
    [Inject] 
    private IOptions<WebSettings> WebSettings { get; set; } = null!;
    
    [Inject]
    private IEventDistributor EventDistributor { get; set; } = null!;

    [Inject]
    private HotKeys HotKeys { get; set; } = null!;
    
    [Inject] 
    private TooltipService TooltipService { get; set; } = null!;

    private RadzenAutoComplete SearchAutoComplete { get; set; } = null!;
    
    private FigHealthStatus? AggregateHealthStatus
    {
        get
        {
            if (SettingClients.Count == 0)
                return null;
            if (SettingClients.Any(c => c.CurrentHealth == FigHealthStatus.Unhealthy))
                return FigHealthStatus.Unhealthy;
            if (SettingClients.Any(c => c.CurrentHealth == FigHealthStatus.Healthy))
                return FigHealthStatus.Healthy;
            if (SettingClients.Any(c => c.CurrentHealth == FigHealthStatus.Degraded))
                return FigHealthStatus.Degraded;
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _timer?.Stop();
        _timer?.Dispose();
        if (_hotKeysContext != null)
            await _hotKeysContext.DisposeAsync();
        _subscription?.Dispose();
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
        
        foreach (var client in SettingClients)
            client.RegisterEventAction(SettingRequest);
        
        FilteredSettingClients = SettingClients;
        
        _timer = TimerFactory.Create(async () =>
        {
            await SettingClientFacade.CheckClientRunSessions();
            StateHasChanged();
        }, TimeSpan.FromSeconds(15));
        _timer.Start();
        
        await SettingClientFacade.CheckClientRunSessions();
        ShowAdvancedChanged(false);

        EventDistributor.Subscribe(EventConstants.RefreshView, StateHasChanged);
        EventDistributor.Subscribe(EventConstants.Search, ShowSearch);

        SetUpKeyboardShortcuts();
        
        _subscription = _filterTerm
            .Throttle(TimeSpan.FromMilliseconds(600))
            .Subscribe(ts => {
                FilterSettings(ts.Value?.ToString());
                InvokeAsync(StateHasChanged);
            });
        
        _isLoadingSettings = false;
        
        SettingClientFacade.OnLoadProgressed -= HandleLoadProgressed;
        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
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

        if (settingEventArgs.EventType == SettingEventType.RunVerification)
        {
            if (!IsSaveDisabled)
                NotificationService.Notify(NotificationFactory.Info("Save Client",
                    "Verifications are only performed on saved values."));
            
            if (settingEventArgs.Client != null)
                return await SettingClientFacade.RunVerification(settingEventArgs.Client, settingEventArgs.Name);

            return Task.CompletedTask;
        }

        if (settingEventArgs.EventType == SettingEventType.VerificationHistoryRequested)
        {
            if (settingEventArgs.Client != null)
                return await SettingClientFacade.GetVerificationHistory(settingEventArgs.Client,
                    settingEventArgs.Name);

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
        else
        {
            await InvokeAsync(StateHasChanged);
        }

        return Task.CompletedTask;
    }

    private void ShowAdvancedChanged(bool showAdvanced)
    {
        SettingClients.ForEach(a => a.ShowAdvancedChanged(showAdvanced));
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
    }

    private async Task OnFilter(LoadDataArgs args)
    {
        if (!string.IsNullOrEmpty(args.Filter))
        {
            var filter = args.Filter.ToLowerInvariant();
            FilteredSettingClients = SettingClients.Where(a => a.Name.ToLowerInvariant().Contains(filter)).ToList();
        }
        else
        {
            FilteredSettingClients = SettingClients;
        }

        _currentFilter = args.Filter;

        await InvokeAsync(StateHasChanged);
    }

    private async ValueTask OnSave()
    {
        var changeDetails = new ChangeDetailsModel();
        var pendingChanges = SelectedSettingClient?.GetChangedSettings().ToChangeModelList(ClientStatusFacade.ClientRunSessions);
        if (pendingChanges is not null && await AskUserForChangeMessage(pendingChanges, changeDetails) != true)
            return;
            
        _isSaveInProgress = true;
        
        try
        {
            var changes = await SaveClient(SelectedSettingClient, changeDetails);

            if (changeDetails.ApplyAtUtc is not null)
            {
                foreach (var setting in changes.Keys.SelectMany(a => a.Settings))
                    setting.UndoChanges();
            }
            else
            {
                foreach (var change in changes)
                    change.Key.MarkAsSaved(change.Value);
            }

            if (SelectedSettingClient?.IsGroup == true)
                SelectedSettingClient?.MarkAsSaved(SelectedSettingClient.Settings.Select(a => a.Name).ToList());

            ShowNotification(NotificationFactory.Success("Save",
                $"Successfully saved {changes.Values.Select(a => a.Count).Sum()} setting(s)."));
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

    private async Task OnChangeSecret()
    {
        if (SelectedSettingClient != null)
        {
            await PerformSecretChange(SelectedSettingClient.Name);
        }
    }

    private async Task OnDelete()
    {
        if (SelectedSettingClient != null)
        {
            if (SelectedSettingClient.Instances.Any())
            {
                ShowNotification(NotificationFactory.Failure("Delete Denied",
                    "Cannot delete a client with instances. Delete the instances first."));
                return;
            }
            
            var instancePart = $" (Instance: {SelectedSettingClient.Instance})";
            var confirmationName =
                $"{SelectedSettingClient.Name}{(SelectedSettingClient.Instance != null ? instancePart : string.Empty)}";
            if (!await GetDeleteConfirmation(confirmationName))
                return;

            try
            {
                var clientName = SelectedSettingClient.Name;
                var clientInstance = SelectedSettingClient.Instance;

                _isDeleteInProgress = true;
                await SettingClientFacade.DeleteClient(SelectedSettingClient);
                SettingClients.Remove(SelectedSettingClient);
                if (!string.IsNullOrEmpty(SelectedSettingClient.Instance))
                {
                    var client = SettingClients.FirstOrDefault(a => a.Instances.Contains(SelectedSettingClient.Instance));
                    client?.Instances.Remove(SelectedSettingClient.Instance);
                }
                SelectedSettingClient = null;
                RefreshGroups();
                
                var instanceNotification = clientInstance != null ? $" (instance '{clientInstance}')" : string.Empty;
                ShowNotification(NotificationFactory.Success("Delete",
                    $"Client '{clientName}'{instanceNotification} deleted successfully."));
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
    }

    private void ClearSettingFilter()
    {
        SelectedSettingClient?.FilterSettings(string.Empty);
    }
    
    private void ClearShowDifferencesFromBase()
    {
        ShowDifferentOnlyChanged(false);
    }
    
    private async Task ShowSearch()
    {
        await ShowSearchDialog();
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
            SelectedSettingClient = setting.Parent;
            await InvokeAsync(async () =>
            {
                setting.Expand();
                if (setting.Advanced)
                {
                    _showAdvanced = true;
                    ShowAdvancedChanged(_showAdvanced);
                    await Task.Delay(100); // Little extra wait for it to appear
                }
                await Task.Delay(50); // Wait for UI to update
                await ScrollToElementId(setting.ScrollId);
                await JavascriptRuntime.InvokeVoidAsync("highlightSetting", setting.ScrollId);
            });
            
            DialogService.Close();
        }
    }
}