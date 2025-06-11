using System;
using Fig.Client.ExtensionMethods;
using Fig.Client.Validation;

namespace Fig.Client.Attributes;

/// <summary>
/// This attribute can be used to apply validation to numeric properties.
/// It will assert that the value is between a specified lower and higher value.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ValidateIsBetweenAttribute : Attribute, IValidatableAttribute, IDisplayScriptProvider
{
    private readonly double _lower;
    private readonly double _higher;
    private readonly bool _includeInHealthCheck;

    public ValidateIsBetweenAttribute(double lower, double higher, bool includeInHealthCheck = true)
    {
        _lower = lower;
        _higher = higher;
        _includeInHealthCheck = includeInHealthCheck;
    }

    public Type[] ApplyToTypes => [typeof(double), typeof(int), typeof(long)];

    public (bool, string) IsValid(object? value)
    {
        if (!_includeInHealthCheck)
            return (true, "Not validated");

        var message = $"{value} is not between {_lower} and {_higher}";
        if (value == null)
            return (false, message);

        // Allow numeric types only
        Type type = value.GetType();
        if (!type.IsNumeric())
            return (false, message);

        try
        {
            double numericValue = Convert.ToDouble(value);
            var isBetween = numericValue >= _lower && numericValue <= _higher;
            return isBetween ? (true, "Valid") : (false, message);
        }
        catch
        {
            return (false, message);
        }
    }

    public string GetScript(string propertyName)
    {
        var script = $"if ({propertyName}.Value > {_lower} && {propertyName}.Value < {_higher}) " +
                     $"{{ {propertyName}.IsValid = true; {propertyName}.ValidationExplanation = ''; }} " +
                     $"else " +
                     $"{{ {propertyName}.IsValid = false; {propertyName}.ValidationExplanation = '{propertyName} must be between {_lower} and {_higher}'; }}";

        return script;
    }
}