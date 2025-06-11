using System;

namespace Fig.Client.Attributes;

/// <summary>
/// This attribute should only be applied to bool properties.
/// Any settings that are specified will automatically be hidden when the property is false and shown when the property is true.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class EnablesSettingsAttribute : Attribute
{
    public EnablesSettingsAttribute(params string[] settingNames)
    {
        SettingNames = settingNames;
    }
    
    public string[] SettingNames { get; }
}