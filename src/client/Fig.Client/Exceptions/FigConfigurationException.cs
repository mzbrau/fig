using System;

namespace Fig.Client.Exceptions;

public class FigConfigurationException : Exception
{
    public FigConfigurationException(string message) 
        : base(message)
    {
    }
}