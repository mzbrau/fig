using Fig.Web.Facades;
using Fig.Web.Models.Clients;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor;

namespace Fig.Web.Pages;

public partial class Insights
{
    private bool _showTrendline;
    
    [Inject]
    private IClientStatusFacade ClientStatusFacade { get; set; } = null!;
    
    private RadzenDataGrid<MemoryUsageAnalysisModel> _possibleMemoryLeaksGrid = default!;
    
    protected override async Task OnInitializedAsync()
    {
        await ClientStatusFacade.Refresh();
        await _possibleMemoryLeaksGrid.Reload();
        await base.OnInitializedAsync();
    }

    private void ShowAllSessionsMemory()
    {
        SetMemoryDataVisibility(true);
    }
    
    private void HideAllSessionsMemory()
    {
        SetMemoryDataVisibility(false);
    }

    private void SetMemoryDataVisibility(bool isVisible)
    {
        foreach (var session in ClientStatusFacade.ClientRunSessions)
        {
            session.HideMemoryUsageOnChart = !isVisible;
        }
    }
}