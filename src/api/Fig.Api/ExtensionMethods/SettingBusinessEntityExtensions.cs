using Fig.Api.Exceptions;
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
            StringFormat = original.StringFormat
        };
    }

    public static void Validate(this SettingBusinessEntity? setting)
    {
        if (setting?.Value != null && setting?.Value?.GetType() != setting?.ValueType)
            throw new InvalidSettingException(
                $"Value for setting {setting?.Name} had type {setting?.Value?.GetType()} but should have been {setting?.ValueType}");

        if (setting?.DefaultValue != null && setting?.DefaultValue?.GetType() != setting?.ValueType)
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