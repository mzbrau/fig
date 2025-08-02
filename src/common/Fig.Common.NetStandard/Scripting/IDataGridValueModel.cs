using System.Collections.Generic;

namespace Fig.Common.NetStandard.Scripting;

public interface IDataGridValueModel
{
    object? ReadOnlyValue { get; }
    
    IEnumerable<string>? ValidValues { get; set; }
    
    int? EditorLineCount { get; set; }
    
    string? ValidationRegex { get; }
    
    string? ValidationExplanation { get; }
    
    bool IsSecret { get; }
    
    bool IsReadOnly { get; set; }

    void RevertAllChanges();

    void RevertRowChanged();

    void RowSaved();

    void SetValue(object value);
}
