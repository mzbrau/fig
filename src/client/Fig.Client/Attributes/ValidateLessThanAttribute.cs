using System;
using Fig.Client.Validation;

namespace Fig.Client.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ValidateLessThanAttribute : Attribute, IValidatableAttribute, IDisplayScriptProvider
{
    private readonly double _maxValue;
    private readonly bool _includeInHealthCheck;

    public ValidateLessThanAttribute(double maxValue, bool includeInHealthCheck = true)
    {
        _maxValue = maxValue;
        _includeInHealthCheck = includeInHealthCheck;
    }

    public Type[] ApplyToTypes => [typeof(double), typeof(int), typeof(long)];

    public (bool, string) IsValid(object? value)
    {
        if (!_includeInHealthCheck)
            return (true, "Not validated");

        var message = $"{value} is not less than {_maxValue}";
        if (value == null)
            return (false, message);

        // Allow numeric types only
        Type type = value.GetType();
        if (!IsNumericType(type))
            return (false, message);

        try
        {
            double numericValue = Convert.ToDouble(value);
            var isValid = numericValue < _maxValue;
            return isValid ? (true, "Valid") : (false, message);
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
        var script = $"if ({propertyName}.Value < {_maxValue}) " +
                     $"{{ {propertyName}.IsValid = true; {propertyName}.ValidationExplanation = ''; }} " +
                     $"else " +
                     $"{{ {propertyName}.IsValid = false; {propertyName}.ValidationExplanation = '{propertyName} must be less than {_maxValue}'; }}";

        return script;
    }
}
