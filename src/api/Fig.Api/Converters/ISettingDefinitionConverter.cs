using Fig.Contracts.Authentication;
using Fig.Contracts.SettingDefinitions;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public interface ISettingDefinitionConverter
{
    SettingClientBusinessEntity Convert(SettingsClientRegistrationDefinitionDataContract dataContract);

    Task<SettingsClientDefinitionDataContract> Convert(SettingClientBusinessEntity businessEntity,
        bool allowDisplayScripts, UserDataContract? authenticatedUser);
}