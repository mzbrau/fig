using System;

namespace Fig.Contracts.Settings;

public class DateTimeSettingDataContract : SettingValueBaseDataContract
{
    public DateTimeSettingDataContract(DateTime? value)
    {
        Value = value;
    }

    public DateTime? Value { get; set; }
    
    public override object? GetValue()
    {
        return Value;
    }
}