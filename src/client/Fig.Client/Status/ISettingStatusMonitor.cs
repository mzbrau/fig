using System;
using Fig.Client.Configuration;

namespace Fig.Client.Status
{
    public interface ISettingStatusMonitor
    {
        event EventHandler SettingsChanged;
        
        void Initialize<T>(T settings, IFigOptions figOptions, Action<string> logger) where T : SettingsBase;

        void SettingsUpdated();
    }
}