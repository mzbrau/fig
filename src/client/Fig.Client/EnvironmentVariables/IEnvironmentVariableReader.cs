using System.Collections.Generic;
using Fig.Client.Configuration;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;

namespace Fig.Client.EnvironmentVariables;

internal interface IEnvironmentVariableReader
{
    IEnumerable<SettingDataContract> ReadSettingOverrides(
        string clientName, 
        IList<SettingDefinitionDataContract> settings,
        Dictionary<string, List<CustomConfigurationSection>> configurationSections);
    
    void ApplyConfigurationOverrides(List<SettingDefinitionDataContract> settings);
}