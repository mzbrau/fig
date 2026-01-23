using System;
using System.Text.RegularExpressions;
using Fig.Client.Abstractions.ExtensionMethods;
using Fig.Client.Abstractions.Validation;

namespace Fig.Client.Abstractions.Attributes;

/// <summary>
/// This attribute is used to specify a validation regex for the property.
/// For class level validation, use <see cref="ValidationOfAllTypes"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
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
        ValidationType = validationType;
        IncludeInHealthCheck = includeInHealthCheck;
    }

    public string? ValidationRegex { get; }

    public string? Explanation { get; }

    public ValidationType ValidationType { get; }
    
    public Type[]? ApplyToTypes { get; protected set; }
    
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