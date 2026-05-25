using Fig.Contracts.SettingDefinitions;

namespace Fig.Api.Services;

public record SettingsClientLoadResult(
    IList<SettingsClientDefinitionDataContract> Clients,
    IList<ClientLoadFailureDataContract> Failures);
