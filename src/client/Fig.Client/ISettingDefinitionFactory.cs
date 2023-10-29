using System.Reflection;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Client
{
    internal interface ISettingDefinitionFactory
    {
        SettingDefinitionDataContract Create(PropertyInfo settingProperty, bool liveReload, SettingsBase parent);

        string GetConfigurationSection(PropertyInfo settingProperty);
    }
}