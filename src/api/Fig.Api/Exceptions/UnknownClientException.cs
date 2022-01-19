namespace Fig.Api.Exceptions;

public class UnknownClientException : Exception
{
    public UnknownClientException(string clientName) : base($"Unknown client: {clientName}")
    {
    }
}