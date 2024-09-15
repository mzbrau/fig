using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.BusinessEntities.SettingValues;

namespace Fig.Api.Converters;

public interface ISettingConverter
{
    SettingDataContract Convert(SettingBusinessEntity setting);

    SettingBusinessEntity Convert(SettingDataContract setting, SettingBusinessEntity? originalSetting);

    SettingValueDataContract Convert(SettingValueBusinessEntity settingValue);

    SettingValueBaseDataContract? Convert(SettingValueBaseBusinessEntity? value, bool hasSchema,
        DataGridDefinitionDataContract? dataGridDefinition = null);

    SettingValueBaseBusinessEntity? Convert(SettingValueBaseDataContract? value, SettingBusinessEntity? originalSetting = null);
}