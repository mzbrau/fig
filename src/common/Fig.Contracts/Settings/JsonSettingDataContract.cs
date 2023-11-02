namespace Fig.Contracts.Settings;

public class JsonSettingDataContract : SettingValueBaseDataContract
{
    public JsonSettingDataContract(string? value)
    {
        Value = value;
    }

    public string? Value { get; set; }

    public override object? GetValue()
    {
        return Value;
    }
}