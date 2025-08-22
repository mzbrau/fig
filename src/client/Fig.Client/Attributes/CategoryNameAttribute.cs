using System;

namespace Fig.Client.Attributes;

/// <summary>
/// Attribute to specify the display name for a category enum value.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class CategoryNameAttribute : Attribute
{
    public CategoryNameAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
