using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Web.ExtensionMethods;

namespace Fig.Web.Models.Setting.ConfigurationModels.DataGrid;

public class DataGridConfigurationModel
{
    public DataGridConfigurationModel(SettingDefinitionDataContract dataContract)
    {
        var definition = dataContract.DataGridDefinition;
        Columns = new List<DataGridColumn>();
        IsLocked = definition!.IsLocked;
        var columnWidths = ColumnWidthHelper.GetColumnWidths(dataContract.Value as DataGridSettingDataContract);
        var evenWidth = $"{(double)100 / definition.Columns.Count}%";
        foreach (var column in definition!.Columns)
        {
            var width = columnWidths.GetValueOrDefault(column.Name, evenWidth);
            Columns.Add(new DataGridColumn(column.Name,
                column.ValueType,
                column.ValidValues,
                column.EditorLineCount,
                column.IsReadOnly,
                column.ValidationRegex,
                column.ValidationExplanation,
                width));
        }
    }

    public List<DataGridColumn> Columns { get; }

    public bool IsLocked { get; }

    public Dictionary<string, IDataGridValueModel> CreateRow(
        DataGridSettingConfigurationModel setting)
    {
        return Columns.ToDictionary(column => column.Name,
            column => column.Type.ConvertToDataGridValueModel(
                isReadOnly: column.IsReadOnly,
                setting,
                validValues: column.ValidValues,
                editorLineCount: column.EditorLineCount,
                validationRegex: column.ValidationRegex,
                validationExplanation: column.ValidationExplanation));
    }
}