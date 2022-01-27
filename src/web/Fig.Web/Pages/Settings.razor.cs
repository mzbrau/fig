using Fig.Contracts.SettingDefinitions;
using Fig.Web.Models;
using Fig.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Fig.Web.Pages;

public partial class Settings
{
    private IList<SettingsConfigurationModel>? services { get; set; }

    private SettingsConfigurationModel? selectedService { get; set; }

    [Inject]
    private ISettingsDataService? settingsDataService { get; set; }

    protected override Task OnInitializedAsync()
    {
        services = settingsDataService?.Services;
        Console.WriteLine($"loaded {services?.Count} services");
        return base.OnInitializedAsync();
    }

    void OnChange(object value)
    {
        selectedService = services?.FirstOrDefault(a => a.Name == value as string);
        Console.WriteLine(@"service:" + value);
    }
}