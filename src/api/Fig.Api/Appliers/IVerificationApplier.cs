using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Appliers;

public interface IVerificationApplier
{
    void ApplyVerificationUpdates(
        List<SettingClientBusinessEntity> existingRegistrations, 
        SettingClientBusinessEntity updatedRegistration);
}