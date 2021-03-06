namespace Fig.Web.Models.Setting.ConfigurationModels.DataGrid;

public class DataGridValueModel<T> : IDataGridValueModel
{
    private readonly T? _initialValue;
    private T? _rowSavedValue;

    public DataGridValueModel(T? value, IEnumerable<string>? validValues = null)
    {
        Value = value;
        ValidValues = validValues;
        _initialValue = value;
        _rowSavedValue = value;
    }

    public T? Value { get; set; }
    
    public object? ReadOnlyValue => Value;
    
    public IEnumerable<string>? ValidValues { get; }

    public void RevertRowChanged()
    {
        Value = _rowSavedValue;
    }

    public void RevertAllChanges()
    {
        Value = _initialValue;
    }

    public void RowSaved()
    {
        _rowSavedValue = Value;
    }
}