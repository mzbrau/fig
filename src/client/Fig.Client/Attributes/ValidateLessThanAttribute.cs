using System;
using System.Globalization;
using Fig.Api.Enums;
using Fig.Client.ExtensionMethods;
using Fig.Client.Validation;

namespace Fig.Client.Attributes;

/// <summary>
/// This attribute can be used to apply validation to numeric properties.
/// It will assert that the value is less than a specified maximum value.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ValidateLessThanAttribute : Attribute, IValidatableAttribute, IDisplayScriptProvider
{
    private readonly double _maxValue;
    private readonly bool _includeInHealthCheck;
    private readonly Inclusion _inclusion;

    [Obsolete("Use ValidateLessThanAttribute(double maxValue, Inclusion inclusion, bool includeInHealthCheck = true) instead.")]
    public ValidateLessThanAttribute(double maxValue, bool includeEquals = false, bool includeInHealthCheck = true)
    {
        _maxValue = maxValue;
        _includeInHealthCheck = includeInHealthCheck;
        _inclusion = includeEquals ? Inclusion.Inclusive : Inclusion.Exclusive;
    }

    public ValidateLessThanAttribute(double maxValue, Inclusion inclusion = Inclusion.Exclusive, bool includeInHealthCheck = true)
    {
        _maxValue = maxValue;
        _includeInHealthCheck = includeInHealthCheck;
        _inclusion = inclusion;
    }

    public Type[] ApplyToTypes => [typeof(double), typeof(int), typeof(long)];

    public (bool, string) IsValid(object? value)
    {
        if (!_includeInHealthCheck)
            return (true, "Not validated");

        var operatorText = _inclusion == Inclusion.Inclusive ? "less than or equal to" : "less than";
        var message = $"{value} is not {operatorText} {_maxValue}";
        if (value == null)
            return (false, message);

        // Allow numeric types only
        Type type = value.GetType();
        if (!type.IsNumeric())
            return (false, message);

        try
        {
            double numericValue = Convert.ToDouble(value);
            var isValid = _inclusion == Inclusion.Inclusive ? numericValue <= _maxValue : numericValue < _maxValue;
            return isValid ? (true, "Valid") : (false, message);
        }
        catch
        {
            return (false, message);
        }
    }

    public string GetScript(string propertyName)
    {
        var comparisonOperator = _inclusion == Inclusion.Inclusive ? "<=" : "<";
        var operatorText = _inclusion == Inclusion.Inclusive ? "less than or equal to" : "less than";
        var maxValueStr = _maxValue.ToString(CultureInfo.InvariantCulture);
        var script = $"if ({propertyName}.Value {comparisonOperator} {maxValueStr}) " +
                     $"{{ {propertyName}.IsValid = true; {propertyName}.ValidationExplanation = ''; }} " +
                     $"else " +
                     $"{{ {propertyName}.IsValid = false; {propertyName}.ValidationExplanation = '{propertyName} must be {operatorText} {maxValueStr}'; }}";

        return script;
    }
}
