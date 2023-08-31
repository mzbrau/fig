using System;

namespace Fig.Client.Attributes;

[AttributeUsage(AttributeTargets.Field)]
internal class ColorHexAttribute : Attribute
{
    public string HexValue { get; }

    public ColorHexAttribute(string hexValue)
    {
        HexValue = hexValue;
    }
}