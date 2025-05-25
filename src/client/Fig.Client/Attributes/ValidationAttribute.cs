using System;
using Fig.Client.Exceptions;
using Fig.Client.Validation;

namespace Fig.Client.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = true)]
public class ValidationAttribute : Attribute
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
}