using Fig.Common;
using Fig.Web.Facades;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace Fig.Web.Pages;

public partial class ConnectionStatus : IDisposable
{
    private string _webVersion = null!;
    
    [Inject] 
    public IApiVersionFacade Facade { get; set; } = null!;
    
    [Inject]
    private TooltipService TooltipService { get; set; } = null!;

    [Inject]
    private IVersionHelper VersionHelper { get; set; } = null!;

    [Parameter]
    public bool ShowStaleStatus { get; set; } = true;

    private bool AreSettingsStale => Facade.AreSettingsStale;

    protected override async Task OnInitializedAsync()
    {
        _webVersion = VersionHelper.GetVersion();
        Facade.IsConnectedChanged += OnIsConnectedChanged;
        await base.OnInitializedAsync();
    }

    private void OnIsConnectedChanged(object? sender, EventArgs e)
    {
        _ = InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        Facade.IsConnectedChanged -= OnIsConnectedChanged;
    }

    private void ShowTooltip(ElementReference elementReference, string tooltipText)
    {
        var style = "background-color: black; white-space: nowrap; max-width: none;";
        TooltipService.Open(elementReference, tooltipText, new TooltipOptions
        {
            Style = style,
            Position = TooltipPosition.Left,
            Duration = 6000
        });
    }

    private void BuildTooltip(ElementReference elementReference)
    {
        var settingsStaleMessage =
            AreSettingsStale ? "Settings are stale. Refresh the page to see latest. " : string.Empty;
        
        if (Facade.IsConnected)
        {
            ShowTooltip(elementReference, $"{settingsStaleMessage}Version {_webVersion}. Connected to API version {Facade.ApiVersion} at address {Facade.ApiAddress}");
        }
        else
        {
            string lastSeen;
            if (Facade.LastConnected is null)
                lastSeen = "Never";
            else
            {
                var span = (DateTime.UtcNow - Facade.LastConnected).Value;
                lastSeen = span.Humanize();
            }
            
            ShowTooltip(elementReference, $"Version {_webVersion}. Could not contact API at address {Facade.ApiAddress}. Last seen: {lastSeen} ago");
        }
    }
}
