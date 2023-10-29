using System;

namespace Fig.Client.Attributes;

[AttributeUsage(AttributeTargets.Field)]
internal class ColorHexAttribute : Attribute
{
    public ColorHexAttribute(string hexValue)
    {
        HexValue = hexValue;
    }

    public string HexValue { get; }
}