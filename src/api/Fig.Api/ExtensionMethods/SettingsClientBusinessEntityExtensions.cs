using Fig.Api.Comparers;
using Fig.Api.Datalayer.BusinessEntities;
using NHibernate.Cfg;

namespace Fig.Api.ExtensionMethods;

public static class SettingsClientBusinessEntityExtensions
{
    public static SettingClientBusinessEntity CreateOverride(this SettingClientBusinessEntity original, string? instance)
    {
        return new SettingClientBusinessEntity
        {
            Name = original.Name,
            ClientSecret = original.ClientSecret,
            Instance = instance,
            Settings = original.Settings.Select(a => a.Clone()).ToList()
        };
    }

    public static bool HasEquivalentDefinitionTo(this SettingClientBusinessEntity original,
        SettingClientBusinessEntity other)
    {
        return new ClientComparer().Equals(original, other);
    }
}