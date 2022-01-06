using Fig.Api.Datalayer.BusinessEntities;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Api.Converters;

public interface ISettingDefinitionConverter
{
    SettingClientBusinessEntity Convert(SettingsClientDefinitionDataContract dataContract);
    
    SettingsClientDefinitionDataContract Convert(SettingClientBusinessEntity businessEntity);
}