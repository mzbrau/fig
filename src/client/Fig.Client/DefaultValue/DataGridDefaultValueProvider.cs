using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Client.DefaultValue;

internal class DataGridDefaultValueProvider : IDataGridDefaultValueProvider
{
    public List<Dictionary<string, object?>>? Convert(object? value, List<DataGridColumnDataContract> columns)
    {
        if (value?.GetType().IsSupportedDataGridType() != true)
            return null;

        return columns.Count == 1 ? 
            GetSingleColumnDefault(value, columns) : 
            GetMultiColumnDefault(value, columns);
    }

    private List<Dictionary<string, object?>> GetSingleColumnDefault(object value, List<DataGridColumnDataContract> columns)
    {
        var result = new List<Dictionary<string, object?>>();
        foreach (var item in (IEnumerable)value)
        {
            result.Add(new Dictionary<string, object?>()
            {
                { columns[0].Name, item }
            });
        }

        return result;
    }

    private List<Dictionary<string, object?>> GetMultiColumnDefault(object value, List<DataGridColumnDataContract> columns)
    {
        var result = new List<Dictionary<string, object?>>();
        foreach (var item in (IEnumerable)value)
        {
            var properties = GetValidPropertiesAndValues(columns.Select(a => a.Name), item);
            result.Add(properties);
        }

        return result;
    }

    private Dictionary<string, object?> GetValidPropertiesAndValues(IEnumerable<string> columnNames, object item)
    {
        var result = new Dictionary<string, object?>();
        var properties = item.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (var property in properties.Where(a => columnNames.Contains(a.Name)))
        {
            var value = property.GetValue(item, null);
            if (property.PropertyType.IsEnum())
            {
                value = value.ToString();
            }
            
            result.Add(property.Name, value);
        }

        return result;
    }
}