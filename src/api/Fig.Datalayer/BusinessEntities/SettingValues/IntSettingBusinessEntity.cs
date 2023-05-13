namespace Fig.Datalayer.BusinessEntities.SettingValues;

public class IntSettingBusinessEntity : SettingValueBaseBusinessEntity
{
    public IntSettingBusinessEntity(int value)
    {
        Value = value;
    }

    public int Value { get; set; }
    
    public override object? GetValue()
    {
        return Value;
    }
}