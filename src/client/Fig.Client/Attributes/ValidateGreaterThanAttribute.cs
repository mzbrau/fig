using System;
using System.Globalization;
using Fig.Api.Enums;
using Fig.Client.ExtensionMethods;
using Fig.Client.Validation;

namespace Fig.Client.Attributes;

/// <summary>
/// This attribute can be used to apply validation to numeric properties.
/// It will assert that the value is greater than a specified minimum value.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ValidateGreaterThanAttribute : Attribute, IValidatableAttribute, IDisplayScriptProvider
{
    private readonly double _minValue;
    private readonly bool _includeInHealthCheck;
    private readonly Inclusion _inclusion;

    [Obsolete("Use ValidateGreaterThanAttribute(double minValue, Inclusion inclusion, bool includeInHealthCheck = true) instead.")]
    public ValidateGreaterThanAttribute(double minValue, bool includeEquals = false, bool includeInHealthCheck = true)
    {
        _minValue = minValue;
        _includeInHealthCheck = includeInHealthCheck;
        _inclusion = includeEquals ? Inclusion.Inclusive : Inclusion.Exclusive;
    }

    public ValidateGreaterThanAttribute(double minValue, Inclusion inclusion = Inclusion.Exclusive, bool includeInHealthCheck = true)
    {
        _minValue = minValue;
        _includeInHealthCheck = includeInHealthCheck;
        _inclusion = inclusion;
    }

    public Type[] ApplyToTypes => [typeof(double), typeof(int), typeof(long)];

    public (bool, string) IsValid(object? value)
    {
        if (!_includeInHealthCheck)
            return (true, "Not validated");

        var operatorText = _inclusion == Inclusion.Inclusive ? "greater than or equal to" : "greater than";
        var message = $"{value} is not {operatorText} {_minValue}";
        if (value == null)
            return (false, message);

        // Allow numeric types only
        Type type = value.GetType();
        if (!type.IsNumeric())
            return (false, message);

        try
        {
            double numericValue = Convert.ToDouble(value);
            var isValid = _inclusion == Inclusion.Inclusive ? numericValue >= _minValue : numericValue > _minValue;
            return isValid ? (true, "Valid") : (false, message);
        }
        catch
        {
            return (false, message);
        }
    }

    public string GetScript(string propertyName)
    {
        var comparisonOperator = _inclusion == Inclusion.Inclusive ? ">=" : ">";
        var operatorText = _inclusion == Inclusion.Inclusive ? "greater than or equal to" : "greater than";
        var minValueStr = _minValue.ToString(CultureInfo.InvariantCulture);
        var script = $"if ({propertyName}.Value {comparisonOperator} {minValueStr}) " +
                     $"{{ {propertyName}.IsValid = true; {propertyName}.ValidationExplanation = ''; }} " +
                     $"else " +
                     $"{{ {propertyName}.IsValid = false; {propertyName}.ValidationExplanation = '{propertyName} must be {operatorText} {minValueStr}'; }}";

        return script;
    }
}
