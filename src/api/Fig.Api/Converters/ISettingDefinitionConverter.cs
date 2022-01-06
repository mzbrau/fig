using Fig.Api.Datalayer.BusinessEntities;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Api.Converters;

public interface ISettingDefinitionConverter
{
    SettingsClientBusinessEntity Convert(SettingsClientDefinitionDataContract dataContract);
    
    SettingsClientDefinitionDataContract Convert(SettingsClientBusinessEntity businessEntity);
}