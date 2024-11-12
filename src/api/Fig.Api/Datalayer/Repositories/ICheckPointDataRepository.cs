using Fig.Datalayer;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface ICheckPointDataRepository
{
    CheckPointDataBusinessEntity? GetData(Guid id);

    Guid Add(CheckPointDataBusinessEntity data);

    IEnumerable<CheckPointDataBusinessEntity> GetCheckPointsForEncryptionMigration(DateTime secretChangeDate);

    void UpdateCheckPointsAfterEncryptionMigration(List<CheckPointDataBusinessEntity> updatedCheckPoints);
}