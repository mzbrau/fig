using Fig.Contracts.ClientRegistrationHistory;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Api.Services;

public interface IClientRegistrationHistoryService
{
    Task RecordRegistration(SettingsClientDefinitionDataContract client);

    Task<ClientRegistrationHistoryCollectionDataContract> GetAllHistory();

    Task ClearHistory();
}
