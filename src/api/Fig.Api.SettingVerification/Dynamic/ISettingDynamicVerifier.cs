using Fig.Contracts.SettingVerification;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.SettingVerification.Dynamic;

public interface ISettingDynamicVerifier
{
    Task Compile(SettingDynamicVerificationBusinessEntity verification);

    Task<VerificationResultDataContract> RunVerification(SettingDynamicVerificationBusinessEntity verification,
        IDictionary<string, object?> settings);
}