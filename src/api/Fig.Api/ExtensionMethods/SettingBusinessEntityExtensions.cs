using Fig.Api.Exceptions;
using Fig.Common.NetStandard.Json;
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
            ValueType = original.ValueType,
            Value = original.Value,
            DefaultValue = original.DefaultValue,
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
            SupportsLiveUpdate = original.SupportsLiveUpdate,
            LastChanged = original.LastChanged,
            CategoryColor = original.CategoryColor,
            CategoryName = original.CategoryName
        };
    }
    
    public static void Validate(this SettingBusinessEntity? setting)
    {
        if (setting?.Value?.GetValue() != null && setting?.Value?.GetValue()?.GetType()?.FigPropertyType() !=
            setting?.ValueType.FigPropertyType())
            throw new InvalidSettingException(
                $"Value for setting {setting?.Name} had type {setting?.Value?.GetValue()?.GetType()} but should have been {setting?.ValueType}");

        if (setting?.DefaultValue?.GetValue() != null && (setting?.DefaultValue?.GetValue()?.GetType()).FigPropertyType() !=
            setting?.ValueType.FigPropertyType())
            throw new InvalidSettingException(
                $"Default value for setting {setting?.Name} had type {setting?.Value?.GetValue()?.GetType()} but should have been {setting?.ValueType}");

        if (string.IsNullOrWhiteSpace(setting?.Description))
            throw new InvalidSettingException($"Setting {setting?.Name} did not have a description set");
    }

    public static void Serialize(this SettingBusinessEntity setting)
    {
        setting.ValueAsJson = JsonConvert.SerializeObject(setting.Value, JsonSettings.FigDefault);
        setting.DefaultValueAsJson = JsonConvert.SerializeObject(setting.Value, JsonSettings.FigDefault);
    }
}