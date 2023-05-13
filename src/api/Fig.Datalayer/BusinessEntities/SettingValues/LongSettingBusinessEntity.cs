namespace Fig.Datalayer.BusinessEntities.SettingValues;

public class LongSettingBusinessEntity : SettingValueBaseBusinessEntity
{
    public LongSettingBusinessEntity(long value)
    {
        Value = value;
    }

    public long Value { get; set; }
    
    public override object? GetValue()
    {
        return Value;
    }
}