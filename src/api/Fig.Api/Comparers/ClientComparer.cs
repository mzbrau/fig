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

        var basicPropertiesAreSame = x.Name == y.Name && x.Instance == y.Instance && 
                                     x.Description == y.Description &&
                                     x.Settings.Count == y.Settings.Count;

        var settingsAreRemoved = x.Settings.Except(y.Settings, new SettingComparer()).Any();
        var settingsAreAdded = y.Settings.Except(x.Settings, new SettingComparer()).Any();

        return basicPropertiesAreSame &&
               !settingsAreRemoved &&
               !settingsAreAdded;
    }

    public int GetHashCode(SettingClientBusinessEntity obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(obj.Name);
        hashCode.Add(obj.Description);
        hashCode.Add(obj.Instance);

        // TODO: This is not really correct as we are hash coding a hash code, maybe ok for now.
        var settingComparer = new SettingComparer();
        foreach (var setting in obj.Settings)
            hashCode.Add(settingComparer.GetHashCode(setting));

        return hashCode.ToHashCode();
    }
}