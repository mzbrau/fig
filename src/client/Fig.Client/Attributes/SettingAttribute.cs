using System;
using Fig.Common.NetStandard.Data;

namespace Fig.Client.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class SettingAttribute : Attribute
{
    public SettingAttribute(string description,
        bool supportsLiveUpdate = true,
        string? defaultValueMethodName = null,
        Classification classification = Classification.Technical)
    {
        Description = description;
        SupportsLiveUpdate = supportsLiveUpdate;
        DefaultValueMethodName = defaultValueMethodName;
        Classification = classification;
    }
        
    public string Description { get; }
        
    public bool SupportsLiveUpdate { get; }

    public string? DefaultValueMethodName { get; }
    
    public Classification Classification { get; }
}