using System;

namespace Fig.Client.Exceptions;

public class NoOfflineSettingsException : Exception
{
    public NoOfflineSettingsException(string message)
        : base(message)
    {
    }
}