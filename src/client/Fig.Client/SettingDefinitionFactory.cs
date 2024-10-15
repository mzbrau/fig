using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Fig.Client.Attributes;
using Fig.Client.Configuration;
using Fig.Client.DefaultValue;
using Fig.Client.Description;
using Fig.Client.Exceptions;
using Fig.Client.ExtensionMethods;
using Fig.Client.Validation;
using Fig.Common.NetStandard.Utils;
using Fig.Contracts;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using NJsonSchema;

namespace Fig.Client;

internal class SettingDefinitionFactory : ISettingDefinitionFactory
{
    private readonly IDescriptionProvider _descriptionProvider;
    private readonly IDataGridDefaultValueProvider _dataGridDefaultValueProvider;

    public SettingDefinitionFactory(IDescriptionProvider descriptionProvider, IDataGridDefaultValueProvider dataGridDefaultValueProvider)
    {
        _descriptionProvider = descriptionProvider;
        _dataGridDefaultValueProvider = dataGridDefaultValueProvider;
    }
    
    public SettingDefinitionDataContract Create(SettingDetails settingDetails, int displayOrder)
    {
        var setting = new SettingDefinitionDataContract(settingDetails.Name, string.Empty);
        SetValuesFromAttributes(settingDetails, setting);
        setting.DisplayOrder = displayOrder;
        return setting;
    }

    public CustomConfigurationSection GetConfigurationSection(SettingDetails settingDetails)
    {
        var configurationSectionAttribute = settingDetails.Property.GetCustomAttribute<ConfigurationSectionOverride>();

        // If a configuration section attribute is explicitly included, use that. Otherwise, look at the nested settings.
        return string.IsNullOrWhiteSpace(configurationSectionAttribute?.SectionName)
            ? new CustomConfigurationSection(settingDetails.Path, null)
            : new CustomConfigurationSection(configurationSectionAttribute?.SectionName ?? string.Empty, configurationSectionAttribute?.SettingNameOverride);
    }

    private void SetValuesFromAttributes(SettingDetails settingDetails,
        SettingDefinitionDataContract setting)
    {
        foreach (var attribute in settingDetails.Property.GetCustomAttributes(true)
                     .OrderBy(a => a is SettingAttribute))
            if (attribute is ValidationAttribute validateAttribute)
            {
                SetValidation(validateAttribute, setting);
            }
            else if (attribute is SecretAttribute)
            {
                ThrowIfNotString(settingDetails.Property);
                setting.IsSecret = true;
            }
            else if (attribute is AdvancedAttribute)
            {
                setting.Advanced = true;
            }
            else if (attribute is SettingAttribute settingAttribute)
            {
                SetSettingAttribute(settingAttribute, settingDetails, setting);
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
            else if (attribute is MultiLineAttribute multiLineAttribute)
            {
                setting.EditorLineCount = multiLineAttribute.NumberOfLines;
            }
            else if (attribute is EnablesSettingsAttribute enablesSettingsAttribute)
            {
                setting.EnablesSettings = enablesSettingsAttribute.SettingNames.ToList();
            }
            else if (attribute is CategoryAttribute categoryAttribute)
            {
                setting.CategoryName = categoryAttribute.Name;
                setting.CategoryColor = categoryAttribute.ColorHex;
            }
            else if (attribute is DisplayScriptAttribute scriptAttribute)
            {
                setting.DisplayScript = scriptAttribute.DisplayScript;
            }
    }

    private void SetValidation(ValidationAttribute validateAttribute, SettingDefinitionDataContract setting)
    {
        if (validateAttribute.ValidationType != ValidationType.Custom)
        {
            var definition = validateAttribute.ValidationType.GetDefinition();
            setting.ValidationRegex = definition.Regex;
            setting.ValidationExplanation = definition.Explanation;
        }
        else
        {
            setting.ValidationRegex = validateAttribute.ValidationRegex;
            setting.ValidationExplanation = validateAttribute.Explanation;
        }
    }

    private void ThrowIfNotString(PropertyInfo settingProperty)
    {
        if (settingProperty.PropertyType.FigPropertyType() != FigPropertyType.String)
            throw new InvalidSettingException(
                $"'{settingProperty.Name}' is misconfigured. Secrets can only be applied to strings.");
    }

    private void SetSettingAttribute(SettingAttribute settingAttribute, SettingDetails settingDetails,
        SettingDefinitionDataContract setting)
    {
        if (settingDetails.Property.PropertyType.IsSupportedBaseType())
        {
            if (NullValueForNonNullableProperty(settingDetails.Property, settingDetails.DefaultValue))
                throw new InvalidSettingException(
                    $"Property {settingDetails.Property.Name} is non nullable but will be set to a null value. " +
                    "Make the property nullable or set a default value.");
            
            if (settingDetails.Property.PropertyType.IsEnum())
            {
                ValidateDefaultValueForEnum(settingDetails.Property, settingDetails.DefaultValue?.ToString());
                SetTypeAndDefaultValue(settingDetails.DefaultValue?.ToString(), typeof(string));
            }
            else
                SetTypeAndDefaultValue(settingDetails.DefaultValue, settingDetails.Property.PropertyType);
        }
        else if (settingDetails.Property.PropertyType.IsSupportedDataGridType())
        {
            setting.ValueType = typeof(List<Dictionary<string, object>>);
            var columns = CreateDataGridColumns(settingDetails.Property.PropertyType, setting.ValidValues);
            var isLocked = GetIsLocked(settingDetails.Property);
            setting.DataGridDefinition = new DataGridDefinitionDataContract(columns, isLocked);
            var dataGridDefault = _dataGridDefaultValueProvider.Convert(settingDetails.Property.GetDefaultValue(settingAttribute, settingDetails.ParentInstance), columns);
            setting.DefaultValue = new DataGridSettingDataContract(dataGridDefault);
        }
        else
        {
            // Custom defined object.
            var schema = JsonSchema.FromType(settingDetails.Property.PropertyType);
            setting.JsonSchema = schema.ToJson();
            setting.ValueType = typeof(string);
        }

        setting.Description = _descriptionProvider.GetDescription(settingAttribute.Description);
        if (string.IsNullOrWhiteSpace(setting.Description))
        {
            var validResourceKeys = _descriptionProvider.GetAllMarkdownResourceKeys();
            throw new InvalidSettingException($"Setting {setting.Name} is missing a description. " +
                                              $"Valid resource keys are: {string.Join(", ", validResourceKeys)}");
        }
        
        setting.SupportsLiveUpdate = settingAttribute.SupportsLiveUpdate;

        void SetTypeAndDefaultValue(object? defaultVal, Type type)
        {
            setting.ValueType = type;
            if (defaultVal != null)
            {
                try
                {
                    var value = Convert.ChangeType(defaultVal, type);
                    setting.DefaultValue = ValueDataContractFactory.CreateContract(value, type);
                }
                catch (Exception)
                {
                    throw new InvalidDefaultValueException(
                        $"Unable to convert default value '{defaultVal}' to type {type.FullName}");
                }
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
        {
            // List<string> or similar
            result.Add(new DataGridColumnDataContract("Values", genericType!, parentValidValues));
        }
        else
        {
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
                    var isReadOnly = GetIsReadOnly(property);
                    var validation = GetValidation(property);
                    var isSecret = GetIsSecret(property);

                    if (isSecret && property.PropertyType != typeof(string))
                    {
                        throw new InvalidSettingException(
                            $"'{property.Name}' inside list property is misconfigured. Secrets can only be applied to strings.");
                    }

                    if (property.PropertyType.IsEnumerableType())
                    {
                        if (property.PropertyType == typeof(List<string>))
                        {
                            if (validValues?.Any() != true)
                            {
                                throw new InvalidSettingException(
                                    $"'{property.Name}' inside list property is misconfigured. " +
                                    $"String collections must have valid values set.");
                            }
                        }
                        else
                        {
                            throw new InvalidSettingException(
                                $"'{property.Name}' inside list property is misconfigured. " +
                                $"Only string lists with valid values are supported.");
                        }
                    }
                    
                    
                    
                    column = new DataGridColumnDataContract(
                        property.Name, 
                        property.PropertyType, 
                        validValues?.ToList(),
                        editorLineCount,
                        isReadOnly,
                        validation.Regex,
                        validation.Explanation,
                        isSecret);
                }

                result.Add(column);
            }
        }
            

        return result;
    }

    private bool GetIsSecret(PropertyInfo property)
    {
        var isSecretAttribute = property.GetCustomAttributes(true)
            .FirstOrDefault(a => a is SecretAttribute) as SecretAttribute;

        return isSecretAttribute != null;
    }

    private bool GetIsLocked(PropertyInfo property)
    {
        var dataGridLockedAttribute = property.GetCustomAttributes(true)
            .FirstOrDefault(a => a is DataGridLockedAttribute) as DataGridLockedAttribute;

        return dataGridLockedAttribute != null;
    }

    private bool GetIsReadOnly(PropertyInfo property)
    {
        var readOnlyAttribute = property.GetCustomAttributes(true)
            .FirstOrDefault(a => a is ReadOnlyAttribute) as ReadOnlyAttribute;

        return readOnlyAttribute != null;
    }
    
    private (string? Regex, string? Explanation) GetValidation(PropertyInfo property)
    {
        var validationAttribute = property.GetCustomAttributes(true)
            .FirstOrDefault(a => a is ValidationAttribute) as ValidationAttribute;

        if (validationAttribute == null)
        {
            return (null, null);
        }

        if (validationAttribute.ValidationType != ValidationType.Custom)
        {
            return validationAttribute.ValidationType.GetDefinition();
        }
        
        return (validationAttribute.ValidationRegex, validationAttribute.Explanation);
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
        if (nullable is { ConstructorArguments.Count: 1 })
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