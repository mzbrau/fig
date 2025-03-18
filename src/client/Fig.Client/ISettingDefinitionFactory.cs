using System.Collections.Generic;
using Fig.Client.Configuration;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Client;

internal interface ISettingDefinitionFactory
{
    SettingDefinitionDataContract Create(SettingDetails settingDetails, int displayOrder);
    List<CustomConfigurationSection> GetConfigurationSections(SettingDetails settingDetails);
}