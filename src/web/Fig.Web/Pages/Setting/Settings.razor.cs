using Fig.Web.Events;
using Fig.Web.Facades;
using Fig.Web.Models.Setting;
using Fig.Web.Notifications;
using Fig.Web.Services;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace Fig.Web.Pages.Setting;

public partial class Settings
{
    private string _instanceName = string.Empty;
    private bool _isDeleteInProgress;
    private bool _isSaveAllInProgress;
    private bool _isSaveInProgress;
    private bool _showAdvancedSettings;
    private bool IsSaveDisabled => SelectedSettingClient?.IsValid != true && SelectedSettingClient?.IsDirty != true;
    private bool IsSaveAllDisabled => SettingClients.Any(a => a.IsDirty || a.IsValid) != true;

    private bool IsInstanceDisabled => SelectedSettingClient is not {Instance: null} ||
                                        SelectedSettingClient?.IsGroup == true;

    private bool IsDeleteDisabled => SelectedSettingClient == null || SelectedSettingClient.IsGroup;

    private List<SettingClientConfigurationModel> SettingClients => SettingClientFacade.SettingClients;

    private SettingClientConfigurationModel? SelectedSettingClient
    {
        get => SettingClientFacade.SelectedSettingClient;
        set => SettingClientFacade.SelectedSettingClient = value;
    }

    [Inject]
    private NotificationService NotificationService { get; set; } = null!;

    [Inject]
    private INotificationFactory NotificationFactory { get; set; } = null!;

    [Inject]
    private DialogService DialogService { get; set; } = null!;

    [Inject]
    private ISettingClientFacade SettingClientFacade { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await SettingClientFacade.LoadAllClients();
        foreach (var client in SettingClients)
            client.RegisterEventAction(SettingRequest);
        
        ShowAdvancedChanged(false);
        await base.OnInitializedAsync();
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
            Console.WriteLine($"Show group {settingEventArgs.Name}");
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

    private async Task OnSave()
    {
        try
        {
            _isSaveInProgress = true;
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
        }
    }

    private async Task OnSaveAll()
    {
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

            var instance = SelectedSettingClient.CreateInstance(_instanceName);
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
            return await SettingClientFacade.SaveClient(client);

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
}