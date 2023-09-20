using System;

namespace Fig.Common.NetStandard.Exceptions;

public class InvalidClientNameException : Exception
{
    public InvalidClientNameException(string clientName) : 
        base($"'{clientName}' is not a valid name. Reserved characters used in regular expressions are not allowed")

    {
    }
}