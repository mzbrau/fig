using System.Reflection;
using Fig.Client.Configuration;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Client
{
    internal interface ISettingDefinitionFactory
    {
        SettingDefinitionDataContract Create(PropertyInfo settingProperty, bool liveReload, SettingsBase parent);

        CustomConfigurationSection GetConfigurationSection(PropertyInfo settingProperty);
    }
}