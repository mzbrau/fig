using System;
using System.Collections.Generic;

namespace Fig.Client.ConfigurationProvider;

public static class RunSession
{
    private static readonly Dictionary<(string ClientName, string? Instance), RunSessionEntry> Sessions = new();
    private static readonly object LockObject = new();

    public static Guid GetId(string clientName, string? instance = null)
    {
        var normalizedInstance = InstanceNormalization.Normalize(instance);
        lock (LockObject)
        {
            return Sessions.TryGetValue((clientName, normalizedInstance), out var entry)
                ? entry.Id
                : Guid.Empty;
        }
    }

    internal static Guid Acquire(string clientName, string? instance = null)
    {
        var normalizedInstance = InstanceNormalization.Normalize(instance);
        lock (LockObject)
        {
            var entry = GetOrCreateEntry(clientName, normalizedInstance);
            entry.ReferenceCount++;
            return entry.Id;
        }
    }

    internal static void Release(string clientName, string? instance = null)
    {
        var normalizedInstance = InstanceNormalization.Normalize(instance);
        lock (LockObject)
        {
            var key = (clientName, normalizedInstance);
            if (!Sessions.TryGetValue(key, out var entry))
                return;

            if (entry.ReferenceCount > 0)
                entry.ReferenceCount--;

            if (entry.ReferenceCount == 0)
                Sessions.Remove(key);
        }
    }

    internal static int Count
    {
        get
        {
            lock (LockObject)
            {
                return Sessions.Count;
            }
        }
    }

    internal static void Clear()
    {
        lock (LockObject)
        {
            Sessions.Clear();
        }
    }

    private static RunSessionEntry GetOrCreateEntry(string clientName, string? instance)
    {
        var key = (clientName, InstanceNormalization.Normalize(instance));
        if (Sessions.TryGetValue(key, out var entry))
            return entry;

        entry = new RunSessionEntry(Guid.NewGuid());
        Sessions[key] = entry;
        return entry;
    }

    private class RunSessionEntry
    {
        public RunSessionEntry(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; }

        public int ReferenceCount { get; set; }
    }
}
