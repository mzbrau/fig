namespace Fig.Web.Models.Setting.ConfigurationModels.DataGrid;

public class DataGridColumn
{
    public DataGridColumn(string name, Type type, List<string>? validValues, int? editorLineCount)
    {
        Name = name;
        Type = type;
        ValidValues = validValues;
        EditorLineCount = editorLineCount;
    }

    public string Name { get; }

    public Type Type { get; }

    public List<string>? ValidValues { get; }
    
    public int? EditorLineCount { get; }
}