using Fig.Contracts.SettingVerification;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.SettingVerification;

public interface ISettingVerification
{
    Task<VerificationResultDataContract> RunVerification(
        SettingVerificationBusinessEntity verification, ICollection<SettingBusinessEntity> settings);
}