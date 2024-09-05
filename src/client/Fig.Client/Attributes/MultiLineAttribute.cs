using System;

namespace Fig.Client.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class MultiLineAttribute : Attribute
{
    public MultiLineAttribute(int numberOfLines)
    {
        NumberOfLines = numberOfLines;
    }
        
    public int NumberOfLines { get; }
}