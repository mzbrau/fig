using System;

namespace Fig.Client.Abstractions.Exceptions;

public class FigConfigurationException : Exception
{
    public FigConfigurationException(string message) 
        : base(message)
    {
    }
}