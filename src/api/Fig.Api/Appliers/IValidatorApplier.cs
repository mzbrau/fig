using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Appliers;

public interface IValidatorApplier
{
    void ApplyVerificationUpdates(
        List<SettingClientBusinessEntity> existingRegistrations, 
        SettingClientBusinessEntity updatedRegistration);
}