using System;
using System.Reflection;

namespace Fig.Client.Abstractions.Attributes;

public static class CategoryHelper
{
    /// <summary>
    /// Extracts the category name from a custom enum value decorated with CategoryNameAttribute.
    /// </summary>
    /// <param name="enumValue">The enum value to extract the name from.</param>
    /// <returns>The category name if found, otherwise null.</returns>
    public static string? GetName(Enum enumValue)
    {
        FieldInfo fieldInfo = enumValue.GetType().GetField(enumValue.ToString());
        var attribute = fieldInfo?.GetCustomAttribute<CategoryNameAttribute>();
        return attribute?.Name;
    }

    /// <summary>
    /// Extracts the category color hex value from a custom enum value decorated with ColorHexAttribute.
    /// </summary>
    /// <param name="enumValue">The enum value to extract the color from.</param>
    /// <returns>The color hex value if found, otherwise null.</returns>
    public static string? GetColorHex(Enum enumValue)
    {
        FieldInfo fieldInfo = enumValue.GetType().GetField(enumValue.ToString());
        var attribute = fieldInfo?.GetCustomAttribute<ColorHexAttribute>();
        return attribute?.HexValue;
    }

    /// <summary>
    /// Creates a CategoryAttribute instance from a custom enum value.
    /// The enum value must be decorated with CategoryNameAttribute and/or ColorHexAttribute.
    /// </summary>
    /// <param name="enumValue">The enum value to create the category from.</param>
    /// <returns>A new CategoryAttribute with the name and color extracted from the enum.</returns>
    public static CategoryAttribute FromEnum(Enum enumValue)
    {
        var name = GetName(enumValue);
        var colorHex = GetColorHex(enumValue);
        return new CategoryAttribute(name ?? enumValue.ToString(), colorHex ?? "#000000");
    }
}