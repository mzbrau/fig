using System;

namespace Fig.Client.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class EnablesSettingsAttribute : Attribute
{
    public EnablesSettingsAttribute(params string[] settingNames)
    {
        SettingNames = settingNames;
    }
    
    public string[] SettingNames { get; }
}