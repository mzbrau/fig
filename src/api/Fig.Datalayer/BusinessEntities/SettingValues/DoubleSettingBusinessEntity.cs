namespace Fig.Datalayer.BusinessEntities.SettingValues;

public class DoubleSettingBusinessEntity : SettingValueBaseBusinessEntity
{
    public DoubleSettingBusinessEntity(double value)
    {
        Value = value;
    }

    public double Value { get; set; }
    
    public override object? GetValue()
    {
        return Value;
    }
}