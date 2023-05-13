namespace Fig.Datalayer.BusinessEntities.SettingValues;

public class StringSettingBusinessEntity : SettingValueBaseBusinessEntity
{
    public StringSettingBusinessEntity(string? value)
    {
        Value = value;
    }

    public string? Value { get; set; }
    
    public override object? GetValue()
    {
        return Value;
    }
}