namespace Fig.Contracts.Settings;

public class DoubleSettingDataContract : SettingValueBaseDataContract
{
    public DoubleSettingDataContract(double value)
    {
        Value = value;
    }

    public double Value { get; set; }
    
    public override object? GetValue()
    {
        return Value;
    }
}