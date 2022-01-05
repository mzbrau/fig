using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Api.BusinessEntities;

public class SettingBusinessEntity
{
    public virtual string Name { get; set; }

    public virtual string Description { get; set; }
    
    public virtual bool IsSecret { get; set; }
    
    public virtual dynamic? Value { get; set; }

    public virtual dynamic? DefaultValue { get; set; }

    public virtual ValidationType ValidationType { get; set; }

    public virtual string? ValidationRegex { get; set; }
    
    public virtual string? ValidationExplanation { get; set; }
    
    public virtual List<string> ValidValues { get; set; }

    public virtual string Group { get; set; }

    public virtual int? DisplayOrder { get; set; }

    public virtual bool Advanced { get; set; }
    
    public virtual string? StringFormat { get; set; }

    public SettingBusinessEntity Clone()
    {
        return new SettingBusinessEntity
        {
            Name = Name,
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