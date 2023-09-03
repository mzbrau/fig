using Fig.Api.Comparers;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Appliers;

public class ValidatorApplier : IValidatorApplier
{
    public void ApplyVerificationUpdates(List<SettingClientBusinessEntity> existingRegistrations, SettingClientBusinessEntity updatedRegistration)
    {
        foreach (var registration in existingRegistrations)
        {
            UpdateDynamicVerifications(registration.DynamicVerifications, updatedRegistration.DynamicVerifications);
            UpdatePluginVerifications(registration.PluginVerifications, updatedRegistration.PluginVerifications);
        }
    }
    
    private void UpdatePluginVerifications(ICollection<SettingPluginVerificationBusinessEntity> existingVerifications,
        ICollection<SettingPluginVerificationBusinessEntity> newVerifications)
    {
        UpdateVerifications(
            existingVerifications, 
            newVerifications, 
            new PluginVerificationComparer(),
            ApplyUpdate);

        void ApplyUpdate(SettingPluginVerificationBusinessEntity existingVerification,
            SettingPluginVerificationBusinessEntity newVerification)
        {
            existingVerification.PropertyArguments = newVerification.PropertyArguments;
        }
    }

    private void UpdateDynamicVerifications(ICollection<SettingDynamicVerificationBusinessEntity> existingVerifications,
        ICollection<SettingDynamicVerificationBusinessEntity> newVerifications)
    {
        UpdateVerifications(
            existingVerifications, 
            newVerifications, 
            new DynamicVerificationComparer(),
            ApplyUpdate);

        void ApplyUpdate(SettingDynamicVerificationBusinessEntity existingVerification,
            SettingDynamicVerificationBusinessEntity newVerification)
        {
            existingVerification.Code = newVerification.Code;
            existingVerification.Description = newVerification.Description;
            existingVerification.SettingsVerified = newVerification.SettingsVerified;
            existingVerification.TargetRuntime = newVerification.TargetRuntime;
        }
    }
    
    private void UpdateVerifications<T>(ICollection<T> existingVerifications,
        ICollection<T> newVerifications, IEqualityComparer<T> comparer, Action<T, T> applyUpdate) where T: SettingVerificationBase
    {
        var removedVerifications = existingVerifications.Except(newVerifications, comparer).ToList();
        foreach (var verification in removedVerifications)
        {
            existingVerifications.Remove(verification);
        }

        var addedOrUpdatedVerifications =
            newVerifications.Except(existingVerifications, comparer).ToList();
        foreach (var updatedVerification in addedOrUpdatedVerifications)
        {
            var existingVerification = existingVerifications.FirstOrDefault(a => a.Name == updatedVerification.Name);
            if (existingVerification == null)
            {
                existingVerifications.Add(updatedVerification);
            }
            else
            {
                applyUpdate(existingVerification, updatedVerification);
            }
        }
    }
}