using System;
using System.Globalization;
using Fig.Client.Abstractions.Enums;
using Fig.Client.Abstractions.ExtensionMethods;
using Fig.Client.Abstractions.Validation;

namespace Fig.Client.Abstractions.Attributes;

/// <summary>
/// This attribute can be used to apply validation to numeric properties.
/// It will assert that the value is between a specified lower and higher value.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ValidateIsBetweenAttribute : Attribute, IValidatableAttribute, IDisplayScriptProvider
{
    private readonly bool _includeInHealthCheck;
    private readonly Inclusion _inclusion;

    /// <summary>
    /// Gets the lower bound of the range.
    /// </summary>
    public double Lower { get; }
    
    /// <summary>
    /// Gets the higher bound of the range.
    /// </summary>
    public double Higher { get; }

    [Obsolete("Use ValidateIsBetweenAttribute(double lower, double higher, Inclusion inclusion, bool includeInHealthCheck = true) instead.")]
    public ValidateIsBetweenAttribute(double lower, double higher, bool includeInHealthCheck = true)
    {
        Lower = lower;
        Higher = higher;
        _inclusion = Inclusion.Inclusive; // default value
        _includeInHealthCheck = includeInHealthCheck;
    }

    public ValidateIsBetweenAttribute(double lower, double higher, Inclusion inclusion, bool includeInHealthCheck = true)
    {
        Lower = lower;
        Higher = higher;
        _inclusion = inclusion;
        _includeInHealthCheck = includeInHealthCheck;
    }

    public Type[] ApplyToTypes => [typeof(double), typeof(int), typeof(long)];

    public (bool, string) IsValid(object? value)
    {
        if (!_includeInHealthCheck)
            return (true, "Not validated");

        var operatorText = _inclusion == Inclusion.Inclusive ? "between (inclusive)" : "between (exclusive)";
        var lowerStr = Lower.ToString(CultureInfo.InvariantCulture);
        var higherStr = Higher.ToString(CultureInfo.InvariantCulture);
        var valueStr = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        var message = $"{valueStr} is not {operatorText} {lowerStr} and {higherStr}";
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
                ? numericValue >= Lower && numericValue <= Higher
                : numericValue > Lower && numericValue < Higher;
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
        var lowerStr = Lower.ToString(CultureInfo.InvariantCulture);
        var higherStr = Higher.ToString(CultureInfo.InvariantCulture);
        var script = $"if ({propertyName}.Value {lowerOperator} {lowerStr} && {propertyName}.Value {higherOperator} {higherStr}) " +
                     $"{{ {propertyName}.IsValid = true; {propertyName}.ValidationExplanation = ''; }} " +
                     $"else " +
                     $"{{ {propertyName}.IsValid = false; {propertyName}.ValidationExplanation = '{propertyName} must be {operatorText} {lowerStr} and {higherStr}'; }}";

        return script;
    }
}