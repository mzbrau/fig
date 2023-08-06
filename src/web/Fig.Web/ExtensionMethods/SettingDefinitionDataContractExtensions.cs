using System.Reflection;
using Fig.Contracts;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Web.Models.Setting.ConfigurationModels.DataGrid;

namespace Fig.Web.ExtensionMethods;

public static class SettingDefinitionDataContractExtensions
{
    public static object? GetEditableValue(this SettingDefinitionDataContract? dataContract)
    {
        if (dataContract is null)
            return null;
        
        if (!dataContract.ValueType?.Is(FigPropertyType.DataGrid) == true)
            return dataContract.Value?.GetValue();

        var dataGridValue = (DataGridSettingDataContract?)dataContract.Value ??
                            new DataGridSettingDataContract(new List<Dictionary<string, object?>>());

        dataGridValue.Value ??= new List<Dictionary<string, object?>>();

        var result = new List<Dictionary<string, IDataGridValueModel>>();

        foreach (Dictionary<string, object?> row in dataGridValue.Value)
        {
            var newRow = new Dictionary<string, IDataGridValueModel>();
            foreach (var column in dataContract.DataGridDefinition?.Columns ?? Array.Empty<DataGridColumnDataContract>().ToList())
            {
                if (!row.TryGetValue(column.Name, out var value))
                {
                    value = GetDefault(column.ValueType);
                }
                newRow.Add(column.Name, column.ValueType.ConvertToDataGridValueModel(value, column.ValidValues, column.EditorLineCount));
            }

            result.Add(newRow);
        }

        return result;
    }

    public static object? GetDefaultValue(this SettingDefinitionDataContract dataContract)
    {
        return dataContract.DefaultValue?.GetValue();
    }
    
    private static object? GetDefault(Type type)
    {
        return type.GetTypeInfo().IsValueType ? Activator.CreateInstance(type) : null;
    }
}