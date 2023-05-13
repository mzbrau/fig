namespace Fig.Contracts.Settings;

public class BoolSettingDataContract : SettingValueBaseDataContract
{
    public BoolSettingDataContract(bool value)
    {
        Value = value;
    }

    public bool Value { get; set; }
    
    public override object GetValue()
    {
        return Value;
    }
}