using Fig.Contracts.SettingVerification;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public interface ISettingVerificationConverter
{
    SettingVerificationBusinessEntity Convert(SettingVerificationDefinitionDataContract verification);

    SettingVerificationDefinitionDataContract Convert(SettingVerificationBusinessEntity verification);

    VerificationResultDataContract Convert(VerificationResultBusinessEntity verificationResult);

    VerificationResultBusinessEntity Convert(VerificationResultDataContract verificationResult, Guid clientId, string verificationName,
        string? requestingUser);
}