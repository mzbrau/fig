using Fig.Client.Attributes;
using System.Reflection;
using System;
using System.Linq;

namespace Fig.Client.ExtensionMethods;

public static class SettingBaseExtensionMethods
{
    /// <summary>
    /// For most default values, they are just set in the class but because of the way configuration providers work,
    /// Microsoft will just append values to collections rather than replacing them. As a result, default values for
    /// collections are handled by an attribute and this value must be applied here rather than in the class.
    /// </summary>
    public static void OverrideCollectionDefaultValues(this SettingsBase settings)
    {
        var properties = settings.GetType().GetProperties()
            .Where(prop => HasSettingAttribute(prop) && HasSettingAttributeWithDefaultMethodName(prop));

        foreach (var property in properties)
        {
            if (property.GetValue(settings) is not null)
                continue;
            
            var settingAttribute =
                (SettingAttribute)property.GetCustomAttributes(true).First(a => a is SettingAttribute);
            var defaultValue = property.GetDefaultValue(settingAttribute, settings);

            if (defaultValue is not null)
                property.SetValue(settings, defaultValue);
        }

        bool HasSettingAttribute(PropertyInfo propertyInfo)
        {
            return Attribute.IsDefined(propertyInfo, typeof(SettingAttribute));
        }

        bool HasSettingAttributeWithDefaultMethodName(PropertyInfo propertyInfo)
        {
            return ((SettingAttribute)propertyInfo.GetCustomAttributes(true).First(a => a is SettingAttribute))
                .DefaultValueMethodName is not null;
        }
    }
}