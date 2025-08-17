using System;
using Fig.Client.Abstractions.Data;

namespace Fig.Client.Abstractions.Attributes;

/// <summary>
/// The Setting Attribute needs to be added on all properties within the Setting class that should be managed by Fig.
/// The description can be a string or can reference an embedded markdown file using `$` and the name of the resource.
/// DefaultValueMethodName is used to set the default value of collections.
/// </summary>
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