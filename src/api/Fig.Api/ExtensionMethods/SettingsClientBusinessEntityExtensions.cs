using Fig.Api.Datalayer.BusinessEntities;
using NHibernate.Cfg;

namespace Fig.Api.ExtensionMethods;

public static class SettingsClientBusinessEntityExtensions
{
    public static SettingsClientBusinessEntity CreateOverride(this SettingsClientBusinessEntity original, string? instance)
    {
        return new SettingsClientBusinessEntity
        {
            Name = original.Name,
            ClientSecret = original.ClientSecret,
            Instance = instance,
            Settings = original.Settings.Select(a => a.Clone()).ToList()
        };
    }
}