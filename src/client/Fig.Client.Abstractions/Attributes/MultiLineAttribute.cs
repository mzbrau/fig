using System;

namespace Fig.Client.Abstractions.Attributes;

/// <summary>
/// This attribute results in the string property being displayed as a multi-line text box in the UI.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class MultiLineAttribute : Attribute
{
    public MultiLineAttribute(int numberOfLines)
    {
        NumberOfLines = numberOfLines;
    }
        
    public int NumberOfLines { get; }
}