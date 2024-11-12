using Fig.Contracts.CheckPoint;
using Fig.Contracts.ImportExport;

namespace Fig.Api.Services;

public interface ITimeMachineService
{
    CheckPointCollectionDataContract GetCheckPoints(DateTime startDate, DateTime endDate);

    FigDataExportDataContract? GetCheckPointData(Guid dataId);

    void CreateCheckPoint(string message);

    bool ApplyCheckPoint(Guid id);

    bool UpdateCheckPoint(Guid checkPointId, CheckPointUpdateDataContract contract);
}