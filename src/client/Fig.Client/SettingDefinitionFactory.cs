using System.Linq;
using System.Reflection;
using Fig.Client.Attributes;
using Fig.Client.ExtensionMethods;
using Fig.Contracts.SettingDefinitions;
using NJsonSchema;

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
                    SetSettingAttribute(settingAttribute, settingProperty, setting);
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

        private void SetSettingAttribute(SettingAttribute settingAttribute, PropertyInfo settingProperty,
            SettingDefinitionDataContract setting)
        {
            if (settingProperty.PropertyType.IsFigSupported())
            {
                if (settingProperty.PropertyType.IsEnum())
                {
                    setting.ValueType = typeof(string);
                    setting.DefaultValue = settingAttribute.DefaultValue?.ToString();
                }
                else
                {
                    setting.ValueType = settingProperty.PropertyType;
                    setting.DefaultValue = settingAttribute.DefaultValue;
                }
            }
            else
            {
                // Custom defined object.
                var schema = JsonSchema.FromType(settingProperty.PropertyType);
                setting.JsonSchema = schema.ToJson();
                setting.ValueType = typeof(string);
            }

            setting.Description = settingAttribute.Description;
        }
    }
}