using System;
using Fig.Client.Validation;

namespace Fig.Client.Attributes;
/// <summary>
/// This validation is applied at the class level and will apply to all properties of a specific type unless they have their own specific validation rules (including validation none)
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ValidationOfAllTypes : ValidationAttribute
{
    public ValidationOfAllTypes(ValidationType validationType, bool includeInHealthCheck = true, params Type[] applyToTypes)
        : base(validationType, includeInHealthCheck)
    {
        ApplyToTypes = applyToTypes;
    }

    public ValidationOfAllTypes(string validationRegex, string explanation, bool includeInHealthCheck = true, params Type[] applyToTypes)
        : base(validationRegex, explanation, includeInHealthCheck)
    {
        ApplyToTypes = applyToTypes;
    }
}