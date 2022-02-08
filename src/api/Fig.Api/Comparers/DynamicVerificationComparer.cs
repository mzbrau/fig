using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Comparers;

public class DynamicVerificationComparer : IEqualityComparer<SettingDynamicVerificationBusinessEntity>
{
    public bool Equals(SettingDynamicVerificationBusinessEntity x, SettingDynamicVerificationBusinessEntity y)
    {
        if (ReferenceEquals(x, y))
            return true;
        if (ReferenceEquals(x, null))
            return false;
        if (ReferenceEquals(y, null))
            return false;
        if (x.GetType() != y.GetType())
            return false;
        return x.Description == y.Description &&
               x.Code == y.Code &&
               x.TargetRuntime == y.TargetRuntime &&
               x.SettingsVerified.Equals(y.SettingsVerified);
    }

    public int GetHashCode(SettingDynamicVerificationBusinessEntity obj)
    {
        return HashCode.Combine(obj.Description, obj.Code, (int) obj.TargetRuntime, obj.CodeHash);
    }
}