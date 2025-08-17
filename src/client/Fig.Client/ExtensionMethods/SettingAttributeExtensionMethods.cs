using System.Linq;
using System.Reflection;
using Fig.Client.Abstractions.Attributes;

namespace Fig.Client.ExtensionMethods;

internal static class SettingAttributeExtensionMethods
{
    public static object? GetDefaultValue(this SettingAttribute attribute, object settings)
    {
        if (attribute.DefaultValueMethodName is not null)
        {
            var staticMethods = settings.GetType().GetMethods(BindingFlags.Public | BindingFlags.Static);
            var match = staticMethods.FirstOrDefault(a => a.Name == attribute.DefaultValueMethodName);
            if (match is not null)
            {
                return match.Invoke(null, null);
            }
        }

        return null;
    }
}