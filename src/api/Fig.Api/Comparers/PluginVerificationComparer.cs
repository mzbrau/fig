using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Comparers;

public class PluginVerificationComparer : IEqualityComparer<SettingPluginVerificationBusinessEntity>
{
    public bool Equals(SettingPluginVerificationBusinessEntity x, SettingPluginVerificationBusinessEntity y)
    {
        if (ReferenceEquals(x, y))
            return true;
        if (ReferenceEquals(x, null))
            return false;
        if (ReferenceEquals(y, null))
            return false;
        if (x.GetType() != y.GetType())
            return false;
        return x.PropertyArguments.Equals(y.PropertyArguments);
    }

    public int GetHashCode(SettingPluginVerificationBusinessEntity obj)
    {
        return obj.PropertyArguments.GetHashCode();
    }
}