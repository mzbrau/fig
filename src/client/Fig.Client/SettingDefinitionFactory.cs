using System;
using System.Linq;
using System.Reflection;
using Fig.Client.Attributes;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Client
{
    public class SettingDefinitionFactory : ISettingDefinitionFactory
    {
        public SettingDefinitionDataContract Create(PropertyInfo settingProperty)
        {
            var setting = new SettingDefinitionDataContract
            {
                Name = settingProperty.Name
            };
            SetValuesFromAttributes(settingProperty, setting);
            return setting;
        }

        private void SetValuesFromAttributes(PropertyInfo settingProperty, SettingDefinitionDataContract setting)
        {
            foreach (var attribute in settingProperty.GetCustomAttributes(true))
                if (attribute is ValidationAttribute validateAttribute)
                {
                    setting.ValidationRegex = validateAttribute.ValidationRegex;
                    setting.ValidationExplanation = validateAttribute.Explanation;
                    setting.ValidationType = validateAttribute.ValidationType;
                }
                else if (attribute is SecretAttribute)
                {
                    setting.IsSecret = true;
                }
                else if (attribute is AdvancedAttribute)
                {
                    setting.Advanced = true;
                }
                else if (attribute is SettingAttribute settingAttribute)
                {
                    setting.Description = settingAttribute.Description;
                    setting.DefaultValue = settingAttribute.DefaultValue is Enum
                        ? settingAttribute.DefaultValue.ToString()
                        : settingAttribute.DefaultValue;
                    setting.ValueType = settingProperty.PropertyType;
                }
                else if (attribute is SettingStringFormatAttribute stringFormatAttribute)
                {
                    setting.StringFormat = stringFormatAttribute.StringFormat;
                }
                else if (attribute is GroupAttribute groupAttribute)
                {
                    setting.Group = groupAttribute.GroupName;
                }
                else if (attribute is ValidValuesAttribute validValuesAttribute)
                {
                    setting.ValidValues = validValuesAttribute.Values?.ToList();
                }
                else if (attribute is DisplayOrderAttribute orderAttribute)
                {
                    setting.DisplayOrder = orderAttribute.DisplayOrder;
                }
                else if (attribute is MultiLineAttribute multiLineAttribute)
                {
                    setting.EditorLineCount = multiLineAttribute.NumberOfLines;
                }
        }
    }
}