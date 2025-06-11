using System;

namespace Fig.Client.Attributes;

/// <summary>
/// This attribute can be used to provide the user with a dropdown of valid values for a property.
/// The items can be specified manually or values read from an enum.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ValidValuesAttribute : Attribute
{
    public ValidValuesAttribute(Type enumType)
    {
        Values = Enum.GetNames(enumType);
    }
        
    public ValidValuesAttribute(params string[] values)
    {
        Values = values;
    }
        
    public string[] Values { get; }
}