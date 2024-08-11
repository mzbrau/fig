using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface ISettingHistoryRepository
{
    void Add(SettingValueBusinessEntity settingValue);

    IList<SettingValueBusinessEntity> GetAll(Guid clientId, string settingName);

    IList<SettingValueBusinessEntity> GetValuesForEncryptionMigration(DateTime secretChangeDate);

    void UpdateValuesAfterEncryptionMigration(List<SettingValueBusinessEntity> values);
}