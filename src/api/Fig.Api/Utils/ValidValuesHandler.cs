using System.ComponentModel;
using System.Globalization;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Factories;
using Fig.Common.NetStandard.Utils;
using Fig.Datalayer.BusinessEntities.SettingValues;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.SettingDefinitions;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Utils;

public class ValidValuesHandler : IValidValuesHandler
{
    private const string ValueSeparator = "->";
    private readonly ILookupTablesRepository _lookupTablesRepository;
    private readonly ILogger<ValidValuesHandler> _logger;
    private IList<LookupTableBusinessEntity>? _lookupTables;
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    public ValidValuesHandler(ILookupTablesRepository lookupTablesRepository, ILogger<ValidValuesHandler> logger)
    {
        _lookupTablesRepository = lookupTablesRepository;
        _logger = logger;
    }

    public async Task<List<string>?> GetValidValues(IList<string>? validValuesProperty, string? lookupTableKey,
        Type? valueType, SettingValueBaseBusinessEntity? value)
    {
        try
        {
            return await GetValidValuesInternal(validValuesProperty, lookupTableKey, valueType, value);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting valid values with lookup table key {LookupTableKey}", lookupTableKey);
        }

        return null;
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
            if (TryParse(valuePart, valueType, out var parsedValue))
                return parsedValue;
        }

        return value;

        string ExtractValuePart(object val)
        {
            var stringValue = val.ToString()!;

            if (!stringValue.Contains(ValueSeparator))
                return stringValue;
            
            var separatorIndex = stringValue.IndexOf(ValueSeparator, StringComparison.InvariantCulture);
            return stringValue.Substring(0, separatorIndex).Trim();
        }
    }

    public SettingValueBaseBusinessEntity GetValueFromValidValues(object? value, IList<string> validValues,
        DataGridDefinitionDataContract? dataGridDefinition, string? lookupKeySettingName = null)
    {
        if (value == null && dataGridDefinition is null)
            return new StringSettingBusinessEntity(validValues.First());

        if (value is null)
            return new DataGridSettingBusinessEntity(null);

        if (value is List<Dictionary<string, object>> list)
        {
            foreach (var row in list)
            {
                var firstColumn = row.FirstOrDefault();
                row[firstColumn.Key] = GetRawValueFromValidValues(firstColumn.Value, validValues, lookupKeySettingName);
            }

            return new DataGridSettingBusinessEntity(list!);
        }

        var match = GetRawValueFromValidValues(value, validValues, lookupKeySettingName);

        return new StringSettingBusinessEntity(match);
    }

    private async Task<List<string>?> GetValidValuesInternal(IList<string>? validValuesProperty, string? lookupTableKey,
        Type? valueType, SettingValueBaseBusinessEntity? value)
    {
        if (validValuesProperty != null)
            return validValuesProperty.ToList();

        if (lookupTableKey == null || valueType == null)
            return null;

        var match = await GetMatchingLookupTable(lookupTableKey);

        if (match == null)
            return null;

        var result = new List<string>();

        var baseValueType = valueType;
        if (valueType.IsSupportedDataGridType())
        {
            if (ListUtilities.TryGetGenericListType(valueType, out var listType) && listType is not null)
            {
                baseValueType = listType;
            }
        }
        
        foreach (var (key, alias) in match.LookupTable)
        {
            if (TryParse(key, baseValueType, out _))
            {
                result.Add(string.IsNullOrWhiteSpace(alias)
                    ? key.ToString(CultureInfo.InvariantCulture)
                    : $"{key.ToString(CultureInfo.InvariantCulture)} {ValueSeparator} {alias}");
            }
        }
        
        if (!result.Any())
            return null;

        var suffixes = new HashSet<string>();
        AddExistingInvalidValues();

        return result;

        void AddExistingInvalidValues()
        {
            var aliasPart = $" {ValueSeparator} [INVALID]";

            if (value is null)
            {
                result.Insert(0, aliasPart);
            }
            else if (value is DataGridSettingBusinessEntity &&
                value.GetValue() is List<Dictionary<string, object>> items &&
                items.Any())
            {
                var firstColumnValues = items
                    .Select(a => a.Values.FirstOrDefault())
                    .Where(a => a is not null);
                foreach (var val in firstColumnValues)
                {
                    var valString = Convert.ToString(val, CultureInfo.InvariantCulture)!;
                    if (!IsValueValidInLookupTable(valString, match.LookupTable))
                    {
                        result.Insert(0, $"{val}{aliasPart}");
                    }
                }
            }
            else if (value is not DataGridSettingBusinessEntity)
            {
                if (value?.GetValue() != null)
                {
                    var valString = Convert.ToString(value.GetValue(), CultureInfo.InvariantCulture)!;
                    if (!IsValueValidInLookupTable(valString, match.LookupTable))
                    {
                        result.Insert(0, $"{valString}{aliasPart}");
                    }
                }
            }
        }
        
        bool IsValueValidInLookupTable(string valueToCheck, Dictionary<string, string?> lookupTable)
        {
            if (lookupTable.ContainsKey(valueToCheck))
                return true;

            if (!suffixes.Any())
                PopulateCachedSuffixes(lookupTable);
            
            return suffixes.Contains(valueToCheck);
        }
        
        void PopulateCachedSuffixes(Dictionary<string, string?> lookupTable)
        {
            // Cache suffixes in a HashSet for efficient lookups
            foreach (var key in lookupTable.Keys)
            {
                if (key.Contains(']'))
                {
                    var suffix = key.Substring(key.IndexOf(']') + 1);
                    suffixes.Add(suffix);
                }
            }
        }
    }

    private async Task<LookupTableBusinessEntity?> GetMatchingLookupTable(string key)
    {

        try
        {
            if (await _semaphoreSlim.WaitAsync(TimeSpan.FromSeconds(5)))
            {
                _lookupTables ??= await _lookupTablesRepository.GetAllItems();
            }
        }
        finally
        {
            _semaphoreSlim.Release();
        }

        return _lookupTables?.FirstOrDefault(a => a.Name == key);
    }

    private static string GetRawValueFromValidValues(object? value, IList<string> validValues, string? lookupKeySettingName = null)
    {
        var stringValue = value?.ToString();

        if (stringValue is not null)
        {
            var exactMatch = validValues.FirstOrDefault(a => a == stringValue);
            if (exactMatch is not null)
                return exactMatch;
            
            // If no direct match and we have a lookup key setting, check for prefixed values like [prefix]displayValue
            // This handles the case where user provides "Item1" and valid values contain "[Bug]Item1"
            if (!string.IsNullOrWhiteSpace(lookupKeySettingName))
            {
                var prefixMatch = validValues.FirstOrDefault(a => 
                    a.Contains(']') && 
                    a.Substring(a.IndexOf(']') + 1) == stringValue);
                if (prefixMatch is not null)
                    return stringValue; // Return the display value, not the prefixed value
            }
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