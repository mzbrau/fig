using System;
using Fig.Client.Abstractions.Enums;

namespace Fig.Client.Abstractions.Attributes;

/// <summary>
/// The lookup table attribute is used to add a dropdown list to a property in the UI.
/// If a lookup table of the same name is defined, the items from that table will be options in the dropdown for this setting.
/// If no lookup table is defined, the property can be edited as normal.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class LookupTableAttribute : Attribute
{
    public LookupTableAttribute(string lookupTableKey, LookupSource lookupSource, string? keySettingName = null)
    {
        LookupTableKey = lookupTableKey;
        LookupSource = lookupSource;
        KeySettingName = keySettingName;
    }
        
    public string LookupTableKey { get; }
    
    public LookupSource LookupSource { get; }
    
    public string? KeySettingName { get; }
}