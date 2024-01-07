using Newtonsoft.Json;

namespace Fig.Web.Models.Setting.ConfigurationModels.DataGrid;

public class DataGridValueModel<T> : IDataGridValueModel
{
    private readonly T? _initialValue;
    private readonly ISetting _parent;
    private T? _rowSavedValue;
    private T? _value;
    private bool _raiseValueChangeEvents;

    public DataGridValueModel(T? value,
        bool isReadOnly,
        ISetting parent,
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
        _parent = parent;
        _rowSavedValue = value;
        _raiseValueChangeEvents = true;
    }

    public T? Value
    {
        get => _value;
        set
        {
            if (JsonConvert.SerializeObject(_value) != JsonConvert.SerializeObject(value))
            {
                _value = value;
                if (_raiseValueChangeEvents)
                    _parent.RunDisplayScript();
            }
        }
    }
    
    public object? ReadOnlyValue => Value;

    public IEnumerable<string>? ValidValues { get; set; }

    public int? EditorLineCount { get; set; }

    public bool IsReadOnly { get; set; }
    
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

    public void SetValue(object value)
    {
        Value = (T)value;
    }
}