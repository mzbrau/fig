using Fig.Web.Events;
using Fig.Web.Models;
using Fig.Web.Notifications;
using Fig.Web.Services;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace Fig.Web.Pages;

public partial class Settings
{
    private bool _isSaveInProgress;
    private bool _isSaveDisabled => _selectedSettingClient?.IsValid != true && _selectedSettingClient?.IsDirty != true;
    private bool _isSaveAllInProgress;
    private bool _isSaveAllDisabled => _settingClients?.Any(a => a.IsDirty || a.IsValid) != true;
    private bool _isInstanceDisabled => _selectedSettingClient == null || _selectedSettingClient?.Instance != null;
    private bool _isDeleteInProgress;
    private bool _isDeleteDisabled => _selectedSettingClient == null;

    private List<SettingClientConfigurationModel> _settingClients { get; set; } = new List<SettingClientConfigurationModel>();
    private SettingClientConfigurationModel? _selectedSettingClient { get; set; }

    [Inject]
    private ISettingsDataService? _settingsDataService { get; set; }

    [Inject]
    private NotificationService _notificationService { get; set; }

    [Inject]
    private INotificationFactory _notificationFactory { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await _settingsDataService!.LoadAllClients();
        foreach (var client in _settingsDataService.SettingsClients.OrderBy(client => client.Name))
        {
            client.StateChanged += SettingClientStateChanged;
            _settingClients.Add(client);
        }

        Console.WriteLine($"loaded {_settingClients?.Count} services");
        await base.OnInitializedAsync();
    }

    private void SettingClientStateChanged(object? sender, SettingEventArgs settingEventArgs)
    {
        if (settingEventArgs.EventType == SettingEventType.HistoryRequested)
        {
            // TODO: Currently mocked data - request the data for real. Notification if none found.
            settingEventArgs.CallbackData = new List<SettingHistoryModel>()
            {
                new SettingHistoryModel
                {
                    DateTime = DateTime.Now - TimeSpan.FromHours(2),
                    Value = "Some old val",
                    User = "John"
                },
                new SettingHistoryModel
                {
                    DateTime = DateTime.Now - TimeSpan.FromHours(1),
                    Value = "previous value",
                    User = "Sue"
                }
            };
        }
        else
        {
            InvokeAsync(StateHasChanged);
        }
    }

    private void OnChange(object value)
    {
        _selectedSettingClient = _settingClients?.FirstOrDefault(a => a.DisplayName == value as string);
    }

    private async Task OnSave()
    {
        try
        {
            _isSaveInProgress = true;
            var settingCount = await SaveClient(_selectedSettingClient);
            _selectedSettingClient?.MarkAsSaved();
            ShowNotification(_notificationFactory.Success("Save", $"Successfully saved {settingCount} setting(s)."));
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
            List<int> successes = new List<int>();
            List<string> failures = new List<string>();
            foreach (var client in _settingClients)
            {
                try
                {
                    successes.Add(await SaveClient(client));
                    client.MarkAsSaved();
                }
                catch (Exception ex)
                {
                    failures.Add(ex.Message);
                }
            }

            if (failures.Any())
            {
                ShowNotification(_notificationFactory.Failure("Save All", $"Failed to save {failures.Count} clients. {successes.Sum()} settings saved."));
            }
            else if (successes.Any(a => a > 0))
            {
                ShowNotification(_notificationFactory.Success("Save All", $"Successfully saved {successes.Sum()} setting(s) from {successes.Count(a => a > 0)} client(s)."));
            }
        }
        finally
        {
            _isSaveAllInProgress = false;
        }
    }

    private async Task OnAddInstance()
    {
        // TODO: Popup to get name
        if (_selectedSettingClient != null)
        {
            var instance = _selectedSettingClient.CreateInstance("MyInstance");
            instance.StateChanged += SettingClientStateChanged;
            var existingIndex = _settingClients.IndexOf(_selectedSettingClient);
            _settingClients.Insert(existingIndex + 1, instance);
            ShowNotification(_notificationFactory.Info("Instance", $"New instance for client '{_selectedSettingClient.Name}' created."));
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task OnDelete()
    {
        // TODO: Confirmation page
        if (_selectedSettingClient != null && _settingsDataService != null)
        {
            try
            {
                var clientName = _selectedSettingClient.Name;
                var clientInstance = _selectedSettingClient.Instance;

                _isDeleteInProgress = true;
                await _settingsDataService.DeleteClient(_selectedSettingClient);
                _selectedSettingClient.StateChanged -= SettingClientStateChanged;
                _settingClients.Remove(_selectedSettingClient);
                _selectedSettingClient = null;
                var instanceNotification = clientInstance != null ? $" (instance '{clientInstance}')" : string.Empty;
                ShowNotification(_notificationFactory.Success("Delete", $"Client '{clientName}'{instanceNotification} deleted successfully."));
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

    private async Task<int> SaveClient(SettingClientConfigurationModel? client)
    {
        if (client != null && _settingsDataService != null)
            return await _settingsDataService.SaveClient(client);

        return 0;
    }

    private void ShowNotification(NotificationMessage message)
    {
        _notificationService.Notify(message);
    }
}