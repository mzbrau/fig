namespace Fig.Contracts.Settings;

public class StringSettingDataContract : SettingValueBaseDataContract
{
    public StringSettingDataContract(string? value)
    {
        Value = value;
    }

    public string? Value { get; set; }
    
    public override object? GetValue()
    {
        return Value;
    }
}