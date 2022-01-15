using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.SettingVerification;

namespace Fig.Api.SettingVerification;

public interface ISettingDynamicVerificationRunner
{
    Task<VerificationResultDataContract> Run(SettingVerificationDefinitionDataContract verification,
        IEnumerable<SettingDefinitionDataContract> settings);
}