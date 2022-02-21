namespace Fig.Web.Models;

public class DataGridColumn
{
    public DataGridColumn(string name, Type type, List<string>? validValues)
    {
        Name = name;
        Type = type;
        ValidValues = validValues;
    }
    
    public string Name { get; set; }
    
    public Type Type { get; set; }
    
    public List<string>? ValidValues { get; }
}