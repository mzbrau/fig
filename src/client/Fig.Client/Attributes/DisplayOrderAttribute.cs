using System;

namespace Fig.Client.Attributes;

[Obsolete("Fig now uses the order that the settings are listed in the settings class. This attribute is no longer required.")]
[AttributeUsage(AttributeTargets.Property)]
public class DisplayOrderAttribute : Attribute
{
    public DisplayOrderAttribute(int displayOrder)
    {
        DisplayOrder = displayOrder;
    }
        
    public int DisplayOrder { get; }
}