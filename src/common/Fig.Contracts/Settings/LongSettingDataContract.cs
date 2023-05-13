namespace Fig.Contracts.Settings;

public class LongSettingDataContract : SettingValueBaseDataContract
{
    public LongSettingDataContract(long value)
    {
        Value = value;
    }

    public long Value { get; set; }
    
    public override object? GetValue()
    {
        return Value;
    }
}