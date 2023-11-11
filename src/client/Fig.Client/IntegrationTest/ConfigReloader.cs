using System;

namespace Fig.Client.IntegrationTest;

public class ConfigReloader
{
    public event EventHandler<ConfigurationUpdatedEventArgs>? ConfigurationUpdated;

    public void Reload(SettingsBase settings)
    {
        ConfigurationUpdated?.Invoke(this, new ConfigurationUpdatedEventArgs(settings));
    }
}