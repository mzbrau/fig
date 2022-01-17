using Fig.Contracts.SettingVerification;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.SettingVerification;

public interface ISettingVerifier
{
    Task Compile(SettingDynamicVerificationBusinessEntity verification);
    
    Task<VerificationResultDataContract> Verify(SettingVerificationBase verification, IEnumerable<SettingBusinessEntity> settings);
}