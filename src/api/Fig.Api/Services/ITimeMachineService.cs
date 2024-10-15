using Fig.Api.Utils;
using Fig.Contracts.CheckPoint;
using Fig.Contracts.ImportExport;

namespace Fig.Api.Services;

public interface ITimeMachineService
{
    CheckPointCollectionDataContract GetCheckPoints(DateTime startDate, DateTime endDate);

    FigDataExportDataContract? GetCheckPointData(Guid dataId);

    void CreateCheckPoint(CheckPointRecord record);

    bool ApplyCheckPoint(Guid id);

    bool UpdateCheckPoint(Guid checkPointId, CheckPointUpdateDataContract contract);
}