using System;
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

    public string ValidationRegex { get; }

    public string Explanation { get; }

    public ValidationType ValidationType { get; }
}