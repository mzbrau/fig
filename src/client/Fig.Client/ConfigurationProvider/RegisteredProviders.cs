using System.Collections.Generic;
using System.Linq;
using System;

namespace Fig.Client.ConfigurationProvider;

public static class RegisteredProviders
{
    private static readonly Dictionary<ProviderRegistrationKey, WeakReference<FigConfigurationProvider>> Providers = new();
    private static readonly object LockObject = new();

    public static void Register(FigConfigurationProvider provider)
    {
        if (provider is null)
            throw new ArgumentNullException(nameof(provider));

        lock (LockObject)
        {
            PruneDeadEntries();
            Providers[provider.RegistrationKey] = new WeakReference<FigConfigurationProvider>(provider);
        }
    }

    public static void Unregister(FigConfigurationProvider provider)
    {
        if (provider is null)
            throw new ArgumentNullException(nameof(provider));

        lock (LockObject)
        {
            if (!Providers.TryGetValue(provider.RegistrationKey, out var registration))
                return;

            if (!registration.TryGetTarget(out var registeredProvider) ||
                registeredProvider.IsDisposed ||
                ReferenceEquals(registeredProvider, provider))
            {
                Providers.Remove(provider.RegistrationKey);
            }
        }
    }

    public static bool TryGet(string name, out FigConfigurationProvider? provider)
    {
        if (name is null)
        {
            provider = null;
            return false;
        }

        lock (LockObject)
        {
            PruneDeadEntries();
            var matchingProviders = Providers
                .Where(a => string.Equals(a.Key.ClientName, name, StringComparison.Ordinal))
                .Select(a => a.Value.TryGetTarget(out var target) ? target : null)
                .Where(a => a is not null && !a.IsDisposed)
                .ToList();

            if (matchingProviders.Count > 1)
            {
                provider = null;
                return false;
            }

            provider = matchingProviders.FirstOrDefault();
        }

        return provider is not null;
    }

    internal static bool TryGet(
        string clientName,
        string? instance,
        Type settingsType,
        out FigConfigurationProvider? provider)
    {
        var key = new ProviderRegistrationKey(clientName, instance, settingsType);

        lock (LockObject)
        {
            PruneDeadEntries();
            if (Providers.TryGetValue(key, out var registration) &&
                registration.TryGetTarget(out var registeredProvider) &&
                !registeredProvider.IsDisposed)
            {
                provider = registeredProvider;
                return true;
            }

            Providers.Remove(key);
        }

        provider = null;
        return false;
    }

    public static void Clear()
    {
        lock (LockObject)
        {
            Providers.Clear();
        }
    }

    internal static int Count
    {
        get
        {
            lock (LockObject)
            {
                PruneDeadEntries();
                return Providers.Count;
            }
        }
    }

    private static void PruneDeadEntries()
    {
        foreach (var key in Providers
                     .Where(a => !a.Value.TryGetTarget(out var provider) || provider.IsDisposed)
                     .Select(a => a.Key)
                     .ToList())
        {
            Providers.Remove(key);
        }
    }
}
