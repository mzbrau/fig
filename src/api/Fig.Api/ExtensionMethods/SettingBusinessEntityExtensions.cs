using Fig.Api.Exceptions;
using Fig.Contracts.ExtensionMethods;
using Fig.Datalayer.BusinessEntities;
using Newtonsoft.Json;

namespace Fig.Api.ExtensionMethods;

public static class SettingBusinessEntityExtensions
{
    public static SettingBusinessEntity Clone(this SettingBusinessEntity original)
    {
        return new SettingBusinessEntity
        {
            Name = original.Name,
            Description = original.Description,
            IsSecret = original.IsSecret,
            Value = original.Value,
            DefaultValue = original.DefaultValue,
            ValueType = original.ValueType,
            ValidationType = original.ValidationType,
            ValidationRegex = original.ValidationRegex,
            ValidationExplanation = original.ValidationExplanation,
            ValidValues = original.ValidValues,
            Group = original.Group,
            DisplayOrder = original.DisplayOrder,
            Advanced = original.Advanced,
            LookupTableKey = original.LookupTableKey,
            JsonSchema = original.JsonSchema,
            DataGridDefinitionJson = original.DataGridDefinitionJson,
            EditorLineCount = original.EditorLineCount,
            EnablesSettings = original.EnablesSettings,
            SupportsLiveUpdate = original.SupportsLiveUpdate
        };
    }

    public static void Validate(this SettingBusinessEntity? setting)
    {
        if (setting?.Value != null && ((Type?) setting?.Value?.GetType())?.FigPropertyType() !=
            setting?.ValueType.FigPropertyType())
            throw new InvalidSettingException(
                $"Value for setting {setting?.Name} had type {setting?.Value?.GetType()} but should have been {setting?.ValueType}");

        if (setting?.DefaultValue != null && ((Type?) setting?.DefaultValue?.GetType()).FigPropertyType() !=
            setting?.ValueType.FigPropertyType())
            throw new InvalidSettingException(
                $"Default value for setting {setting?.Name} had type {setting?.Value?.GetType()} but should have been {setting?.ValueType}");

        if (string.IsNullOrWhiteSpace(setting?.Description))
            throw new InvalidSettingException($"Setting {setting?.Name} did not have a description set");
    }

    public static void Serialize(this SettingBusinessEntity setting)
    {
        setting.ValueAsJson = JsonConvert.SerializeObject(setting.Value);
        setting.DefaultValueAsJson = JsonConvert.SerializeObject(setting.Value);
    }
}