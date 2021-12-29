using Fig.Contracts.SettingDefinitions;
using Fig.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Fig.Web.Pages;

public partial class Settings
{
    private IList<SettingsDefinitionDataContract>? services { get; set; }
    
    [Inject]
    private ISettingsDataService? settingsDataService { get; set; }

    protected override Task OnInitializedAsync()
    {
        services = settingsDataService?.Services;
        
        return base.OnInitializedAsync();
    }
}