namespace Fig.Datalayer.BusinessEntities.SettingValues;

public class BoolSettingBusinessEntity : SettingValueBaseBusinessEntity
{
    public BoolSettingBusinessEntity(bool value)
    {
        Value = value;
    }

    public bool Value { get; set; }
    
    public override object? GetValue()
    {
        return Value;
    }
}