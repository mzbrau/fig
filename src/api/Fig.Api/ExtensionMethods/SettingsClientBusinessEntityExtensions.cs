using Fig.Api.Comparers;
using Fig.Datalayer.BusinessEntities;
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

    public static SettingVerificationBase? GetVerification(this SettingClientBusinessEntity client, string name)
    {
        var pluginVerification = client.PluginVerifications.FirstOrDefault(a => a.Name == name);
        if (pluginVerification != null)
            return pluginVerification;
        
        var dynamicVerification = client.DynamicVerifications.FirstOrDefault(a => a.Name == name);
        return dynamicVerification;
    }
}