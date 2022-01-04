using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Api.BusinessEntities;

public class SettingBusinessEntity
{
    public string Name { get; set; }

    public string FriendlyName { get; set; }
    
    public string Description { get; set; }
    
    public bool IsSecret { get; set; }
    
    public dynamic? Value { get; set; }

    public dynamic? DefaultValue { get; set; }

    public ValidationType ValidationType { get; set; }

    public string? ValidationRegex { get; set; }
    
    public string? ValidationExplanation { get; set; }
    
    public List<string> ValidValues { get; set; }

    public string Group { get; set; }

    public int? DisplayOrder { get; set; }

    public bool Advanced { get; set; }
    
    public string? StringFormat { get; set; }

    public SettingBusinessEntity Clone()
    {
        return new SettingBusinessEntity
        {
            Name = Name,
            FriendlyName = FriendlyName,
            Description = Description,
            IsSecret = IsSecret,
            Value = Value,
            DefaultValue = DefaultValue,
            ValidationType = ValidationType,
            ValidationRegex = ValidationRegex,
            ValidationExplanation = ValidationExplanation,
            ValidValues = ValidValues,
            Group = Group,
            DisplayOrder = DisplayOrder,
            Advanced = Advanced,
            StringFormat = StringFormat
        };
    }
}