using Fig.Contracts.SettingDefinitions;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public interface ISettingDefinitionConverter
{
    SettingClientBusinessEntity Convert(SettingsClientDefinitionDataContract dataContract);
    
    SettingsClientDefinitionDataContract Convert(SettingClientBusinessEntity businessEntity);
}