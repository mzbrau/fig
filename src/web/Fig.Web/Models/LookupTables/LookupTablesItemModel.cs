namespace Fig.Web.Models.LookupTables;

public class LookupTablesItemModel
{
    public LookupTablesItemModel(string key, string value)
    {
        Key = key;
        Value = value;
    }

    public string Key { get; set; }

    public string Value { get; set; }
}