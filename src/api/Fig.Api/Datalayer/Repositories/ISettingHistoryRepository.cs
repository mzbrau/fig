using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface ISettingHistoryRepository
{
    void Add(SettingValueBusinessEntity settingValue);

    IEnumerable<SettingValueBusinessEntity> GetAll(Guid clientId, string settingName);

    IEnumerable<SettingValueBusinessEntity> GetValuesForEncryptionMigration(DateTime secretChangeDate);

    void UpdateValuesAfterEncryptionMigration(List<SettingValueBusinessEntity> values);
}