using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Comparers;

public class ClientComparer : IEqualityComparer<SettingClientBusinessEntity>
{
    public bool Equals(SettingClientBusinessEntity? x, SettingClientBusinessEntity? y)
    {
        if (ReferenceEquals(x, y))
            return true;
        if (ReferenceEquals(x, null))
            return false;
        if (ReferenceEquals(y, null))
            return false;
        if (x.GetType() != y.GetType())
            return false;

        var basicPropertiesAreSame = x.Name == y.Name && x.Instance == y.Instance &&
                                     x.Settings.Count == y.Settings.Count;

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
        var hashCode = new HashCode();
        hashCode.Add(obj.Name);
        hashCode.Add(obj.Instance);

        // TODO: This is not really correct as we are hash coding a hash code, maybe ok for now.
        var settingComparer = new SettingComparer();
        foreach (var setting in obj.Settings)
            hashCode.Add(settingComparer.GetHashCode(setting));

        var dynamicComparer = new DynamicVerificationComparer();
        foreach (var verification in obj.DynamicVerifications)
            hashCode.Add(dynamicComparer.GetHashCode(verification));

        var pluginComparer = new PluginVerificationComparer();
        foreach (var verification in obj.PluginVerifications)
            hashCode.Add(pluginComparer.GetHashCode(verification));

        return hashCode.ToHashCode();
    }
}