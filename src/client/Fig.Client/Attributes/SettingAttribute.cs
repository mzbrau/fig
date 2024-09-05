using System;
namespace Fig.Client.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class SettingAttribute : Attribute
{
    public SettingAttribute(string description,
        bool supportsLiveUpdate = true,
        string? defaultValueMethodName = null)
    {
        Description = description;
        SupportsLiveUpdate = supportsLiveUpdate;
        DefaultValueMethodName = defaultValueMethodName;
    }
        
    public string Description { get; }
        
    public bool SupportsLiveUpdate { get; }

    public string? DefaultValueMethodName { get; }
}