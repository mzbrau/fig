using System;

namespace Fig.Client.Exceptions;

public class ConfigurationException : Exception
{
    public ConfigurationException(string message)
        : base(message)
    {
    }
}