using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
using Newtonsoft.Json;
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

    public List<CustomConfigurationSection> GetConfigurationSections(SettingDetails settingDetails)
    {
        var configurationSectionAttributes = settingDetails.Property.GetCustomAttributes<ConfigurationSectionOverride>().ToList();
        
        // If no configuration section attributes are explicitly included, check if the setting is in a path structure
        if (!configurationSectionAttributes.Any())
        {
            if (!string.IsNullOrEmpty(settingDetails.Path))
            {
                return [new CustomConfigurationSection(settingDetails.Path, null)];
            }
            
            return [];
        }
        
        // Otherwise, return a list of all configuration section overrides defined for this setting
        return configurationSectionAttributes
            .Select(a => new CustomConfigurationSection(a.SectionName, a.SettingNameOverride))
            .ToList();
    }

    private void SetValuesFromAttributes(SettingDetails settingDetails,
        SettingDefinitionDataContract setting)
    {
        // Get validation attribute from property
        var propertyValidationAttribute = settingDetails.Property.GetCustomAttributes(true)
            .FirstOrDefault(a => a is ValidationAttribute) as ValidationAttribute;

        // Get class-level validation attributes that apply to this property's type
        var classValidationAttributes = settingDetails.Property.DeclaringType?.GetCustomAttributes(true)
            .Where(a => a is ValidationAttribute)
            .Cast<ValidationAttribute>()
            .Where(va => va.ApplyToTypes?.Any(t => t == settingDetails.Property.PropertyType || 
                                                 (Nullable.GetUnderlyingType(settingDetails.Property.PropertyType) != null && 
                                                  t == Nullable.GetUnderlyingType(settingDetails.Property.PropertyType))) ?? false)
            .ToList() ?? [];

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
            else if (attribute is EnvironmentSpecificAttribute)
            {
                setting.EnvironmentSpecific = true;
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
            else if (attribute is IDisplayScriptProvider displayScriptProvider)
            {
                setting.DisplayScript = displayScriptProvider.GetScript(setting.Name);
            }

        // Apply class-level validation if no property-level validation exists
        if (propertyValidationAttribute == null && classValidationAttributes.Any())
        {
            // Apply the first matching class-level validation attribute
            SetValidation(classValidationAttributes.First(), setting);
        }
    }

    private void SetValidation(ValidationAttribute validateAttribute, SettingDefinitionDataContract setting)
    {
        if (validateAttribute.ValidationType == ValidationType.None)
        {
            setting.ValidationRegex = null;
            setting.ValidationExplanation = null;
        }
        else if (validateAttribute.ValidationType != ValidationType.Custom)
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
                SetTypeAndDefaultValue(Convert.ToString(settingDetails.DefaultValue, CultureInfo.InvariantCulture), typeof(string));
                
                if (!settingDetails.Property.PropertyType.IsEnum && setting.ValidValues is not null) // It is nullable
                {
                    setting.ValidValues.Insert(0, Constants.EnumNullPlaceholder);
                }
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
            var defaultVal = settingDetails.Property.GetDefaultValue(settingAttribute, settingDetails.ParentInstance);
            if (defaultVal is not null)
            {
                setting.DefaultValue =
                    new StringSettingDataContract(JsonConvert.SerializeObject(defaultVal,
                        new JsonSerializerSettings()
                        {
                            Culture = CultureInfo.InvariantCulture,
                            Formatting = Formatting.Indented,
                        }));
            }
        }

        setting.Description = _descriptionProvider.GetDescription(settingAttribute.Description);
        if (string.IsNullOrWhiteSpace(setting.Description))
        {
            var validResourceKeys = _descriptionProvider.GetAllMarkdownResourceKeys();
            throw new InvalidSettingException($"Setting {setting.Name} is missing a description. " +
                                              $"Valid resource keys are: {string.Join(", ", validResourceKeys)}");
        }
        
        setting.SupportsLiveUpdate = settingAttribute.SupportsLiveUpdate;
        setting.Classification = settingAttribute.Classification;

        void SetTypeAndDefaultValue(object? defaultVal, Type type)
        {
            setting.ValueType = type;
            if (defaultVal != null)
            {
                try
                {
                    object? value;
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        var underlyingType = Nullable.GetUnderlyingType(type);
                        var convertedValue = Convert.ChangeType(defaultVal, underlyingType!, CultureInfo.InvariantCulture);
                        value = Activator.CreateInstance(type, convertedValue);
                    }
                    else
                    {
                        value = Convert.ChangeType(defaultVal, type, CultureInfo.InvariantCulture);
                    }
                    
                    setting.DefaultValue = ValueDataContractFactory.CreateContract(value, type);
                }
                catch (Exception ex)
                {
                    throw new InvalidDefaultValueException(
                        $"Unable to convert default value '{defaultVal}' to type {type.FullName}", ex);
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
        var validValues = GetEnumValues(property.PropertyType);
        
        if (defaultValue != null && !validValues.Contains(defaultValue))
            throw new InvalidDefaultValueException(
                $"Property {property.Name} has default value {defaultValue} " +
                $"which is not valid for type {property.PropertyType}");
    }
    
    private List<string> GetEnumValues(Type type)
    {
        var enumType = Nullable.GetUnderlyingType(type) ?? type;
        var validValues = Enum.GetNames(enumType).ToList();
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            validValues.Insert(0, Constants.EnumNullPlaceholder);
        }

        return validValues;
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
            foreach (var property in genericType!.GetProperties(
                             BindingFlags.Public | BindingFlags.Instance)
                                       .Where(p => p.GetGetMethod() != null &&
                                                              p.GetSetMethod() != null))
            {
                var ignore = GetIsIgnore(property);
                if (ignore)
                    continue;
                
                DataGridColumnDataContract column;
                if (property.PropertyType.IsEnum())
                {
                    var validValues = GetEnumValues(property.PropertyType);
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
    
    private bool GetIsIgnore(PropertyInfo property)
    {
        var ignoreAttribute = property.GetCustomAttributes(true)
            .FirstOrDefault(a => a is FigIgnoreAttribute) as FigIgnoreAttribute;

        return ignoreAttribute != null;
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