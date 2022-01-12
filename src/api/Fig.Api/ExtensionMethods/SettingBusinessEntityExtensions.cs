using Fig.Api.Datalayer.BusinessEntities;

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

    public static bool Isvalid(this SettingBusinessEntity setting)
    {
        if (setting.Value != null && setting.Value.GetType() != setting.ValueType) return false;

        if (setting.DefaultValue != null && setting.DefaultValue.GetType() != setting.ValueType) return false;

        if (string.IsNullOrWhiteSpace(setting.Description)) return false;

        return true;
    }
}