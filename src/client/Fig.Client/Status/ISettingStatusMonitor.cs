using System;
using Fig.Client.Events;

namespace Fig.Client.Status
{
    public interface ISettingStatusMonitor
    {
        event EventHandler<ChangedSettingsEventArgs> SettingsChanged;

        event EventHandler ReconnectedToApi;

        event EventHandler OfflineSettingsDisabled;

        event EventHandler RestartRequested;

        bool AllowOfflineSettings { get; }

        void SettingsUpdated();
    }
}