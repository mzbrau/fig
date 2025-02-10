using System.ComponentModel;
using System.Globalization;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Factories;
using Fig.Common.NetStandard.Utils;
using Fig.Datalayer.BusinessEntities.SettingValues;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Api.Utils;

public class ValidValuesHandler : IValidValuesHandler
{
    private const string ValueSeparator = "->";
    private readonly ILookupTablesRepository _lookupTablesRepository;

    public ValidValuesHandler(ILookupTablesRepository lookupTablesRepository)
    {
        _lookupTablesRepository = lookupTablesRepository;
    }

    public async Task<List<string>?> GetValidValues(IList<string>? validValuesProperty, string? lookupTableKey, 
        Type valueType, SettingValueBaseBusinessEntity? value)
    {
        if (validValuesProperty != null)
            return validValuesProperty.ToList();

        if (lookupTableKey == null)
            return null;

        var match = await _lookupTablesRepository.GetItem(lookupTableKey);

        if (match == null || value == null)
            return null;

        var result = new List<string>();

        Type baseValueType = valueType;
        if (valueType.IsSupportedDataGridType())
        {
            if (ListUtilities.TryGetGenericListType(valueType, out var listType) && listType is not null)
            {
                baseValueType = listType;
            }
        }

        foreach (var (key, description) in match.LookupTable)
            if (TryParse(key, baseValueType, out _))
                result.Add($"{key.ToString(CultureInfo.InvariantCulture)} {ValueSeparator} {description}");

        if (!result.Any())
            return null;
        
        AddExistingInvalidValues();

        return result;
        
        void AddExistingInvalidValues()
        {
            if (value is DataGridSettingBusinessEntity && 
                value.GetValue() is List<Dictionary<string, object>> items &&
                items.Any())
            {
                var firstColumnValues = items
                    .Select(a => a.Values.FirstOrDefault())
                    .Where(a => a is not null);
                foreach (var val in firstColumnValues)
                {
                    if (!match.LookupTable.ContainsKey(Convert.ToString(val, CultureInfo.InvariantCulture)!))
                    {
                        result.Insert(0, $"{val} {ValueSeparator} [INVALID]");
                    }
                }
            }
            else if (value is not DataGridSettingBusinessEntity)
            {
                if (value.GetValue() != null && !match.LookupTable.ContainsKey(Convert.ToString(value.GetValue(), CultureInfo.InvariantCulture)!))
                    result.Insert(0, $"{Convert.ToString(value.GetValue(), CultureInfo.InvariantCulture)} {ValueSeparator} [INVALID]");
            }
        }
    }

    public SettingValueBaseBusinessEntity? GetValue(SettingValueBaseBusinessEntity? value, IList<string>? validValuesProperty,
        Type valueType, string? lookupTableKey, DataGridDefinitionDataContract? dataGridDefinition)
    {
        if (value == null || value.GetValue() == null || lookupTableKey == null && validValuesProperty == null)
            return value;

        if (value is DataGridSettingBusinessEntity dataGridValue && 
            dataGridValue.GetValue() is List<Dictionary<string, object>> items &&
            items.Any() &&
            dataGridDefinition is not null && 
            dataGridDefinition.Columns.Any())
        {
            var columnDefinition = dataGridDefinition.Columns.First();
            var firstColumnValues = items
                .Select(a => a.FirstOrDefault()).ToList();

            for (int i = 0; i < firstColumnValues.Count(); i++)
            {
                var kvp = firstColumnValues[i];
                var valuePart = ExtractValuePart(kvp.Value);
                if (TryParseDataGridValue(valuePart, columnDefinition.ValueType, out var parsedValue) && parsedValue is not null)
                {
                    items[i][kvp.Key] = parsedValue;
                }
            }
        }
        else
        {
            var valuePart = ExtractValuePart(value.GetValue()!);
            if (valuePart is not null && TryParse(valuePart, valueType, out var parsedValue))
                return parsedValue;
        }
        
        

        return value;

        string? ExtractValuePart(object val)
        {
            string stringValue = val.ToString()!;
            var separatorIndex = stringValue.IndexOf(ValueSeparator, StringComparison.InvariantCulture);
            if (separatorIndex > 0)
            {
                return stringValue.Substring(0, separatorIndex).Trim();
            }

            return null;
        }
    }

    public SettingValueBaseBusinessEntity GetValueFromValidValues(object? value, IList<string> validValues)
    {
        if (value == null)
            return new StringSettingBusinessEntity(validValues.First());

        if (value is List<Dictionary<string, object>> list)
        {
            foreach (var row in list)
            {
                var firstColumn = row.FirstOrDefault();
                row[firstColumn.Key] = GetRawValueFromValidValues(firstColumn.Value, validValues);
            }

            return new DataGridSettingBusinessEntity(list);
        }

        var match = GetRawValueFromValidValues(value, validValues);

        return new StringSettingBusinessEntity(match);
    }

    private static string GetRawValueFromValidValues(object? value, IList<string> validValues)
    {
        var stringValue = value?.ToString();

        if (stringValue is not null)
        {
            var match = validValues.FirstOrDefault(a => a.StartsWith(stringValue));
            if (match is not null)
                return match;
        }
        
        return validValues.First();
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
    
    private static bool TryParseDataGridValue(string? input, Type targetType, out object? value)
    {
        if (input is null)
        {
            value = null;
            return false;
        }
        
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