using System;

namespace Fig.Client.Abstractions.Attributes;

/// <summary>
/// Settings that have the same group attribute will be grouped together in the UI.
/// This allows a value to be configured once and applied to multiple settings for different clients.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class GroupAttribute : Attribute
{
    public GroupAttribute(string groupName)
    {
        GroupName = groupName;
    }
        
    public string GroupName { get; }
}