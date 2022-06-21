using System.ComponentModel;
using System.Globalization;
using Fig.Api.Datalayer.Repositories;

namespace Fig.Api.Utils;

public class ValidValuesHandler : IValidValuesHandler
{
    private const string ValueSeparator = "->";
    private readonly ICommonEnumerationsRepository _commonEnumerationsRepository;

    public ValidValuesHandler(ICommonEnumerationsRepository commonEnumerationsRepository)
    {
        _commonEnumerationsRepository = commonEnumerationsRepository;
    }

    public List<string>? GetValidValues(IList<string>? validValuesProperty, string? commonEnumerationKey,
        Type valueType, dynamic? value)
    {
        if (validValuesProperty != null)
            return validValuesProperty.ToList();

        if (commonEnumerationKey == null)
            return null;

        var match = _commonEnumerationsRepository.GetItem(commonEnumerationKey);

        if (match == null)
            return null;

        var result = new List<string>();


        foreach (var (key, description) in match.Enumeration)
            if (TryParse(key, valueType, out _))
                result.Add($"{key.ToString(CultureInfo.InvariantCulture)} {ValueSeparator} {description}");

        if (!result.Any())
            return null;

        if (value != null && !match.Enumeration.ContainsKey(value.ToString()))
            result.Insert(0, $"{value} {ValueSeparator} [INVALID]");

        return result;
    }

    public dynamic? GetValue(dynamic? value, Type valueType, IList<string>? validValuesProperty,
        string? commonEnumerationKey)
    {
        if (value == null || commonEnumerationKey == null && validValuesProperty == null)
            return value;

        string stringValue = value.ToString();
        var separatorIndex = stringValue.IndexOf(ValueSeparator);
        if (separatorIndex > 0)
        {
            var valuePart = stringValue.Substring(0, separatorIndex).Trim();
            if (TryParse(valuePart, valueType, out var parsedValue))
                return parsedValue;
        }

        return value;
    }

    public dynamic GetValueFromValidValues(dynamic value, IList<string> validValues)
    {
        if (value == null)
            return validValues.First();

        if (value is List<Dictionary<string, object>> list)
        {
            foreach (var column in list)
            foreach (var row in column)
                column[row.Key] = GetValueFromValidValues(row.Value, validValues);

            return list;
        }

        var stringValue = value.ToString();

        var match = validValues.FirstOrDefault(a => a.StartsWith(stringValue));

        if (match != null)
            return match;

        return validValues.First();
    }

    private static bool TryParse(string input, Type targetType, out dynamic? value)
    {
        try
        {
            value = TypeDescriptor.GetConverter(targetType).ConvertFromString(input);
            return true;
        }
        catch
        {
            value = null;
            return false;
        }
    }
}