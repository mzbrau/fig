using System;
using System.Collections.Generic;

namespace Fig.Client.ConfigurationProvider;

public static class RunSession
{
    private static readonly Dictionary<string, Guid> _ids = new();
    
    public static Guid GetId(string clientName)
    {
        if (_ids.TryGetValue(clientName, out var id))
        {
            return id;
        }

        id = Guid.NewGuid();
        _ids[clientName] = id;
        return id;
    }
}