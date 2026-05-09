using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.SettingDefinitions;
using Newtonsoft.Json.Linq;

namespace Fig.Client.DefaultValue;

internal static class DataGridValueConverter
{
    private const string SimpleValueColumnName = "Values";

    public static List<Dictionary<string, object?>>? Convert(
        object? value,
        IReadOnlyList<DataGridColumnDataContract>? columns = null)
    {
        if (value is null)
            return null;

        if (value is List<Dictionary<string, object?>> nullableRows)
            return nullableRows;

        if (value is List<Dictionary<string, object>> rows)
            return rows.Select(row => row.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value)).ToList();

        if (value is JArray array)
            return array.ToObject<List<Dictionary<string, object?>>>();

        if (value.GetType().IsSupportedDataGridType() != true)
            return null;

        var elementType = value.GetType().GetGenericArguments().FirstOrDefault();
        var isSimpleElementType = elementType == null ||
                                  elementType == typeof(string) ||
                                  elementType.IsPrimitive ||
                                  elementType == typeof(decimal);

        if ((columns == null || columns.Count == 1) && isSimpleElementType)
            return GetSingleColumnValue(value, columns?.FirstOrDefault()?.Name ?? SimpleValueColumnName);

        return GetMultiColumnValue(value, columns);
    }

    private static List<Dictionary<string, object?>> GetSingleColumnValue(object value, string columnName)
    {
        var result = new List<Dictionary<string, object?>>();
        foreach (var item in (IEnumerable)value)
        {
            result.Add(new Dictionary<string, object?>
            {
                { columnName, item }
            });
        }

        return result;
    }

    private static List<Dictionary<string, object?>> GetMultiColumnValue(
        object value,
        IReadOnlyList<DataGridColumnDataContract>? columns)
    {
        var result = new List<Dictionary<string, object?>>();
        var columnNames = columns == null
            ? null
            : new HashSet<string>(columns.Select(a => a.Name));

        foreach (var item in (IEnumerable)value)
            result.Add(GetValidPropertiesAndValues(columnNames, item));

        return result;
    }

    private static Dictionary<string, object?> GetValidPropertiesAndValues(ISet<string>? columnNames, object item)
    {
        var result = new Dictionary<string, object?>();
        var properties = item.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (var property in properties.Where(a => columnNames == null || columnNames.Contains(a.Name)))
        {
            var value = property.GetValue(item, null);
            if (property.PropertyType.IsEnum())
                value = value?.ToString();

            result.Add(property.Name, value);
        }

        return result;
    }
}
