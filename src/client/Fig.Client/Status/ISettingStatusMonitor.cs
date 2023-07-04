using System;
using Fig.Client.ClientSecret;
using Fig.Client.Configuration;
using Fig.Client.Events;
using Microsoft.Extensions.Logging;

namespace Fig.Client.Status
{
    public interface ISettingStatusMonitor
    {
        event EventHandler<ChangedSettingsEventArgs> SettingsChanged;

        event EventHandler ReconnectedToApi;

        event EventHandler OfflineSettingsDisabled;

        bool AllowOfflineSettings { get; }

        void Initialize<T>(T settings, IFigOptions figOptions, IClientSecretProvider clientSecretProvider, ILogger logger) where T: SettingsBase;

        void SettingsUpdated();
    }
}