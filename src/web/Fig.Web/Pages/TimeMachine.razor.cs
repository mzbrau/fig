using System.Text;
using Fig.Common.NetStandard.Json;
using Fig.Web.Facades;
using Fig.Web.Models.TimeMachine;
using Fig.Web.Notifications;
using Fig.Web.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Radzen;
using Radzen.Blazor;

namespace Fig.Web.Pages;

public partial class TimeMachine
{
    private bool _isRefreshInProgress;
    private string? _note;

    private bool IsRefreshDisabled => TimeMachineFacade.StartTime > TimeMachineFacade.EndTime;
    
    [Inject]
    private ITimeMachineFacade TimeMachineFacade { get; set; } = null!;
    
    [Inject]
    public IJSRuntime JavascriptRuntime { get; set; } = null!;
    
    [Inject]
    private NotificationService NotificationService { get; set; } = null!;

    [Inject]
    private INotificationFactory NotificationFactory { get; set; } = null!;
    
    [Inject]
    private DialogService DialogService { get; set; } = null!;
    
    private RadzenDataGrid<CheckPointModel> _checkPointsGrid = null!;

    private List<CheckPointModel> CheckPoints => TimeMachineFacade.CheckPoints;

    private DateTime StartTime
    {
        get => TimeMachineFacade.StartTime;
        set => TimeMachineFacade.StartTime = value;
    }

    private DateTime EndTime
    {
        get => TimeMachineFacade.EndTime;
        set => TimeMachineFacade.EndTime = value;
    }

    protected override async Task OnInitializedAsync()
    {
        StartTime = DateTime.Now - TimeSpan.FromDays(1);
        EndTime = DateTime.Now;
        _isRefreshInProgress = true;
        try
        {
            await TimeMachineFacade.QueryCheckPoints(StartTime, EndTime);
            await _checkPointsGrid.Reload();
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationFactory.Failure("Event Load Failed", ex.Message));
        }
        finally
        {
            _isRefreshInProgress = false;
        }
        
        await base.OnInitializedAsync();
    }
    
    private async Task OnRefresh()
    {
        _isRefreshInProgress = true;
        try
        {
            await TimeMachineFacade.QueryCheckPoints(StartTime, EndTime);
            await _checkPointsGrid.Reload();
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationFactory.Failure("Refresh Failed", ex.Message));
        }
        
        _isRefreshInProgress = false;
    }
    
    private void DateRender(DateRenderEventArgs args)
    {
        args.Disabled = args.Date.Date < TimeMachineFacade.EarliestDate.Date;
    }
    
    private async Task DownloadCheckpoint(CheckPointModel checkPoint)
    {
        var data = await TimeMachineFacade.DownloadCheckPoint(checkPoint);
        if (data is not null)
        {
            var text = JsonConvert.SerializeObject(data, JsonSettings.FigUserFacing);
            await DownloadExport(text, $"FigCheckPoint-{checkPoint.Timestamp:s}.json");
        }
    }

    private async Task ApplyCheckPoint(CheckPointModel checkPoint)
    {
        if (await GetApplyConfirmation())
        {
            try
            {
                checkPoint.ApplyInProgress = true;
                await TimeMachineFacade.ApplyCheckPoint(checkPoint);
                NotificationService.Notify(NotificationFactory.Success("Checkpoint Applied",
                    "The checkpoint was successfully applied"));
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationFactory.Failure("Checkpoint Apply Failed", ex.Message));
            }
            finally
            {
                checkPoint.ApplyInProgress = false;
            }
        }
    }

    private async Task AddNoteToCheckPoint(CheckPointModel checkPoint)
    {
        if (await UpdateNote(checkPoint.Note) && _note != null)
        {
            await TimeMachineFacade.AddNoteToCheckPoint(checkPoint, _note!);
            await OnRefresh();
        }
    }
    
    private async Task DownloadExport(string text, string fileName)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        await FileUtil.SaveAs(JavascriptRuntime, fileName, bytes);
    }
}