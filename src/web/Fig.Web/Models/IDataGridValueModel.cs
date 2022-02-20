namespace Fig.Web.Models;

public interface IDataGridValueModel
{
    object? ReadOnlyValue { get; }

    void RevertAllChanges();

    void RevertRowChanged();

    void RowSaved();
}