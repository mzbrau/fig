using System;
using Fig.Contracts;

namespace Fig.Client.Exceptions;

public class FigRegistrationException : Exception
{
    public FigRegistrationException(ErrorResultDataContract? result)
    {
        Result = result;
    }
    
    public ErrorResultDataContract? Result { get; }
}