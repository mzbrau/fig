using System;
using Fig.Client.ClientSecret;
using Fig.Client.Configuration;
using Microsoft.Extensions.Logging;

namespace Fig.Client.Status
{
    public interface ISettingStatusMonitor
    {
        event EventHandler SettingsChanged;

        event EventHandler ReconnectedToApi;

        event EventHandler OfflineSettingsDisabled;

        bool AllowOfflineSettings { get; }

        void Initialize<T>(T settings, IFigOptions figOptions, IClientSecretProvider clientSecretProvider, ILogger logger) where T: SettingsBase;

        void SettingsUpdated();
    }
}