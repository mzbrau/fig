using Fig.Common.NetStandard.Scripting;

namespace Fig.Web.Models.Setting.ConfigurationModels.DataGrid;

public class DataGridColumn : IDataGridColumn
{
    public DataGridColumn(string name,
        Type type,
        List<string>? validValues,
        int? editorLineCount,
        bool isReadOnly,
        string? validationRegex,
        string? validationExplanation,
        bool isSecret,
        string startingWidth)
    {
        Name = name;
        Type = type;
        ValidValues = validValues;
        EditorLineCount = editorLineCount;
        IsReadOnly = isReadOnly;
        ValidationRegex = validationRegex;
        ValidationExplanation = validationExplanation;
        IsSecret = isSecret;
        StartingWidth = startingWidth;
    }

    public string Name { get; }

    public Type Type { get; }

    public List<string>? ValidValues { get; }
    
    public int? EditorLineCount { get; }

    public bool IsReadOnly { get; }
    
    public string? ValidationRegex { get; }
    
    public string? ValidationExplanation { get; }
    
    public bool IsSecret { get; }
    
    public string StartingWidth { get; }
}