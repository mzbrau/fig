using System;
using Fig.Client.Validation;

namespace Fig.Client.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ValidateLessThanAttribute : Attribute, IValidatableAttribute, IDisplayScriptProvider
{
    private readonly double _maxValue;
    private readonly bool _includeInHealthCheck;
    private readonly bool _includeEquals;

    public ValidateLessThanAttribute(double maxValue, bool includeEquals = false, bool includeInHealthCheck = true)
    {
        _maxValue = maxValue;
        _includeInHealthCheck = includeInHealthCheck;
        _includeEquals = includeEquals;
    }

    public Type[] ApplyToTypes => [typeof(double), typeof(int), typeof(long)];

    public (bool, string) IsValid(object? value)
    {
        if (!_includeInHealthCheck)
            return (true, "Not validated");

        var operatorText = _includeEquals ? "less than or equal to" : "less than";
        var message = $"{value} is not {operatorText} {_maxValue}";
        if (value == null)
            return (false, message);

        // Allow numeric types only
        Type type = value.GetType();
        if (!IsNumericType(type))
            return (false, message);

        try
        {
            double numericValue = Convert.ToDouble(value);
            var isValid = _includeEquals ? numericValue <= _maxValue : numericValue < _maxValue;
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
        var comparisonOperator = _includeEquals ? "<=" : "<";
        var operatorText = _includeEquals ? "less than or equal to" : "less than";
        var script = $"if ({propertyName}.Value {comparisonOperator} {_maxValue}) " +
                     $"{{ {propertyName}.IsValid = true; {propertyName}.ValidationExplanation = ''; }} " +
                     $"else " +
                     $"{{ {propertyName}.IsValid = false; {propertyName}.ValidationExplanation = '{propertyName} must be {operatorText} {_maxValue}'; }}";

        return script;
    }
}
