namespace Fig.Web.Models.Setting.ConfigurationModels.DataGrid;

public class DataGridValueModel<T> : IDataGridValueModel
{
    private readonly T? _initialValue;
    private T? _rowSavedValue;

    public DataGridValueModel(T? value,
        bool isReadOnly,
        IEnumerable<string>? validValues = null,
        int? editorLineCount = null,
        string? validationRegex = null,
        string? validationExplanation = null)
    {
        Value = value;
        ValidValues = validValues;
        EditorLineCount = editorLineCount;
        IsReadOnly = isReadOnly;
        ValidationRegex = validationRegex;
        ValidationExplanation = validationExplanation;
        _initialValue = value;
        _rowSavedValue = value;
    }

    public T? Value { get; set; }
    
    public object? ReadOnlyValue => Value;
    
    public IEnumerable<string>? ValidValues { get; }
    
    public int? EditorLineCount { get; }

    public bool IsReadOnly { get; }
    
    public string? ValidationRegex { get; }
    
    public string? ValidationExplanation { get; }

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