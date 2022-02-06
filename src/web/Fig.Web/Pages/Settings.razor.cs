using Fig.Contracts.SettingDefinitions;
using Fig.Web.Events;
using Fig.Web.Models;
using Fig.Web.Services;
using Microsoft.AspNetCore.Components;

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
            // TODO: Currently mocked data - request the data for real.
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
            await SaveClient(_selectedSettingClient);
            _selectedSettingClient?.ClearDirty();
        }
        catch (Exception ex)
        {
            // TODO: Notification
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
            foreach (var client in _settingClients)
            {
                await SaveClient(client);
                client.ClearDirty();
            }
        }
        catch (Exception ex)
        {
            // TODO: Notification
            Console.WriteLine(ex);
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
                _isDeleteInProgress = true;
                await _settingsDataService.DeleteClient(_selectedSettingClient);
                _selectedSettingClient.StateChanged -= SettingClientStateChanged;
                _settingClients.Remove(_selectedSettingClient);
                _selectedSettingClient = null;
            }
            catch (Exception ex)
            {
                // TODO: Notification
                Console.WriteLine(ex);
            }
            finally
            {
                _isDeleteInProgress = false;
            }
        }
    }

    private async Task SaveClient(SettingClientConfigurationModel? client)
    {
        if (client != null && _settingsDataService != null)
            await _settingsDataService.SaveClient(client);
    }
}