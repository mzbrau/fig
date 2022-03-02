using System.Reflection;
using Fig.Contracts;
using Fig.Contracts.ExtensionMethods;
using Fig.Contracts.SettingDefinitions;
using Fig.Web.Models.Setting.ConfigurationModels.DataGrid;

namespace Fig.Web.ExtensionMethods;

public static class SettingDefinitionDataContractExtensions
{
    public static dynamic GetEditableValue(this SettingDefinitionDataContract dataContract)
    {
        if (!dataContract.ValueType.Is(FigPropertyType.DataGrid))
            return dataContract.Value;

        if (dataContract.Value == null)
            dataContract.Value = new List<Dictionary<string, object>>();

        var result = new List<Dictionary<string, IDataGridValueModel>>();

        foreach (Dictionary<string, object> row in dataContract.Value)
        {
            var newRow = new Dictionary<string, IDataGridValueModel>();
            foreach (var column in dataContract.DataGridDefinition.Columns)
            {
                var value = row.ContainsKey(column.Name) ? 
                    row[column.Name] : 
                    GetDefault(column.Type);
                newRow.Add(column.Name, column.Type.ConvertToDataGridValueModel(value, column.ValidValues));
            }

            result.Add(newRow);
        }

        return result;
    }
    
    private static object? GetDefault(Type type)
    {
        return type.GetTypeInfo().IsValueType ? Activator.CreateInstance(type) : null;
    }
}