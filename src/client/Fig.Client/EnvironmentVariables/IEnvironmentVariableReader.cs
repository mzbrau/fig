using System.Collections.Generic;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;

namespace Fig.Client.EnvironmentVariables;

public interface IEnvironmentVariableReader
{
    IEnumerable<SettingDataContract> ReadSettingOverrides(IList<SettingDefinitionDataContract> settings);
}