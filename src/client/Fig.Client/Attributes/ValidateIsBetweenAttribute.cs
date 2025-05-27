using System;
using Fig.Client.Validation;

namespace Fig.Client.Attributes;

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
        if (!IsNumericType(type))
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

    private static bool IsNumericType(Type type)
    {
        // Unwrap nullable
        if (Nullable.GetUnderlyingType(type) is { } underlying)
            type = underlying;

        return
            type == typeof(int) || 
            type == typeof(long) ||
            type == typeof(double);
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