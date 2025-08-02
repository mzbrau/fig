using System.Reflection;
using Fig.Common.NetStandard.Scripting;
using Fig.Contracts;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Web.Models.Setting;

namespace Fig.Web.ExtensionMethods;

public static class SettingDefinitionDataContractExtensions
{
    public static object? GetEditableValue(this SettingDefinitionDataContract? dataContract, ISetting parent, bool preferDefault = false)
    {
        if (dataContract is null)
            return null;
        
        if (dataContract.ValueType?.Is(FigPropertyType.DataGrid) != true)
            return dataContract.Value?.GetValue();

        DataGridSettingDataContract? dataGridValue;
        if (dataContract.Value?.GetValue() != null && !preferDefault)
            dataGridValue = (DataGridSettingDataContract)dataContract.Value;
        else if (dataContract.DefaultValue != null)
            dataGridValue = (DataGridSettingDataContract?)dataContract.DefaultValue;
        else
            dataGridValue = new DataGridSettingDataContract(new List<Dictionary<string, object?>>());


        dataGridValue!.Value ??= new List<Dictionary<string, object?>>();

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
                newRow.Add(column.Name,
                    column.ValueType.ConvertToDataGridValueModel(column.IsReadOnly,
                        parent,
                        value,
                        column.ValidValues,
                        column.EditorLineCount,
                        column.ValidationRegex,
                        column.ValidationExplanation,
                        column.IsSecret));
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