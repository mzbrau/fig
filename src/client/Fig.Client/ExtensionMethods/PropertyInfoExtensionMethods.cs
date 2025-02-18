using System;
using System.Collections;
using Fig.Client.Attributes;
using System.Reflection;
using Fig.Client.Exceptions;

namespace Fig.Client.ExtensionMethods;

public static class PropertyInfoExtensionMethods
{
    public static object? GetDefaultValue(this PropertyInfo propertyInfo, SettingAttribute attribute, object parent)
    {
        var defaultFromMethod = attribute.GetDefaultValue(parent);

        if (defaultFromMethod is not null)
        {
            return defaultFromMethod;
        }

        var defaultFromProperty = propertyInfo.GetValue(parent);

        if (defaultFromProperty is not null && IsCollection(propertyInfo.PropertyType))
            throw new InvalidDefaultValueException(
                $"The default value for {propertyInfo.Name} should be set via {nameof(SettingAttribute.DefaultValueMethodName)} in the {nameof(SettingAttribute)}");
        
        return defaultFromProperty;
    }

    private static bool IsCollection(Type type)
    {
        // Check if the type implements IEnumerable, and is not a string
        return typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string);
    }
}