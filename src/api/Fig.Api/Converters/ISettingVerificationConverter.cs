using Fig.Contracts.SettingVerification;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public interface ISettingVerificationConverter
{
    SettingDynamicVerificationBusinessEntity Convert(SettingDynamicVerificationDefinitionDataContract verification);

    SettingPluginVerificationBusinessEntity Convert(SettingPluginVerificationDefinitionDataContract verification);

    SettingDynamicVerificationDefinitionDataContract Convert(SettingDynamicVerificationBusinessEntity verification);

    SettingPluginVerificationDefinitionDataContract Convert(SettingPluginVerificationBusinessEntity verification);

    VerificationResultDataContract Convert(VerificationResultBusinessEntity verificationResult);

    VerificationResultBusinessEntity Convert(VerificationResultDataContract verificationResult, Guid clientId, string verificationName,
        string? requestingUser);
}