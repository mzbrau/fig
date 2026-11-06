using System.Collections.Generic;
using Fig.Client.Configuration;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Microsoft.Extensions.Logging;

namespace Fig.Client.EnvironmentVariables;

internal interface IEnvironmentVariableReader
{
    IEnumerable<SettingDataContract> ReadSettingOverrides(
        string clientName, 
        IList<SettingDefinitionDataContract> settings,
        Dictionary<string, List<CustomConfigurationSection>> configurationSections);
    
    /// <summary>
    /// Applies setting-configuration environment variable overrides (group, validation, lookup key, classification).
    /// </summary>
    /// <param name="settings">Setting definitions to update.</param>
    /// <param name="clientName">Client name included in diagnostic logs.</param>
    /// <param name="logger">Optional logger for match, mismatch, and invalid-value diagnostics.</param>
    void ApplyConfigurationOverrides(
        List<SettingDefinitionDataContract> settings,
        string? clientName = null,
        ILogger? logger = null);
}