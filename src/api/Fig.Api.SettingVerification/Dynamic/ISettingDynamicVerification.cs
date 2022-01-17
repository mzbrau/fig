using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.SettingVerification;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.SettingVerification.Dynamic;

public interface ISettingDynamicVerification
{
    Task Compile(SettingDynamicVerificationBusinessEntity verification);

    Task<VerificationResultDataContract> RunVerification(SettingDynamicVerificationBusinessEntity verification,
        IDictionary<string, object?> settings);
}