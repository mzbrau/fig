using System;
using System.Reflection;
using Fig.Client.Enums;

namespace Fig.Client.Attributes;

/// <summary>
/// Categories are used to visually group settings in the UI.
/// The category color is shown on the left side of the setting card and the category appears as a tooltip.
/// It is recommended to group similar settings together in the same category to improve usability.
/// </summary>
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
    
    public CategoryAttribute(Category category)
    {
        Name = GetCategoryName(category);
        ColorHex = GetCategoryHexValue(category);
    }
    
    public string? Name { get; }
    
    public string? ColorHex { get; }
    
    private string? GetHexValue(CategoryColor color)
    {
        FieldInfo fieldInfo = typeof(CategoryColor).GetField(color.ToString());
        var attribute = fieldInfo?.GetCustomAttribute<ColorHexAttribute>();
        return attribute?.HexValue;
    }
    
    private string? GetCategoryName(Category category)
    {
        FieldInfo fieldInfo = typeof(Category).GetField(category.ToString());
        var attribute = fieldInfo?.GetCustomAttribute<CategoryNameAttribute>();
        return attribute?.Name;
    }
    
    private string? GetCategoryHexValue(Category category)
    {
        FieldInfo fieldInfo = typeof(Category).GetField(category.ToString());
        var attribute = fieldInfo.GetCustomAttribute<ColorHexAttribute>();
        return attribute?.HexValue;
    }
}

/// <summary>
/// Generic category attribute that allows using custom enum types directly.
/// This enables syntax like [Category&lt;MyCustomCategories&gt;(MyCustomCategories.DatabaseOps)]
/// where MyCustomCategories is a custom enum decorated with CategoryNameAttribute and ColorHexAttribute.
/// </summary>
/// <typeparam name="TEnum">The custom enum type that contains category definitions.</typeparam>
[AttributeUsage(AttributeTargets.Property)]
public class CategoryAttribute<TEnum> : Attribute where TEnum : struct, Enum
{
    public CategoryAttribute(TEnum enumValue)
    {
        Name = CategoryHelper.GetName(enumValue);
        ColorHex = CategoryHelper.GetColorHex(enumValue);
    }
    
    public string? Name { get; }
    
    public string? ColorHex { get; }
}