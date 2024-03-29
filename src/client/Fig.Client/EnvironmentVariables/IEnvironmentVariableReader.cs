using System.Collections.Generic;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;

namespace Fig.Client.EnvironmentVariables;

internal interface IEnvironmentVariableReader
{
    IEnumerable<SettingDataContract> ReadSettingOverrides(string clientName, IList<SettingDefinitionDataContract> settings);
    
    void ApplyConfigurationOverrides(List<SettingDefinitionDataContract> settings);
}