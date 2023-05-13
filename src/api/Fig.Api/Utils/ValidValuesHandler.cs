using System.ComponentModel;
using System.Globalization;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Factories;
using Fig.Datalayer.BusinessEntities.SettingValues;

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
        Type valueType, SettingValueBaseBusinessEntity? value)
    {
        if (validValuesProperty != null)
            return validValuesProperty.ToList();

        if (lookupTableKey == null)
            return null;

        var match = _lookupTablesRepository.GetItem(lookupTableKey);

        if (match == null || value == null)
            return null;

        var result = new List<string>();


        foreach (var (key, description) in match.LookupTable)
            if (TryParse(key, valueType, out _))
                result.Add($"{key.ToString(CultureInfo.InvariantCulture)} {ValueSeparator} {description}");

        if (!result.Any())
            return null;

        if (value.GetValue() != null && !match.LookupTable.ContainsKey(value.GetValue()!.ToString()!))
            result.Insert(0, $"{value.GetValue()} {ValueSeparator} [INVALID]");

        return result;
    }

    public SettingValueBaseBusinessEntity? GetValue(SettingValueBaseBusinessEntity? value, IList<string>? validValuesProperty,
        Type valueType, string? lookupTableKey)
    {
        if (value == null || value.GetValue() == null || lookupTableKey == null && validValuesProperty == null)
            return value;

        string stringValue = value.GetValue()!.ToString()!;
        var separatorIndex = stringValue.IndexOf(ValueSeparator, StringComparison.InvariantCulture);
        if (separatorIndex > 0)
        {
            var valuePart = stringValue.Substring(0, separatorIndex).Trim();
            if (TryParse(valuePart, valueType, out var parsedValue))
                return parsedValue;
        }

        return value;
    }

    public SettingValueBaseBusinessEntity GetValueFromValidValues(object? value, IList<string> validValues)
    {
        if (value == null)
            return new StringSettingBusinessEntity(validValues.First());

        if (value is List<Dictionary<string, object>> list)
        {
            foreach (var column in list)
            foreach (var row in column)
                column[row.Key] = GetValueFromValidValues(row.Value, validValues);

            return new DataGridSettingBusinessEntity(list);
        }

        var stringValue = value.ToString();

        var match = validValues.FirstOrDefault(a => a.StartsWith(stringValue));

        return match != null
            ? new StringSettingBusinessEntity(match)
            : new StringSettingBusinessEntity(validValues.First());
    }

    private static bool TryParse(string input, Type targetType, out SettingValueBaseBusinessEntity? value)
    {
        try
        {
            var convertedValue = TypeDescriptor.GetConverter(targetType).ConvertFromString(input);
            value = ValueBusinessEntityFactory.CreateBusinessEntity(convertedValue, targetType);
            return true;
        }
        catch
        {
            value = null;
            return false;
        }
    }
}