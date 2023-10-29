using System;

namespace Fig.Client.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ConfigurationSectionOverride : Attribute
{
    public ConfigurationSectionOverride(string sectionName)
    {
        SectionName = sectionName;
    }

    public string SectionName { get; }
}