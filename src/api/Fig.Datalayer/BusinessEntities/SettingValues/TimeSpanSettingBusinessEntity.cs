namespace Fig.Datalayer.BusinessEntities.SettingValues;

public class TimeSpanSettingBusinessEntity : SettingValueBaseBusinessEntity
{
    public TimeSpanSettingBusinessEntity(TimeSpan value)
    {
        Value = value;
    }

    public TimeSpan Value { get; set; }
    
    public override object? GetValue()
    {
        return Value;
    }
}