using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface ICheckPointRepository
{
    Task<IEnumerable<CheckPointBusinessEntity>> GetCheckPoints(DateTime startDate, DateTime endDate);

    Task Add(CheckPointBusinessEntity checkPoint);
    
    Task<DateTime> GetEarliestEntry();
    
    Task<CheckPointBusinessEntity?> GetCheckPoint(Guid id);

    Task UpdateCheckPoint(CheckPointBusinessEntity checkPoint);
}