using System;
using System.Globalization;
using Fig.Api.Enums;
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
    private readonly Inclusion _inclusion;

    [Obsolete("Use ValidateIsBetweenAttribute(double lower, double higher, Inclusion inclusion, bool includeInHealthCheck = true) instead.")]
    public ValidateIsBetweenAttribute(double lower, double higher, bool includeInHealthCheck = true)
    {
        _lower = lower;
        _higher = higher;
        _inclusion = Inclusion.Inclusive; // default value
        _includeInHealthCheck = includeInHealthCheck;
    }

    public ValidateIsBetweenAttribute(double lower, double higher, Inclusion inclusion, bool includeInHealthCheck = true)
    {
        _lower = lower;
        _higher = higher;
        _inclusion = inclusion;
        _includeInHealthCheck = includeInHealthCheck;
    }

    public Type[] ApplyToTypes => [typeof(double), typeof(int), typeof(long)];

    public (bool, string) IsValid(object? value)
    {
        if (!_includeInHealthCheck)
            return (true, "Not validated");

        var operatorText = _inclusion == Inclusion.Inclusive ? "between (inclusive)" : "between (exclusive)";
        var message = $"{value} is not {operatorText} {_lower} and {_higher}";
        if (value == null)
            return (false, message);

        // Allow numeric types only
        Type type = value.GetType();
        if (!type.IsNumeric())
            return (false, message);

        try
        {
            double numericValue = Convert.ToDouble(value);
            var isBetween = _inclusion == Inclusion.Inclusive
                ? numericValue >= _lower && numericValue <= _higher
                : numericValue > _lower && numericValue < _higher;
            return isBetween ? (true, "Valid") : (false, message);
        }
        catch
        {
            return (false, message);
        }
    }

    public string GetScript(string propertyName)
    {
        var lowerOperator = _inclusion == Inclusion.Inclusive ? ">=" : ">";
        var higherOperator = _inclusion == Inclusion.Inclusive ? "<=" : "<";
        var operatorText = _inclusion == Inclusion.Inclusive ? "between (inclusive)" : "between (exclusive)";
        var lowerStr = _lower.ToString(CultureInfo.InvariantCulture);
        var higherStr = _higher.ToString(CultureInfo.InvariantCulture);
        var script = $"if ({propertyName}.Value {lowerOperator} {lowerStr} && {propertyName}.Value {higherOperator} {higherStr}) " +
                     $"{{ {propertyName}.IsValid = true; {propertyName}.ValidationExplanation = ''; }} " +
                     $"else " +
                     $"{{ {propertyName}.IsValid = false; {propertyName}.ValidationExplanation = '{propertyName} must be {operatorText} {lowerStr} and {higherStr}'; }}";

        return script;
    }
}