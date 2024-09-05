using System;

namespace Fig.Client.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class GroupAttribute : Attribute
{
    public GroupAttribute(string groupName)
    {
        GroupName = groupName;
    }
        
    public string GroupName { get; }
}