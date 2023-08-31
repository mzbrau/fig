using System;
using System.Drawing;
using System.Reflection;
using Fig.Client.Enums;

namespace Fig.Client.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class CategoryAttribute : Attribute
{
    public CategoryAttribute(string name, string colorHex)
    {
        Name = name;
        ColorHex = colorHex;
    }

    public CategoryAttribute(string name, CategoryColor color)
    {
        Name = name;
        ColorHex = GetHexValue(color);
    }
    
    public string? Name { get; }
    
    public string? ColorHex { get; }
    
    private string? GetHexValue(CategoryColor color)
    {
        FieldInfo fieldInfo = typeof(CategoryColor).GetField(color.ToString());
        var attribute = fieldInfo.GetCustomAttribute<ColorHexAttribute>();
        return attribute?.HexValue;
    }
}