using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Fig.Client.Abstractions.Attributes;
using Fig.Client.Abstractions.Enums;
using Fig.Client.Abstractions.ExtensionMethods;
using Fig.Client.Abstractions.Validation;
using Fig.Client.Configuration;
using Fig.Client.DefaultValue;
using Fig.Client.Description;
using Fig.Client.Exceptions;
using Fig.Client.ExtensionMethods;
using Fig.Common.NetStandard.ExtensionMethods;
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

    public SettingDefinitionDataContract Create(SettingDetails settingDetails, string clientName, int displayOrder, List<SettingDetails> allSettings, bool automaticallyGenerateHeadings = true)
    {
        var setting = new SettingDefinitionDataContract(settingDetails.Name, string.Empty);
        SetValuesFromAttributes(settingDetails, clientName, setting, allSettings);
        
        // Apply automatic heading generation if enabled
        ApplyAutomaticHeadingGeneration(settingDetails, allSettings, setting, automaticallyGenerateHeadings);
        
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
        string clientName,
        SettingDefinitionDataContract setting,
        List<SettingDetails>? allSettings = null)
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

        // Process inherited attributes first (so they can be overridden by direct attributes)
        foreach (var inheritedAttribute in settingDetails.InheritedAttributes)
        {
            ProcessAttributeForSetting(inheritedAttribute, settingDetails, clientName, setting, allSettings);
        }

        // Process all attributes except HeadingAttribute first
        foreach (var attribute in settingDetails.Property.GetCustomAttributes(true)
                     .Cast<Attribute>()
                     .Where(a => !(a is HeadingAttribute))
                     .OrderBy(a => a is SettingAttribute))
        {
            ProcessAttributeForSetting(attribute, settingDetails, clientName, setting, allSettings);
        }

        // Process HeadingAttribute last so it can inherit final values from other attributes
        var headingAttribute = settingDetails.Property.GetCustomAttribute<HeadingAttribute>();
        if (headingAttribute != null)
        {
            // Validate heading text
            ValidateHeadingAttribute(headingAttribute, settingDetails.Name);
            
            // Create the heading data contract with inherited values
            var headingColor = headingAttribute.Color ?? setting.CategoryColor;
            var headingAdvanced = setting.Advanced;
            
            setting.Heading = new HeadingDataContract(
                headingAttribute.Text,
                headingColor,
                headingAdvanced);
        }

        // Apply class-level validation if no property-level validation exists
        if (propertyValidationAttribute == null && classValidationAttributes.Any())
        {
            // Apply the first matching class-level validation attribute
            SetValidation(classValidationAttributes.First(), setting);
        }
    }

    private void ProcessAttributeForSetting(Attribute attribute, SettingDetails settingDetails, string clientName, SettingDefinitionDataContract setting, List<SettingDetails>? allSettings)
    {
        switch (attribute)
        {
            case ValidationAttribute validateAttribute:
                ValidateValidationAttribute(validateAttribute, settingDetails.Name);
                SetValidation(validateAttribute, setting);
                break;
            case ValidateIsBetweenAttribute validateIsBetweenAttribute:
                ValidateIsBetweenAttributeValues(validateIsBetweenAttribute, settingDetails.Name);
                setting.DisplayScript = validateIsBetweenAttribute.GetScript(setting.Name);
                break;
            case ValidateCountAttribute validateCountAttribute:
                ValidateCountAttributeValues(validateCountAttribute, settingDetails.Name);
                setting.DisplayScript = validateCountAttribute.GetScript(setting.Name);
                break;
            case SecretAttribute:
                ThrowIfNotString(settingDetails.Property);
                setting.IsSecret = true;
                break;
            case AdvancedAttribute:
                setting.Advanced = true;
                break;
            case SettingAttribute settingAttribute:
                SetSettingAttribute(settingAttribute, settingDetails, setting);
                break;
            case LookupTableAttribute lookupTableAttribute:
                if (lookupTableAttribute.LookupSource == LookupSource.ProviderDefined)
                {
                    setting.LookupTableKey = $"{clientName}:{lookupTableAttribute.LookupTableKey}";
                }
                else
                {
                    setting.LookupTableKey = lookupTableAttribute.LookupTableKey;
                }

                ValidateKeySettingExists(lookupTableAttribute, setting, allSettings, settingDetails);
                
                setting.LookupKeySettingName = lookupTableAttribute.KeySettingName;
                break;
            case GroupAttribute groupAttribute:
                setting.Group = groupAttribute.GroupName;
                break;
            case ValidValuesAttribute validValuesAttribute:
                setting.ValidValues = validValuesAttribute.Values?.ToList();
                break;
            case MultiLineAttribute multiLineAttribute:
                setting.EditorLineCount = multiLineAttribute.NumberOfLines;
                break;
#pragma warning disable CS0618 // Type or member is obsolete
            case EnablesSettingsAttribute enablesSettingsAttribute:
#pragma warning restore CS0618 // Type or member is obsolete
                setting.EnablesSettings = enablesSettingsAttribute.SettingNames.ToList();
                break;
            case EnvironmentSpecificAttribute:
                setting.EnvironmentSpecific = true;
                break;
            case InitOnlyExportAttribute:
                setting.InitOnlyExport = true;
                break;
            case CategoryAttribute categoryAttribute:
                ValidateCategoryAttribute(categoryAttribute, settingDetails.Name);
                setting.CategoryName = categoryAttribute.Name;
                setting.CategoryColor = categoryAttribute.ColorHex;
                break;
            case { } genericCategoryAttribute when genericCategoryAttribute.GetType().IsGenericType && 
                                                   genericCategoryAttribute.GetType().GetGenericTypeDefinition() == typeof(CategoryAttribute<>):
                // Handle CategoryAttribute<TEnum>
                var nameProperty = genericCategoryAttribute.GetType().GetProperty("Name");
                var colorProperty = genericCategoryAttribute.GetType().GetProperty("ColorHex");
                var categoryName = nameProperty?.GetValue(genericCategoryAttribute) as string;
                var categoryColor = colorProperty?.GetValue(genericCategoryAttribute) as string;

                ValidateGenericCategoryAttribute(categoryColor, settingDetails.Name);

                setting.CategoryName = categoryName;
                setting.CategoryColor = categoryColor;
                break;
            case DisplayScriptAttribute scriptAttribute:
                setting.DisplayScript = scriptAttribute.DisplayScript;
                break;
            case IDisplayScriptProvider displayScriptProvider:
                setting.DisplayScript = displayScriptProvider.GetScript(setting.Name);
                break;
            case IndentAttribute indentAttribute:
                ValidateIndentAttribute(indentAttribute, settingDetails.Name);
                setting.Indent = indentAttribute.Level;
                break;
            case DependsOnAttribute dependsOnAttribute:
                ValidateDependsOnAttribute(dependsOnAttribute, settingDetails.Name, allSettings);
                
                setting.DependsOnProperty = dependsOnAttribute.DependsOnProperty;
                setting.DependsOnValidValues = dependsOnAttribute.ValidValues.ToList();
                
                // Automatically increment indent level by 1
                setting.Indent = (setting.Indent ?? 0) + 1;
                break;
        }
    }

    private void ValidateKeySettingExists(LookupTableAttribute lookupTableAttribute, SettingDefinitionDataContract setting, List<SettingDetails>? allSettings, SettingDetails settingDetails)
    {
        // Validate that KeySettingName exists if provided
        if (!string.IsNullOrEmpty(lookupTableAttribute.KeySettingName) && allSettings != null)
        {
            var keySettingExists = allSettings.Any(s => s.Name == lookupTableAttribute.KeySettingName);
            if (!keySettingExists)
            {
                throw new InvalidSettingException(
                    $"[LookupTable] on '{settingDetails.Name}': KeySettingName '{lookupTableAttribute.KeySettingName}' " +
                    $"does not match any setting. Available settings: {string.Join(", ", allSettings.Select(s => s.Name))}");
            }
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

    private static void ValidateHeadingAttribute(HeadingAttribute attribute, string propertyName)
    {
        if (string.IsNullOrEmpty(attribute.Text))
        {
            throw new InvalidSettingException(
                $"[Heading] on '{propertyName}': Text cannot be null or empty. " +
                $"Example: [Heading(\"My Section\")]");
        }
    }

    private static void ValidateIndentAttribute(IndentAttribute attribute, string propertyName)
    {
        if (attribute.Level < IndentAttribute.MinIndentLevel || attribute.Level > IndentAttribute.MaxIndentLevel)
        {
            throw new InvalidSettingException(
                $"[Indent] on '{propertyName}': Level must be between {IndentAttribute.MinIndentLevel} and {IndentAttribute.MaxIndentLevel} inclusive, but was {attribute.Level}. " +
                $"Example: [Indent(1)]");
        }
    }

    private static void ValidateValidationAttribute(ValidationAttribute attribute, string propertyName)
    {
        if (attribute.ValidationType == ValidationType.Custom && string.IsNullOrWhiteSpace(attribute.ValidationRegex))
        {
            throw new InvalidSettingException(
                $"[Validation] on '{propertyName}': Custom validation type must specify a regex. " +
                $"Example: [Validation(\"^[a-z]+$\", \"Must be lowercase letters only\")]");
        }
    }

    private static void ValidateIsBetweenAttributeValues(ValidateIsBetweenAttribute attribute, string propertyName)
    {
        if (attribute.Lower > attribute.Higher)
        {
            throw new InvalidSettingException(
                $"[ValidateIsBetween] on '{propertyName}': Lower bound ({attribute.Lower}) cannot be greater than higher bound ({attribute.Higher}). " +
                $"Example: [ValidateIsBetween(0, 100, Inclusion.Inclusive)]");
        }
    }

    private static void ValidateCountAttributeValues(ValidateCountAttribute attribute, string propertyName)
    {
        // Validate that the correct constructor was used based on Condition value
        if (attribute.Condition == Constraint.Between)
        {
            // Between constraint requires the two-parameter constructor (lowerCount, higherCount)
            if (!attribute.WasConstructedWithBounds)
            {
                throw new InvalidSettingException(
                    $"[ValidateCount] on '{propertyName}': Between constraint requires the two-parameter constructor. " +
                    $"Use: [ValidateCount(Constraint.Between, lowerCount, higherCount)]");
            }
            
            if (attribute.LowerCount > attribute.HigherCount)
            {
                throw new InvalidSettingException(
                    $"[ValidateCount] on '{propertyName}': Lower count ({attribute.LowerCount}) cannot be greater than higher count ({attribute.HigherCount}). " +
                    $"Example: [ValidateCount(Constraint.Between, 1, 10)]");
            }
        }
        else
        {
            // Exactly, AtLeast, AtMost constraints require the single-parameter constructor (count)
            // If WasConstructedWithBounds is true, the wrong constructor was used
            if (attribute.WasConstructedWithBounds)
            {
                var conditionName = attribute.Condition switch
                {
                    Constraint.Exactly => "Exactly",
                    Constraint.AtLeast => "AtLeast",
                    Constraint.AtMost => "AtMost",
                    _ => attribute.Condition.ToString()
                };
                
                throw new InvalidSettingException(
                    $"[ValidateCount] on '{propertyName}': {conditionName} constraint requires the single-parameter constructor. " +
                    $"Use: [ValidateCount(Constraint.{conditionName}, count)] instead of the two-parameter constructor.");
            }
        }
    }

    private static void ValidateCategoryAttribute(CategoryAttribute attribute, string propertyName)
    {
        if (attribute.ColorHex?.IsValidCssColor() != true)
        {
            throw new InvalidSettingException(
                $"[Category] on '{propertyName}': Color '{attribute.ColorHex}' is not a valid CSS color. " +
                $"Example: [Category(\"My Category\", \"#FF5733\")]");
        }
    }

    private static void ValidateGenericCategoryAttribute(string? categoryColor, string propertyName)
    {
        if (categoryColor?.IsValidCssColor() != true)
        {
            throw new InvalidSettingException(
                $"[Category<TEnum>] on '{propertyName}': Color '{categoryColor}' is not a valid CSS color. " +
                $"Ensure your category enum has valid ColorHex attributes.");
        }
    }

    private static void ValidateDependsOnAttribute(DependsOnAttribute attribute, string propertyName, List<SettingDetails>? allSettings)
    {
        // Validate DependsOnProperty is specified
        if (string.IsNullOrWhiteSpace(attribute.DependsOnProperty))
        {
            throw new InvalidSettingException(
                $"[DependsOn] on '{propertyName}': The dependent property name cannot be null or empty.");
        }
        
        // Validate that at least one valid value is specified
        if (attribute.ValidValues == null || attribute.ValidValues.Length == 0)
        {
            throw new InvalidSettingException(
                $"[DependsOn] on '{propertyName}': At least one valid value must be specified. " +
                $"Example: [DependsOn(nameof(OtherProperty), true)] or [DependsOn(nameof(OtherProperty), \"Value1\", \"Value2\")]");
        }
        
        // Validate that the referenced property exists
        if (allSettings != null)
        {
            var dependentSettingExists = allSettings.Any(s => s.Name == attribute.DependsOnProperty);
            if (!dependentSettingExists)
            {
                throw new InvalidSettingException(
                    $"[DependsOn] on '{propertyName}': References property '{attribute.DependsOnProperty}' " +
                    $"which does not exist. Available settings: {string.Join(", ", allSettings.Select(s => s.Name))}");
            }
        }
    }

    private void ThrowIfNotString(PropertyInfo settingProperty)
    {
        if (settingProperty.PropertyType.FigPropertyType() != FigPropertyType.String)
            throw new InvalidSettingException(
                $"[Secret] on '{settingProperty.Name}': Secrets can only be applied to string properties.");
    }
    
    private void ApplyAutomaticHeadingGeneration(SettingDetails settingDetails, List<SettingDetails> allSettings, SettingDefinitionDataContract setting, bool automaticallyGenerateHeadings)
    {
        if (!automaticallyGenerateHeadings || 
            setting.Heading != null || // Don't override manual headings
            string.IsNullOrEmpty(setting.CategoryName))
        {
            return;
        }

        // Check if this is the first setting with this category by examining previous settings
        var currentSettingIndex = allSettings.FindIndex(s => s.Name == settingDetails.Name);
        var isFirstSettingWithCategory = IsFirstSettingWithCategory(allSettings, currentSettingIndex, setting.CategoryName);

        if (isFirstSettingWithCategory)
        {
            // This is the first setting with this category, add a heading
            setting.Heading = new HeadingDataContract(
                setting.CategoryName ?? "Category",
                setting.CategoryColor,
                setting.Advanced);
        }
    }

    private static bool IsFirstSettingWithCategory(List<SettingDetails> allSettings, int currentSettingIndex, string? categoryName)
    {
        for (var i = 0; i < currentSettingIndex; i++)
        {
            var previousSetting = allSettings[i];
            
            // Check all attributes for both CategoryAttribute and CategoryAttribute<TEnum>
            foreach (var attr in previousSetting.Property.GetCustomAttributes())
            {
                string? previousCategoryName = null;
                
                if (attr is CategoryAttribute categoryAttribute)
                {
                    previousCategoryName = categoryAttribute.Name;
                }
                else if (attr.GetType().IsGenericType && 
                         attr.GetType().GetGenericTypeDefinition() == typeof(CategoryAttribute<>))
                {
                    var nameProperty = attr.GetType().GetProperty("Name");
                    previousCategoryName = nameProperty?.GetValue(attr) as string;
                }

                if (previousCategoryName == categoryName)
                {
                    return false;
                }
            }
        }
        
        return true;
    }

    private void SetSettingAttribute(SettingAttribute settingAttribute, SettingDetails settingDetails,
        SettingDefinitionDataContract setting)
    {
        // Validate that unsupported primitive types are not used
        if (IsUnsupportedPrimitiveType(settingDetails.Property.PropertyType))
        {
            var typeName = GetFriendlyTypeName(settingDetails.Property.PropertyType);
            throw new InvalidSettingException(
                $"[Setting] on '{settingDetails.Property.Name}': Type '{typeName}' is not supported. " +
                $"Supported types: bool, int, long, double, string, DateTime, TimeSpan, enums, List<T>, and custom classes (serialized as JSON).");
        }
        
        if (settingDetails.Property.PropertyType.IsSupportedBaseType())
        {
            if (NullValueForNonNullableProperty(settingDetails.Property, settingDetails.DefaultValue))
                throw new InvalidSettingException(
                    $"[Setting] on '{settingDetails.Property.Name}': Non-nullable property cannot have a null default value. " +
                    $"Make the property nullable (e.g., int?) or set a default value.");
            
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
            throw new InvalidSettingException(
                $"[Setting] on '{setting.Name}': Description is required but was not found. " +
                $"Valid resource keys: {string.Join(", ", validResourceKeys)}");
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
                            $"[Secret] on DataGrid column '{property.Name}': Secrets can only be applied to string columns.");
                    }

                    if (property.PropertyType.IsEnumerableType())
                    {
                        if (property.PropertyType == typeof(List<string>))
                        {
                            if (validValues?.Any() != true)
                            {
                                throw new InvalidSettingException(
                                    $"DataGrid column '{property.Name}': String collections must have valid values defined. " +
                                    $"Add [ValidValues(\"Value1\", \"Value2\")] to the property.");
                            }
                        }
                        else
                        {
                            throw new InvalidSettingException(
                                $"DataGrid column '{property.Name}': Only List<string> with valid values is supported for collection properties.");
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
    
    private static bool IsUnsupportedPrimitiveType(Type type)
    {
        // Get the underlying type if nullable
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        
        // Check if it's a supported base type, data grid, or enum
        if (type.IsSupportedBaseType() || type.IsSupportedDataGridType() || type.IsEnum())
        {
            return false;
        }
        
        // Check if it's an unsupported primitive type (value type but not supported)
        // Custom classes (reference types) are allowed as they become JSON
        if (underlyingType.IsValueType && !underlyingType.IsEnum)
        {
            // These are unsupported primitive types
            return true;
        }
        
        return false;
    }
    
    private static string GetFriendlyTypeName(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type);
        var isNullable = underlyingType != null;
        var baseType = underlyingType ?? type;
        
        var friendlyName = baseType.Name switch
        {
            "Single" => "float",
            "Decimal" => "decimal",
            "Byte" => "byte",
            "SByte" => "sbyte",
            "Int16" => "short",
            "UInt16" => "ushort",
            "Int32" => "int",
            "UInt32" => "uint",
            "Int64" => "long",
            "UInt64" => "ulong",
            "Char" => "char",
            "Boolean" => "bool",
            "Double" => "double",
            "String" => "string",
            _ => baseType.Name
        };
        
        return isNullable ? $"{friendlyName}?" : friendlyName;
    }
}
