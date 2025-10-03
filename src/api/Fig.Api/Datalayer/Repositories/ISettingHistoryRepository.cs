using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface ISettingHistoryRepository
{
    Task Add(SettingValueBusinessEntity settingValue);

    Task<IList<SettingValueBusinessEntity>> GetAll(Guid clientId, string settingName);

    Task<IList<SettingValueBusinessEntity>> GetValuesForEncryptionMigration(DateTime secretChangeDate);

    Task UpdateValuesAfterEncryptionMigration(List<SettingValueBusinessEntity> values);
    
    Task<int> DeleteOlderThan(DateTime cutoffDate);
}