using System.ComponentModel;
using System.Globalization;
using Fig.Api.Datalayer.Repositories;

namespace Fig.Api.Utils;

public class ValidValuesHandler : IValidValuesHandler
{
    private const string ValueSeparator = "->";
    private readonly ILookupTablesRepository _lookupTablesRepository;

    public ValidValuesHandler(ILookupTablesRepository lookupTablesRepository)
    {
        _lookupTablesRepository = lookupTablesRepository;
    }

    public List<string>? GetValidValues(IList<string>? validValuesProperty, string? lookupTableKey,
        Type valueType, dynamic? value)
    {
        if (validValuesProperty != null)
            return validValuesProperty.ToList();

        if (lookupTableKey == null)
            return null;

        var match = _lookupTablesRepository.GetItem(lookupTableKey);

        if (match == null)
            return null;

        var result = new List<string>();


        foreach (var (key, description) in match.LookupTable)
            if (TryParse(key, valueType, out _))
                result.Add($"{key.ToString(CultureInfo.InvariantCulture)} {ValueSeparator} {description}");

        if (!result.Any())
            return null;

        if (value != null && !match.LookupTable.ContainsKey(value!.ToString()))
            result.Insert(0, $"{value} {ValueSeparator} [INVALID]");

        return result;
    }

    public dynamic? GetValue(dynamic? value, Type valueType, IList<string>? validValuesProperty,
        string? lookupTableKey)
    {
        if (value == null || lookupTableKey == null && validValuesProperty == null)
            return value;

        string stringValue = value!.ToString();
        var separatorIndex = stringValue.IndexOf(ValueSeparator, StringComparison.InvariantCulture);
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