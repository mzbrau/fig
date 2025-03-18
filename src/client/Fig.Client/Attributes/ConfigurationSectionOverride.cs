using System;

namespace Fig.Client.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class ConfigurationSectionOverride : Attribute
{
    public ConfigurationSectionOverride(string sectionName, string? settingNameOverride = null)
    {
        SectionName = sectionName;
        SettingNameOverride = settingNameOverride;
    }

    public string SectionName { get; }
    
    public string? SettingNameOverride { get; }
}