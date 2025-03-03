using System;
using Fig.Client.Exceptions;
using Fig.Client.Validation;

namespace Fig.Client.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = true)]
public class ValidationAttribute : Attribute
{
    public ValidationAttribute(string validationRegex, string explanation)
    {
        ValidationRegex = validationRegex;
        Explanation = explanation;
        ValidationType = ValidationType.Custom;
    }

    public ValidationAttribute(ValidationType validationType)
    {
        if (validationType == ValidationType.Custom)
            throw new FigConfigurationException("Custom validation type must specify a regex");
        ValidationType = validationType;
    }

    public ValidationAttribute(ValidationType validationType, params Type[] applyToTypes)
        : this(validationType)
    {
        ApplyToTypes = applyToTypes;
    }

    public ValidationAttribute(string validationRegex, string explanation, params Type[] applyToTypes)
        : this(validationRegex, explanation)
    {
        ApplyToTypes = applyToTypes;
    }

    public string? ValidationRegex { get; }

    public string? Explanation { get; }

    public ValidationType ValidationType { get; }
    
    public Type[]? ApplyToTypes { get; }
}