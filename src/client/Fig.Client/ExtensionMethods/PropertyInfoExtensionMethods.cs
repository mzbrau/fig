using System.Reflection;

namespace Fig.Client.ExtensionMethods;

public static class PropertyInfoExtensionMethods
{
    public static object? GetDefaultValue(this PropertyInfo propertyInfo, object parent)
    {
        return propertyInfo.GetValue(parent);
    }
}