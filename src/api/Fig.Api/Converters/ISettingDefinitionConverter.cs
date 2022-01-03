using Fig.Api.BusinessEntities;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Api.Converters;

public interface ISettingDefinitionConverter
{
    SettingsClientBusinessEntity Convert(SettingsClientDefinitionDataContract dataContract);
}