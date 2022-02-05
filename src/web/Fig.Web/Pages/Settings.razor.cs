using Fig.Contracts.SettingDefinitions;
using Fig.Web.Models;
using Fig.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Fig.Web.Pages;

public partial class Settings
{
    private bool _isSaveInProgress;
    private bool _isSaveDisabled = true;
    private bool _isSaveAllInProgress;
    private bool _isSaveAllDisabled = true;
    private bool _isInstanceDisabled = true;
    private bool _isDeleteInProgress;
    private bool _isDeleteDisabled = true;

    private IList<SettingClientConfigurationModel>? _settingClients { get; set; }
    private SettingClientConfigurationModel? _selectedSettingClient { get; set; }

    [Inject]
    private ISettingsDataService? _settingsDataService { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await _settingsDataService!.LoadAllClients();
        _settingClients = _settingsDataService.SettingsClients;
        Console.WriteLine($"loaded {_settingClients?.Count} services");
        await base.OnInitializedAsync();
    }

    private void OnChange(object value)
    {
        _selectedSettingClient = _settingClients?.FirstOrDefault(a => a.DisplayName == value as string);
        if (_selectedSettingClient != null)
        {
            _isDeleteDisabled = false;
            _isInstanceDisabled = _selectedSettingClient.Instance != null;
        }
        else
        {
            _isDeleteDisabled = true;
            _isInstanceDisabled = true;
        }
    }

    private async Task OnSave()
    {
        _isSaveInProgress = true;
        await SaveClient(_selectedSettingClient);
        _isSaveInProgress = false;
    }

    private async Task OnSaveAll()
    {
        _isSaveAllInProgress = true;
        foreach (var client in _settingClients)
        {
            await SaveClient(client);
        }
        _isSaveAllInProgress = false;
    }

    private async Task OnAddInstance()
    {
        // TODO: Popup to get name
        if (_selectedSettingClient != null)
            _settingClients.Add(_selectedSettingClient.CreateInstance("MyInstance"));

        await InvokeAsync(StateHasChanged);
    }

    private async Task OnDelete()
    {
        // TODO: Confirmation page
        if (_selectedSettingClient != null && _settingsDataService != null)
            await _settingsDataService.DeleteClient(_selectedSettingClient);
    }

    private async Task SaveClient(SettingClientConfigurationModel? client)
    {
        if (client != null && _settingsDataService != null)
            await _settingsDataService.SaveClient(client);
    }
}