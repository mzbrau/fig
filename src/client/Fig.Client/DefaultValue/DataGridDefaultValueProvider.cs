using System.Collections;
using System.Collections.Generic;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Client.DefaultValue;

public class DataGridDefaultValueProvider : IDataGridDefaultValueProvider
{
    public List<Dictionary<string, object?>>? Convert(object? value, List<DataGridColumnDataContract> columns)
    {
        if (value?.GetType().IsSupportedDataGridType() != true)
            return null;

        if (columns.Count != 1)
            return null; // We don't support default values for complex objects...yet

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
    
}