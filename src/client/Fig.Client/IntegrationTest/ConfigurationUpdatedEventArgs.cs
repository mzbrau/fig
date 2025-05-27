using System;

namespace Fig.Client.IntegrationTest;

public class ConfigurationUpdatedEventArgs<T> : EventArgs
{
    public ConfigurationUpdatedEventArgs(T settings)
    {
        Settings = settings;
    }

    public T Settings { get; }
}