using System;
using System.Collections.Generic;

namespace Fig.Client.ConfigurationProvider;

public static class RunSession
{
    private static readonly Dictionary<string, RunSessionEntry> Sessions = new();
    private static readonly object LockObject = new();

    public static Guid GetId(string clientName)
    {
        lock (LockObject)
        {
            return Sessions.TryGetValue(clientName, out var entry)
                ? entry.Id
                : Guid.Empty;
        }
    }

    internal static Guid Acquire(string clientName)
    {
        lock (LockObject)
        {
            var entry = GetOrCreateEntry(clientName);
            entry.ReferenceCount++;
            return entry.Id;
        }
    }

    internal static void Release(string clientName)
    {
        lock (LockObject)
        {
            if (!Sessions.TryGetValue(clientName, out var entry))
                return;

            if (entry.ReferenceCount > 0)
                entry.ReferenceCount--;

            if (entry.ReferenceCount == 0)
                Sessions.Remove(clientName);
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

    private static RunSessionEntry GetOrCreateEntry(string clientName)
    {
        if (Sessions.TryGetValue(clientName, out var entry))
            return entry;

        entry = new RunSessionEntry(Guid.NewGuid());
        Sessions[clientName] = entry;
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
