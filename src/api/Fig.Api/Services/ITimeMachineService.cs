using Fig.Api.Utils;
using Fig.Contracts.CheckPoint;
using Fig.Contracts.ImportExport;

namespace Fig.Api.Services;

public interface ITimeMachineService
{
    Task<CheckPointCollectionDataContract> GetCheckPoints(DateTime startDate, DateTime endDate);

    Task<FigDataExportDataContract?> GetCheckPointData(Guid dataId);

    Task CreateCheckPoint(CheckPointRecord record);

    Task<bool> ApplyCheckPoint(Guid id);

    Task<bool> UpdateCheckPoint(Guid checkPointId, CheckPointUpdateDataContract contract);
}