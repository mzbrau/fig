using System;

namespace Fig.Client.Contracts;

public class SecretNotFoundException : Exception
{
    public SecretNotFoundException(string message) 
        : base(message)
    {
    }
}