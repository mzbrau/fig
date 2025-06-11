using System;

namespace Fig.Client.Attributes;

/// <summary>
/// ConfigurationSectionOverride is used for settings that are required to be located in a specific configuration section.
/// All settings provided by fig are in a flat structure and configuration sections allow settings to be placed in a specific section as required by the application.
/// This is usually useful for nuget package configuration such as Serilog.
/// </summary>
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