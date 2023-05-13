namespace Fig.Contracts.Settings;

public class IntSettingDataContract : SettingValueBaseDataContract
{
    public IntSettingDataContract(int value)
    {
        Value = value;
    }

    public int Value { get; set; }
    
    public override object? GetValue()
    {
        return Value;
    }
}