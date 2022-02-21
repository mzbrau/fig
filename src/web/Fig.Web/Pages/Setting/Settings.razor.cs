using Fig.Web.Events;
using Fig.Web.Models;
using Fig.Web.Notifications;
using Fig.Web.Services;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace Fig.Web.Pages.Setting;

public partial class Settings
{
    private string _instanceName;
    private bool _isDeleteInProgress;
    private bool _isSaveAllInProgress;
    private bool _isSaveInProgress;
    private bool _showAdvancedSettings;
    private bool _isSaveDisabled => _selectedSettingClient?.IsValid != true && _selectedSettingClient?.IsDirty != true;
    private bool _isSaveAllDisabled => _settingClients?.Any(a => a.IsDirty || a.IsValid) != true;

    private bool _isInstanceDisabled => _selectedSettingClient is not {Instance: null} ||
                                        _selectedSettingClient?.IsGroup == true;

    private bool _isDeleteDisabled => _selectedSettingClient == null || _selectedSettingClient.IsGroup;

    private List<SettingClientConfigurationModel> _settingClients { get; set; } = new();
    private SettingClientConfigurationModel? _selectedSettingClient { get; set; }

    [Inject]
    private ISettingsDataService? _settingsDataService { get; set; }

    [Inject]
    private NotificationService _notificationService { get; set; }

    [Inject]
    private INotificationFactory _notificationFactory { get; set; }

    [Inject]
    private DialogService _dialogService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await _settingsDataService!.LoadAllClients();
        foreach (var client in _settingsDataService.SettingsClients.OrderBy(client => client.Name))
        {
            client.RegisterEventAction(SettingRequest);
            _settingClients.Add(client);
        }

        await ShowAdvancedChanged(false);

        Console.WriteLine($"loaded {_settingClients?.Count} services");
        await base.OnInitializedAsync();
    }

    private async Task<object> SettingRequest(SettingEventModel settingEventArgs)
    {
        if (settingEventArgs.EventType == SettingEventType.SettingHistoryRequested)
        {
            if (_settingsDataService != null && settingEventArgs.Client != null)
                return await _settingsDataService.GetSettingHistory(settingEventArgs.Client, settingEventArgs.Name);

            return Task.CompletedTask;
        }

        if (settingEventArgs.EventType == SettingEventType.RunVerification)
        {
            if (_settingsDataService != null && settingEventArgs.Client != null)
                return await _settingsDataService.RunVerification(settingEventArgs.Client, settingEventArgs.Name);

            return Task.CompletedTask;
        }

        if (settingEventArgs.EventType == SettingEventType.VerificationHistoryRequested)
        {
            if (_settingsDataService != null && settingEventArgs.Client != null)
                return await _settingsDataService.GetVerificationHistory(settingEventArgs.Client,
                    settingEventArgs.Name);

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

    private async Task ShowAdvancedChanged(bool showAdvanced)
    {
        _settingClients.ForEach(a => a.ShowAdvancedChanged(showAdvanced));
    }

    private async Task OnSave()
    {
        try
        {
            _isSaveInProgress = true;
            var changes = await SaveClient(_selectedSettingClient);
            foreach (var change in changes)
                change.Key.MarkAsSaved(change.Value);

            if (_selectedSettingClient?.IsGroup == true)
                _selectedSettingClient?.MarkAsSaved(_selectedSettingClient.Settings.Select(a => a.Name).ToList());

            ShowNotification(_notificationFactory.Success("Save",
                $"Successfully saved {changes.Values.Select(a => a.Count).Sum()} setting(s)."));
        }
        catch (Exception ex)
        {
            ShowNotification(_notificationFactory.Failure("Save", $"Save Failed: {ex.Message}"));
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
            foreach (var client in _settingClients.Where(a => !a.IsGroup))
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
                ShowNotification(_notificationFactory.Failure("Save All",
                    $"Failed to save {failures.Count} clients. {successes.Sum()} settings saved."));
            else if (successes.Any(a => a > 0))
                ShowNotification(_notificationFactory.Success("Save All",
                    $"Successfully saved {successes.Sum()} setting(s) from {successes.Count(a => a > 0)} client(s)."));
        }
        finally
        {
            _isSaveAllInProgress = false;
        }
    }

    private async Task OnAddInstance()
    {
        if (_selectedSettingClient != null)
        {
            if (!await GetInstanceName(_selectedSettingClient.Name))
            {
                _instanceName = string.Empty;
                return;
            }

            var instance = _selectedSettingClient.CreateInstance(_instanceName);
            instance.RegisterEventAction(SettingRequest);
            var existingIndex = _settingClients.IndexOf(_selectedSettingClient);
            _settingClients.Insert(existingIndex + 1, instance);
            ShowNotification(_notificationFactory.Success("Instance",
                $"New instance for client '{_selectedSettingClient.Name}' created."));
            _instanceName = string.Empty;
            _selectedSettingClient = instance;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task OnDelete()
    {
        if (_selectedSettingClient != null && _settingsDataService != null)
        {
            var instancePart = $" (Instance: {_selectedSettingClient.Instance})";
            var confirmationName =
                $"{_selectedSettingClient.Name}{(_selectedSettingClient.Instance != null ? instancePart : string.Empty)}";
            if (!await GetDeleteConfirmation(confirmationName))
                return;

            try
            {
                var clientName = _selectedSettingClient.Name;
                var clientInstance = _selectedSettingClient.Instance;

                _isDeleteInProgress = true;
                await _settingsDataService.DeleteClient(_selectedSettingClient);
                _selectedSettingClient.MarkAsDeleted();
                _settingClients.Remove(_selectedSettingClient);
                _selectedSettingClient = null;
                RefreshGroups();
                var instanceNotification = clientInstance != null ? $" (instance '{clientInstance}')" : string.Empty;
                ShowNotification(_notificationFactory.Success("Delete",
                    $"Client '{clientName}'{instanceNotification} deleted successfully."));
            }
            catch (Exception ex)
            {
                ShowNotification(_notificationFactory.Failure("Delete", $"Delete Failed: {ex.Message}"));
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
        if (client != null && _settingsDataService != null)
            return await _settingsDataService.SaveClient(client);

        return new Dictionary<SettingClientConfigurationModel, List<string>>();
    }

    private void ShowNotification(NotificationMessage message)
    {
        _notificationService.Notify(message);
    }

    private void ShowGroup(string groupName)
    {
        var group = _settingClients.FirstOrDefault(a => a.Name == groupName);
        if (group != null)
        {
            _selectedSettingClient = group;
            InvokeAsync(StateHasChanged);
        }
    }

    private void RefreshGroups()
    {
        foreach (var settingGroup in _settingClients.Where(a => a.IsGroup).ToList())
        {
            settingGroup.Refresh();
            if (settingGroup.Settings.Count == 0)
                _settingClients.Remove(settingGroup);
        }
    }
}