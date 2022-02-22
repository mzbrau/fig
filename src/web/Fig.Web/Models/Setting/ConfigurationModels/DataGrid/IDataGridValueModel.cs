namespace Fig.Web.Models.Setting.ConfigurationModels.DataGrid;

public interface IDataGridValueModel
{
    object? ReadOnlyValue { get; }

    void RevertAllChanges();

    void RevertRowChanged();

    void RowSaved();
}