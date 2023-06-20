using System.Collections.Generic;

namespace Fig.Contracts.Settings;

public class SettingValueUpdatesDataContract
{
    public SettingValueUpdatesDataContract(IEnumerable<SettingDataContract> valueUpdates, string changeMessage)
    {
        ValueUpdates = valueUpdates;
        ChangeMessage = changeMessage;
    }

    public IEnumerable<SettingDataContract> ValueUpdates { get; set; }
    
    public string ChangeMessage { get; set; }
}