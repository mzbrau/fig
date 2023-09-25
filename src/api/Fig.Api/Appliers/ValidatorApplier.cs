using Fig.Api.Comparers;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Appliers;

public class ValidatorApplier : IValidatorApplier
{
    public void ApplyVerificationUpdates(List<SettingClientBusinessEntity> existingRegistrations, SettingClientBusinessEntity updatedRegistration)
    {
        foreach (var registration in existingRegistrations)
        {
            UpdateVerifications(registration.Verifications, updatedRegistration.Verifications);
        }
    }
    
    private void UpdateVerifications(ICollection<SettingVerificationBusinessEntity> existingVerifications,
        ICollection<SettingVerificationBusinessEntity> newVerifications)
    {
        UpdateVerifications(
            existingVerifications, 
            newVerifications, 
            new VerificationComparer(),
            ApplyUpdate);

        void ApplyUpdate(SettingVerificationBusinessEntity existingVerification,
            SettingVerificationBusinessEntity newVerification)
        {
            existingVerification.PropertyArguments = newVerification.PropertyArguments;
        }
    }
    
    private void UpdateVerifications<T>(ICollection<T> existingVerifications,
        ICollection<T> newVerifications, IEqualityComparer<T> comparer, Action<T, T> applyUpdate) where T: SettingVerificationBusinessEntity
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