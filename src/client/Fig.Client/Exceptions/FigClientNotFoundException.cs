using System;

namespace Fig.Client.Exceptions;

public class FigClientNotFoundException : Exception
{
    public FigClientNotFoundException(string clientName, string? instance)
        : base($"Fig client '{clientName}'{(instance is null ? string.Empty : $" (instance '{instance}')")} was not found on the Fig API.")
    {
        ClientName = clientName;
        Instance = instance;
    }

    public string ClientName { get; }

    public string? Instance { get; }
}
