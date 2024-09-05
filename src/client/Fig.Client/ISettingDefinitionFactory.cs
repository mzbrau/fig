using System.Reflection;
using Fig.Client.Configuration;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Client;

internal interface ISettingDefinitionFactory
{
    SettingDefinitionDataContract Create(SettingDetails settingDetails, int displayOrder);

    CustomConfigurationSection GetConfigurationSection(SettingDetails settingDetails);
}