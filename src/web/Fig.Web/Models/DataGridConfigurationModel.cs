using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;
using Fig.Web.ExtensionMethods;

namespace Fig.Web.Models;

public class DataGridConfigurationModel
{
    public DataGridConfigurationModel(DataGridDefinitionDataContract dataContract)
    {
        Columns = new List<DataGridColumn>();
        foreach (var column in dataContract.Columns)
        {
            Columns.Add(new DataGridColumn(column.Name, column.Type, column.ValidValues));
        }
    }
    
    public List<DataGridColumn> Columns { get; }

    public Dictionary<string, IDataGridValueModel> CreateRow()
    {
        return Columns.ToDictionary(column => column.Name, column => column.Type.ConvertToDataGridValueModel());
    }
}