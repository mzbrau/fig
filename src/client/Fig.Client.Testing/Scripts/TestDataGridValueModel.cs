using System.Collections.Generic;
using Fig.Common.NetStandard.Scripting;

namespace Fig.Client.Testing.Scripts;

/// <summary>
/// A test data grid value model for testing display scripts
/// </summary>
public class TestDataGridValueModel : IDataGridValueModel
{
    private object? _value;

    public TestDataGridValueModel(object? initialValue = null)
    {
        _value = initialValue;
        ValidValues = new List<string>();
    }

    public object? ReadOnlyValue => _value;
    
    public IEnumerable<string>? ValidValues { get; set; }
    
    public int? EditorLineCount { get; set; }
    
    public string? ValidationRegex { get; set; }
    
    public string? ValidationExplanation { get; set; }
    
    public bool IsSecret { get; set; }
    
    public bool IsReadOnly { get; set; }
    

    public void RevertAllChanges()
    {
        // Not needed for testing
    }

    public void RevertRowChanged()
    {
        // Not needed for testing
    }

    public void RowSaved()
    {
        // Not needed for testing
    }

    public void SetValue(object value)
    {
        _value = value;
    }
}
