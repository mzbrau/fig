using System;
using System.Reflection;
using Fig.Client.Attributes;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.SettingTypes;

namespace Fig.Client
{
    public class SettingDefinitionFactory : ISettingDefinitionFactory
    {
        public ISettingDefinition Create(PropertyInfo settingProperty)
        {
            var setting = CreateSetting(settingProperty);
            SetValuesFromAttributes(settingProperty, setting);
            return setting;
        }

        private ISettingDefinition CreateSetting(PropertyInfo settingProperty)
        {
            var type = settingProperty.PropertyType;
            return type switch
            {
                { } when type == typeof(string) => new SettingDefinitionDataContract<StringType>(),
                { } when type == typeof(int) => new SettingDefinitionDataContract<IntType>(),
                _ => throw new ArgumentOutOfRangeException("Unsupported setting type")
            };
        }

        private void SetValuesFromAttributes(PropertyInfo settingProperty, ISettingDefinition setting)
        {
            foreach (var attribute in settingProperty.GetCustomAttributes(true))
            {
                if (attribute is ValidateAttribute validateAttribute)
                {
                    setting.ValidationRegex = validateAttribute.ValidationRegex;
                    setting.ValidationExplanation = validateAttribute.Explanation;
                }
                else if (attribute is SecretAttribute)
                {
                    setting.IsSecret = true;
                }
                else if (attribute is DescriptionAttribute descriptionAttribute)
                {
                    setting.Description = descriptionAttribute.Description;
                }
                else if (attribute is FriendlyNameAttribute friendlyNameAttribute)
                {
                    setting.FriendlyName = friendlyNameAttribute.FriendlyName;
                }
                else if (attribute is GroupAttribute groupAttribute)
                {
                    setting.Group = groupAttribute.GroupName;
                }
                else if (attribute is DefaultValueAttribute defaultValueAttribute)
                {
                    setting.DefaultValue = defaultValueAttribute.Value;
                }
            }
        }
    }
}