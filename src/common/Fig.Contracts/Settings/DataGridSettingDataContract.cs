using System.Collections.Generic;

namespace Fig.Contracts.Settings;

public class DataGridSettingDataContract : SettingValueBaseDataContract
{
    public DataGridSettingDataContract(List<Dictionary<string, object?>>? value)
    {
        Value = value;
    }

    public List<Dictionary<string, object?>>? Value { get; set; }
    
    public override object? GetValue()
    {
        return Value;
    }
}