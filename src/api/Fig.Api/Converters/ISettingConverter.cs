using Fig.Contracts.Settings;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.BusinessEntities.SettingValues;

namespace Fig.Api.Converters;

public interface ISettingConverter
{
    SettingDataContract Convert(SettingBusinessEntity setting);

    SettingBusinessEntity Convert(SettingDataContract setting);

    SettingValueDataContract Convert(SettingValueBusinessEntity settingValue);

    SettingValueBaseDataContract? Convert(SettingValueBaseBusinessEntity? value, bool hasSchema);

    SettingValueBaseBusinessEntity? Convert(SettingValueBaseDataContract? value);
}