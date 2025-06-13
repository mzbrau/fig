namespace Fig.Web.Models.LookupTables;

public class LookupTablesItemModel
{
    public LookupTablesItemModel(string key, string? alias)
    {
        Key = key;
        Alias = alias;
    }

    public string Key { get; set; }

    public string? Alias { get; set; }
}