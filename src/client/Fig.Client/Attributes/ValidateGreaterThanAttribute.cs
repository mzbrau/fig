using System;
using Fig.Client.Validation;

namespace Fig.Client.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ValidateGreaterThanAttribute : Attribute, IValidatableAttribute, IDisplayScriptProvider
{
    private readonly double _minValue;
    private readonly bool _includeInHealthCheck;
    private readonly bool _includeEquals;

    public ValidateGreaterThanAttribute(double minValue, bool includeEquals = false, bool includeInHealthCheck = true)
    {
        _minValue = minValue;
        _includeInHealthCheck = includeInHealthCheck;
        _includeEquals = includeEquals;
    }

    public Type[] ApplyToTypes => [typeof(double), typeof(int), typeof(long)];

    public (bool, string) IsValid(object? value)
    {
        if (!_includeInHealthCheck)
            return (true, "Not validated");

        var operatorText = _includeEquals ? "greater than or equal to" : "greater than";
        var message = $"{value} is not {operatorText} {_minValue}";
        if (value == null)
            return (false, message);

        // Allow numeric types only
        Type type = value.GetType();
        if (!IsNumericType(type))
            return (false, message);

        try
        {
            double numericValue = Convert.ToDouble(value);
            var isValid = _includeEquals ? numericValue >= _minValue : numericValue > _minValue;
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
        var comparisonOperator = _includeEquals ? ">=" : ">";
        var operatorText = _includeEquals ? "greater than or equal to" : "greater than";
        var script = $"if ({propertyName}.Value {comparisonOperator} {_minValue}) " +
                     $"{{ {propertyName}.IsValid = true; {propertyName}.ValidationExplanation = ''; }} " +
                     $"else " +
                     $"{{ {propertyName}.IsValid = false; {propertyName}.ValidationExplanation = '{propertyName} must be {operatorText} {_minValue}'; }}";

        return script;
    }
}
