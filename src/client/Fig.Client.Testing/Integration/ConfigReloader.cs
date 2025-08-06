using System;

namespace Fig.Client.Testing.Integration;

public class ConfigReloader : ConfigReloader<SettingsBase>
{
}

public class ConfigReloader<T>
{
    public event EventHandler<ConfigurationUpdatedEventArgs<T>>? ConfigurationUpdated;

    public void Reload(T settings)
    {
        ConfigurationUpdated?.Invoke(this, new ConfigurationUpdatedEventArgs<T>(settings));
    }
}