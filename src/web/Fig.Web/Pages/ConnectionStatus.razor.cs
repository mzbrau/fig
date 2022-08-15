using Fig.Web.Facades;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace Fig.Web.Pages;

public partial class ConnectionStatus
{
    [Inject] 
    public IApiVersionFacade Facade { get; set; } = null!;
    
    [Inject]
    private TooltipService TooltipService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        Facade.IsConnectedChanged += (sender, args) => StateHasChanged();
        await base.OnInitializedAsync();
    }

    private void ShowTooltip(ElementReference elementReference, string tooltipText)
    {
        var style = "background-color: black";
        TooltipService.Open(elementReference, tooltipText, new TooltipOptions
        {
            Style = style,
            Position = TooltipPosition.Left,
            Duration = 6000
        });
    }

    private void BuildTooltip(ElementReference elementReference)
    {
        if (Facade.IsConnected)
        {
            ShowTooltip(elementReference, $"Connected to API version {Facade.ApiVersion} at address {Facade.ApiAddress}");
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
            
            ShowTooltip(elementReference, $"Could not contact API at address {Facade.ApiAddress}. Last seen: {lastSeen} ago");
        }
    }
}