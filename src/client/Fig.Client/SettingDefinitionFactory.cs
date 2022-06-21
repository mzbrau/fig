using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Fig.Client.Attributes;
using Fig.Client.Exceptions;
using Fig.Client.ExtensionMethods;
using Fig.Contracts.SettingDefinitions;
using NJsonSchema;

namespace Fig.Client;

public class SettingDefinitionFactory : ISettingDefinitionFactory
{
    private const string ValuesColumnName = "Values";
    
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
        foreach (var attribute in settingProperty.GetCustomAttributes(true)
                     .OrderByDescending(attribute => attribute is SettingAttribute))
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
            else if (attribute is CommonEnumerationAttribute commonEnumerationAttribute)
            {
                setting.CommonEnumerationKey = commonEnumerationAttribute.CommonEnumerationKey;
            }
            else if (attribute is GroupAttribute groupAttribute)
            {
                setting.Group = groupAttribute.GroupName;
            }
            else if (attribute is ValidValuesAttribute validValuesAttribute)
            {
                setting.ValidValues = validValuesAttribute.Values?.ToList();
                var valuesColumn = setting.DataGridDefinition?.Columns.FirstOrDefault(a => a.Name == ValuesColumnName);
                if (valuesColumn != null)
                {
                    valuesColumn.ValidValues = validValuesAttribute.Values?.ToList();
                    valuesColumn.Type = typeof(string);
                }
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
        if (settingProperty.PropertyType.IsSupportedBaseType())
        {
            if (NullValueForNonNullableProperty(settingProperty, settingAttribute.DefaultValue))
                throw new InvalidSettingException(
                    $"Property {settingProperty.Name} is non nullable but will be set to a null value. " +
                    "Make the property nullable or set a default value.");

            if (settingProperty.PropertyType.IsEnum() || settingProperty.PropertyType.IsSecureString())
                SetTypeAndDefaultValue(settingAttribute.DefaultValue?.ToString(), typeof(string));
            else
                SetTypeAndDefaultValue(settingAttribute.DefaultValue, settingProperty.PropertyType);
        }
        else if (settingProperty.PropertyType.IsSupportedDataGridType())
        {
            setting.ValueType = typeof(List<Dictionary<string, object>>);
            var columns = CreateDataGridColumns(settingProperty.PropertyType);
            setting.DataGridDefinition = new DataGridDefinitionDataContract(columns);
            // TODO: setting.DefaultValue =
        }
        else
        {
            // Custom defined object.
            var schema = JsonSchema.FromType(settingProperty.PropertyType);
            setting.JsonSchema = schema.ToJson();
            setting.ValueType = typeof(string);
        }

        setting.Description = settingAttribute.Description;

        void SetTypeAndDefaultValue(object? defaultValue, Type type)
        {
            setting.ValueType = type;
            if (defaultValue != null)
                try
                {
                    setting.DefaultValue = Convert.ChangeType(defaultValue, type);
                }
                catch (Exception)
                {
                    throw new InvalidDefaultValueException(
                        $"Unable to convert default value '{defaultValue}' to type {type.FullName}");
                }
        }
    }

    private bool NullValueForNonNullableProperty(PropertyInfo propertyInfo, object defaultValue)
    {
        return !IsNullable(propertyInfo) && defaultValue == null;
    }

    private List<DataGridColumnDataContract> CreateDataGridColumns(Type propertyType)
    {
        var result = new List<DataGridColumnDataContract>();
        var genericType = propertyType.GetGenericArguments().First();

        if (genericType.IsSupportedBaseType())
            result.Add(new DataGridColumnDataContract(ValuesColumnName, genericType));
        else
            foreach (var property in genericType.GetProperties())
            {
                DataGridColumnDataContract column;
                if (property.PropertyType.IsEnum)
                {
                    var validValues = Enum.GetNames(property.PropertyType).ToList();
                    column = new DataGridColumnDataContract(property.Name, typeof(string), validValues);
                }
                else
                {
                    column = new DataGridColumnDataContract(property.Name, property.PropertyType);
                }

                result.Add(column);
            }

        return result;
    }

    public static bool IsNullable(PropertyInfo property)
    {
        return IsNullableHelper(property.PropertyType, property.DeclaringType, property.CustomAttributes);
    }


    private static bool IsNullableHelper(Type memberType, MemberInfo? declaringType,
        IEnumerable<CustomAttributeData> customAttributes)
    {
        if (memberType.IsValueType)
            return Nullable.GetUnderlyingType(memberType) != null;

        var nullable = customAttributes
            .FirstOrDefault(x => x.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute");
        if (nullable != null && nullable.ConstructorArguments.Count == 1)
        {
            var attributeArgument = nullable.ConstructorArguments[0];
            if (attributeArgument.ArgumentType == typeof(byte[]))
            {
                var args = (ReadOnlyCollection<CustomAttributeTypedArgument>) attributeArgument.Value!;
                if (args.Count > 0 && args[0].ArgumentType == typeof(byte))
                    return (byte) args[0].Value! == 2;
            }
            else if (attributeArgument.ArgumentType == typeof(byte))
            {
                return (byte) attributeArgument.Value! == 2;
            }
        }

        for (var type = declaringType; type != null; type = type.DeclaringType)
        {
            var context = type.CustomAttributes
                .FirstOrDefault(x =>
                    x.AttributeType.FullName == "System.Runtime.CompilerServices.NullableContextAttribute");
            if (context != null &&
                context.ConstructorArguments.Count == 1 &&
                context.ConstructorArguments[0].ArgumentType == typeof(byte))
                return (byte) context.ConstructorArguments[0].Value! == 2;
        }

        // Couldn't find a suitable attribute
        return false;
    }
}