using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;
using Fig.Web.Models.Setting.ConfigurationModels.DataGrid;

namespace Fig.Web.ExtensionMethods;

public static class SettingDefinitionDataContractExtensions
{
    public static dynamic GetEditableValue(this SettingDefinitionDataContract dataContract)
    {
        if (dataContract.ValueType.FullName != SupportedTypes.DataGrid)
            return dataContract.Value;

        if (dataContract.Value == null)
            dataContract.Value = new List<Dictionary<string, object>>();

        var result = new List<Dictionary<string, IDataGridValueModel>>();

        foreach (Dictionary<string, object> row in dataContract.Value)
        {
            var newRow = new Dictionary<string, IDataGridValueModel>();
            foreach (var column in dataContract.DataGridDefinition.Columns)
            {
                var value = row[column.Name];
                newRow.Add(column.Name, column.Type.ConvertToDataGridValueModel(value));
            }

            result.Add(newRow);
        }

        return result;
    }
}