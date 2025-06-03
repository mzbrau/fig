using System;

namespace Fig.Client.Exceptions;

public class NoSecretProviderException : Exception
{
    public NoSecretProviderException() :
        base("No client secret provider has been specified. " +
             "Fig requires a client secret provider to supply a client secret that it can use to authenticate communication with the API.")
    {
    }
}