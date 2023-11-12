using System;

namespace Fig.Client.IntegrationTest;

public class ConfigurationUpdatedEventArgs : EventArgs
{
    public ConfigurationUpdatedEventArgs(SettingsBase settings)
    {
        Settings = settings;
    }

    public SettingsBase Settings { get; }
}