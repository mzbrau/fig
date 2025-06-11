using System;
using System.Text.RegularExpressions;
using Fig.Client.Exceptions;
using Fig.Client.ExtensionMethods;
using Fig.Client.Validation;

namespace Fig.Client.Attributes;

/// <summary>
/// This attribute is used to specify a validation regex for the property.
/// It can also be applied at the class level and will apply to all properties of a specific type unless they have their own specific validation rules (including validation none)
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = true)]
public class ValidationAttribute : Attribute, IValidatableAttribute
{
    public ValidationAttribute(string validationRegex, string explanation, bool includeInHealthCheck = true)
    {
        ValidationRegex = validationRegex;
        Explanation = explanation;
        IncludeInHealthCheck = includeInHealthCheck;
        ValidationType = ValidationType.Custom;
    }

    public ValidationAttribute(ValidationType validationType, bool includeInHealthCheck = true)
    {
        if (validationType == ValidationType.Custom)
            throw new FigConfigurationException("Custom validation type must specify a regex");
        ValidationType = validationType;
        IncludeInHealthCheck = includeInHealthCheck;
    }

    public ValidationAttribute(ValidationType validationType, bool includeInHealthCheck = true, params Type[] applyToTypes)
        : this(validationType, includeInHealthCheck)
    {
        ApplyToTypes = applyToTypes;
    }

    public ValidationAttribute(string validationRegex, string explanation, bool includeInHealthCheck = true, params Type[] applyToTypes)
        : this(validationRegex, explanation, includeInHealthCheck)
    {
        ApplyToTypes = applyToTypes;
    }

    public string? ValidationRegex { get; }

    public string? Explanation { get; }

    public ValidationType ValidationType { get; }
    
    public Type[]? ApplyToTypes { get; }
    
    public bool IncludeInHealthCheck { get; }

    public (bool, string) IsValid(object? value)
    {
        if (!IncludeInHealthCheck || ValidationType == ValidationType.None)
            return (true, "Not Validated");
        
        if (value is null)
            return (false, Explanation ?? "Invalid");

        var (regex, explanation) = GetRegex();
        
        if (string.IsNullOrWhiteSpace(regex))
            return (true, "No validation");

        var isMatch = Regex.IsMatch(value.ToString(), regex!);

        return isMatch ? (true, "Valid") : (false, explanation ?? "Invalid");
    }

    private (string?, string?) GetRegex()
    {
        if (ValidationType == ValidationType.Custom && !string.IsNullOrWhiteSpace(ValidationRegex))
            return (ValidationRegex, Explanation);

        var def = ValidationType.GetDefinition();
        return (def.Regex, def.Explanation);
    }
}