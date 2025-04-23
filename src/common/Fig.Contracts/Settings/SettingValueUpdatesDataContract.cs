using System.Collections.Generic;
using Fig.Contracts.Scheduling;

namespace Fig.Contracts.Settings;

public class SettingValueUpdatesDataContract
{
    public SettingValueUpdatesDataContract(IEnumerable<SettingDataContract> valueUpdates, string changeMessage, ScheduleDataContract? schedule = null)
    {
        ValueUpdates = valueUpdates;
        ChangeMessage = changeMessage;
        Schedule = schedule;
    }

    public IEnumerable<SettingDataContract> ValueUpdates { get; }
    
    public string ChangeMessage { get; }
    
    public ScheduleDataContract? Schedule { get; }
}