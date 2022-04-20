using Fig.Web.Facades;
using Fig.Web.Models.Clients;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor;

namespace Fig.Web.Pages;

public partial class Clients
{
    private bool _isRefreshInProgress;
    
    [Inject]
    private IClientStatusFacade ClientStatusFacade { get; set; } = null!;
    
    private RadzenDataGrid<ClientRunSessionModel> clientsGrid;

    private List<ClientRunSessionModel> ClientRunSessions => ClientStatusFacade.ClientRunSessions;

    protected override async Task OnInitializedAsync()
    {
        _isRefreshInProgress = true;
        await ClientStatusFacade.Refresh();
        await clientsGrid.Reload();
        _isRefreshInProgress = false;
        await base.OnInitializedAsync();
    }
    
    private async Task OnRefresh()
    {
        _isRefreshInProgress = true;
        await ClientStatusFacade.Refresh();
        await clientsGrid.Reload();
        _isRefreshInProgress = false;
    }
}