using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Validators;

public interface IValidatorApplier
{
    void ApplyVerificationUpdates(
        List<SettingClientBusinessEntity> existingRegistrations, 
        SettingClientBusinessEntity updatedRegistration);
}