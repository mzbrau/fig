namespace Fig.Web.Models.Setting.ConfigurationModels.DataGrid;

public interface IDataGridValueModel
{
    object? ReadOnlyValue { get; }
    
    IEnumerable<string>? ValidValues { get; }

    void RevertAllChanges();

    void RevertRowChanged();

    void RowSaved();
}