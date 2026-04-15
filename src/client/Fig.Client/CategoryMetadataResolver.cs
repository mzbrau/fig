using System;
using System.Linq;
using Fig.Client.Abstractions.Attributes;

namespace Fig.Client;

internal static class CategoryMetadataResolver
{
    public static bool IsCategoryAttribute(Attribute attribute)
    {
        return attribute is CategoryAttribute || IsGenericCategoryAttributeType(attribute.GetType());
    }

    public static bool TryGetCategoryMetadata(Attribute attribute, out CategoryMetadata metadata)
    {
        if (attribute is CategoryAttribute categoryAttribute)
        {
            metadata = new CategoryMetadata(categoryAttribute.Name, categoryAttribute.ColorHex);
            return true;
        }

        if (IsGenericCategoryAttributeType(attribute.GetType()))
        {
            var nameProperty = attribute.GetType().GetProperty("Name");
            var colorProperty = attribute.GetType().GetProperty("ColorHex");

            metadata = new CategoryMetadata(
                nameProperty?.GetValue(attribute) as string,
                colorProperty?.GetValue(attribute) as string);
            return true;
        }

        metadata = default;
        return false;
    }

    public static CategoryMetadata? GetEffectiveCategory(SettingDetails settingDetails)
    {
        CategoryMetadata? effectiveCategory = null;

        foreach (var attribute in settingDetails.InheritedAttributes
                     .Concat(settingDetails.Property.GetCustomAttributes(true).Cast<Attribute>()))
        {
            if (TryGetCategoryMetadata(attribute, out var categoryMetadata))
            {
                effectiveCategory = categoryMetadata;
            }
        }

        return effectiveCategory;
    }

    private static bool IsGenericCategoryAttributeType(Type attributeType)
    {
        return attributeType.IsGenericType &&
               attributeType.GetGenericTypeDefinition() == typeof(CategoryAttribute<>);
    }
}

internal readonly struct CategoryMetadata
{
    public CategoryMetadata(string? name, string? colorHex)
    {
        Name = name;
        ColorHex = colorHex;
    }

    public string? Name { get; }

    public string? ColorHex { get; }
}
