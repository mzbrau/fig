using System.Reflection;
using Fig.Common.NetStandard.Scripting;
using Fig.Contracts;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Web.Models.Setting;
using Fig.Web.Models.Setting.ConfigurationModels.DataGrid;

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

    /// <summary>
    /// Deep-clones a data-grid editable value without rebuilding from the data contract.
    /// </summary>
    public static List<Dictionary<string, IDataGridValueModel>> CloneDataGridEditableValue(
        List<Dictionary<string, IDataGridValueModel>> source,
        ISetting parent)
    {
        var result = new List<Dictionary<string, IDataGridValueModel>>(source.Count);
        foreach (var row in source)
        {
            var newRow = new Dictionary<string, IDataGridValueModel>(row.Count);
            foreach (var (key, cell) in row)
                newRow[key] = CloneDataGridCell(cell, parent);
            result.Add(newRow);
        }

        return result;
    }

    private static IDataGridValueModel CloneDataGridCell(IDataGridValueModel cell, ISetting parent)
    {
        return cell switch
        {
            DataGridValueModel<int> v => new DataGridValueModel<int>(
                v.Value, v.IsReadOnly, parent, v.ValidValues, v.EditorLineCount, v.ValidationRegex, v.ValidationExplanation, v.IsSecret),
            DataGridValueModel<long> v => new DataGridValueModel<long>(
                v.Value, v.IsReadOnly, parent, v.ValidValues, v.EditorLineCount, v.ValidationRegex, v.ValidationExplanation, v.IsSecret),
            DataGridValueModel<double> v => new DataGridValueModel<double>(
                v.Value, v.IsReadOnly, parent, v.ValidValues, v.EditorLineCount, v.ValidationRegex, v.ValidationExplanation, v.IsSecret),
            DataGridValueModel<bool> v => new DataGridValueModel<bool>(
                v.Value, v.IsReadOnly, parent, v.ValidValues, v.EditorLineCount, v.ValidationRegex, v.ValidationExplanation, v.IsSecret),
            DataGridValueModel<string> v => new DataGridValueModel<string>(
                v.Value, v.IsReadOnly, parent, v.ValidValues, v.EditorLineCount, v.ValidationRegex, v.ValidationExplanation, v.IsSecret),
            DataGridValueModel<DateTime> v => new DataGridValueModel<DateTime>(
                v.Value, v.IsReadOnly, parent, v.ValidValues, v.EditorLineCount, v.ValidationRegex, v.ValidationExplanation, v.IsSecret),
            DataGridValueModel<TimeSpan> v => new DataGridValueModel<TimeSpan>(
                v.Value, v.IsReadOnly, parent, v.ValidValues, v.EditorLineCount, v.ValidationRegex, v.ValidationExplanation, v.IsSecret),
            DataGridValueModel<List<string>> v => new DataGridValueModel<List<string>>(
                v.Value is null ? null : new List<string>(v.Value),
                v.IsReadOnly, parent, v.ValidValues, v.EditorLineCount, v.ValidationRegex, v.ValidationExplanation, v.IsSecret),
            _ => throw new NotSupportedException($"Unsupported data-grid cell type: {cell.GetType().FullName}")
        };
    }
    
    private static object? GetDefault(Type type)
    {
        return type.GetTypeInfo().IsValueType ? Activator.CreateInstance(type) : null;
    }
}