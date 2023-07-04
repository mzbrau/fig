using System;
using System.Collections.Generic;

namespace Fig.Client.Events;

public class ChangedSettingsEventArgs : EventArgs
{
    public ChangedSettingsEventArgs(List<string> settingNames)
    {
        SettingNames = settingNames;
    }
    
    public List<string> SettingNames { get; }
}