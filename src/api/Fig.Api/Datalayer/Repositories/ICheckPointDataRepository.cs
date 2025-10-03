using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface ICheckPointDataRepository
{
    Task<CheckPointDataBusinessEntity?> GetData(Guid id);

    Task<Guid> Add(CheckPointDataBusinessEntity data);

    Task<IEnumerable<CheckPointDataBusinessEntity>> GetCheckPointsForEncryptionMigration(DateTime secretChangeDate);

    Task UpdateCheckPointsAfterEncryptionMigration(List<CheckPointDataBusinessEntity> updatedCheckPoints);
    
    Task<int> DeleteOlderThan(DateTime cutoffDate);
}