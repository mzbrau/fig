using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Comparers;

public class ClientComparer : IEqualityComparer<SettingClientBusinessEntity>
{
    public bool Equals(SettingClientBusinessEntity x, SettingClientBusinessEntity y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null)) return false;
        if (ReferenceEquals(y, null)) return false;
        if (x.GetType() != y.GetType()) return false;

        var basicPropertiesAreSame = x.Name == y.Name && x.Instance == y.Instance &&
                                     x.ClientSecret == y.ClientSecret && x.Settings.Count == y.Settings.Count;

        var settingsAreDifferent = x.Settings.Except(y.Settings, new SettingComparer()).Any();
        var dynamicVerificationsAreDifferent = x.DynamicVerifications
            .Except(y.DynamicVerifications, new DynamicVerificationComparer()).Any();
        var plugInVerificationsAreDifferent = x.PluginVerifications
            .Except(y.PluginVerifications, new PluginVerificationComparer()).Any();
        return basicPropertiesAreSame &&
               !settingsAreDifferent &&
               !dynamicVerificationsAreDifferent &&
               !plugInVerificationsAreDifferent;
    }

    public int GetHashCode(SettingClientBusinessEntity obj)
    {
        return HashCode.Combine(obj.Name, obj.ClientSecret, obj.Instance, obj.Settings);
    }
}