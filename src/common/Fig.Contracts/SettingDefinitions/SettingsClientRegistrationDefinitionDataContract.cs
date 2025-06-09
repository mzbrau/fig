using System.Collections.Generic;
using Fig.Contracts.CustomActions;
using Fig.Contracts.Settings;

namespace Fig.Contracts.SettingDefinitions;

public class SettingsClientRegistrationDefinitionDataContract : SettingsClientDefinitionDataContract
{
    public SettingsClientRegistrationDefinitionDataContract(string name,
        string description,
        string? instance,
        bool hasDisplayScripts,
        List<SettingDefinitionDataContract> settings,
        IEnumerable<SettingDataContract> clientSettingOverrides,
        List<CustomActionDefinitionDataContract>? customActions = null) 
        : base(name, instance, hasDisplayScripts, settings, clientSettingOverrides, customActions)
    {
        Description = description;
    }
    
    public string Description { get; }
}