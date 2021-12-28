using System.Reflection;
using Fig.Client.Attributes;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Client
{
    public class SettingDefinitionFactory : ISettingDefinitionFactory
    {
        public SettingDefinitionDataContract Create(PropertyInfo settingProperty)
        {
            var setting = new SettingDefinitionDataContract()
            {
                Name = settingProperty.Name
            };
            SetValuesFromAttributes(settingProperty, setting);
            return setting;
        }

        private void SetValuesFromAttributes(PropertyInfo settingProperty, SettingDefinitionDataContract setting)
        {
            foreach (var attribute in settingProperty.GetCustomAttributes(true))
            {
                if (attribute is ValidationAttribute validateAttribute)
                {
                    setting.ValidationRegex = validateAttribute.ValidationRegex;
                    setting.ValidationExplanation = validateAttribute.Explanation;
                }
                else if (attribute is SecretAttribute)
                {
                    setting.IsSecret = true;
                }
                else if (attribute is SettingDescriptionAttribute descriptionAttribute)
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