using Fig.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Fig.Web.Pages;

public partial class NavSettings
{
    private IList<string>? services { get; set; }

    [Inject]
    private ISettingsDataService? settingsDataService { get; set; }

    protected override Task OnInitializedAsync()
    {
        services = settingsDataService?.Services;
        Console.WriteLine($"loaded {services?.Count} services");
        return base.OnInitializedAsync();
    }

    private bool collapseNavMenu = true;

    private string? NavMenuCssClass => collapseNavMenu ? "collapse" : null;

    private void ToggleNavMenu()
    {
        collapseNavMenu = !collapseNavMenu;
    }
}