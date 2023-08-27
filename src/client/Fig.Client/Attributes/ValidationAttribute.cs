using System;
using Fig.Client.Exceptions;
using Fig.Client.Validation;
using Fig.Contracts;

namespace Fig.Client.Attributes;

[AttributeUsage(AttributeTargets.Property)]
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

    public string? ValidationRegex { get; }

    public string? Explanation { get; }

    public ValidationType ValidationType { get; }
}