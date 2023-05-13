namespace Fig.Datalayer.BusinessEntities.SettingValues;

public class DataGridSettingBusinessEntity : SettingValueBaseBusinessEntity
{
    public DataGridSettingBusinessEntity(List<Dictionary<string, object>>? value)
    {
        Value = value;
    }

    public List<Dictionary<string, object>>? Value { get; set; }
    
    public override object? GetValue()
    {
        return Value;
    }
}