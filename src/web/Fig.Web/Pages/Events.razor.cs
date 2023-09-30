using System.Text;
using Fig.Web.Facades;
using Fig.Web.Models.Events;
using Fig.Web.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;

namespace Fig.Web.Pages;

public partial class Events
{
    private bool _isRefreshInProgress;

    private bool IsRefreshDisabled => EventsFacade.StartTime > EventsFacade.EndTime;
    
    [Inject]
    private IEventsFacade EventsFacade { get; set; } = null!;
    
    [Inject]
    public IJSRuntime JavascriptRuntime { get; set; } = null!;
    
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

    private async Task ExportEvents()
    {
        var builder = new StringBuilder();

        builder.AppendLine(EventLogModel.CsvHeaders());
        foreach (var eventLog in EventLogs)
        {
            builder.AppendLine(eventLog.ToCsv());
        }
        
        await DownloadExport(builder.ToString(), $"FigEventLog-{StartTime:s}-{EndTime:s}.csv");
    }
    
    private async Task DownloadExport(string text, string fileName)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        await FileUtil.SaveAs(JavascriptRuntime, fileName, bytes);
    }
}