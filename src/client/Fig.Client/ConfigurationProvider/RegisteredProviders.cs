using System.Collections.Generic;
using System.Linq;

namespace Fig.Client.ConfigurationProvider;

public static class RegisteredProviders
{
    private static List<FigConfigurationProvider> _providers = new();
    private static object _lockObject = new();

    public static void Register(FigConfigurationProvider provider)
    {
        lock (_lockObject)
        {
            _providers.Add(provider);
        }
    }

    public static void Unregister(FigConfigurationProvider provider)
    {
        lock (_lockObject)
        {
            _providers.Remove(provider);
        }
    }

    public static bool TryGet(string name, out FigConfigurationProvider? provider)
    {
        lock (_lockObject)
        {
            provider = _providers.FirstOrDefault(a => a.Name == name);
        }

        return provider is not null;
    }

    public static void Clear()
    {
        lock (_lockObject)
        {
            _providers.Clear();
        }
    }
}