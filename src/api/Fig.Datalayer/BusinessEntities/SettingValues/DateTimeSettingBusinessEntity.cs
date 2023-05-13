namespace Fig.Datalayer.BusinessEntities.SettingValues;

public class DateTimeSettingBusinessEntity : SettingValueBaseBusinessEntity
{
    public DateTimeSettingBusinessEntity(DateTime? value)
    {
        Value = value;
    }

    public DateTime? Value { get; set; }
    
    public override object? GetValue()
    {
        return Value;
    }
}