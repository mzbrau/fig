using Fig.Web.Facades;
using Fig.Web.Models.Events;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace Fig.Web.Pages;

public partial class Events
{
    private bool _isRefreshInProgress;
    
    [Inject]
    private IEventsFacade EventsFacade { get; set; } = null!;
    
    private RadzenDataGrid<EventLogModel> _eventLogGrid = null!;

    private List<EventLogModel> EventLogs => EventsFacade.EventLogs;

    private DateTime StartTime
    {
        get => EventsFacade.StartTime;
        set => EventsFacade.StartTime = value;
    }

    private DateTime EndTime
    {
        get => EventsFacade.EndTime;
        set => EventsFacade.EndTime = value;
    }

    protected override async Task OnInitializedAsync()
    {
        _isRefreshInProgress = true;
        await EventsFacade.QueryEvents(StartTime, EndTime);
        await _eventLogGrid.Reload();
        _isRefreshInProgress = false;
        await base.OnInitializedAsync();
    }
    
    private async Task OnRefresh()
    {
        _isRefreshInProgress = true;
        await EventsFacade.QueryEvents(StartTime, EndTime);
        await _eventLogGrid.Reload();
        _isRefreshInProgress = false;
    }
    
    private void DateRender(DateRenderEventArgs args)
    {
        args.Disabled = args.Date.Date < EventsFacade.EarliestDate.Date;
    }
}