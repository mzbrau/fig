using System;
using Fig.Client.ClientSecret;
using Fig.Client.Configuration;
using Microsoft.Extensions.Logging;

namespace Fig.Client.Status
{
    public interface ISettingStatusMonitor
    {
        event EventHandler SettingsChanged;

        void Initialize<T>(T settings, IFigOptions figOptions, IClientSecretProvider clientSecretProvider, ILogger logger) where T: SettingsBase;

        void SettingsUpdated();
    }
}