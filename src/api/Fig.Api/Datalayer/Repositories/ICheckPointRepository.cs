using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface ICheckPointRepository
{
    IEnumerable<CheckPointBusinessEntity> GetCheckPoints(DateTime startDate, DateTime endDate);

    void Add(CheckPointBusinessEntity checkPoint);
    
    DateTime GetEarliestEntry();
    
    CheckPointBusinessEntity? GetCheckPoint(Guid id);

    void UpdateCheckPoint(CheckPointBusinessEntity checkPoint);
}