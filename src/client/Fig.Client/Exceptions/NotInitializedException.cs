using System;

namespace Fig.Client.Exceptions;

public class NotInitializedException : Exception
{
    public NotInitializedException()
        : base("Class must be initialized prior to use")
    { }
}