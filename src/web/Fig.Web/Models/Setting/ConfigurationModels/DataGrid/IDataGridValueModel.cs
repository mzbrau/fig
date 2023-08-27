namespace Fig.Web.Models.Setting.ConfigurationModels.DataGrid;

public interface IDataGridValueModel
{
    object? ReadOnlyValue { get; }
    
    IEnumerable<string>? ValidValues { get; }
    
    int? EditorLineCount { get; }
    
    string? ValidationRegex { get; }
    
    string? ValidationExplanation { get; }

    void RevertAllChanges();

    void RevertRowChanged();

    void RowSaved();
}