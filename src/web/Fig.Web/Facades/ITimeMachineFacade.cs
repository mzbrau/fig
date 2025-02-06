using Fig.Contracts.ImportExport;
using Fig.Web.Models.TimeMachine;

namespace Fig.Web.Facades;

public interface ITimeMachineFacade
{
    List<CheckPointModel> CheckPoints { get; }
    
    DateTime EarliestDate { get; }
    
    DateTime StartTime { get; set; }
    
    DateTime EndTime { get; set; }

    Task QueryCheckPoints(DateTime startTime, DateTime endTime);
    
    Task<FigDataExportDataContract?> DownloadCheckPoint(CheckPointModel checkPoint);
    
    Task ApplyCheckPoint(CheckPointModel checkPoint);
    
    Task AddNoteToCheckPoint(CheckPointModel checkPoint, string note);
}