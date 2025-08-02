using Fig.Common.NetStandard.Scripting;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Web.ExtensionMethods;

namespace Fig.Web.Models.Setting.ConfigurationModels.DataGrid;

public class DataGridConfigurationModel : IDataGridConfigurationModel
{
    public DataGridConfigurationModel(SettingDefinitionDataContract dataContract)
    {
        var definition = dataContract.DataGridDefinition;
        Columns = new List<IDataGridColumn>();
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
                column.IsSecret,
                width));
        }
    }

    public List<IDataGridColumn> Columns { get; }

    public bool IsLocked { get; }

    public Dictionary<string, IDataGridValueModel> CreateRow(
        DataGridSettingConfigurationModel setting)
    {
        return Columns.ToDictionary<IDataGridColumn, string, IDataGridValueModel>(column => column.Name,
            column => column.Type.ConvertToDataGridValueModel(
                isReadOnly: column.IsReadOnly,
                parent: setting,
                validValues: column.ValidValues,
                editorLineCount: column.EditorLineCount,
                validationRegex: column.ValidationRegex,
                validationExplanation: column.ValidationExplanation,
                isSecret: column.IsSecret));
    }
}