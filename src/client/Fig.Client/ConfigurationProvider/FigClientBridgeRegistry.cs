using System;
using System.Collections.Generic;

namespace Fig.Client.ConfigurationProvider;

internal static class FigClientBridgeRegistry
{
    private static readonly object LockObject = new();
    private static readonly Dictionary<Type, Registration> Registrations = new();

    public static void Register(Type settingsType, IFigClientBridge bridge, FigClientBridgeOptions options)
    {
        if (settingsType is null)
            throw new ArgumentNullException(nameof(settingsType));

        if (bridge is null)
            throw new ArgumentNullException(nameof(bridge));

        if (options is null)
            throw new ArgumentNullException(nameof(options));

        lock (LockObject)
        {
            Registrations[settingsType] = new Registration(bridge, options);
        }
    }

    public static bool TryGet(Type settingsType, out IFigClientBridge? bridge, out FigClientBridgeOptions options)
    {
        if (settingsType is null)
            throw new ArgumentNullException(nameof(settingsType));

        lock (LockObject)
        {
            if (Registrations.TryGetValue(settingsType, out var registration))
            {
                if (registration.Bridge.TryGetTarget(out bridge))
                {
                    options = registration.Options;
                    return true;
                }

                Registrations.Remove(settingsType);
            }
        }

        bridge = null;
        options = FigClientBridgeOptions.Default;
        return false;
    }

    public static void Unregister(Type settingsType, IFigClientBridge bridge)
    {
        if (settingsType is null)
            throw new ArgumentNullException(nameof(settingsType));

        if (bridge is null)
            throw new ArgumentNullException(nameof(bridge));

        lock (LockObject)
        {
            if (!Registrations.TryGetValue(settingsType, out var registration))
                return;

            if (!registration.Bridge.TryGetTarget(out var registeredBridge) || ReferenceEquals(registeredBridge, bridge))
            {
                Registrations.Remove(settingsType);
            }
        }
    }

    public static void Clear()
    {
        lock (LockObject)
        {
            Registrations.Clear();
        }
    }

    private class Registration
    {
        public Registration(IFigClientBridge bridge, FigClientBridgeOptions options)
        {
            Bridge = new WeakReference<IFigClientBridge>(bridge);
            Options = options;
        }

        public WeakReference<IFigClientBridge> Bridge { get; }

        public FigClientBridgeOptions Options { get; }
    }
}

