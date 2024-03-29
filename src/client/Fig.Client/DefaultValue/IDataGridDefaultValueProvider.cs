using System.Collections.Generic;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Client.DefaultValue;

internal interface IDataGridDefaultValueProvider
{
    List<Dictionary<string, object?>>? Convert(object? value, List<DataGridColumnDataContract> columns);
}