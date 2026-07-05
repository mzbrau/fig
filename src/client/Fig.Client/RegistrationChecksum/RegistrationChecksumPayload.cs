using System.Collections.Generic;
using Fig.Contracts.CustomActions;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Client.RegistrationChecksum;

internal sealed class RegistrationChecksumPayload
{
    public RegistrationChecksumPayload(
        string? description,
        bool hasDisplayScripts,
        List<SettingDefinitionDataContract> settings,
        List<CustomActionDefinitionDataContract> customActions)
    {
        Description = description;
        HasDisplayScripts = hasDisplayScripts;
        Settings = settings;
        CustomActions = customActions;
    }

    public string? Description { get; }

    public bool HasDisplayScripts { get; }

    public List<SettingDefinitionDataContract> Settings { get; }

    public List<CustomActionDefinitionDataContract> CustomActions { get; }
}
