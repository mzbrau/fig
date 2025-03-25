using System;

namespace Fig.Contracts.Settings;

public class TimeSpanSettingDataContract : SettingValueBaseDataContract
{
    public TimeSpanSettingDataContract(TimeSpan? value)
    {
        Value = value;
    }

    public TimeSpan? Value { get; set; }
    
    public override object? GetValue()
    {
        return Value;
    }
}