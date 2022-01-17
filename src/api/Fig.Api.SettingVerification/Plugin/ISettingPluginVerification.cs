using Fig.Contracts.SettingVerification;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.SettingVerification.Plugin;

public interface ISettingPluginVerification
{
    Task<VerificationResultDataContract> RunVerification(SettingPluginVerificationBusinessEntity verification,
        IDictionary<string, object?> settings);
}