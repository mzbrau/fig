using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Fig.Client.Attributes;
using Fig.Client.Description;
using Fig.Client.Exceptions;
using Fig.Common.NetStandard.Utils;
using Fig.Contracts;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using NJsonSchema;

namespace Fig.Client;

public class SettingDefinitionFactory : ISettingDefinitionFactory
{
    private readonly IDescriptionProvider _descriptionProvider;

    public SettingDefinitionFactory(IDescriptionProvider descriptionProvider)
    {
        _descriptionProvider = descriptionProvider;
    }
    
    public SettingDefinitionDataContract Create(PropertyInfo settingProperty, bool liveReload)
    {
        var setting = new SettingDefinitionDataContract(settingProperty.Name, string.Empty);
        SetValuesFromAttributes(settingProperty, setting, liveReload);
        return setting;
    }

    private void SetValuesFromAttributes(PropertyInfo settingProperty, SettingDefinitionDataContract setting, bool liveReload)
    {
        foreach (var attribute in settingProperty.GetCustomAttributes(true)
                     .OrderBy(a => a is SettingAttribute))
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
                SetSettingAttribute(settingAttribute, settingProperty, setting, liveReload);
            }
            else if (attribute is LookupTableAttribute lookupTableAttribute)
            {
                setting.LookupTableKey = lookupTableAttribute.LookupTableKey;
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
            else if (attribute is EnablesSettingsAttribute enablesSettingsAttribute)
            {
                setting.EnablesSettings = enablesSettingsAttribute.SettingNames.ToList();
            }
    }

    private void SetSettingAttribute(SettingAttribute settingAttribute, PropertyInfo settingProperty,
        SettingDefinitionDataContract setting, bool liveReload)
    {
        if (settingProperty.PropertyType.IsSupportedBaseType())
        {
            if (NullValueForNonNullableProperty(settingProperty, settingAttribute.DefaultValue))
                throw new InvalidSettingException(
                    $"Property {settingProperty.Name} is non nullable but will be set to a null value. " +
                    "Make the property nullable or set a default value.");

            if (settingProperty.PropertyType.IsEnum())
            {
                ValidateDefaultValueForEnum(settingProperty, settingAttribute.DefaultValue?.ToString());
                SetTypeAndDefaultValue(settingAttribute.DefaultValue?.ToString(), typeof(string));
            }
            else if (settingProperty.PropertyType.IsSecureString())
                SetTypeAndDefaultValue(settingAttribute.DefaultValue?.ToString(), typeof(string));
            else
                SetTypeAndDefaultValue(settingAttribute.DefaultValue, settingProperty.PropertyType);
        }
        else if (settingProperty.PropertyType.IsSupportedDataGridType())
        {
            setting.ValueType = typeof(List<Dictionary<string, object>>);
            var columns = CreateDataGridColumns(settingProperty.PropertyType, setting.ValidValues);
            setting.DataGridDefinition = new DataGridDefinitionDataContract(columns);
            setting.DefaultValue = new DataGridSettingDataContract(null);
        }
        else
        {
            // Custom defined object.
            var schema = JsonSchema.FromType(settingProperty.PropertyType);
            setting.JsonSchema = schema.ToJson();
            setting.ValueType = typeof(string);
        }

        setting.Description = _descriptionProvider.GetDescription(settingAttribute.Description);
        setting.SupportsLiveUpdate = liveReload && settingAttribute.SupportsLiveUpdate;

        void SetTypeAndDefaultValue(object? defaultValue, Type type)
        {
            setting.ValueType = type;
            if (defaultValue != null)
                try
                {
                    var value = Convert.ChangeType(defaultValue, type);
                    setting.DefaultValue = ValueDataContractFactory.CreateContract(value, type);
                }
                catch (Exception)
                {
                    throw new InvalidDefaultValueException(
                        $"Unable to convert default value '{defaultValue}' to type {type.FullName}");
                }
        }
    }

    private bool NullValueForNonNullableProperty(PropertyInfo propertyInfo, object? defaultValue)
    {
        return !IsNullable(propertyInfo) && defaultValue == null;
    }

    private void ValidateDefaultValueForEnum(PropertyInfo property, string? defaultValue)
    {
        var validValues = Enum.GetValues(property.PropertyType);
        var match = false;
        foreach (var val in validValues)
        {
            if (defaultValue == val?.ToString())
                match = true;
        }

        if (!match)
            throw new InvalidDefaultValueException(
                $"Property {property.Name} has default value {defaultValue} " +
                $"which is not valid for type {property.PropertyType}");
    }

    private List<DataGridColumnDataContract> CreateDataGridColumns(Type propertyType, List<string>? parentValidValues)
    {
        var result = new List<DataGridColumnDataContract>();
        if (!ListUtilities.TryGetGenericListType(propertyType, out var genericType))
            return result;

        if (genericType!.IsSupportedBaseType())
            result.Add(new DataGridColumnDataContract("Values", genericType!, parentValidValues));
        else
            foreach (var property in genericType!.GetProperties())
            {
                DataGridColumnDataContract column;
                if (property.PropertyType.IsEnum)
                {
                    var validValues = Enum.GetNames(property.PropertyType).ToList();
                    column = new DataGridColumnDataContract(property.Name, typeof(string), validValues);
                }
                else
                {
                    var validValues = GetValidValues(property);
                    var editorLineCount = GetEditorLineCount(property);
                    column = new DataGridColumnDataContract(
                        property.Name, 
                        property.PropertyType, 
                        validValues?.ToList(),
                        editorLineCount);
                }

                result.Add(column);
            }

        return result;
    }

    private int? GetEditorLineCount(PropertyInfo property)
    {
        var multiLineAttribute = property.GetCustomAttributes(true)
                .FirstOrDefault(a => a is MultiLineAttribute) as MultiLineAttribute;

        return multiLineAttribute?.NumberOfLines;
    }

    private string[]? GetValidValues(PropertyInfo property)
    {
        var validValuesAttribute = property.GetCustomAttributes(true)
            .FirstOrDefault(a => a is ValidValuesAttribute) as ValidValuesAttribute;

        return validValuesAttribute?.Values;
    }

    private static bool IsNullable(PropertyInfo property)
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