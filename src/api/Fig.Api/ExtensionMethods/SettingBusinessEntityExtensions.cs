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
}