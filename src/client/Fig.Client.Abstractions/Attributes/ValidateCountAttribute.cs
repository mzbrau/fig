using System;
using System.Collections;
using System.Globalization;
using Fig.Client.Abstractions.Enums;
using Fig.Client.Abstractions.Validation;

namespace Fig.Client.Abstractions.Attributes;

/// <summary>
/// This attribute can be used to apply validation to List and collection properties.
/// It will assert that the number of items in the collection matches the specified count condition.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ValidateCountAttribute : Attribute, IValidatableAttribute, IDisplayScriptProvider
{
    private readonly Constraint _condition;
    private readonly int _count;
    private readonly int _lowerCount;
    private readonly int _higherCount;
    private readonly bool _includeInHealthCheck;

    public ValidateCountAttribute(Constraint condition, int count, bool includeInHealthCheck = true)
    {
        if (condition == Constraint.Between)
            throw new ArgumentException("Use the constructor with lowerCount and higherCount parameters for Between constraint.");
        
        _condition = condition;
        _count = count;
        _includeInHealthCheck = includeInHealthCheck;
    }

    public ValidateCountAttribute(Constraint condition, int lowerCount, int higherCount, bool includeInHealthCheck = true)
    {
        if (condition != Constraint.Between)
            throw new ArgumentException("This constructor is only for Between constraint. Use the single count constructor for other constraints.");
        
        if (lowerCount > higherCount)
            throw new ArgumentException("Lower count cannot be greater than higher count.");
            
        _condition = condition;
        _lowerCount = lowerCount;
        _higherCount = higherCount;
        _includeInHealthCheck = includeInHealthCheck;
    }

    public Type[] ApplyToTypes => [typeof(IList), typeof(ICollection), typeof(IEnumerable)];

    public (bool, string) IsValid(object? value)
    {
        if (!_includeInHealthCheck)
            return (true, "Not validated");

        var conditionText = _condition switch
        {
            Constraint.Exactly => "exactly",
            Constraint.AtLeast => "at least",
            Constraint.AtMost => "at most",
            Constraint.Between => "between",
            _ => "exactly"
        };

        string message;
        if (_condition == Constraint.Between)
        {
            var lowerStr = _lowerCount.ToString(CultureInfo.InvariantCulture);
            var higherStr = _higherCount.ToString(CultureInfo.InvariantCulture);
            message = $"Collection must contain {conditionText} {lowerStr} and {higherStr} items (inclusive)";
        }
        else
        {
            var countStr = _count.ToString(CultureInfo.InvariantCulture);
            message = $"Collection must contain {conditionText} {countStr} item{(_count == 1 ? "" : "s")}";
        }

        if (value == null)
            return (false, "Collection is null - " + message);

        // Check if the value is a collection (but not a string, which is also IEnumerable)
        if (value is not IEnumerable enumerable || value is string)
            return (false, "Value is not a collection - " + message);

        // Count the items in the collection
        int actualCount = 0;
        foreach (var item in enumerable)
        {
            actualCount++;
        }

        // Validate based on condition
        bool isValid = _condition switch
        {
            Constraint.Exactly => actualCount == _count,
            Constraint.AtLeast => actualCount >= _count,
            Constraint.AtMost => actualCount <= _count,
            Constraint.Between => actualCount >= _lowerCount && actualCount <= _higherCount,
            _ => false
        };

        if (isValid)
            return (true, "Valid");

        var actualCountStr = actualCount.ToString(CultureInfo.InvariantCulture);
        if (_condition == Constraint.Between)
        {
            var lowerStr = _lowerCount.ToString(CultureInfo.InvariantCulture);
            var higherStr = _higherCount.ToString(CultureInfo.InvariantCulture);
            return (false, $"Collection has {actualCountStr} item{(actualCount == 1 ? "" : "s")} but must contain between {lowerStr} and {higherStr} items (inclusive)");
        }
        else
        {
            var countStr = _count.ToString(CultureInfo.InvariantCulture);
            return (false, $"Collection has {actualCountStr} item{(actualCount == 1 ? "" : "s")} but must contain {conditionText} {countStr} item{(_count == 1 ? "" : "s")}");
        }
    }

    public string GetScript(string propertyName)
    {
        if (_condition == Constraint.Between)
        {
            var lowerStr = _lowerCount.ToString(CultureInfo.InvariantCulture);
            var higherStr = _higherCount.ToString(CultureInfo.InvariantCulture);
            
            var script = $"if ({propertyName}.Value && {propertyName}.Value.length >= {lowerStr} && {propertyName}.Value.length <= {higherStr}) " +
                         $"{{ {propertyName}.IsValid = true; {propertyName}.ValidationExplanation = ''; }} " +
                         $"else " +
                         $"{{ {propertyName}.IsValid = false; {propertyName}.ValidationExplanation = 'Collection must contain between {lowerStr} and {higherStr} items (inclusive)'; }}";
            
            return script;
        }
        else
        {
            var countStr = _count.ToString(CultureInfo.InvariantCulture);
            var conditionText = _condition switch
            {
                Constraint.Exactly => "exactly",
                Constraint.AtLeast => "at least", 
                Constraint.AtMost => "at most",
                _ => "exactly"
            };

            var comparisonOperator = _condition switch
            {
                Constraint.Exactly => "===",
                Constraint.AtLeast => ">=",
                Constraint.AtMost => "<=",
                _ => "==="
            };

            var script = $"if ({propertyName}.Value && {propertyName}.Value.length {comparisonOperator} {countStr}) " +
                         $"{{ {propertyName}.IsValid = true; {propertyName}.ValidationExplanation = ''; }} " +
                         $"else " +
                         $"{{ {propertyName}.IsValid = false; {propertyName}.ValidationExplanation = 'Collection must contain {conditionText} {countStr} item{(_count == 1 ? "" : "s")}'; }}";

            return script;
        }
    }
}