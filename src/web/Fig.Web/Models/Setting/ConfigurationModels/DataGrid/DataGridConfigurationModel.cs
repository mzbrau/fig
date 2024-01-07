using Fig.Contracts.SettingDefinitions;
using Fig.Web.ExtensionMethods;

namespace Fig.Web.Models.Setting.ConfigurationModels.DataGrid;

public class DataGridConfigurationModel
{
    public DataGridConfigurationModel(DataGridDefinitionDataContract dataContract)
    {
        Columns = new List<DataGridColumn>();
        IsLocked = dataContract.IsLocked;
        foreach (var column in dataContract.Columns)
            Columns.Add(new DataGridColumn(column.Name,
                column.ValueType,
                column.ValidValues,
                column.EditorLineCount,
                column.IsReadOnly,
                column.ValidationRegex,
                column.ValidationExplanation));
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