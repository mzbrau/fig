using System.Collections.Generic;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Client.DefaultValue;

internal class DataGridDefaultValueProvider : IDataGridDefaultValueProvider
{
    public List<Dictionary<string, object?>>? Convert(object? value, List<DataGridColumnDataContract> columns)
    {
        return DataGridValueConverter.Convert(value, columns);
    }
}
