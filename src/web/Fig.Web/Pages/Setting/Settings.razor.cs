using Fig.Common.Timer;
using Fig.Contracts.Authentication;
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

namespace Fig.Web.Pages.Setting;

public partial class Settings : IDisposable
{
    private string _instanceName = string.Empty;
    private string _changeMessage = string.Empty;
    
    private bool _isDeleteInProgress;
    private bool _isSaveAllInProgress;
    private bool _isSaveInProgress;
    private string? _searchedSetting;
    private string? _currentFilter;
    private string _settingFilter = string.Empty;
    private ITimer? _timer;

    private bool IsReadOnlyUser => AccountService.AuthenticatedUser?.Role == Role.ReadOnly;
    private bool IsSaveDisabled => IsReadOnlyUser || (SelectedSettingClient?.IsValid != true && SelectedSettingClient?.IsDirty != true);
    private bool IsClientSelected => SelectedSettingClient == null;
    private bool IsSaveAllDisabled => IsReadOnlyUser || SettingClients.Any(a => a.IsDirty || a.IsValid) != true;

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
    
    [Inject] private IOptions<WebSettings> WebSettings { get; set; } = null!;

    public void Dispose()
    {
        _timer?.Stop();
        _timer?.Dispose();
    }
    
    protected override async Task OnInitializedAsync()
    {
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

    private async void OnFilter(LoadDataArgs args)
    {
        if (!string.IsNullOrEmpty(args.Filter))
        {
            FilteredSettingClients = SettingClients.Where(a => a.IsFilterMatch(args.Filter)).ToList();
        }
        else
        {
            FilteredSettingClients = SettingClients;
        }

        _currentFilter = args.Filter;

        await InvokeAsync(StateHasChanged);
    }

    private async Task OnSave()
    {
        var pendingChanges = SelectedSettingClient?.GetChangedSettings().ToChangeModelList(ClientStatusFacade.ClientRunSessions);
        if (pendingChanges is not null && !await AskUserForChangeMessage(pendingChanges))
            return;
            
        _isSaveInProgress = true;
        
        try
        {
            var changes = await SaveClient(SelectedSettingClient);
            foreach (var change in changes)
                change.Key.MarkAsSaved(change.Value);

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
            _changeMessage = string.Empty;
        }
    }

    private async Task OnSaveAll()
    {
        var pendingChanges = new List<ChangeModel>();
        foreach (var client in SettingClients)
            pendingChanges.AddRange(client.GetChangedSettings().ToChangeModelList(ClientStatusFacade.ClientRunSessions));
        
        if (!await AskUserForChangeMessage(pendingChanges))
            return;
        
        _isSaveAllInProgress = true;

        try
        {
            var successes = new List<int>();
            var failures = new List<string>();
            foreach (var client in SettingClients.Where(a => !a.IsGroup))
                try
                {
                    foreach (var clientGroup in await SaveClient(client))
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
            _changeMessage = string.Empty;
        }
    }

    private async Task OnAddInstance()
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
                SelectedSettingClient.MarkAsDeleted();
                SettingClients.Remove(SelectedSettingClient);
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
        SettingClientConfigurationModel? client)
    {
        if (client != null)
            return await SettingClientFacade.SaveClient(client, _changeMessage);

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
}