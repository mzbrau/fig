using Fig.Datalayer.BusinessEntities;
using Newtonsoft.Json;

namespace Fig.Api.Comparers;

public class DynamicVerificationComparer : IEqualityComparer<SettingDynamicVerificationBusinessEntity>
{
    public bool Equals(SettingDynamicVerificationBusinessEntity? x, SettingDynamicVerificationBusinessEntity? y)
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
               JsonConvert.SerializeObject(x.SettingsVerified) ==
               JsonConvert.SerializeObject(y.SettingsVerified);
    }

    public int GetHashCode(SettingDynamicVerificationBusinessEntity obj)
    {
        var hashcode = new HashCode();
        hashcode.Add(obj.Description);
        hashcode.Add(obj.Code);
        hashcode.Add((int) obj.TargetRuntime);
        hashcode.Add(JsonConvert.SerializeObject(obj.SettingsVerified));
        return hashcode.ToHashCode();
    }
}