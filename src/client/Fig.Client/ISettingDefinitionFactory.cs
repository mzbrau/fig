using System.Collections.Generic;
using Fig.Client.Configuration;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Client;

internal interface ISettingDefinitionFactory
{
    SettingDefinitionDataContract Create(SettingDetails settingDetails, string clientName, int displayOrder, List<SettingDetails> allSettings, bool automaticallyGenerateHeadings = true);
    
    List<CustomConfigurationSection> GetConfigurationSections(SettingDetails settingDetails);
}