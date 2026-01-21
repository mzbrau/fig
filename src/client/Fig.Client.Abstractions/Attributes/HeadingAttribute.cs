using System;
using System.Reflection;
using Fig.Client.Abstractions.Enums;

namespace Fig.Client.Abstractions.Attributes;

/// <summary>
/// HeadingAttribute is used to add a visual heading above a setting in the UI.
/// The heading displays the specified text and can be indented and colored.
/// It inherits the advanced setting value and color from the setting it's applied to if not explicitly set.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class HeadingAttribute : Attribute
{
    /// <summary>
    /// Creates a heading attribute with the specified text.
    /// </summary>
    /// <param name="text">The text to display in the heading.</param>
    /// <param name="color">The color for the heading background. If not provided, inherits from the setting.</param>
    public HeadingAttribute(string text, string? color = null)
    {
        Text = text ?? string.Empty;
        Color = color;
    }

    /// <summary>
    /// Creates a heading attribute with the specified text and predefined color.
    /// </summary>
    /// <param name="text">The text to display in the heading.</param>
    /// <param name="color">The predefined color for the heading border.</param>
    /// <exception cref="ArgumentNullException">Thrown when text is null or empty.</exception>
    public HeadingAttribute(string text, CategoryColor color) : this(text, GetHexValue(color))
    {
    }
    
    /// <summary>
    /// Creates a heading attribute using a predefined category (uses the category name as text and color).
    /// </summary>
    /// <param name="category">The predefined category to use for both text and color.</param>
    public HeadingAttribute(Category category) : this(GetCategoryName(category) ?? category.ToString(), GetCategoryHexValue(category))
    {
    }
    
    /// <summary>
    /// Gets the text to display in the heading.
    /// </summary>
    public string Text { get; }
    
    /// <summary>
    /// Gets the color for the heading border. May be null if it should inherit from the setting.
    /// </summary>
    public string? Color { get; }
    
    private static string? GetHexValue(CategoryColor color)
    {
        FieldInfo fieldInfo = typeof(CategoryColor).GetField(color.ToString());
        var attribute = fieldInfo?.GetCustomAttribute<ColorHexAttribute>();
        return attribute?.HexValue;
    }
    
    private static string? GetCategoryName(Category category)
    {
        FieldInfo fieldInfo = typeof(Category).GetField(category.ToString());
        var attribute = fieldInfo?.GetCustomAttribute<CategoryNameAttribute>();
        return attribute?.Name;
    }
    
    private static string? GetCategoryHexValue(Category category)
    {
        FieldInfo fieldInfo = typeof(Category).GetField(category.ToString());
        var attribute = fieldInfo?.GetCustomAttribute<ColorHexAttribute>();
        return attribute?.HexValue;
    }
}
