using System;
using System.Threading.Tasks;
using Fig.Client.Events;

namespace Fig.Client.Status;

public interface ISettingStatusMonitor : IDisposable
{
    event EventHandler<ChangedSettingsEventArgs> SettingsChanged;

    event EventHandler ReconnectedToApi;

    event EventHandler OfflineSettingsDisabled;

    event EventHandler RestartRequested;

    Guid RunSessionId { get; }

    bool AllowOfflineSettings { get; }
    
    void Initialize();

    void SettingsUpdated();

    Task SyncStatus();
}